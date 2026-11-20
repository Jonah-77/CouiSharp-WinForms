using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

/// <summary>
/// Base class for every CouiSharp control. It owns the three things a self-painted WinForms
/// control must get right: an opaque backdrop under rounded corners, DPI re-measurement, and
/// theme invalidation. Derived controls only implement <see cref="OnPaintContent"/> and
/// <see cref="ApplyMetrics"/>.
/// </summary>
public abstract class CouiControl : Control
{
    private bool _hover;
    private bool _pressed;
    private bool _themeHooked;

    protected CouiControl()
    {
        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw
               | ControlStyles.SupportsTransparentBackColor, true);
        // Never declare Opaque: WinForms would skip the backdrop and the rounded corners would
        // inherit whatever the parent painted last (seen as gray wedges on dark surfaces).
        SetStyle(ControlStyles.Opaque, false);

        BackColor = Color.Transparent;
        DoubleBuffered = true;
        Font = CouiTypography.Body();
    }

    /// <summary>Whether the control tracks hover and press states.</summary>
    protected virtual bool Interactive => false;

    /// <summary>
    /// The opaque surface this control paints, when children must composite against it instead of
    /// against a transparent BackColor (cards, sheets, fields). Transparent means "ask the parent".
    /// </summary>
    public virtual Color SurfaceColor => Color.Transparent;

    /// <summary>Current token set.</summary>
    protected static CouiColors Palette => CouiTheme.Colors;

    protected bool IsHovered => _hover;
    protected bool IsPressed => _pressed;

    protected int Dp(float dp) => CouiTheme.Dp(this, dp);
    protected float Dpf(float dp) => CouiTheme.Dpf(this, dp);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        HookTheme();
        ApplyMetrics();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        UnhookTheme();
        base.OnHandleDestroyed(e);
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        ApplyMetrics();
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ApplyMetrics();
    }

    /// <summary>Recomputes pixel metrics and cached fonts after a DPI, scale or theme change.</summary>
    protected virtual void ApplyMetrics()
    {
        Invalidate();
    }

    private void HookTheme()
    {
        if (_themeHooked) return;
        CouiTheme.Changed += OnThemeChanged;
        DpiChangedAfterParent += OnDpiChangedAfterParent;
        _themeHooked = true;
    }

    private void UnhookTheme()
    {
        if (!_themeHooked) return;
        CouiTheme.Changed -= OnThemeChanged;
        DpiChangedAfterParent -= OnDpiChangedAfterParent;
        _themeHooked = false;
    }

    private void OnDpiChangedAfterParent(object sender, EventArgs e)
    {
        Font = CouiTypography.Body();
        ApplyMetrics();
        Invalidate();
    }

    private void OnThemeChanged(object sender, EventArgs e)
    {
        if (!IsHandleCreated) return;
        Font = CouiTypography.Body();
        ApplyMetrics();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        CouiBackdrop.Fill(g, this);
        OnPaintContent(g);
    }

    /// <summary>Paints the control surface. The backdrop is already filled.</summary>
    protected abstract void OnPaintContent(Graphics g);

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (!Interactive) return;
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!Interactive) return;
        _hover = false;
        _pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Interactive || e.Button != MouseButtons.Left) return;
        _pressed = true;
        if (CanFocus) Focus();
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!Interactive) return;
        _pressed = false;
        Invalidate();
    }

    /// <summary>Draws the primary-color focus ring used across COUI components.</summary>
    protected void DrawFocusRing(Graphics g, GraphicsPath path)
    {
        if (!Focused) return;
        float width = Dpf(CouiShape.FocusRing);
        using var pen = new Pen(Palette.FocusOutline, width) { LineJoin = LineJoin.Round };
        g.DrawPath(pen, path);
    }

    /// <summary>Fills a path with a solid color.</summary>
    protected static void Fill(Graphics g, GraphicsPath path, Color color)
    {
        if (color.A == 0) return;
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }

    /// <summary>Strokes a path with a hairline pen scaled to the display.</summary>
    protected void Stroke(Graphics g, GraphicsPath path, Color color, float dp = CouiShape.Stroke)
    {
        if (color.A == 0) return;
        using var pen = new Pen(color, Dpf(dp)) { LineJoin = LineJoin.Round };
        g.DrawPath(pen, path);
    }

    /// <summary>
    /// Composites a translucent token onto the surface this control sits on. GDI's text renderer
    /// does not blend alpha reliably (a 26% label token can come out solid black), so text colors
    /// are flattened before they reach <see cref="TextRenderer"/>.
    /// </summary>
    protected Color Solid(Color color)
        => color.A == 255 ? color : CouiTheme.Composite(CouiBackdrop.Resolve(this), color);

    /// <summary>Text renderer wrapper that keeps ClearType and a consistent vertical alignment.</summary>
    protected void DrawText(Graphics g, string text, Font font, Color color, Rectangle bounds,
                            TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter)
    {
        if (string.IsNullOrEmpty(text)) return;
        TextRenderer.DrawText(g, text, font, bounds, Solid(color),
            flags | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
    }
}
