using System.Drawing;
using System.Windows.Forms;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

/// <summary>
/// COUI slider: 4dp track, accent fill, 14dp thumb. Minimum interactive height is 20dp; the
/// control defaults to 28dp so the 14dp thumb has room. Keyboard: arrows, Home/End, PageUp/Down.
/// </summary>
public class CouiSlider : CouiControl
{
    private int _value = 50;
    private int _minimum;
    private int _maximum = 100;
    private int _step = 1;
    private bool _dragging;

    public CouiSlider()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.Slider;
        Height = Dp(28);
    }

    protected override bool Interactive => true;

    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum <= _minimum) _maximum = _minimum + 1;
            Value = Math.Clamp(_value, _minimum, _maximum);
            Invalidate();
        }
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(value, _minimum + 1);
            Value = Math.Clamp(_value, _minimum, _maximum);
            Invalidate();
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            int v = Math.Clamp(value, _minimum, _maximum);
            if (v == _value) return;
            _value = v;
            AccessibleDescription = $"{v}";
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Snap increment for keyboard and drag. 0 disables snapping.</summary>
    public int Step
    {
        get => _step;
        set => _step = Math.Max(0, value);
    }

    public event EventHandler ValueChanged;

    public override Color SurfaceColor => Color.Transparent;

    private float Fraction => _maximum <= _minimum ? 0f : (float)(_value - _minimum) / (_maximum - _minimum);

    private const float ThumbDp = 14f;

    protected override void OnPaintContent(Graphics g)
    {
        float trackHeight = Dpf(CouiShape.SliderTrack);
        float inset = Dpf(ThumbDp / 2f);
        RectangleF track = new RectangleF(inset, (Height - trackHeight) / 2f,
                                         Math.Max(1f, Width - inset * 2), trackHeight);

        using (var trackPath = Squircle.Capsule(track))
        {
            Fill(g, trackPath, Palette.Container12);
        }

        float x = track.Left + track.Width * Fraction;
        if (x > track.Left + 0.5f)
        {
            RectangleF fillRect = new RectangleF(track.Left, track.Top, x - track.Left, trackHeight);
            using var fillPath = Squircle.Capsule(fillRect);
            Fill(g, fillPath, Enabled ? Palette.Primary : CouiTheme.WithAlpha(Palette.Primary, 0x66));
        }

        float thumb = Dpf(ThumbDp) * (IsPressed || IsHovered ? 1.12f : 1f);
        RectangleF thumbRect = new RectangleF(x - thumb / 2f, (Height - thumb) / 2f, thumb, thumb);
        using (var halo = Squircle.Circle(new RectangleF(thumbRect.X - Dpf(1), thumbRect.Y - Dpf(1),
                                                         thumbRect.Width + Dpf(2), thumbRect.Height + Dpf(2))))
        {
            Fill(g, halo, Color.FromArgb(0x14, 0, 0, 0));
        }
        using var thumbPath = Squircle.Circle(thumbRect);
        Fill(g, thumbPath, Palette.Primary);

        if (Focused)
        {
            float ringSize = Math.Min(thumb + Dpf(6), Math.Min(Width - 2f, Height - 2f));
            RectangleF ring = new RectangleF(x - ringSize / 2f, (Height - ringSize) / 2f, ringSize, ringSize);
            DrawFocusRing(g, ring, ringSize / 2f);
        }
    }

    private void SetFromPoint(int px)
    {
        float inset = Dpf(ThumbDp / 2f);
        float usable = Math.Max(1f, Width - inset * 2);
        float f = Math.Clamp((px - inset) / usable, 0f, 1f);
        int raw = _minimum + (int)Math.Round(f * (_maximum - _minimum));
        if (_step > 1)
        {
            raw = _minimum + (int)(Math.Round((raw - _minimum) / (float)_step) * _step);
            raw = Math.Clamp(raw, _minimum, _maximum);
        }
        Value = raw;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (Enabled && e.Button == MouseButtons.Left)
        {
            _dragging = true;
            SetFromPoint(e.X);
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging) SetFromPoint(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _dragging = false;
        base.OnMouseUp(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _dragging = false;
        base.OnMouseLeave(e);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        if (key is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End
            or Keys.PageUp or Keys.PageDown) return true;
        return base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        int step = _step > 1 ? _step : 1;
        switch (e.KeyCode)
        {
            case Keys.Left:
            case Keys.Down:
                Value -= step;
                e.Handled = true;
                break;
            case Keys.Right:
            case Keys.Up:
                Value += step;
                e.Handled = true;
                break;
            case Keys.PageUp:
                Value += Math.Max(step, (_maximum - _minimum) / 10);
                e.Handled = true;
                break;
            case Keys.PageDown:
                Value -= Math.Max(step, (_maximum - _minimum) / 10);
                e.Handled = true;
                break;
            case Keys.Home:
                Value = _minimum;
                e.Handled = true;
                break;
            case Keys.End:
                Value = _maximum;
                e.Handled = true;
                break;
        }
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
}
