using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

/// <summary>
/// COUI preference card: a 12dp squircle on the <c>backgroundWithCard</c> page color.
/// Cards are pure containers — rows go inside, and their own surfaces paint over this fill.
/// </summary>
public class CouiCard : CouiControl
{
    private float _radius = CouiShape.CardRadius;
    private bool _showBorder;
    private bool _clickable;

    public CouiCard()
    {
        Padding = new Padding(Dp(CouiShape.CardInset), Dp(12), Dp(CouiShape.CardInset), Dp(12));
        AccessibleRole = AccessibleRole.Grouping;
    }

    /// <summary>Corner radius in dp. Defaults to the 12dp COUI card token.</summary>
    public float Radius
    {
        get => _radius;
        set { _radius = Math.Max(0f, value); Invalidate(); }
    }

    /// <summary>Draws a hairline divider-colored outline (used for tonal cards on white).</summary>
    public bool ShowBorder
    {
        get => _showBorder;
        set { _showBorder = value; Invalidate(); }
    }

    /// <summary>Enables press feedback and the <see cref="Control.Click"/> event on the card body.</summary>
    public bool Clickable
    {
        get => _clickable;
        set { _clickable = value; Cursor = value ? Cursors.Hand : Cursors.Default; Invalidate(); }
    }

    protected override bool Interactive => _clickable;

    /// <summary>The card fill, so nested controls can composite against it.</summary>
    public override Color SurfaceColor => Palette.Card;

    protected override void OnPaintContent(Graphics g)
    {
        RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = Squircle.RoundedRect(rect, Dpf(_radius));

        Color fill = Palette.Card;
        if (IsPressed && _clickable) fill = Palette.CardPressed;
        else if (IsHovered && _clickable) fill = CouiTheme.Composite(Palette.Card, Palette.Hover);

        Fill(g, path, fill);
        if (_showBorder) Stroke(g, path, Palette.Divider);
    }

    protected override void ApplyMetrics()
    {
        Padding = new Padding(Dp(CouiShape.CardInset), Dp(12), Dp(CouiShape.CardInset), Dp(12));
        base.ApplyMetrics();
    }
}
