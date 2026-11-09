using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.ViewManagement;

namespace PromptEnhancer.WinUI;

// A small, procedural 3D height field with a reflected studio lighting environment.
// Render only while an interaction is settling; an idle/hidden launcher does no work.
public sealed class LiquidBlobSurface : Grid, IDisposable
{
    private const int Resolution = 192;
    private readonly WriteableBitmap bitmap = new(Resolution, Resolution);
    private readonly byte[] pixels = new byte[Resolution * Resolution * 4];
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly Stopwatch clock = new();
    private readonly CompositeTransform deformation = new();
    private double paintedX = double.NaN, paintedY = double.NaN;
    private bool paintedFocus, paintedContrast;
    private double x, y, targetX, targetY, pressure, targetPressure, velocity, hover, targetHover;
    private bool keyboardFocus, suspended, disposed;
    public event Action ShapeChanged;

    public LiquidBlobSurface()
    {
        IsHitTestVisible = false;
        Children.Add(new Image { Source = bitmap, Stretch = Stretch.Fill,
            RenderTransform = deformation, RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5) });
        timer.Tick += Tick;
        settings.AnimationsEnabledChanged += AnimationsChanged;
        // HighContrastChanged is unavailable in this unpackaged desktop host;
        // observe the supported UISettings color event instead.
        settings.ColorValuesChanged += ColorsChanged;
        Render();
    }

    public void Interact(double pointerX, double pointerY, bool over, bool down, bool focused)
    {
        if (disposed) return;
        targetX = over ? Math.Clamp(pointerX, -1, 1) : 0;
        targetY = over ? Math.Clamp(pointerY, -1, 1) : 0;
        targetHover = over ? 1 : 0;
        targetPressure = down ? 1 : 0;
        keyboardFocus = focused;
        if (suspended || !settings.AnimationsEnabled) { Settle(); return; }
        if (!timer.IsEnabled) { clock.Restart(); timer.Start(); }
    }

    public void Suspend(bool value)
    {
        if (disposed) return;
        suspended = value;
        if (!value) return;
        targetX = targetY = targetPressure = targetHover = 0;
        Settle();
    }

    private void Tick(object sender, object args)
    {
        double dt = Math.Max(clock.Elapsed.TotalSeconds, 0.001);
        clock.Restart();
        double follow = 1 - Math.Exp(-12 * dt);
        x += (targetX - x) * follow;
        y += (targetY - y) * follow;
        hover += (targetHover - hover) * follow;
        // Solve the damped spring exactly so delayed desktop frames still settle
        // in real time, without unstable steps or a stretched-out animation.
        const double damping = 10, frequency = 11.40175425099138; // sqrt(230 - 10^2)
        double error = pressure - targetPressure;
        double decay = Math.Exp(-damping * dt), cosine = Math.Cos(frequency * dt), sine = Math.Sin(frequency * dt);
        pressure = targetPressure + decay * (error * cosine + (velocity + damping * error) / frequency * sine);
        velocity = decay * (velocity * cosine - (damping * velocity + 230 * error) / frequency * sine);
        Render();
        if (Math.Abs(x - targetX) + Math.Abs(y - targetY) + Math.Abs(hover - targetHover) +
            Math.Abs(pressure - targetPressure) + Math.Abs(velocity) < 0.008) Settle();
    }

    private void Settle()
    {
        timer.Stop();
        clock.Stop();
        x = targetX; y = targetY; hover = targetHover;
        pressure = targetPressure; velocity = 0;
        Render();
    }

    // Shared by the pixels and the native window region, in normalized coordinates.
    public (double X, double Y) Outline(double angle)
    {
        double radius = Radius(angle);
        return (Math.Cos(angle) * radius * ScaleX, Math.Sin(angle) * radius * ScaleY);
    }

    private double ScaleX => 0.90 * (1 + 0.038 * pressure + 0.014 * hover);
    private double ScaleY => 0.90 * (1 - 0.075 * pressure + 0.014 * hover);
    private double Radius(double angle) => 1 + 0.024 * Math.Sin(3 * angle + 0.6 + x * 0.18)
        + 0.016 * Math.Cos(2 * angle - 0.5 + y * 0.2);

    private double SurfaceHeight(double u, double v)
    {
        double radius = Radius(Math.Atan2(v, u));
        double square = (u * u + v * v) / (radius * radius);
        double dome = Math.Sqrt(Math.Max(0, 1 - square));
        double fold = v + 0.42 * u - 0.16 + 0.16 * Math.Sin(3 * u + x * 0.45);
        return dome - 0.18 * Math.Exp(-22 * fold * fold) * Math.Max(0, 1 - square)
            + 0.045 * Math.Sin(4 * u + 2 * v + y * 0.6) * Math.Max(0, 1 - square);
    }

    private void Render()
    {
        // Pressure and hover deformation use the compositor; only pointer lighting
        // changes require repainting the height field. Native hit-testing follows both.
        deformation.ScaleX = ScaleX / 0.90;
        deformation.ScaleY = ScaleY / 0.90;
        bool highContrast = accessibility.HighContrast;
        if (Math.Abs(x - paintedX) < 0.005 && Math.Abs(y - paintedY) < 0.005 &&
            keyboardFocus == paintedFocus && highContrast == paintedContrast)
        {
            ShapeChanged?.Invoke();
            return;
        }
        const double sx = 0.90, sy = 0.90;
        var foreground = settings.GetColorValue(UIColorType.Foreground);
        var background = settings.GetColorValue(UIColorType.Background);
        var light = Vector3.Normalize(new Vector3(-0.5f, -0.65f, 1));
        for (int row = 0; row < Resolution; row++)
        {
            double v = ((row + 0.5) * 2 / Resolution - 1) / sy;
            for (int column = 0; column < Resolution; column++)
            {
                int index = (row * Resolution + column) * 4;
                double u = ((column + 0.5) * 2 / Resolution - 1) / sx;
                double distance = Math.Sqrt(u * u + v * v) / Radius(Math.Atan2(v, u));
                double alpha = Math.Clamp((1 - distance) * Resolution * 0.43, 0, 1);
                if (alpha <= 0) { pixels[index] = pixels[index + 1] = pixels[index + 2] = pixels[index + 3] = 0; continue; }
                double red, green, blue;
                if (highContrast)
                {
                    // A system-colored disc with an unmistakable open (+) glyph.
                    bool ink = distance > 0.91 || (Math.Abs(u) < 0.045 && Math.Abs(v) < 0.28) ||
                        (Math.Abs(v) < 0.045 && Math.Abs(u) < 0.28);
                    var color = ink ? foreground : background;
                    red = color.R; green = color.G; blue = color.B;
                }
                else
                {
                    const double step = 0.003;
                    double dx = (SurfaceHeight(u + step, v) - SurfaceHeight(u - step, v)) / (2 * step);
                    double dy = (SurfaceHeight(u, v + step) - SurfaceHeight(u, v - step)) / (2 * step);
                    var normal = Vector3.Normalize(new Vector3((float)-dx, (float)-dy, 1));
                    // Slightly rotate the view with the pointer: reflections travel over the fold.
                    normal = Vector3.Normalize(normal + new Vector3((float)(x * 0.10), (float)(y * 0.10), 0));
                    var reflection = 2 * normal.Z * normal - Vector3.UnitZ;
                    double diffuse = Math.Max(0, Vector3.Dot(normal, light));
                    double fresnel = Math.Pow(1 - normal.Z, 3);
                    double softbox = Math.Exp(-Math.Pow((reflection.X + 0.40) / 0.55, 4)
                        - Math.Pow((reflection.Y + 0.58) / 0.32, 4));
                    double hot = Math.Exp(-Math.Pow((reflection.X + 0.38) / 0.22, 2)
                        - Math.Pow((reflection.Y + 0.63) / 0.14, 2));
                    double right = Math.Exp(-Math.Pow((reflection.X - 0.78) / 0.21, 2)
                        - Math.Pow((reflection.Y + 0.10) / 0.64, 2));
                    double bounce = Math.Exp(-Math.Pow((reflection.X + 0.15) / 0.65, 2)
                        - Math.Pow((reflection.Y - 0.88) / 0.18, 2));
                    double shine = Math.Clamp(0.56 * softbox + 0.63 * hot + 0.38 * right, 0, 0.96);
                    red = 8 + 16 * diffuse + 28 * fresnel + 16 * bounce;
                    green = 34 + 65 * diffuse + 75 * fresnel + 63 * bounce;
                    blue = 118 + 95 * diffuse + 39 * fresnel + 35 * bounce;
                    red += (239 - red) * shine;
                    green += (251 - green) * shine;
                    blue += (255 - blue) * shine;
                }
                if (keyboardFocus && distance is > 0.80 and < 0.90)
                {
                    bool keyline = distance is < 0.82 or > 0.88;
                    red = highContrast ? foreground.R : keyline ? 8 : 248;
                    green = highContrast ? foreground.G : keyline ? 43 : 253;
                    blue = highContrast ? foreground.B : keyline ? 90 : 255;
                }
                pixels[index] = (byte)(Math.Clamp(blue, 0, 255) * alpha);
                pixels[index + 1] = (byte)(Math.Clamp(green, 0, 255) * alpha);
                pixels[index + 2] = (byte)(Math.Clamp(red, 0, 255) * alpha);
                pixels[index + 3] = (byte)(255 * alpha);
            }
        }
        using var stream = bitmap.PixelBuffer.AsStream();
        stream.Write(pixels, 0, pixels.Length);
        bitmap.Invalidate();
        paintedX = x; paintedY = y;
        paintedFocus = keyboardFocus; paintedContrast = highContrast;
        ShapeChanged?.Invoke();
    }

    private void AnimationsChanged(UISettings sender, object args) =>
        DispatcherQueue.TryEnqueue(() => { if (!disposed && !settings.AnimationsEnabled) Settle(); });

    private void ColorsChanged(UISettings sender, object args) =>
        DispatcherQueue.TryEnqueue(() => { if (!disposed) { paintedX = double.NaN; Render(); } });

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer.Stop();
        settings.AnimationsEnabledChanged -= AnimationsChanged;
        settings.ColorValuesChanged -= ColorsChanged;
    }
}
