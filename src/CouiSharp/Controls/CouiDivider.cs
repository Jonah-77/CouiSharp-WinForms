using System.Drawing;
using System.Windows.Forms;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

/// <summary>1dp separator. Supports the 16dp leading inset COUI uses between list rows.</summary>
public class CouiDivider : CouiControl
{
    private float _inset = CouiShape.CardInset;

    public CouiDivider()
    {
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        Height = Dp(CouiShape.Divider);
    }

    /// <summary>Leading inset in dp. Set 0 for an edge-to-edge rule.</summary>
    public float Inset
    {
        get => _inset;
        set { _inset = Math.Max(0f, value); Invalidate(); }
    }

    public override Color SurfaceColor => Color.Transparent;

    protected override void OnPaintContent(Graphics g)
    {
        int inset = Dp(_inset);
        int thickness = Math.Max(1, Dp(CouiShape.Divider));
        using var brush = new SolidBrush(Palette.Divider);
        g.FillRectangle(brush, new Rectangle(inset, (Height - thickness) / 2, Math.Max(0, Width - inset), thickness));
    }

    protected override void ApplyMetrics()
    {
        Height = Math.Max(1, Dp(CouiShape.Divider));
        base.ApplyMetrics();
    }
}
