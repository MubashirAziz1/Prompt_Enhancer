using Microsoft.UI.Xaml;
using System.Net.Http;

namespace PromptEnhancer.WinUI;

public partial class App : Application
{
    private EditorWindow editor;
    private HttpClient client;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var credentials = new DpapiCredentialStore(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PromptEnhancer", "openrouter-key.dpapi"));
        client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(60),
            MaxResponseContentBufferSize = 1024 * 1024
        };
        editor = new EditorWindow(new OpenRouterProvider(client, credentials, OpenRouterProvider.InitialModel), credentials);
        editor.Closed += (_, _) => client.Dispose();
        editor.Activate();
    }
}
