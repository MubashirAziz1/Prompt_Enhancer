using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace PromptEnhancer.WinUI.Tests;

public partial class App
{
    private async Task SettingsCompatibility(ICredentialStore credentials, string path)
    {
        byte[] protectedKey = File.ReadAllBytes(path);
        Invoke("Settings");
        await Until(() => Elements().OfType<PasswordBox>().Any());
        Require(Find<PasswordBox>("ApiKey").Password == "", "Settings must reopen blank after saving.");
        InvokeDialog("Save");
        await SettingsClosed();
        Require(credentials.Load() == "replacement-fixture-key" && File.ReadAllBytes(path).SequenceEqual(protectedKey),
            "Blank Save must preserve the exact protected credential file.");

        Invoke("Settings");
        await Until(() => Elements().OfType<PasswordBox>().Any());
        Find<PasswordBox>("ApiKey").Password = "discarded-fixture-key";
        InvokeDialog("Cancel");
        await SettingsClosed();

        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Invoke("Settings");
            await Until(() => Elements().OfType<PasswordBox>().Any());
            Find<PasswordBox>("ApiKey").Password = "replacement-that-cannot-be-saved";
            InvokeDialog("Save");
            await Until(() => Find<TextBlock>("CredentialStatus").Text.Contains("could not be saved"));
            Require(credentials.Load() == "replacement-fixture-key", "A failed replacement must preserve the previous protected key.");
            InvokeDialog("Cancel");
            await SettingsClosed();
        }
        Require(credentials.Load() == "replacement-fixture-key" && File.ReadAllBytes(path).SequenceEqual(protectedKey),
            "Cancel must preserve the saved protected key.");

        Invoke("Settings");
        await Until(() => Elements().OfType<PasswordBox>().Any());
        Require(Find<PasswordBox>("ApiKey").Password == "", "Canceled input must not reappear.");
        Find<PasswordBox>("ApiKey").Password = "invalid key";
        InvokeDialog("Save");
        await Task.Delay(100);
        Require(Elements().OfType<PasswordBox>().Any() && credentials.Load() == "replacement-fixture-key",
            "Invalid input must stay in Settings without replacing the key.");
        InvokeDialog("Cancel");
        await SettingsClosed();
    }

    private static async Task Probe(string action, string path, string key)
    {
        var info = new ProcessStartInfo(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "CredentialProbe.exe"))) { UseShellExecute = false, CreateNoWindow = true };
        info.ArgumentList.Add(action);
        info.ArgumentList.Add(path);
        info.ArgumentList.Add(key);
        using var process = Process.Start(info);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await process.WaitForExitAsync(timeout.Token);
        Require(process.ExitCode == 0, "The retained .NET Framework credential store must read/write the isolated fixture.");
    }

    private static async Task FreshWinUIProcess(string path)
    {
        var info = new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = false };
        info.ArgumentList.Add("--credential-reuse");
        info.ArgumentList.Add(path);
        using var process = Process.Start(info);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await process.WaitForExitAsync(timeout.Token);
        Require(process.ExitCode == 0, "A fresh WinUI process must enhance using the replacement protected key without re-entry.");
    }
}
