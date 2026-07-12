using SwitchDesktops.Core;
using SwitchDesktops.Interop;
using SwitchDesktops.UI;

namespace SwitchDesktops.Services;

public sealed class DesktopSwitcher
{
    private readonly ScreenCapture _capture;
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(280);

    public DesktopSwitcher(ScreenCapture capture)
    {
        _capture = capture;
    }

    public async Task SwitchAsync(Desktop from, Desktop to)
    {
        from.LastFocusedHandle = NativeMethods.GetForegroundWindow();

        var fromBitmap = _capture.CapturePrimaryScreen();

        HideAll(from);
        ShowAll(to);

        var toBitmap = _capture.CapturePrimaryScreen();

        var overlay = new CrossfadeOverlay(fromBitmap, toBitmap);
        overlay.Show();

        await overlay.RunAsync(FadeDuration);
        RestoreFocus(to);
        await overlay.FadeOutAsync(TimeSpan.FromMilliseconds(120));
        overlay.Close();
    }

    private static void HideAll(Desktop desktop)
    {
        foreach (var w in desktop.Windows)
        {
            if (NativeMethods.IsWindow(w.Handle))
                NativeMethods.ShowWindow(w.Handle, NativeMethods.SW_HIDE);
        }
    }

    private static void ShowAll(Desktop desktop)
    {
        foreach (var w in desktop.Windows)
        {
            if (NativeMethods.IsWindow(w.Handle))
                NativeMethods.ShowWindow(w.Handle, NativeMethods.SW_SHOWNOACTIVATE);
        }
    }

    private static void RestoreFocus(Desktop desktop)
    {
        var target = desktop.LastFocusedHandle;
        if (target == IntPtr.Zero || !NativeMethods.IsWindow(target))
            target = desktop.Windows.FirstOrDefault(w => NativeMethods.IsWindow(w.Handle))?.Handle
                     ?? IntPtr.Zero;
        if (target == IntPtr.Zero) return;

        var foreground = NativeMethods.GetForegroundWindow();
        var currentThread = NativeMethods.GetCurrentThreadId();
        var foregroundThread = NativeMethods.GetWindowThreadProcessId(foreground, out _);

        if (foregroundThread != 0 && foregroundThread != currentThread)
            NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);

        NativeMethods.SetForegroundWindow(target);

        if (foregroundThread != 0 && foregroundThread != currentThread)
            NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
    }
}
