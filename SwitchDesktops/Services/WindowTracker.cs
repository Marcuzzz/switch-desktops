using System.Text;
using SwitchDesktops.Core;
using SwitchDesktops.Interop;

namespace SwitchDesktops.Services;

public sealed class WindowTracker
{
    public IReadOnlyList<ManagedWindow> EnumerateTopLevelWindows()
    {
        var shell = NativeMethods.GetShellWindow();
        var self = System.Diagnostics.Process.GetCurrentProcess().Id;
        var results = new List<ManagedWindow>();

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (hWnd == shell) return true;
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;
            if (NativeMethods.GetAncestor(hWnd, NativeMethods.GA_ROOT) != hWnd) return true;

            var exStyle = NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            var hasAppWindow = (exStyle & NativeMethods.WS_EX_APPWINDOW) != 0;
            var isToolWindow = (exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;
            if (isToolWindow && !hasAppWindow) return true;

            if (NativeMethods.DwmGetWindowAttribute(hWnd,
                    NativeMethods.DWMWA_CLOAKED, out var cloaked, sizeof(int)) == 0
                && cloaked != 0)
            {
                return true;
            }

            var titleLength = NativeMethods.GetWindowTextLength(hWnd);
            if (titleLength == 0) return true;

            var title = new StringBuilder(titleLength + 1);
            NativeMethods.GetWindowText(hWnd, title, title.Capacity);

            var className = new StringBuilder(256);
            NativeMethods.GetClassName(hWnd, className, className.Capacity);

            NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
            if (pid == self) return true;

            results.Add(new ManagedWindow(hWnd, title.ToString(), className.ToString(), pid));
            return true;
        }, IntPtr.Zero);

        return results;
    }
}
