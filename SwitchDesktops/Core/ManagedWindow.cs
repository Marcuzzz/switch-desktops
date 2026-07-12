namespace SwitchDesktops.Core;

public sealed class ManagedWindow
{
    public IntPtr Handle { get; }
    public string Title { get; set; }
    public string ClassName { get; }
    public uint ProcessId { get; }

    public ManagedWindow(IntPtr handle, string title, string className, uint processId)
    {
        Handle = handle;
        Title = title;
        ClassName = className;
        ProcessId = processId;
    }

    public override bool Equals(object? obj) =>
        obj is ManagedWindow other && other.Handle == Handle;

    public override int GetHashCode() => Handle.GetHashCode();

    public override string ToString() => $"{Title} ({ClassName})";
}
