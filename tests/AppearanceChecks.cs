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

namespace PromptEnhancer.WinUI.Tests;

public partial class App
{
    private async Task AppearanceWorkflow()
    {
        var root = (FrameworkElement)editor.Content;
        var rough = Find<TextBox>("RoughPrompt");
        var result = Find<TextBox>("EnhancedPrompt");
        double initialScale = root.XamlRoot.RasterizationScale;
        Require(Math.Abs(editor.AppWindow.Size.Width / initialScale - 420) <= 2 &&
                Math.Abs(editor.AppWindow.Size.Height / initialScale - 530) <= 2,
            "The editor must open at the reference proportions: 420 by 530 logical pixels.");
        Require(root.RequestedTheme == ElementTheme.Dark && root.ActualTheme == ElementTheme.Dark,
            "The reference-inspired editor must use the dark palette.");
        Require(((OverlappedPresenter)editor.AppWindow.Presenter).IsAlwaysOnTop,
            "The compact prompt editor must remain above ordinary application windows.");
        var roughPoint = rough.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point());
        var resultPoint = result.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point());
        Require(resultPoint.Y < roughPoint.Y && result.ActualHeight >= 100 && rough.ActualHeight >= 50,
            "The editable result must sit above a usable bottom rough-prompt composer.");
        var drag = Microsoft.UI.Input.InputNonClientPointerSource.GetForWindowId(editor.AppWindow.Id)
            .GetRegionRects(Microsoft.UI.Input.NonClientRegionKind.Caption);
        Require(drag.Length == 1 && drag[0].Width > 100 && drag[0].Height >= 32 * initialScale,
            "The compact toolbar must reserve a native caption drag region.");

        rough.Text = "Write a short email.";
        rough.Focus(FocusState.Keyboard);
        NativeDesktop.Press(editor, 0x09);
        await Until(() => FocusManager.GetFocusedElement(root.XamlRoot) is Button focused && focused == Find<Button>("Paste"));
        result.Focus(FocusState.Keyboard);
        NativeDesktop.Chord(editor, 0x41);
        await Until(() => result.SelectedText == "Replacement provider result.");
        NativeDesktop.TypeText(editor, "Reviewed using the keyboard.");
        await Until(() => result.Text == "Reviewed using the keyboard.");
        await CopyResult();
        Require(await ClipboardFixture.Text() == "Reviewed using the keyboard.", "Native selection and typing must edit the copyable result.");
        checks.Add("PASS Native Tab focus, Ctrl+A selection, ordinary keyboard editing, and edited Copy");

        rough.Text = "write email boss tomorrow leave personal work short";
        result.Text = "Write a short email to my boss requesting leave tomorrow for personal reasons. Keep the tone polite and professional.";
        Find<Button>("Copy").Focus(FocusState.Pointer);
        await Task.Delay(100);
        await Snapshot(root, "editor-Dark.png");
        string savedRough = rough.Text, savedResult = result.Text;
        Invoke("Clear");
        await Task.Delay(100);
        await Snapshot(root, "editor-empty.png");
        rough.Text = savedRough;
        result.Text = savedResult;

        Invoke("Settings");
        await Until(() => Elements().OfType<PasswordBox>().Any());
        var dialog = Elements().OfType<ContentDialog>().Single();
        Require(dialog.ActualTheme == ElementTheme.Dark && Find<PasswordBox>("ApiKey").ActualTheme == ElementTheme.Dark,
            "Settings must follow the dark editor theme.");
        Find<PasswordBox>("ApiKey").Focus(FocusState.Keyboard);
        NativeDesktop.TypeText(editor, "discarded-keyboard-fixture");
        await Until(() => Find<PasswordBox>("ApiKey").Password == "discarded-keyboard-fixture");
        NativeDesktop.Press(editor, 0x09);
        await Until(() => FocusManager.GetFocusedElement(root.XamlRoot) is Button focused && (string)focused.Content == "Save");
        await Snapshot(dialog, "settings-Dark.png");
        InvokeDialog("Cancel");
        await SettingsClosed();

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
                editor.AppWindow.Resize(new SizeInt32((int)(360 * scale), (int)(450 * scale)));
                await Task.Delay(200);
                var viewport = (ScrollViewer)root.FindName("Viewport");
                Require(rough.ActualWidth >= 200 && rough.ActualHeight >= 50 && result.ActualHeight >= 100 && viewport.ScrollableHeight > 0,
                    "Smaller windows must retain readable editors and scroll to lower actions and feedback.");
                viewport.ChangeView(null, viewport.ScrollableHeight, null, true);
                await Task.Delay(100);
                await Snapshot(root, "compact-" + scale.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ".png");
                editor.AppWindow.Resize(new SizeInt32((int)(540 * scale), (int)(650 * scale)));
                await Task.Delay(150);
                Require(viewport.ScrollableHeight < 2 && result.ActualHeight > 200,
                    "Larger windows must expand the result area without unnecessary page scrolling.");
                checks.Add("PASS 360 by 450 scrolling and 540 by 650 expanded editor at " + (scale * 100).ToString("0") + "% display scale");
            }
        }
        finally
        {
            editor.AppWindow.Move(originalPosition);
            editor.AppWindow.Resize(originalSize);
        }
        checks.Add("PASS 420 by 530 reference proportions, dark palette, native caption region, bottom composer, and Settings keyboard input");
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
