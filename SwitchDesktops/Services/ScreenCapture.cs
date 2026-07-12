using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using SwitchDesktops.Core;
using SwitchDesktops.Interop;

namespace SwitchDesktops.Services;

public sealed class ScreenCapture
{
    public BitmapSource? CapturePrimaryScreen()
    {
        var bounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds
                     ?? new Rectangle(0, 0, 1920, 1080);
        using var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }
        return ToBitmapSource(bmp);
    }

    public BitmapSource? CaptureWindows(IEnumerable<ManagedWindow> windows)
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen?.Bounds
                     ?? new Rectangle(0, 0, 1920, 1080);
        using var composite = new Bitmap(screen.Width, screen.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(composite))
        {
            g.Clear(Color.Black);
            foreach (var w in windows)
            {
                if (!NativeMethods.IsWindow(w.Handle)) continue;
                if (!NativeMethods.GetWindowRect(w.Handle, out var rect)) continue;
                if (rect.Width <= 0 || rect.Height <= 0) continue;

                using var shot = CaptureSingle(w.Handle, rect.Width, rect.Height);
                if (shot != null)
                {
                    g.DrawImage(shot, rect.Left - screen.Left, rect.Top - screen.Top,
                        rect.Width, rect.Height);
                }
            }
        }
        return ToBitmapSource(composite);
    }

    private static Bitmap? CaptureSingle(IntPtr hWnd, int width, int height)
    {
        try
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            var hdc = g.GetHdc();
            try
            {
                var ok = NativeMethods.PrintWindow(hWnd, hdc, NativeMethods.PW_RENDERFULLCONTENT);
                if (!ok)
                {
                    bmp.Dispose();
                    return null;
                }
                return bmp;
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }
        catch
        {
            return null;
        }
    }

    private static BitmapSource? ToBitmapSource(Bitmap bmp)
    {
        var hBitmap = bmp.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
