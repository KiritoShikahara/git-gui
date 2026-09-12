using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GitGui.App.Native;

/// <summary>Enables the dark window caption/title bar on Windows 10 (20H1+) and 11 so the
/// native chrome matches the app's dark theme instead of showing a light default title bar.</summary>
internal static class DarkTitleBar
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window)
    {
        void TrySet(IntPtr hwnd)
        {
            int useDark = 1;
            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref useDark, sizeof(int));
            }
        }

        if (PresentationSource.FromVisual(window) is HwndSource source)
        {
            TrySet(source.Handle);
        }
        else
        {
            window.SourceInitialized += (_, _) =>
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                TrySet(hwnd);
            };
        }
    }
}
