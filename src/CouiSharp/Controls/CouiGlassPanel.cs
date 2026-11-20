using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

/// <summary>
/// A frosted panel: the page behind it is re-rendered, blurred on the CPU and clipped to a rounded
/// shape. It is the carrier for floating toolbars, sheets and dialogs under the "B" rendering plan —
/// see <see cref="CouiGlass"/> for why the backdrop comes from the app's own paint tree rather than
/// from the desktop.
/// </summary>
public class CouiGlassPanel : CouiControl
{
    private Bitmap _sample;
    private bool _dirty = true;
    private float _radius = 22f;
    private float _blur = 9f;
    private float _tintStrength = 0.16f;

    public CouiGlassPanel()
    {
        SetStyle(ControlStyles.ContainerControl, true);
        Size = new Size(Dp(320), Dp(64));
    }

    /// <summary>Corner radius in dp.</summary>
    [Category("CouiSharp")]
    [Description("Corner radius in dp.")]
    public float Radius
    {
        get => _radius;
        set
        {
            if (Math.Abs(_radius - value) < 0.001f) return;
            _radius = Math.Max(0f, value);
            Invalidate();
        }
    }

    /// <summary>Blur strength: the backdrop is downscaled by this factor before blurring.</summary>
    [Category("CouiSharp")]
    [Description("Blur downscale factor; larger is blurrier.")]
    public float BlurStrength
    {
        get => _blur;
        set
        {
            if (Math.Abs(_blur - value) < 0.001f) return;
            _blur = Math.Max(1f, value);
            Refresh();
        }
    }

    /// <summary>How strongly the frosted layer is flooded with the card colour (0 = clear).</summary>
    [Category("CouiSharp")]
    [Description("White/black wash over the blurred layer, 0 to 1.")]
    public float TintStrength
    {
        get => _tintStrength;
        set
        {
            float v = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_tintStrength - v) < 0.001f) return;
            _tintStrength = v;
            Invalidate();
        }
    }

    /// <summary>Discards the cached backdrop and repaints; the next paint samples the page again.</summary>
    public override void Refresh()
    {
        _dirty = true;
        base.Refresh();
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        Refresh();
    }

    protected override void OnThemeChangedCore()
    {
        base.OnThemeChangedCore();
        Refresh();
    }

    protected override void ApplyMetrics()
    {
        base.ApplyMetrics();
        Refresh();
    }

    protected override void OnPaintContent(Graphics g)
    {
        RectangleF rect = new RectangleF(0f, 0f, Width, Height);
        using var path = Squircle.RoundedRect(rect, Dpf(_radius));

        EnsureSample();

        GraphicsState state = g.Save();
        try
        {
            g.SetClip(path, CombineMode.Replace);
            if (_sample is not null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(_sample, 0f, 0f, Width, Height);
            }
            else
            {
                Fill(g, path, Palette.Card);
            }
        }
        finally
        {
            g.Restore(state);
        }

        // A wash lifts the blurred content toward the card colour so text above it stays legible;
        // ColorOS glass is frosted white in light and frosted charcoal in dark. Dark needs a much
        // stronger wash: blurring near-black page pixels gives near-black, so the panel would read as
        // a hole in the page instead of a sheet floating over it.
        Color wash = Palette.Card;
        float alpha = _tintStrength;
        if (CouiTheme.IsDark)
        {
            wash = CouiTheme.Mix(Palette.Card, Color.White, 0.22f);
            alpha = Math.Min(1f, _tintStrength * 3.2f);
        }
        if (alpha > 0f)
        {
            using var brush = new SolidBrush(CouiTheme.WithAlpha(wash, (int)Math.Round(255f * alpha)));
            g.FillPath(brush, path);
        }

        // Hairline: a light rim on the top edge only, the way a glass sheet catches the light.
        StrokeInset(g, rect, Dpf(_radius), Palette.Divider);
    }

    private void EnsureSample()
    {
        if (!_dirty && _sample is not null) return;
        _dirty = false;

        Control root = TopLevelControl;
        if (root is null || Width <= 0 || Height <= 0)
        {
            _sample?.Dispose();
            _sample = null;
            return;
        }

        Rectangle screen = RectangleToScreen(ClientRectangle);
        Rectangle area = new Rectangle(root.PointToClient(screen.Location), screen.Size);
        area = Rectangle.Intersect(area, root.ClientRectangle);
        if (area.Width <= 0 || area.Height <= 0) return;

        Bitmap blurred = CouiGlass.CaptureBlurred(root, area, _blur);
        _sample?.Dispose();
        _sample = blurred;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sample?.Dispose();
            _sample = null;
        }
        base.Dispose(disposing);
    }
}
