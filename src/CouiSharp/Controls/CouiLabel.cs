using System.Drawing;
using System.Windows.Forms;
using CouiSharp.Theme;

namespace CouiSharp.Controls;

public enum CouiTextTone
{
    Primary,
    Secondary,
    Tertiary,
    Accent,
    Danger
}

/// <summary>
/// Text primitive. <see cref="Control"/> labels use the system background pipeline, which leaves
/// stale pixels on a custom-painted card; this one paints the resolved backdrop itself.
/// </summary>
public class CouiLabel : CouiControl
{
    private CouiTextTone _tone = CouiTextTone.Primary;
    private ContentAlignment _align = ContentAlignment.MiddleLeft;
    private bool _wrap;

    public CouiLabel()
    {
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        AutoSize = false;
        Size = new Size(200, Font.Height + Dp(4));
    }

    public override Color SurfaceColor => Color.Transparent;

    public CouiTextTone Tone
    {
        get => _tone;
        set { _tone = value; Invalidate(); }
    }

    public ContentAlignment TextAlign
    {
        get => _align;
        set { _align = value; Invalidate(); }
    }

    /// <summary>Allows multi-line text; measured height follows the wrapped content.</summary>
    public bool Wrap
    {
        get => _wrap;
        set { _wrap = value; ApplyMetrics(); Invalidate(); }
    }

    private Color ToneColor => _tone switch
    {
        CouiTextTone.Secondary => Palette.LabelSecondary,
        CouiTextTone.Tertiary => Palette.LabelTertiary,
        CouiTextTone.Accent => Palette.PrimaryText,
        CouiTextTone.Danger => Palette.Error,
        _ => Palette.LabelPrimary
    };

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        ApplyMetrics();
        Invalidate();
    }

    protected override void OnPaintContent(Graphics g)
    {
        TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
        if (_wrap)
        {
            flags |= TextFormatFlags.WordBreak;
        }
        else
        {
            flags |= TextFormatFlags.SingleLine;
        }

        flags |= _align switch
        {
            ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => TextFormatFlags.Left,
            ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter,
            _ => TextFormatFlags.Right
        };
        flags |= _align switch
        {
            ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => TextFormatFlags.Top,
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => TextFormatFlags.Bottom,
            _ => TextFormatFlags.VerticalCenter
        };

        TextRenderer.DrawText(g, Text ?? string.Empty, Font, ClientRectangle,
            Enabled ? ToneColor : Palette.Disabled, flags);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        int width = proposedSize.Width > 0 && proposedSize.Width < int.MaxValue ? proposedSize.Width : Width;
        width = Math.Max(width, Dp(40));
        Size measured = TextRenderer.MeasureText(Text ?? string.Empty, Font, new Size(width, int.MaxValue),
            _wrap ? TextFormatFlags.WordBreak | TextFormatFlags.NoPadding : TextFormatFlags.NoPadding);
        return new Size(width, Math.Max(measured.Height, Font.Height));
    }

    protected override void ApplyMetrics()
    {
        if (AutoSize)
        {
            Size = GetPreferredSize(new Size(Width, int.MaxValue));
            return;
        }
        if (_wrap)
        {
            Height = GetPreferredSize(new Size(Width, int.MaxValue)).Height;
        }
        base.ApplyMetrics();
    }
}
