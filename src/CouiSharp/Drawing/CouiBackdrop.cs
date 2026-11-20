using System.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Drawing;

/// <summary>
/// Resolves the effective background behind a control so self-painted rounded corners never show
/// a foreign color. Controls paint this first, then their own surface on top.
/// </summary>
public static class CouiBackdrop
{
    /// <summary>Walks the parent chain and composites until an opaque color is found.</summary>
    public static Color Resolve(Control control)
    {
        Color result = Color.Transparent;

        for (Control c = control; c != null; c = c.Parent)
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
