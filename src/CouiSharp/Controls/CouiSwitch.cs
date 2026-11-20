using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

/// <summary>
/// COUI switch: 36×22dp track, 18dp thumb, capsule by definition. Off uses <c>container16</c>,
/// on uses the accent; the thumb is the on-color token with a 1dp shadow.
/// </summary>
public class CouiSwitch : CouiControl
{
    private bool _checked;
    private bool _keyPressed;

    public CouiSwitch()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.CheckButton;
        Size = new Size(Dp(CouiShape.SwitchWidth), Dp(CouiShape.SwitchHeight));
    }

    protected override bool Interactive => true;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            OnCheckedChanged(EventArgs.Empty);
        }
    }

    public event EventHandler CheckedChanged;

    protected virtual void OnCheckedChanged(EventArgs e)
    {
        CheckedChanged?.Invoke(this, e);
    }

    public override Color SurfaceColor => Color.Transparent;

    protected override void OnPaintContent(Graphics g)
    {
        float width = Dpf(CouiShape.SwitchWidth);
        float height = Dpf(CouiShape.SwitchHeight);
        RectangleF track = new RectangleF(
            (Width - width) / 2f, (Height - height) / 2f, width, height);
        using var trackPath = Squircle.Capsule(track);

        Color trackColor = _checked ? Palette.Primary : Palette.Container16;
        if (!Enabled) trackColor = CouiTheme.WithAlpha(trackColor, 0x66);
        else if (IsPressed || _keyPressed) trackColor = CouiTheme.Composite(trackColor, CouiTheme.IsDark ? Palette.Press : Palette.Hover);
        else if (IsHovered) trackColor = CouiTheme.Composite(trackColor, CouiTheme.IsDark ? Palette.Hover : Color.FromArgb(0x14, 0, 0, 0));

        Fill(g, trackPath, trackColor);

        float thumb = Dpf(CouiShape.SwitchThumb);
        float inset = (height - thumb) / 2f;
        float x = _checked ? track.Right - inset - thumb : track.Left + inset;
        RectangleF thumbRect = new RectangleF(x, track.Top + inset, thumb, thumb);

        // Shadow ring: a 1dp translucent ring reads as elevation without a blur pass.
        using (var shadow = Squircle.Circle(new RectangleF(thumbRect.X - Dpf(1), thumbRect.Y - Dpf(1),
                                                          thumbRect.Width + Dpf(2), thumbRect.Height + Dpf(2))))
        {
            Fill(g, shadow, Color.FromArgb(0x1A, 0, 0, 0));
        }

        using var thumbPath = Squircle.Circle(thumbRect);
        Fill(g, thumbPath, Enabled ? Palette.LabelOnColor : CouiTheme.WithAlpha(Palette.LabelOnColor, 0xB0));

        DrawFocusRing(g, track, height / 2f);
    }

    protected override void ApplyMetrics()
    {
        Size = new Size(Dp(CouiShape.SwitchWidth), Dp(CouiShape.SwitchHeight));
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
            Checked = !Checked;
            e.Handled = true;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        bool fire = Enabled && e.Button == MouseButtons.Left && IsPressed && ClientRectangle.Contains(e.Location);
        base.OnMouseUp(e);
        if (fire) Checked = !Checked;
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
