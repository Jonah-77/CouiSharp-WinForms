using System.Drawing;

namespace CouiSharp.Theme;

/// <summary>
/// COUI (ColorOS 17) color tokens. Light and dark values follow the token table in
/// <c>coui-ui/theme/Colors.kt</c>; where COUI only defines an alpha layer we keep the alpha so
/// the control composites over whatever surface it sits on.
/// </summary>
public sealed record CouiColors
{
    // ---- accent -------------------------------------------------------------
    public Color Primary { get; init; } = Color.FromArgb(0xFF, 0x00, 0x80, 0xFF);
    public Color OnPrimary { get; init; } = Color.White;
    public Color PrimaryText { get; init; } = Color.FromArgb(0xFF, 0x00, 0x80, 0xFF);
    public Color Secondary { get; init; } = Color.FromArgb(0x26, 0x00, 0x80, 0xFF);
    public Color ContainerTheme { get; init; } = Color.FromArgb(0xFF, 0x00, 0x80, 0xFF);
    public Color ContainerThemeHalftone { get; init; } = Color.FromArgb(0x26, 0x00, 0x80, 0xFF);
    public Color FocusOutline { get; init; } = Color.FromArgb(0x66, 0x00, 0x80, 0xFF);

    // ---- surfaces ------------------------------------------------------------
    public Color Background { get; init; } = Color.White;
    public Color BackgroundWithCard { get; init; } = Color.FromArgb(0xFF, 0xF0, 0xF1, 0xF2);
    public Color Card { get; init; } = Color.White;
    public Color CardPressed { get; init; } = Color.FromArgb(0xFF, 0xE6, 0xE6, 0xE6);
    public Color Surface { get; init; } = Color.White;
    public Color SurfaceTop { get; init; } = Color.White;
    public Color Bar { get; init; } = Color.FromArgb(0xFF, 0xFA, 0xFA, 0xFA);

    // ---- text ----------------------------------------------------------------
    public Color LabelPrimary { get; init; } = Color.FromArgb(0xE6, 0, 0, 0);
    public Color LabelSecondary { get; init; } = Color.FromArgb(0x8A, 0, 0, 0);
    public Color LabelTertiary { get; init; } = Color.FromArgb(0x42, 0, 0, 0);
    public Color LabelOnColor { get; init; } = Color.White;

    // ---- layers / lines -----------------------------------------------------
    public Color Container8 { get; init; } = Color.FromArgb(0x14, 0, 0, 0);
    public Color Container12 { get; init; } = Color.FromArgb(0x1F, 0, 0, 0);
    public Color Container16 { get; init; } = Color.FromArgb(0x29, 0, 0, 0);
    public Color Divider { get; init; } = Color.FromArgb(0x1F, 0, 0, 0);
    public Color Press { get; init; } = Color.FromArgb(0x14, 0, 0, 0);
    public Color Hover { get; init; } = Color.FromArgb(0x0A, 0, 0, 0);
    public Color Disabled { get; init; } = Color.FromArgb(0x42, 0, 0, 0);
    public Color Mask { get; init; } = Color.FromArgb(0x33, 0, 0, 0);

    // ---- semantic ------------------------------------------------------------
    public Color Success { get; init; } = Color.FromArgb(0xFF, 0x00, 0xBD, 0x13);
    public Color Warning { get; init; } = Color.FromArgb(0xFF, 0xFF, 0x77, 0x00);
    public Color Error { get; init; } = Color.FromArgb(0xFF, 0xE8, 0x2A, 0x18);
    public Color Red { get; init; } = Color.FromArgb(0xFF, 0xEB, 0x3B, 0x2F);
    public Color Orange { get; init; } = Color.FromArgb(0xFF, 0xFF, 0x77, 0x00);
    public Color Yellow { get; init; } = Color.FromArgb(0xFF, 0xFF, 0xB2, 0x00);
    public Color Green { get; init; } = Color.FromArgb(0xFF, 0x00, 0xBD, 0x13);

    /// <summary>COUI light scheme (<c>coui_color_*</c>, non-night resources).</summary>
    public static CouiColors Light() => new();

    /// <summary>
    /// Returns the same scheme with a different accent seed. Every non-accent token is copied —
    /// building a partial instance silently fell back to the light defaults, which turned the page
    /// white when an accent was picked while the dark theme was active.
    /// </summary>
    public CouiColors WithAccent(Color seed, bool dark)
    {
        Color primary = dark ? CouiTheme.Mix(seed, Color.White, 0.25f) : seed;
        Color primaryText = dark ? CouiTheme.Mix(seed, Color.White, 0.45f) : seed;
        int halftoneAlpha = dark ? 0x40 : 0x26;
        return this with
        {
            Primary = primary,
            PrimaryText = primaryText,
            ContainerTheme = primary,
            Secondary = CouiTheme.WithAlpha(primary, halftoneAlpha),
            ContainerThemeHalftone = CouiTheme.WithAlpha(primary, halftoneAlpha),
            FocusOutline = CouiTheme.WithAlpha(primaryText, 0x66)
        };
    }

    /// <summary>COUI dark scheme (<c>values-night</c> resources).</summary>
    public static CouiColors Dark() => new()
    {
        Primary = Color.FromArgb(0xFF, 0x1A, 0x8C, 0xFF),
        OnPrimary = Color.White,
        PrimaryText = Color.FromArgb(0xFF, 0x5C, 0x9D, 0xFF),
        Secondary = Color.FromArgb(0x40, 0x1A, 0x8C, 0xFF),
        ContainerTheme = Color.FromArgb(0xFF, 0x1A, 0x8C, 0xFF),
        ContainerThemeHalftone = Color.FromArgb(0x40, 0x1A, 0x8C, 0xFF),
        FocusOutline = Color.FromArgb(0x66, 0x5C, 0x9D, 0xFF),

        Background = Color.Black,
        BackgroundWithCard = Color.Black,
        Card = Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E),
        CardPressed = Color.FromArgb(0xFF, 0x2C, 0x2C, 0x2C),
        Surface = Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E),
        SurfaceTop = Color.FromArgb(0xFF, 0x33, 0x33, 0x33),
        Bar = Color.FromArgb(0xFF, 0x14, 0x14, 0x14),

        LabelPrimary = Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF),
        LabelSecondary = Color.FromArgb(0x8A, 0xFF, 0xFF, 0xFF),
        LabelTertiary = Color.FromArgb(0x42, 0xFF, 0xFF, 0xFF),
        LabelOnColor = Color.White,

        Container8 = Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF),
        Container12 = Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF),
        Container16 = Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF),
        Divider = Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF),
        Press = Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF),
        Hover = Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
        Disabled = Color.FromArgb(0x29, 0xFF, 0xFF, 0xFF),
        Mask = Color.FromArgb(0x66, 0, 0, 0),

        Success = Color.FromArgb(0xFF, 0x24, 0xB2, 0x32),
        Warning = Color.FromArgb(0xFF, 0xFF, 0x8F, 0x1F),
        Error = Color.FromArgb(0xFF, 0xFF, 0x6C, 0x61),
        Red = Color.FromArgb(0xFF, 0xFF, 0x6C, 0x61),
        Orange = Color.FromArgb(0xFF, 0xFF, 0x8F, 0x1F),
        Yellow = Color.FromArgb(0xFF, 0xE5, 0xAF, 0x2E),
        Green = Color.FromArgb(0xFF, 0x24, 0xB2, 0x32),
    };
}
