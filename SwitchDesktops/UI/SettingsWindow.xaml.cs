using System.Windows;
using System.Windows.Input;
using SwitchDesktops.Core;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColumnDefinition = System.Windows.Controls.ColumnDefinition;
using Grid = System.Windows.Controls.Grid;
using GridLength = System.Windows.GridLength;
using GridUnitType = System.Windows.GridUnitType;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = System.Windows.Controls.TextBox;

namespace SwitchDesktops.UI;

public partial class SettingsWindow : Window
{
    private readonly Dictionary<int, HotkeyBinding> _desktopBindings = new();
    private HotkeyBinding _moveBinding;

    public AppSettings Settings { get; }

    public SettingsWindow(AppSettings settings, DesktopManager manager)
    {
        InitializeComponent();
        Settings = Clone(settings);

        CrossfadeRadio.IsChecked = Settings.Transition == TransitionMode.Crossfade;
        HardCutRadio.IsChecked = Settings.Transition == TransitionMode.HardCut;

        foreach (var d in manager.Desktops)
        {
            _desktopBindings[d.Id] = Settings.DesktopHotkeys.TryGetValue(d.Id, out var b)
                ? new HotkeyBinding(b.Modifiers, b.Key)
                : new HotkeyBinding();
            AddHotkeyRow($"Switch to {d.Name}", d.Id);
        }

        _moveBinding = new HotkeyBinding(Settings.MoveWindowHotkey.Modifiers, Settings.MoveWindowHotkey.Key);
        AddHotkeyRow("Move window", null);
    }

    private void AddHotkeyRow(string label, int? desktopId)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var text = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.White
        };
        Grid.SetColumn(text, 0);

        var binding = desktopId.HasValue ? _desktopBindings[desktopId.Value] : _moveBinding;
        var box = new TextBox
        {
            IsReadOnly = true,
            Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 4, 6, 4),
            Text = binding.ToString(),
            Tag = desktopId
        };
        box.PreviewKeyDown += OnHotkeyKeyDown;
        Grid.SetColumn(box, 1);

        row.Children.Add(text);
        row.Children.Add(box);
        HotkeyPanel.Children.Add(row);
    }

    private void OnHotkeyKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var box = (TextBox)sender;
        var desktopId = (int?)box.Tag;
        e.Handled = true;
        ErrorText.Text = "";

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                 or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.None)
            return;

        if (key == Key.Escape)
        {
            var cleared = new HotkeyBinding();
            SetBinding(desktopId, cleared);
            box.Text = cleared.ToString();
            return;
        }

        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.None)
        {
            ErrorText.Text = "Shortcuts must include at least one modifier key (Ctrl, Alt, Shift, or Win).";
            return;
        }

        var binding = new HotkeyBinding(modifiers, key);
        SetBinding(desktopId, binding);
        box.Text = binding.ToString();
    }

    private void SetBinding(int? desktopId, HotkeyBinding binding)
    {
        if (desktopId.HasValue) _desktopBindings[desktopId.Value] = binding;
        else _moveBinding = binding;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var all = _desktopBindings.Values.Concat(new[] { _moveBinding })
            .Where(b => b.IsValid)
            .ToList();

        if (all.Select(b => (b.Modifiers, b.Key)).Distinct().Count() != all.Count)
        {
            ErrorText.Text = "Two shortcuts can't use the same key combination.";
            return;
        }

        Settings.Transition = HardCutRadio.IsChecked == true ? TransitionMode.HardCut : TransitionMode.Crossfade;
        Settings.DesktopHotkeys = new Dictionary<int, HotkeyBinding>(_desktopBindings);
        Settings.MoveWindowHotkey = _moveBinding;

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    private static AppSettings Clone(AppSettings src) => new()
    {
        Transition = src.Transition,
        DesktopHotkeys = src.DesktopHotkeys.ToDictionary(
            kv => kv.Key, kv => new HotkeyBinding(kv.Value.Modifiers, kv.Value.Key)),
        MoveWindowHotkey = new HotkeyBinding(src.MoveWindowHotkey.Modifiers, src.MoveWindowHotkey.Key)
    };
}
