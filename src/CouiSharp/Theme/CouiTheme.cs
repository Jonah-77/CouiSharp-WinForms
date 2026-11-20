using System.Drawing;
using Microsoft.Win32;

namespace CouiSharp.Theme;

public enum CouiThemeMode
{
    System,
    Light,
    Dark
}

/// <summary>
/// Global theme state for CouiSharp: mode (light / dark / follow system), accent seed and
/// display density. Controls subscribe through <see cref="Changed"/>; raising it invalidates every
/// subscriber's cached metrics and fonts.
/// </summary>
public static class CouiTheme
{
    private static CouiThemeMode _mode = CouiThemeMode.System;
    private static Color _seed = Color.FromArgb(0xFF, 0x00, 0x80, 0xFF);
    private static float _scale = 1f;
    private static bool? _systemDarkCache;
    private static DateTime _systemDarkStamp = DateTime.MinValue;
    private static bool _watcherHooked;

    /// <summary>Raised when mode, accent or scale changes.</summary>
    public static event EventHandler Changed;

    /// <summary>Light / dark / follow the Windows app theme.</summary>
    public static CouiThemeMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            _mode = value;
            Raise();
        }
    }

    /// <summary>Accent seed. ColorOS ships #0080FF; keep it as the default.</summary>
    public static Color SeedColor
    {
        get => _seed;
        set
        {
            if (_seed.ToArgb() == value.ToArgb()) return;
            _seed = value;
            Raise();
        }
    }

    /// <summary>
    /// User display scale, the WinForms equivalent of ColorOS "显示大小". Multiplied by the
    /// monitor DPI ratio, so 1.0 means "follow the system".
    /// </summary>
    public static float Scale
    {
        get => _scale;
        set
        {
            float v = Math.Clamp(value, 0.75f, 2.5f);
            if (Math.Abs(_scale - v) < 0.0001f) return;
            _scale = v;
            Raise();
        }
    }

    /// <summary>True when the effective scheme is dark.</summary>
    public static bool IsDark
    {
        get
        {
            switch (_mode)
            {
                case CouiThemeMode.Light: return false;
                case CouiThemeMode.Dark: return true;
                default: return SystemPrefersDark();
            }
        }
    }

    /// <summary>Current token set, derived from <see cref="IsDark"/> and <see cref="SeedColor"/>.</summary>
    public static CouiColors Colors => IsDark ? DarkColors() : LightColors();

    private static CouiColors LightColors()
    {
        if (_seed.ToArgb() == Color.FromArgb(0xFF, 0x00, 0x80, 0xFF).ToArgb()) return CouiColors.Light();
        return new CouiColors
        {
            Primary = _seed,
            PrimaryText = _seed,
            ContainerTheme = _seed,
            Secondary = WithAlpha(_seed, 0x26),
            ContainerThemeHalftone = WithAlpha(_seed, 0x26),
            FocusOutline = WithAlpha(_seed, 0x66),
        };
    }

    private static CouiColors DarkColors()
    {
        var d = CouiColors.Dark();
        if (_seed.ToArgb() == Color.FromArgb(0xFF, 0x00, 0x80, 0xFF).ToArgb()) return d;
        Color lifted = Mix(_seed, Color.White, 0.25f);
        return new CouiColors
        {
            Primary = lifted,
            PrimaryText = Mix(_seed, Color.White, 0.45f),
            ContainerTheme = lifted,
            Secondary = WithAlpha(lifted, 0x40),
            ContainerThemeHalftone = WithAlpha(lifted, 0x40),
            FocusOutline = WithAlpha(Mix(_seed, Color.White, 0.45f), 0x66),
        };
    }

    /// <summary>Device-pixel ratio for a control: monitor DPI x user scale.</summary>
    public static float DensityFor(Control control)
    {
        float baseRatio = control == null || control.DeviceDpi <= 0 ? 1f : control.DeviceDpi / 96f;
        return baseRatio * _scale;
    }

    /// <summary>Converts dp to device pixels for a control.</summary>
    public static int Dp(Control control, float dp) => (int)Math.Round(dp * DensityFor(control));

    /// <summary>Converts dp to device pixels as a float (for stroke widths and centres).</summary>
    public static float Dpf(Control control, float dp) => dp * DensityFor(control);

    /// <summary>Density of the primary screen, for code that has no control at hand.</summary>
    public static float Density
    {
        get
        {
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            return g.DpiX / 96f * _scale;
        }
    }

    /// <summary>SP to points factor used by the type scale.</summary>
    public static float SpFactor => 0.75f * Density;

    public static Color WithAlpha(Color color, int alpha)
        => Color.FromArgb(alpha, color.R, color.G, color.B);

    public static Color Mix(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)Math.Round(a.A + (b.A - a.A) * t),
            (int)Math.Round(a.R + (b.R - a.R) * t),
            (int)Math.Round(a.G + (b.G - a.G) * t),
            (int)Math.Round(a.B + (b.B - a.B) * t));
    }

    /// <summary>Composites <paramref name="top"/> over <paramref name="bottom"/> (source-over).</summary>
    public static Color Composite(Color bottom, Color top)
    {
        if (top.A >= 255) return top;
        float ta = top.A / 255f;
        float ba = bottom.A / 255f;
        float outA = ta + ba * (1 - ta);
        if (outA <= 0f) return Color.Transparent;
        float r = (top.R * ta + bottom.R * ba * (1 - ta)) / outA;
        float g = (top.G * ta + bottom.G * ba * (1 - ta)) / outA;
        float b = (top.B * ta + bottom.B * ba * (1 - ta)) / outA;
        return Color.FromArgb((int)Math.Round(outA * 255), (int)r, (int)g, (int)b);
    }

    /// <summary>Forces every subscriber to repaint and re-measure.</summary>
    public static void Raise() => Changed?.Invoke(null, EventArgs.Empty);

    /// <summary>Reads the Windows "app theme" preference, cached for two seconds.</summary>
    public static bool SystemPrefersDark()
    {
        if (_systemDarkCache.HasValue && (DateTime.UtcNow - _systemDarkStamp).TotalSeconds < 2)
            return _systemDarkCache.Value;

        bool dark = false;
        try
        {
            object v = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", null);
            if (v is int i) dark = i == 0;
        }
        catch
        {
            dark = false;
        }

        _systemDarkCache = dark;
        _systemDarkStamp = DateTime.UtcNow;
        HookWatcher();
        return dark;
    }

    private static void HookWatcher()
    {
        if (_watcherHooked) return;
        try
        {
            SystemEvents.UserPreferenceChanged += (_, e) =>
            {
                if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.Color)
                {
                    _systemDarkCache = null;
                    Raise();
                }
            };
            _watcherHooked = true;
        }
        catch
        {
            // System events are unavailable in some hosted environments; polling still works.
        }
    }
}
