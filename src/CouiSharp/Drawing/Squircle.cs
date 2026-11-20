using System.Drawing;
using System.Drawing.Drawing2D;

namespace CouiSharp.Drawing;

/// <summary>
/// Squircle path builder. COUI derives its shape from a continuous-curvature superellipse
/// (extension ratio M = 1.33 for the XL tier, ~1.25 recommended). At control sizes that reads as
/// a corner that is slightly fuller than a circular arc, so we approximate it with a cubic
/// control factor above the circle's 0.5523 instead of sampling a superellipse (which would
/// produce hundreds of segments per control).
/// </summary>
public static class Squircle
{
    /// <summary>Cubic control factor for a circular quarter arc.</summary>
    public const float CircleControl = 0.5523f;

    /// <summary>COUI-flavoured corner: ~1.25 curvature extension, still tangent-continuous.</summary>
    public const float CouiControl = 0.68f;

    /// <summary>Builds a rounded rectangle path. Radius is clamped to half the shorter side.</summary>
    public static GraphicsPath RoundedRect(RectangleF bounds, float radius, float control = CouiControl)
    {
        var path = new GraphicsPath();
        AddRoundedRect(path, bounds, radius, control);
        return path;
    }

    /// <summary>Adds a rounded rectangle to an existing path (reused for rings and fills).</summary>
    public static void AddRoundedRect(GraphicsPath path, RectangleF bounds, float radius, float control = CouiControl)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        float r = Math.Max(0f, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f));
        if (r <= 0.01f)
        {
            path.AddRectangle(bounds);
            return;
        }

        float c = Math.Clamp(control, 0.2f, 0.95f) * r;
        float l = bounds.Left, t = bounds.Top, rt = bounds.Right, b = bounds.Bottom;

        // top edge, left -> right
        path.AddLine(l + r, t, rt - r, t);
        // top-right corner
        path.AddBezier(rt - r, t, rt - r + c, t, rt, t + r - c, rt, t + r);
        // right edge
        path.AddLine(rt, t + r, rt, b - r);
        // bottom-right
        path.AddBezier(rt, b - r, rt, b - r + c, rt - r + c, b, rt - r, b);
        // bottom edge
        path.AddLine(rt - r, b, l + r, b);
        // bottom-left
        path.AddBezier(l + r, b, l + r - c, b, l, b - r + c, l, b - r);
        // left edge
        path.AddLine(l, b - r, l, t + r);
        // top-left
        path.AddBezier(l, t + r, l, t + r - c, l + r - c, t, l + r, t);

        path.CloseFigure();
    }

    /// <summary>Capsule (pill) path for buttons and chips.</summary>
    public static GraphicsPath Capsule(RectangleF bounds)
        => RoundedRect(bounds, Math.Min(bounds.Width, bounds.Height) / 2f, CircleControl);

    /// <summary>Circle path, used for radio buttons and slider thumbs.</summary>
    public static GraphicsPath Circle(RectangleF bounds)
    {
        var path = new GraphicsPath();
        path.AddEllipse(bounds);
        return path;
    }
}
