using System.Windows.Interop;
using SwitchDesktops.Interop;

namespace SwitchDesktops.Services;

public sealed class HotkeyService : IDisposable
{
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 1;

    public HotkeyService()
    {
        var parameters = new HwndSourceParameters("SwitchDesktopsHotkeys")
        {
            HwndSourceHook = WndProc,
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE
            Width = 0,
            Height = 0
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    public bool Register(uint modifiers, uint virtualKey, Action handler)
    {
        var id = _nextId++;
        if (!NativeMethods.RegisterHotKey(_source.Handle, id,
                modifiers | NativeMethods.MOD_NOREPEAT, virtualKey))
        {
            return false;
        }
        _handlers[id] = handler;
        return true;
    }

    public void Clear()
    {
        foreach (var id in _handlers.Keys)
            NativeMethods.UnregisterHotKey(_source.Handle, id);
        _handlers.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY
            && _handlers.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _handlers.Keys)
            NativeMethods.UnregisterHotKey(_source.Handle, id);
        _handlers.Clear();
        _source.Dispose();
    }
}
