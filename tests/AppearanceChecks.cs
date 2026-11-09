using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;

namespace PromptEnhancer.WinUI.Tests;

public partial class App
{
    private async Task AppearanceWorkflow()
    {
        var root = (FrameworkElement)editor.Content;
        var rough = Find<TextBox>("RoughPrompt");
        var result = Find<TextBox>("EnhancedPrompt");
        rough.Text = "Write a short email.";
        rough.Focus(FocusState.Keyboard);
        NativeDesktop.Press(editor, 0x09); // Actual Tab navigation.
        await Until(() => FocusManager.GetFocusedElement(root.XamlRoot) is Button focused && focused == Find<Button>("Paste"));
        result.Focus(FocusState.Keyboard);
        NativeDesktop.Chord(editor, 0x41);
        await Until(() => result.SelectedText == "Replacement provider result.");
        NativeDesktop.TypeText(editor, "Reviewed using the keyboard.");
        await Until(() => result.Text == "Reviewed using the keyboard.");
        await CopyResult();
        Require(await ClipboardFixture.Text() == "Reviewed using the keyboard.", "Native selection and typing must edit the copyable result.");
        checks.Add("PASS Native Tab focus, Ctrl+A selection, ordinary keyboard editing, and edited Copy");

        var settings = new UISettings();
        var background = settings.GetColorValue(UIColorType.Background);
        ElementTheme systemTheme = background.R + background.G + background.B > 384 ? ElementTheme.Light : ElementTheme.Dark;
        Require(root.RequestedTheme == ElementTheme.Default && root.ActualTheme == systemTheme,
            "The editor must default to the current Windows application theme.");
        Require(!editor.ExtendsContentIntoTitleBar, "The editor must retain standard native window styling.");
        var palette = new[] { UIColorType.Accent, UIColorType.AccentDark1, UIColorType.AccentDark2,
            UIColorType.AccentDark3, UIColorType.AccentLight1, UIColorType.AccentLight2, UIColorType.AccentLight3 }
            .Select(settings.GetColorValue).ToArray();
        foreach (ElementTheme theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            root.RequestedTheme = theme;
            await Until(() => root.ActualTheme == theme && rough.ActualTheme == theme && result.ActualTheme == theme);
            await Task.Delay(100);
            Require(editor.AppWindow.TitleBar.PreferredTheme == (theme == ElementTheme.Dark ? TitleBarTheme.Dark : TitleBarTheme.Light),
                "Standard window chrome must follow the editor theme.");
            var accent = (SolidColorBrush)Find<Button>("Enhance").Background;
            Require(palette.Contains(accent.Color), "The primary action must use the current Windows accent palette.");
            await Snapshot(root, "editor-" + theme + ".png");
            Invoke("Settings");
            await Until(() => Elements().OfType<PasswordBox>().Any());
            var dialog = Elements().OfType<ContentDialog>().Single();
            Require(dialog.ActualTheme == theme && Find<PasswordBox>("ApiKey").ActualTheme == theme,
                "Settings must use the same current theme as the editor.");
            Find<PasswordBox>("ApiKey").Focus(FocusState.Keyboard);
            NativeDesktop.TypeText(editor, "discarded-keyboard-fixture");
            await Until(() => Find<PasswordBox>("ApiKey").Password == "discarded-keyboard-fixture");
            NativeDesktop.Press(editor, 0x09);
            await Until(() => FocusManager.GetFocusedElement(root.XamlRoot) is Button focused && (string)focused.Content == "Save");
            await Snapshot(dialog, "settings-" + theme + ".png");
            InvokeDialog("Cancel");
            await SettingsClosed();
        }
        root.RequestedTheme = ElementTheme.Default;
        var originalSize = editor.AppWindow.Size;
        var originalPosition = editor.AppWindow.Position;
        try
        {
            var displays = DisplayArea.FindAll();
            for (int index = 0; index < displays.Count; index++)
            {
                var display = displays[index];
                editor.AppWindow.Move(new PointInt32(display.WorkArea.X + 20, display.WorkArea.Y + 20));
                await Task.Delay(250);
                double scale = root.XamlRoot.RasterizationScale;
                editor.AppWindow.Resize(new SizeInt32((int)(540 * scale), (int)(580 * scale)));
                await Task.Delay(200);
                var viewport = Elements().OfType<ScrollViewer>().First();
                Require(rough.ActualWidth >= 200 && rough.ActualHeight >= 100 && result.ActualHeight >= 100 && viewport.ScrollableHeight > 0,
                    "At the actual display scale a compact window must retain readable editors and scrollable actions.");
                viewport.ChangeView(null, viewport.ScrollableHeight, null, true);
                await Task.Delay(100);
                await Snapshot(root, "compact-" + scale.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ".png");
                checks.Add("PASS Display scale " + (scale * 100).ToString("0") + "% with 540×580 logical window, scrolling, and text scale " + settings.TextScaleFactor.ToString("0.00"));
            }
        }
        finally
        {
            editor.AppWindow.Move(originalPosition);
            editor.AppWindow.Resize(originalSize);
            root.RequestedTheme = ElementTheme.Default;
        }
        checks.Add("PASS Light/dark editor and Settings rendering, Settings keyboard input/Tab, native title bar, and Windows accent integration");
    }

    private static async Task Snapshot(FrameworkElement element, string name)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(element);
        var buffer = await bitmap.GetPixelsAsync();
        byte[] pixels = new byte[buffer.Length];
        using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        string directory = Path.Combine(AppContext.BaseDirectory, "snapshots");
        Directory.CreateDirectory(directory);
        var folder = await StorageFolder.GetFolderFromPathAsync(directory);
        var file = await folder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
        using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels);
        await encoder.FlushAsync();
    }
}
