using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

/// <summary>
/// COUI checkbox: 18dp squircle box, 4dp radius, accent fill with a white tick when checked.
/// The label sits 8dp to the right and the entire row is clickable.
/// </summary>
public class CouiCheckBox : CouiControl
{
    private bool _checked;

    public CouiCheckBox()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.CheckButton;
        Text = "选项";
        Size = new Size(Dp(160), Dp(24));
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
        float box = Dpf(18);
        float top = (Height - box) / 2f;
        RectangleF rect = new RectangleF(0, top, box, box);
        using var path = Squircle.RoundedRect(rect, Dpf(4), Squircle.CircleControl);

        if (_checked)
        {
            Fill(g, path, Enabled ? Palette.Primary : CouiTheme.WithAlpha(Palette.Primary, 0x66));
            // Tick drawn as two capsules so it scales with DPI.
            using var pen = new Pen(Palette.LabelOnColor, Dpf(1.6f)) { LineJoin = LineJoin.Round };
            PointF a = new PointF(rect.Left + box * 0.24f, rect.Top + box * 0.52f);
            PointF b = new PointF(rect.Left + box * 0.43f, rect.Top + box * 0.72f);
            PointF c = new PointF(rect.Left + box * 0.77f, rect.Top + box * 0.30f);
            g.DrawLines(pen, new[] { a, b, c });
            Stroke(g, path, Palette.Primary);
        }
        else
        {
            Fill(g, path, Palette.Container8);
            Stroke(g, path, Palette.Divider);
        }

        int textLeft = (int)Math.Round(box + Dpf(8));
        DrawText(g, Text, Font, Enabled ? Palette.LabelPrimary : Palette.Disabled,
            new Rectangle(textLeft, 0, Width - textLeft, Height));
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        int width = Dp(18) + Dp(8) + TextRenderer.MeasureText(Text ?? string.Empty, Font).Width + Dp(2);
        return new Size(width, Math.Max(Dp(24), Font.Height + Dp(4)));
    }

    protected override void ApplyMetrics()
    {
        Size = GetPreferredSize(Size.Empty);
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

/// <summary>
/// COUI radio button: 18dp ring with an 8dp accent dot when selected. Grouping is left to the
/// host (set <see cref="GroupName"/> and the control unchecks siblings on selection).
/// </summary>
public class CouiRadioButton : CouiControl
{
    private bool _checked;

    public CouiRadioButton()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.RadioButton;
        Text = "选项";
        Size = new Size(Dp(160), Dp(24));
    }

    protected override bool Interactive => true;

    public string GroupName { get; set; } = string.Empty;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            if (value) UncheckSiblings();
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler CheckedChanged;

    public override Color SurfaceColor => Color.Transparent;

    private void UncheckSiblings()
    {
        if (string.IsNullOrEmpty(GroupName) || Parent == null) return;
        foreach (Control sibling in Parent.Controls)
        {
            if (sibling is CouiRadioButton radio && radio != this
                && string.Equals(radio.GroupName, GroupName, StringComparison.Ordinal))
            {
                radio._checked = false;
                radio.Invalidate();
            }
        }
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        ApplyMetrics();
        Invalidate();
    }

    protected override void OnPaintContent(Graphics g)
    {
        float size = Dpf(18);
        RectangleF rect = new RectangleF(0, (Height - size) / 2f, size, size);
        using var ring = Squircle.Circle(rect);

        Stroke(g, ring, _checked ? Palette.Primary : Palette.Container16, 1.5f);
        if (_checked)
        {
            float dot = Dpf(8);
            using var dotPath = Squircle.Circle(new RectangleF(
                rect.Left + (size - dot) / 2f, rect.Top + (size - dot) / 2f, dot, dot));
            Fill(g, dotPath, Enabled ? Palette.Primary : CouiTheme.WithAlpha(Palette.Primary, 0x66));
        }

        int textLeft = (int)Math.Round(size + Dpf(8));
        DrawText(g, Text, Font, Enabled ? Palette.LabelPrimary : Palette.Disabled,
            new Rectangle(textLeft, 0, Width - textLeft, Height));
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        int width = Dp(18) + Dp(8) + TextRenderer.MeasureText(Text ?? string.Empty, Font).Width + Dp(2);
        return new Size(width, Math.Max(Dp(24), Font.Height + Dp(4)));
    }

    protected override void ApplyMetrics()
    {
        Size = GetPreferredSize(Size.Empty);
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
            Checked = true;
            e.Handled = true;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        bool fire = Enabled && e.Button == MouseButtons.Left && IsPressed && ClientRectangle.Contains(e.Location);
        base.OnMouseUp(e);
        if (fire) Checked = true;
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
}
