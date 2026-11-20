using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

public enum CouiButtonVariant
{
    /// <summary>Filled accent capsule — the one primary action on a page.</summary>
    Primary,

    /// <summary>Tonal capsule on the 15% accent layer, used for secondary actions.</summary>
    Tonal,

    /// <summary>Borderless text button for low-emphasis actions.</summary>
    Text,

    /// <summary>Destructive action, filled with the error token.</summary>
    Danger
}

/// <summary>
/// COUI capsule button: height 44dp, radius = height / 2, 20dp horizontal padding. Because the
/// label is drawn by this control after the backdrop is filled, a dark theme can never leak the
/// system button color underneath.
/// </summary>
public class CouiButton : CouiControl
{
    private CouiButtonVariant _variant = CouiButtonVariant.Primary;
    private bool _keyPressed;

    public CouiButton()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PushButton;
        Size = new Size(Dp(112), Dp(CouiShape.ButtonHeight));
    }

    protected override bool Interactive => true;

    public CouiButtonVariant Variant
    {
        get => _variant;
        set { _variant = value; Invalidate(); }
    }

    /// <summary>Optional accent override; falls back to the theme primary token.</summary>
    public Color? AccentColor { get; set; }

    public override Color SurfaceColor => Color.Transparent;

    private Color Accent => AccentColor ?? Palette.Primary;

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    protected override void OnPaintContent(Graphics g)
    {
        float height = Math.Min(Height, Dpf(CouiShape.ButtonHeight));
        // Bounds hug the client box exactly: a 0.5px offset used to leave a half-covered row
        // along the top edge, which showed up as a translucent rim at non-100% scale factors.
        RectangleF rect = new RectangleF(0f, (Height - height) / 2f, Width, height);
        using var path = Squircle.Capsule(rect);

        float radius = height / 2f;
        Color fill = Color.Transparent;
        Color text;
        Color border = Color.Transparent;

        switch (_variant)
        {
            case CouiButtonVariant.Primary:
                fill = Accent;
                text = Palette.LabelOnColor;
                break;
            case CouiButtonVariant.Tonal:
                fill = CouiTheme.WithAlpha(Accent, 0x26);
                text = Palette.PrimaryText;
                break;
            case CouiButtonVariant.Danger:
                fill = Palette.Error;
                text = Palette.LabelOnColor;
                break;
            default:
                text = Palette.PrimaryText;
                break;
        }

        if (!Enabled)
        {
            if (_variant is CouiButtonVariant.Text)
            {
                text = Palette.Disabled;
            }
            else
            {
                fill = CouiTheme.WithAlpha(fill, 0x5A);
                text = Palette.LabelOnColor;
            }
        }
        else if (IsPressed || _keyPressed)
        {
            fill = _variant == CouiButtonVariant.Text
                ? Palette.Press
                : CouiTheme.Composite(fill, CouiTheme.IsDark ? Palette.Press : Color.FromArgb(0x1F, 0, 0, 0));
            if (_variant == CouiButtonVariant.Tonal) fill = CouiTheme.WithAlpha(Accent, 0x3D);
        }
        else if (IsHovered)
        {
            if (_variant == CouiButtonVariant.Text) fill = Palette.Container8;
            else if (_variant == CouiButtonVariant.Tonal) fill = CouiTheme.WithAlpha(Accent, 0x33);
            else fill = CouiTheme.Composite(fill, CouiTheme.IsDark ? Palette.Hover : Color.FromArgb(0x14, 0, 0, 0));
        }

        if (fill.A > 0) Fill(g, path, fill);
        if (border.A > 0) StrokeInset(g, rect, radius, border, CouiShape.Stroke, Squircle.CircleControl);

        DrawFocusRing(g, rect, radius);

        Rectangle textRect = new Rectangle(
            (int)Math.Round(radius),
            0,
            Math.Max(0, Width - (int)Math.Round(radius * 2)),
            Height);

        // Text is ellipsized rather than auto-shrunk: allocating a fitted Font on every paint
        // would leak GDI handles at 60fps hover repaints.
        TextRenderer.DrawText(g, Text, Font, textRect, Solid(text),
            TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        int textWidth = TextRenderer.MeasureText(Text ?? string.Empty, Font).Width;
        return new Size(textWidth + Dp(CouiShape.ButtonPadding) * 2, Dp(CouiShape.ButtonHeight));
    }

    protected override void ApplyMetrics()
    {
        if (Height < Dp(CouiShape.ButtonHeight) || Size == Size.Empty)
            Size = new Size(Math.Max(Width, Dp(112)), Dp(CouiShape.ButtonHeight));
        base.ApplyMetrics();
    }

    protected override bool IsInputKey(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        if (key is Keys.Space or Keys.Enter) return true;
        return base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            _keyPressed = true;
            Invalidate();
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (_keyPressed && e.KeyCode is Keys.Space or Keys.Enter)
        {
            _keyPressed = false;
            Invalidate();
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        bool fire = Enabled && e.Button == MouseButtons.Left && IsPressed && ClientRectangle.Contains(e.Location);
        base.OnMouseUp(e);
        if (fire) OnClick(EventArgs.Empty);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }
}
