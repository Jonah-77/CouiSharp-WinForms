using System.Drawing;
using System.Windows.Forms;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

/// <summary>
/// COUI text field: 10dp squircle, 44dp tall, <c>container8</c> fill over the page surface, accent
/// outline on focus. A real <see cref="TextBox"/> is hosted borderless inside so IME, selection
/// and caret behaviour stay native — only chrome is hand-painted.
/// </summary>
public class CouiTextBox : CouiControl
{
    private readonly TextBox _inner;
    private string _placeholder = string.Empty;
    private bool _syncing;
    private bool _showingHint;

    public CouiTextBox()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = false; // focus lives on the inner TextBox
        AccessibleRole = AccessibleRole.Text;

        _inner = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = CouiTypography.Body(),
            AutoSize = false,
            TabStop = false
        };
        _inner.TextChanged += (_, _) =>
        {
            if (_showingHint || _syncing) return; // the hint is chrome, not a value
            _syncing = true;
            base.Text = _inner.Text;
            _syncing = false;
            Invalidate();
        };
        _inner.GotFocus += (_, _) =>
        {
            HideHint();
            _inner.ForeColor = Palette.LabelPrimary;
            Invalidate();
        };
        _inner.LostFocus += (_, _) =>
        {
            if (_inner.Text.Length == 0) ShowHint();
            Invalidate();
        };
        _inner.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) OnTextSubmitted(EventArgs.Empty);
        };
        Controls.Add(_inner);

        Height = Dp(CouiShape.InputHeight);
        Font = CouiTypography.Body();
    }

    /// <summary>Hint shown while the value is empty. Painted through the inner TextBox's own
    /// text/fore-color so it can follow theme tokens (native PlaceholderText cannot).</summary>
    public string PlaceholderText
    {
        get => _placeholder;
        set
        {
            _placeholder = value ?? string.Empty;
            if (_inner.Text.Length == 0 || _showingHint)
            {
                bool wasHint = _showingHint;
                _showingHint = false;
                if (wasHint || !_inner.Focused) ShowHint();
            }
            Invalidate();
        }
    }

    public bool UseSystemPasswordChar
    {
        get => _inner.UseSystemPasswordChar;
        set => _inner.UseSystemPasswordChar = value;
    }

    public event EventHandler TextSubmitted;

    protected virtual void OnTextSubmitted(EventArgs e) => TextSubmitted?.Invoke(this, e);

    /// <summary>Field fill over the current page, exposed so children match the painted chrome.</summary>
    public override Color SurfaceColor => FieldFill();

    private Color FieldFill()
    {
        // The surface the field sits on, not the one it paints: resolving from this control would
        // re-enter SurfaceColor, and resolving from its parent's parent skipped the card colour.
        Color page = Parent == null ? CouiTheme.Colors.Card : CouiBackdrop.ResolveSurface(Parent);
        return CouiTheme.Composite(page, Palette.Container8);
    }

    public override string Text
    {
        get => _showingHint ? string.Empty : _inner.Text;
        set
        {
            bool focused = _inner.Focused;
            _showingHint = false;
            _inner.Text = value ?? string.Empty;
            if (_inner.Text.Length == 0 && !focused) ShowHint();
            Invalidate();
        }
    }

    private void ShowHint()
    {
        if (_placeholder.Length == 0 || _inner.Focused) return;
        _showingHint = true;
        _inner.ForeColor = CouiTheme.Composite(FieldFill(), Palette.LabelTertiary);
        _inner.Text = _placeholder;
    }

    private void HideHint()
    {
        if (!_showingHint) return;
        _showingHint = false;
        _inner.Text = string.Empty;
        _inner.ForeColor = Palette.LabelPrimary;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _inner.Focus();
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    protected override void OnPaintContent(Graphics g)
    {
        RectangleF rect = new RectangleF(0f, 0f, Width, Height);
        using var path = Squircle.RoundedRect(rect, Dpf(CouiShape.InputRadius));

        Fill(g, path, FieldFill());
        bool focused = _inner.Focused || Focused;
        float radius = Dpf(CouiShape.InputRadius);
        StrokeInset(g, rect, radius, focused ? Palette.Primary : Palette.Divider,
            focused ? 1.5f : CouiShape.Stroke);

        if (!_inner.Visible) _inner.Visible = true;
    }

    protected override void ApplyMetrics()
    {
        int pad = Dp(CouiShape.InputPadding);
        Height = Dp(CouiShape.InputHeight);
        // Do not reset this.Font here: the host may have assigned a different size, and applying it
        // to the hosted editor is all that a metrics pass needs to do.
        _inner.Font = Font;
        _inner.BackColor = FieldFill();
        _inner.ForeColor = _showingHint
            ? CouiTheme.Composite(FieldFill(), Palette.LabelTertiary)
            : Palette.LabelPrimary;
        int textHeight = Math.Max(Dp(18), _inner.Font.Height + Dp(2));
        _inner.SetBounds(pad, (Height - textHeight) / 2, Math.Max(0, Width - pad * 2), textHeight);
        base.ApplyMetrics();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ApplyMetrics();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }
}

