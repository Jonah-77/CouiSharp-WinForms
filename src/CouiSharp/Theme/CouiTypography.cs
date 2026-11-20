using System.Drawing;

namespace CouiSharp.Theme;

/// <summary>
/// COUI type scale (<c>coui-ui/theme/TextStyles.kt</c>). Sizes are sp values converted to points
/// with the familiar 0.75 factor, then scaled by display density (<see cref="CouiTheme.Density"/>).
/// <para>
/// Every face is 霞鹜 (LXGW). <see cref="CouiTheme.FontFace"/> picks 文楷 (default) or 臻楷; the
/// weight split is resolved as a family name, because CJK releases ship "霞鹜文楷 Medium" as its own
/// family instead of a Bold style of 霞鹜文楷.
/// </para>
/// </summary>
public static class CouiTypography
{
    /// <summary>Family the app renders in, after face resolution (falls back to Microsoft YaHei UI).</summary>
    public static string Family
        => CouiFonts.Resolve(CouiTheme.FontFace, false) ?? CouiFonts.Resolve(CouiTheme.FontFace, true) ?? CouiFonts.FallbackFamily;

    /// <summary>True when the configured 霞鹜 face is actually installed.</summary>
    public static bool UsingBundledFace => CouiFonts.IsAvailable(CouiTheme.FontFace);

    /// <summary>18sp / medium — section headers and preference group titles.</summary>
    public static Font Header() => Create(18f, true);

    /// <summary>20sp / medium — dialog and page titles.</summary>
    public static Font Title() => Create(20f, true);

    /// <summary>24sp / semibold — display numbers and hero text.</summary>
    public static Font Display() => Create(24f, true);

    /// <summary>15sp / regular — default body text, list row labels.</summary>
    public static Font Body() => Create(15f, false);

    /// <summary>14sp / regular — dense body text.</summary>
    public static Font BodySmall() => Create(14f, false);

    /// <summary>13sp / regular — secondary descriptions.</summary>
    public static Font Caption() => Create(13f, false);

    /// <summary>12sp / regular — hints, helper text.</summary>
    public static Font Note() => Create(12f, false);

    /// <summary>10sp / medium — badges and overline labels.</summary>
    public static Font Label() => Create(10f, true);

    /// <summary>Creates a font at <paramref name="sp"/> scaled to the user display scale.</summary>
    public static Font Create(float sp, bool bold)
    {
        // Points already follow the monitor DPI in GDI, so only the user "display size" factor is
        // applied here. Multiplying by DeviceDpi as well double-scaled the type on 150% displays.
        float points = sp * 0.75f * CouiTheme.Scale;
        // Keep a floor so text stays legible when a host forces 96 dpi on a scaled display.
        points = Math.Max(points, 6f);
        return CouiFonts.Create(CouiTheme.FontFace, points, bold);
    }

    /// <summary>Preferred line box for a font, mirroring COUI's lineSpacingMultiplier behaviour.</summary>
    public static int LineHeight(Font font, float multiplier = 1.2f)
        => (int)Math.Ceiling(font.GetHeight() * multiplier);
}
