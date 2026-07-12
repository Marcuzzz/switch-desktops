using System.Windows;
using SwitchDesktops.Core;
using SwitchDesktops.Interop;
using SwitchDesktops.Services;

namespace SwitchDesktops.UI;

public partial class MoveWindowPicker : Window
{
    private readonly DesktopManager _manager;
    private readonly List<ManagedWindow> _visibleWindows;

    public MoveWindowPicker(DesktopManager manager, WindowTracker tracker)
    {
        InitializeComponent();
        _manager = manager;

        _visibleWindows = tracker.EnumerateTopLevelWindows()
            .Where(w => NativeMethods.IsWindowVisible(w.Handle))
            .ToList();

        foreach (var w in _visibleWindows)
        {
            var current = _manager.FindContaining(w.Handle);
            var suffix = current != null ? $"  ·  {current.Name}" : "";
            WindowList.Items.Add($"{w.Title}{suffix}");
        }
        if (WindowList.Items.Count > 0) WindowList.SelectedIndex = 0;

        foreach (var d in _manager.Desktops)
            DesktopCombo.Items.Add(d.Name);
        DesktopCombo.SelectedIndex = _manager.Desktops.ToList().IndexOf(_manager.Active);
    }

    private void OnMove(object sender, RoutedEventArgs e)
    {
        var wi = WindowList.SelectedIndex;
        var di = DesktopCombo.SelectedIndex;
        if (wi < 0 || di < 0) { Close(); return; }

        var window = _visibleWindows[wi];
        var target = _manager.Desktops[di];
        _manager.MoveWindow(window, target);

        if (target != _manager.Active)
            NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_HIDE);

        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
