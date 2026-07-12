namespace SwitchDesktops.Core;

public sealed class DesktopManager
{
    private readonly List<Desktop> _desktops = new();

    public IReadOnlyList<Desktop> Desktops => _desktops;
    public Desktop Active { get; private set; }

    public event EventHandler<DesktopChangedEventArgs>? ActiveChanged;

    public DesktopManager(int initialCount = 3)
    {
        for (var i = 1; i <= initialCount; i++)
            _desktops.Add(new Desktop(i, $"Desktop {i}"));
        Active = _desktops[0];
    }

    public Desktop? GetById(int id) => _desktops.FirstOrDefault(d => d.Id == id);

    public Desktop? FindContaining(IntPtr handle) =>
        _desktops.FirstOrDefault(d => d.Contains(handle));

    public void MoveWindow(ManagedWindow window, Desktop target)
    {
        foreach (var d in _desktops)
            d.Remove(window.Handle);
        target.Add(window);
    }

    public void SetActive(Desktop desktop)
    {
        if (desktop == Active) return;
        var previous = Active;
        Active = desktop;
        ActiveChanged?.Invoke(this, new DesktopChangedEventArgs(previous, desktop));
    }
}

public sealed class DesktopChangedEventArgs : EventArgs
{
    public Desktop From { get; }
    public Desktop To { get; }
    public DesktopChangedEventArgs(Desktop from, Desktop to)
    {
        From = from;
        To = to;
    }
}
