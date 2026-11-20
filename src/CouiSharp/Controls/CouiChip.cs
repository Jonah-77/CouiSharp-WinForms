using System.Drawing;
using System.Windows.Forms;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

/// <summary>
/// COUI filter chip: 32dp capsule, <c>container8</c> when idle, 15% accent + accent text when
/// selected. Chips are checkable and expose <see cref="CheckedChanged"/>.
/// </summary>
public class CouiChip : CouiControl
{
    private bool _checked;

    public CouiChip()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.CheckButton;
        Size = new Size(Dp(80), Dp(CouiShape.ChipHeight));
        Font = CouiTypography.Caption();
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
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler CheckedChanged;

    public override Color SurfaceColor => Color.Transparent;

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        ApplyMetrics();
        Invalidate();
    }

    protected override void OnPaintContent(Graphics g)
    {
        float height = Math.Min(Height, Dpf(CouiShape.ChipHeight));
        RectangleF rect = new RectangleF(0.5f, (Height - height) / 2f + 0.5f, Width - 1f, height - 1f);
        using var path = Squircle.Capsule(rect);

        Color fill = _checked ? CouiTheme.WithAlpha(Palette.Primary, 0x26) : Palette.Container8;
        if (!Enabled) fill = CouiTheme.WithAlpha(fill, 0x66);
        else if (IsPressed) fill = _checked ? CouiTheme.WithAlpha(Palette.Primary, 0x3D) : Palette.Press;
        else if (IsHovered) fill = _checked ? CouiTheme.WithAlpha(Palette.Primary, 0x33) : Palette.Container12;

        Fill(g, path, fill);
        DrawFocusRing(g, path);

        DrawText(g, Text, Font, _checked ? Palette.PrimaryText : Palette.LabelPrimary,
            new Rectangle(0, 0, Width, Height),
            TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    public override Size GetPreferredSize(Size proposedSize)
        => new Size(TextRenderer.MeasureText(Text ?? string.Empty, Font).Width + Dp(28), Dp(CouiShape.ChipHeight));

    protected override void ApplyMetrics()
    {
        Size = new Size(Math.Max(Width, GetPreferredSize(Size.Empty).Width), Dp(CouiShape.ChipHeight));
        base.ApplyMetrics();
    }

    protected override bool IsInputKey(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        if (key is Keys.Space or Keys.Enter) return true;
        return base.IsInputKey(keyData);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
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

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
}
