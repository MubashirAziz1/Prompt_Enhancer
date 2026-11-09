using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics;

namespace PromptEnhancer.WinUI.Tests;

public partial class App
{
    private async Task BlobWorkflow()
    {
        var launcher = new LauncherWindow { Title = "Liquid blob acceptance " + Environment.ProcessId };
        try
        {
            int activations = 0;
            launcher.OpenRequested += () => activations++;
            launcher.Activate();
            var root = (FrameworkElement)launcher.Content;
            await Until(() => root.ActualHeight > 0);
            await Task.Delay(200); // Allow the first loaded layout to apply monitor DPI.
            var surface = (LiquidBlobSurface)root.FindName("Blob");
            var button = (FloatingButton)root.FindName("OpenButton");
            double scale = root.XamlRoot.RasterizationScale;
            Require(Math.Abs(launcher.AppWindow.Size.Width / scale - 96) < 1 &&
                Math.Abs(launcher.AppWindow.Size.Height / scale - 96) < 1 &&
                root.ActualWidth >= 90 && root.ActualHeight >= 90,
                $"The liquid floating button must have a compact square viewport at actual DPI. Actual={root.ActualWidth}x{root.ActualHeight}, native={launcher.AppWindow.Size.Width}x{launcher.AppWindow.Size.Height}, scale={root.XamlRoot.RasterizationScale}.");
            await Snapshot(root, "blob-rest.png");
            var bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(root);
            var pixels = (await bitmap.GetPixelsAsync()).ToArray();
            Require(pixels[3] == 0 && pixels[(bitmap.PixelWidth * bitmap.PixelHeight / 2 + bitmap.PixelWidth / 2) * 4 + 3] > 240,
                "Rendered corners must be transparent and the sphere center opaque.");
            nint region = CreateRectRgn(0, 0, 0, 0);
            try
            {
                Require(GetWindowRgn(WinRT.Interop.WindowNative.GetWindowHandle(launcher), region) > 0 &&
                    !PtInRegion(region, 0, 0) && PtInRegion(region, launcher.AppWindow.Size.Width / 2, launcher.AppWindow.Size.Height / 2),
                    "The native window must expose the desktop at its corners and accept input at the sphere center.");
            }
            finally { DeleteObject(region); }
            double restHeight = surface.Outline(Math.PI / 2).Y;
            await NativeDesktop.Hold(launcher, async () =>
            {
                await Until(() => surface.Outline(Math.PI / 2).Y < restHeight - 0.025);
                await Snapshot(root, "blob-pressed.png");
            });
            await Task.Delay(1200);
            Require(surface.Outline(Math.PI / 2).Y >= restHeight - 0.003,
                "The released liquid surface must return to rest.");
            var quiet = Stopwatch.StartNew();
            surface.ShapeChanged += () => quiet.Restart();
            await Until(() => quiet.ElapsedMilliseconds >= 250);
            button.Focus(FocusState.Keyboard);
            await Task.Delay(150);
            await Snapshot(root, "blob-keyboard.png");
            NativeDesktop.Press(launcher, 0x20);
            await Until(() => activations == 2);
            checks.Add("PASS Liquid blob transparency, shaped native region, press compression, spring return, idle rendering, and keyboard activation");

            var origin = launcher.AppWindow.Position;
            await NativeDesktop.Stroke(launcher, NativeDesktop.Center(launcher), new PointInt32(-120, 40));
            Require(launcher.AppWindow.Position.X < origin.X - 80 && activations == 2,
                "Native dragging must move the liquid button without opening the editor.");
            await NativeDesktop.Stroke(launcher, NativeDesktop.Center(launcher), new PointInt32());
            await Until(() => activations == 3);
            checks.Add("PASS Native liquid button drag and click at " + root.XamlRoot.RasterizationScale.ToString("0.00") + " DPI scale");

            var menu = (MenuFlyout)button.ContextFlyout;
            bool hidden = false, quit = false;
            launcher.HideRequested += () => hidden = true;
            launcher.QuitRequested += () => quit = true;
            await NativeDesktop.Stroke(launcher, NativeDesktop.Center(launcher), new PointInt32(), true);
            await Until(() => menu.IsOpen);
            ((IInvokeProvider)new MenuFlyoutItemAutomationPeer(menu.Items.OfType<MenuFlyoutItem>().Single(item => item.Text == "Hide"))
                .GetPattern(PatternInterface.Invoke)).Invoke();
            await Until(() => hidden);
            menu.Hide();
            await Task.Delay(300);
            await NativeDesktop.Stroke(launcher, NativeDesktop.Center(launcher), new PointInt32(), true);
            await Until(() => menu.IsOpen);
            ((IInvokeProvider)new MenuFlyoutItemAutomationPeer(menu.Items.OfType<MenuFlyoutItem>().Single(item => item.Text == "Quit"))
                .GetPattern(PatternInterface.Invoke)).Invoke();
            await Until(() => quit);
            menu.Hide();
            checks.Add("PASS Native liquid button right-click menu opens and dispatches Hide and Quit");
        }
        finally { launcher.Close(); }
    }


    [DllImport("gdi32.dll")] private static extern nint CreateRectRgn(int left, int top, int right, int bottom);
    [DllImport("user32.dll")] private static extern int GetWindowRgn(nint window, nint region);
    [DllImport("gdi32.dll")] private static extern bool PtInRegion(nint region, int x, int y);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint value);
}
