using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CouiSharp.Drawing;

/// <summary>Backdrop the Windows compositor draws behind a top-level window.</summary>
public enum CouiBackdropStyle
{
    /// <summary>No effect: the window paints its own background.</summary>
    None,

    /// <summary>Desktop wallpaper sampled and blurred across the whole window (Windows 11 22H2+).</summary>
    Mica,

    /// <summary>Blurred, tinted copy of what is behind the window; the classic acrylic sheet.</summary>
    Acrylic,

    /// <summary>Mica variant that also samples the tabs behind the window (Windows 11 22H2+).</summary>
    Tabbed
}

/// <summary>
/// Applies a backdrop the <b>operating system</b> draws, instead of blurring pixels in managed code.
/// <para>
/// Two paths, both native:
/// </para>
/// <list type="bullet">
/// <item><description>
/// Windows 11 22H2 (build 22621) and later: <c>DWMWA_SYSTEMBACKDROP_TYPE</c> through dwmapi.dll,
/// which is the same Mica / Acrylic the system uses for Settings and the Start menu. It is free
/// (DWM does the work) and follows the wallpaper and the OS light/dark setting.
/// </description></item>
/// <item><description>
/// Windows 10 1803 to Windows 11 21H2: <c>SetWindowCompositionAttribute</c> with the acrylic accent
/// state. Undocumented, but stable across those builds and used by most Win32 apps that ship acrylic.
/// </description></item>
/// </list>
/// <para>
/// A WinForms window only shows the backdrop where it does not paint an opaque background, so the
/// realistic use is a small top-level surface (a floating bar, a palette, a toast) drawn with
/// controls that paint only their own glyphs — <see cref="Controls.CouiIcon"/> does exactly that.
/// </para>
/// </summary>
public static class CouiBackdropEffect
{
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

    private const int DWMWCP_ROUND = 2;
    private const int DWMSBT_NONE = 1;
    private const int DWMSBT_MAINWINDOW = 2;   // Mica
    private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic
    private const int DWMSBT_TABBEDWINDOW = 4; // Mica Alt

    private const int WCA_ACCENT_POLICY = 19;
    private const int ACCENT_ENABLE_BLURBEHIND = 3;
    private const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public int GradientColor; // 0xAABBGGRR
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    /// <summary>Windows build number of the current machine (e.g. 22621 for Windows 11 22H2).</summary>
    public static int BuildNumber
    {
        get
        {
            try { return Environment.OSVersion.Version.Build; }
            catch { return 0; }
        }
    }

    /// <summary>True when this build can draw at least one of the backdrop styles.</summary>
    public static bool IsSupported => BuildNumber >= 17763;

    /// <summary>True when the modern DWM backdrop attribute is available (Mica / Acrylic / Tabbed).</summary>
    public static bool SupportsSystemBackdrop => BuildNumber >= 22621;

    /// <summary>
    /// Tells the compositor whether the window wants the dark variant of its backdrop. Call it
    /// whenever the app theme flips, otherwise the acrylic sheet keeps the system's own shade.
    /// </summary>
    public static void ApplyDarkMode(IntPtr hwnd, bool dark)
    {
        if (hwnd == IntPtr.Zero || BuildNumber < 18985) return;
        int value = dark ? 1 : 0;
        // 20 on 20H1+, 19 was the pre-release attribute id; the newer one superseded it.
        if (DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, 19, ref value, sizeof(int));
    }

    /// <summary>
    /// Applies <paramref name="style"/> to a top-level window. Returns false when the build cannot
    /// provide it, in which case the caller should leave the window painting its own background.
    /// </summary>
    public static bool Apply(Form window, CouiBackdropStyle style, Color? tint = null)
    {
        if (window == null || !window.IsHandleCreated) return false;
        return Apply(window.Handle, style, tint);
    }

    /// <summary>
    /// Applies <paramref name="style"/> to a raw window handle. <paramref name="tint"/> is the wash
    /// laid over the blur on the legacy accent path (Windows 10); the system backdrop of Windows 11
    /// takes its tint from the OS theme and ignores it.
    /// </summary>
    public static bool Apply(IntPtr hwnd, CouiBackdropStyle style, Color? tint = null)
    {
        if (hwnd == IntPtr.Zero) return false;

        if (style == CouiBackdropStyle.None)
        {
            Clear(hwnd);
            return true;
        }

        if (SupportsSystemBackdrop)
        {
            int value = style switch
            {
                CouiBackdropStyle.Mica => DWMSBT_MAINWINDOW,
                CouiBackdropStyle.Acrylic => DWMSBT_TRANSIENTWINDOW,
                CouiBackdropStyle.Tabbed => DWMSBT_TABBEDWINDOW,
                _ => DWMSBT_NONE
            };
            // The frame has to cover the client area, otherwise the backdrop stops at the border.
            Margins margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
            int round = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
            int hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref value, sizeof(int));
            if (hr == 0) return true;
            // Fall through to the accent policy when the attribute is rejected (some GPU drivers).
        }

        if (BuildNumber < 17763) return false;
        return ApplyAccent(hwnd, tint ?? Color.FromArgb(0xCC, 0x20, 0x20, 0x20),
                           style == CouiBackdropStyle.Acrylic ? ACCENT_ENABLE_ACRYLICBLURBEHIND : ACCENT_ENABLE_BLURBEHIND);
    }

    /// <summary>Removes any backdrop and lets the window paint normally again.</summary>
    public static void Clear(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;
        if (SupportsSystemBackdrop)
        {
            int none = DWMSBT_NONE;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref none, sizeof(int));
            Margins zero = new Margins();
            DwmExtendFrameIntoClientArea(hwnd, ref zero);
        }
        ApplyAccent(hwnd, Color.Empty, 0);
    }

    private static bool ApplyAccent(IntPtr hwnd, Color tint, int state)
    {
        AccentPolicy policy = new AccentPolicy
        {
            AccentState = state,
            AccentFlags = 2,
            // GradientColor is 0xAABBGGRR, not the GDI+ 0xAARRGGBB order.
            GradientColor = state == 0
                ? 0
                : (tint.A << 24) | (tint.B << 16) | (tint.G << 8) | tint.R,
            AnimationId = 0
        };
        int size = Marshal.SizeOf<AccentPolicy>();
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(policy, buffer, false);
            WindowCompositionAttributeData data = new WindowCompositionAttributeData
            {
                Attribute = WCA_ACCENT_POLICY,
                Data = buffer,
                SizeOfData = size
            };
            return SetWindowCompositionAttribute(hwnd, ref data) != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}


