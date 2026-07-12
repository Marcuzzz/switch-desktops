using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace SwitchDesktops.UI;

public partial class CrossfadeOverlay : Window
{
    public CrossfadeOverlay(BitmapSource? from, BitmapSource? to)
    {
        InitializeComponent();
        FromImage.Source = from;
        ToImage.Source = to;

        var screen = System.Windows.Forms.Screen.PrimaryScreen?.Bounds;
        if (screen is { } b)
        {
            Left = b.Left;
            Top = b.Top;
            Width = b.Width;
            Height = b.Height;
        }
    }

    public Task RunAsync(TimeSpan duration)
    {
        var tcs = new TaskCompletionSource();
        var anim = new DoubleAnimation(0, 1, new Duration(duration))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        anim.Completed += (_, _) => tcs.TrySetResult();
        ToImage.BeginAnimation(OpacityProperty, anim);
        return tcs.Task;
    }

    public Task FadeOutAsync(TimeSpan duration)
    {
        var tcs = new TaskCompletionSource();
        var anim = new DoubleAnimation(1, 0, new Duration(duration))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        anim.Completed += (_, _) => tcs.TrySetResult();
        BeginAnimation(OpacityProperty, anim);
        return tcs.Task;
    }
}
