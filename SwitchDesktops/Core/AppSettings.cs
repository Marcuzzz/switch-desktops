using System.Text.Json.Serialization;
using System.Windows.Input;

namespace SwitchDesktops.Core;

public enum TransitionMode
{
    Crossfade,
    HardCut
}

public sealed class HotkeyBinding
{
    public ModifierKeys Modifiers { get; set; }
    public Key Key { get; set; }

    public HotkeyBinding() { }

    public HotkeyBinding(ModifierKeys modifiers, Key key)
    {
        Modifiers = modifiers;
        Key = key;
    }

    [JsonIgnore]
    public bool IsValid => Modifiers != ModifierKeys.None && Key != Key.None;

    public override string ToString()
    {
        if (!IsValid) return "(none)";

        var parts = new List<string>();
        if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(FormatKey(Key));
        return string.Join("+", parts);
    }

    private static string FormatKey(Key key) =>
        key is >= Key.D0 and <= Key.D9 ? ((int)(key - Key.D0)).ToString() : key.ToString();
}

public sealed class AppSettings
{
    public TransitionMode Transition { get; set; } = TransitionMode.Crossfade;

    public Dictionary<int, HotkeyBinding> DesktopHotkeys { get; set; } = new()
    {
        [1] = new HotkeyBinding(ModifierKeys.Control | ModifierKeys.Alt, Key.D1),
        [2] = new HotkeyBinding(ModifierKeys.Control | ModifierKeys.Alt, Key.D2),
        [3] = new HotkeyBinding(ModifierKeys.Control | ModifierKeys.Alt, Key.D3),
    };

    public HotkeyBinding MoveWindowHotkey { get; set; } =
        new HotkeyBinding(ModifierKeys.Control | ModifierKeys.Alt, Key.M);
}
