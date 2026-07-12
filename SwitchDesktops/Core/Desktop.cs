namespace SwitchDesktops.Core;

public sealed class Desktop
{
    public int Id { get; }
    public string Name { get; set; }
    public List<ManagedWindow> Windows { get; } = new();
    public IntPtr LastFocusedHandle { get; set; }

    public Desktop(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public void Add(ManagedWindow window)
    {
        if (!Windows.Any(w => w.Handle == window.Handle))
            Windows.Add(window);
    }

    public void Remove(IntPtr handle) =>
        Windows.RemoveAll(w => w.Handle == handle);

    public bool Contains(IntPtr handle) =>
        Windows.Any(w => w.Handle == handle);
}
