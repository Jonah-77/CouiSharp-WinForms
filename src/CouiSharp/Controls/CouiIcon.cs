using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

/// <summary>
/// A vector icon from the built-in set. Icons are stroked paths on a 24 x 24 canvas, so they stay
/// sharp at any size and take the current theme colour instead of baking a bitmap.
/// The control is square by default and sits inline in rows, buttons and toolbars.
/// </summary>
[ToolboxItem(true)]
public class CouiIcon : CouiControl
{
    private CouiIconKind _kind = CouiIconKind.Search;
    private Color _tint = Color.Empty;
    private float _iconSize = 24f;

    public CouiIcon()
    {
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        AccessibleRole = AccessibleRole.Graphic;
        Size = new Size(Dp(24), Dp(24));
        Font = CouiTypography.Body();
    }

    /// <summary>Which outline to draw.</summary>
    [Category("CouiSharp")]
    [Description("Which built-in outline to draw.")]
    public CouiIconKind Kind
    {
        get => _kind;
        set
        {
            if (_kind == value) return;
            _kind = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Overrides the stroke colour. Empty means the theme's primary label colour, so an icon
    /// automatically follows light / dark without the host tracking the mode.
    /// </summary>
    [Category("CouiSharp")]
    [Description("Stroke colour; empty follows the theme label colour.")]
    public Color Tint
    {
        get => _tint;
        set
        {
            if (_tint.ToArgb() == value.ToArgb()) return;
            _tint = value;
            Invalidate();
        }
    }

    /// <summary>Drawn at this logical size (dp) when the host does not size the control explicitly.</summary>
    [Category("CouiSharp")]
    [Description("Logical icon size in dp.")]
    public float IconSize
    {
        get => _iconSize;
        set
        {
            if (Math.Abs(_iconSize - value) < 0.001f) return;
            _iconSize = Math.Max(8f, value);
            ApplyMetrics();
            Invalidate();
        }
    }

    protected override bool FillsBackdrop => false;

    protected override void ApplyMetrics()
    {
        int side = Dp(_iconSize);
        if (Width != side || Height != side) Size = new Size(side, side);
        base.ApplyMetrics();
    }

    protected override void OnPaintContent(Graphics g)
    {
        float side = Math.Min(Width, Height);
        if (side <= 0.5f) return;

        float scale = side / CouiIconPaths.ViewBox;
        float penWidth = CouiIconPaths.Stroke;
        var (stroke, fill) = CouiIconPaths.Get(_kind);

        GraphicsState state = g.Save();
        try
        {
            g.TranslateTransform((Width - side) / 2f, (Height - side) / 2f);
            g.ScaleTransform(scale, scale);
            // Pen width is expressed in canvas units, so it scales with the icon instead of staying
            // a fixed hairline on a 48dp instance.

            Color tint = _tint.IsEmpty ? Palette.LabelPrimary : _tint;
            if (!Enabled) tint = CouiTheme.WithAlpha(tint, 0x66);

            if (fill.PointCount > 0)
            {
                using var brush = new SolidBrush(Solid(tint));
                g.FillPath(brush, fill);
            }
            if (stroke.PointCount > 0)
            {
                using var pen = new Pen(Solid(tint), penWidth)
                {
                    LineJoin = LineJoin.Round,
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                g.DrawPath(pen, stroke);
            }
        }
        finally
        {
            g.Restore(state);
        }
    }

    public override Size GetPreferredSize(Size proposedSize) => new Size(Dp(_iconSize), Dp(_iconSize));
}


