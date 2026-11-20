using System.Drawing;
using System.Windows.Forms;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

/// <summary>
/// COUI linear progress bar: 4dp capsule track on <c>container8</c>, accent fill. Indeterminate
/// mode animates a 30% segment on a 1.6s loop.
/// </summary>
public class CouiProgressBar : CouiControl
{
    private readonly System.Windows.Forms.Timer _timer;
    private int _value;
    private int _maximum = 100;
    private bool _indeterminate;
    private float _phase;

    public CouiProgressBar()
    {
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        AccessibleRole = AccessibleRole.ProgressBar;
        Height = Dp(4);
        _timer = new System.Windows.Forms.Timer { Interval = 30 };
        _timer.Tick += (_, _) =>
        {
            _phase = (_phase + 0.02f) % 1f;
            Invalidate();
        };
    }

    public int Value
    {
        get => _value;
        set
        {
            int v = Math.Clamp(value, 0, _maximum);
            if (v == _value) return;
            _value = v;
            AccessibleDescription = $"{v} / {_maximum}";
            Invalidate();
        }
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(1, value);
            _value = Math.Min(_value, _maximum);
            Invalidate();
        }
    }

    /// <summary>Animates an endless segment; <see cref="Value"/> is ignored while true.</summary>
    public bool Indeterminate
    {
        get => _indeterminate;
        set
        {
            if (_indeterminate == value) return;
            _indeterminate = value;
            if (value) _timer.Start(); else _timer.Stop();
            Invalidate();
        }
    }

    public override Color SurfaceColor => Color.Transparent;

    protected override void OnPaintContent(Graphics g)
    {
        float height = Math.Min(Height, Dpf(4));
        RectangleF track = new RectangleF(0, (Height - height) / 2f, Width, height);
        using var trackPath = Squircle.Capsule(track);
        Fill(g, trackPath, Palette.Container12);

        if (_indeterminate)
        {
            float segment = track.Width * 0.3f;
            float x = track.Left - segment + (track.Width + segment) * _phase;
            RectangleF piece = RectangleF.Intersect(track, new RectangleF(x, track.Top, segment, height));
            if (piece.Width > 1f)
            {
                using var piecePath = Squircle.Capsule(piece);
                Fill(g, piecePath, Palette.Primary);
            }
            return;
        }

        float fraction = _maximum <= 0 ? 0f : (float)_value / _maximum;
        if (fraction <= 0) return;
        RectangleF fillRect = new RectangleF(track.Left, track.Top, Math.Max(height, track.Width * fraction), height);
        using var fillPath = Squircle.Capsule(fillRect);
        Fill(g, fillPath, Enabled ? Palette.Primary : CouiTheme.WithAlpha(Palette.Primary, 0x66));
    }

    protected override void ApplyMetrics()
    {
        if (Height > Dp(8)) Height = Dp(4);
        base.ApplyMetrics();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
