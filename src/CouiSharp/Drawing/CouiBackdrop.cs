using System.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Drawing;

/// <summary>
/// Resolves the effective background behind a control so self-painted rounded corners never show
/// a foreign color. Controls paint this first, then their own surface on top.
/// </summary>
public static class CouiBackdrop
{
    /// <summary>
    /// Resolves the surface a control sits on. The walk starts at the control's parent: starting at
    /// the control itself returned its own surface color, which filled the whole client rect and
    /// turned every rounded shape into a square with a rounded hairline drawn over it — the corners
    /// showed a rectangular remnant (an "L") instead of the bare backdrop.
    /// </summary>
    public static Color Resolve(Control control) => ResolveCore(control?.Parent);

    /// <summary>
    /// Resolves the surface a control's own paint lands on, i.e. starting at the control itself.
    /// Used to flatten translucent tokens (text colors, hairlines) onto the fill they sit on.
    /// </summary>
    public static Color ResolveSurface(Control control) => ResolveCore(control);

    private static Color ResolveCore(Control start)
    {
        Color result = Color.Transparent;

        for (Control c = start; c != null; c = c.Parent)
        {
            Color layer = c.BackColor;
            if (c is Controls.CouiControl cc && cc.SurfaceColor.A > 0) layer = cc.SurfaceColor;
            if (layer.A == 0 && c is not Form && c is not UserControl)
            {
                continue;
            }

            result = result.A == 0 ? layer : CouiTheme.Composite(layer, result);
            if (result.A >= 255) return result;
        }

        if (result.A == 0) return CouiTheme.Colors.Background;
        if (result.A < 255) return CouiTheme.Composite(CouiTheme.Colors.Background, result);
        return result;
    }

    /// <summary>Fills the whole client area with the resolved backdrop.</summary>
    public static void Fill(Graphics graphics, Control control, Color? overrideColor = null)
    {
        Color color = overrideColor ?? Resolve(control);
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, control.ClientRectangle);
    }
}
