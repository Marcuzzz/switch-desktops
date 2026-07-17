using System.Windows;
using System.Windows.Input;
using H.NotifyIcon;
using SwitchDesktops.Core;
using SwitchDesktops.Services;
using SwitchDesktops.UI;

namespace SwitchDesktops;

public partial class App : System.Windows.Application
{
    private DesktopManager _manager = null!;
    private WindowTracker _tracker = null!;
    private ScreenCapture _capture = null!;
    private DesktopSwitcher _switcher = null!;
    private HotkeyService _hotkeys = null!;
    private TaskbarIcon _trayIcon = null!;
    private AppSettings _settings = null!;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        _settings = SettingsService.Load();
        _manager = new DesktopManager(initialCount: 3);
        _tracker = new WindowTracker();
        _capture = new ScreenCapture();
        _switcher = new DesktopSwitcher(_capture);
        _hotkeys = new HotkeyService();

        AssignExistingWindowsToActiveDesktop();

        RegisterHotkeys();

        _trayIcon = BuildTrayIcon();
        _trayIcon.ForceCreate(false);
    }

    private void RegisterHotkeys()
    {
        _hotkeys.Clear();

        foreach (var d in _manager.Desktops)
        {
            if (!_settings.DesktopHotkeys.TryGetValue(d.Id, out var binding) || !binding.IsValid)
                continue;

            var id = d.Id;
            _hotkeys.Register((uint)binding.Modifiers, (uint)KeyInterop.VirtualKeyFromKey(binding.Key),
                () => _ = SwitchTo(id));
        }

        if (_settings.MoveWindowHotkey.IsValid)
        {
            _hotkeys.Register((uint)_settings.MoveWindowHotkey.Modifiers,
                (uint)KeyInterop.VirtualKeyFromKey(_settings.MoveWindowHotkey.Key), ShowMovePicker);
        }
    }

    private void AssignExistingWindowsToActiveDesktop()
    {
        foreach (var w in _tracker.EnumerateTopLevelWindows())
            _manager.Active.Add(w);
    }

    private async Task SwitchTo(int desktopId)
    {
        var target = _manager.GetById(desktopId);
        if (target == null || target == _manager.Active) return;

        var current = _manager.Active;
        AbsorbNewWindows(current);

        await _switcher.SwitchAsync(current, target, _settings.Transition == TransitionMode.HardCut);
        _manager.SetActive(target);
        UpdateTrayTooltip();
    }

    private void AbsorbNewWindows(Desktop into)
    {
        var known = _manager.Desktops.SelectMany(d => d.Windows.Select(w => w.Handle))
                                    .ToHashSet();
        foreach (var w in _tracker.EnumerateTopLevelWindows())
        {
            if (!known.Contains(w.Handle))
                into.Add(w);
        }
    }

    private void ShowMovePicker()
    {
        AbsorbNewWindows(_manager.Active);
        var picker = new MoveWindowPicker(_manager, _tracker);
        picker.Show();
        picker.Activate();
    }

    private TaskbarIcon BuildTrayIcon()
    {
        var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "tray.ico");
        var icon = new TaskbarIcon
        {
            Icon = new System.Drawing.Icon(iconPath),
            ToolTipText = TrayTooltip(),
            ContextMenu = BuildContextMenu()
        };
        return icon;
    }

    private System.Windows.Controls.ContextMenu BuildContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();
        foreach (var d in _manager.Desktops)
        {
            var item = new System.Windows.Controls.MenuItem { Header = $"Switch to {d.Name}" };
            var id = d.Id;
            item.Click += async (_, _) => await SwitchTo(id);
            menu.Items.Add(item);
        }
        menu.Items.Add(new System.Windows.Controls.Separator());

        var moveItem = new System.Windows.Controls.MenuItem
        {
            Header = $"Move window… ({_settings.MoveWindowHotkey})"
        };
        moveItem.Click += (_, _) => ShowMovePicker();
        menu.Items.Add(moveItem);

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings…" };
        settingsItem.Click += (_, _) => ShowSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);
        return menu;
    }

    private void ShowSettings()
    {
        var window = new SettingsWindow(_settings, _manager);
        if (window.ShowDialog() == true)
        {
            _settings = window.Settings;
            SettingsService.Save(_settings);
            RegisterHotkeys();
            _trayIcon.ContextMenu = BuildContextMenu();
        }
    }

    private string TrayTooltip() => $"SwitchDesktops — {_manager.Active.Name}";

    private void UpdateTrayTooltip()
    {
        if (_trayIcon != null) _trayIcon.ToolTipText = TrayTooltip();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
