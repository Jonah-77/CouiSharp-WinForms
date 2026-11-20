using System.Drawing;
using System.Drawing.Drawing2D;

namespace CouiSharp.Drawing;

/// <summary>The icon set that ships with CouiSharp, drawn by hand instead of traced from a system pack.</summary>
public enum CouiIconKind
{
    Search,
    Close,
    Add,
    Minus,
    More,
    ChevronLeft,
    ChevronRight,
    ChevronUp,
    ChevronDown,
    ArrowBack,
    Check,
    Delete,
    Edit,
    Settings,
    Info,
    Warning,
    Refresh,
    Download,
    Copy,
    Share
}

/// <summary>
/// Outlines for the built-in icons. Every icon lives on a 24 x 24 canvas with a 3-unit safety margin
/// and a 1.5-unit stroke with round caps, matching the "line" style COUI uses. Paths are built once
/// at canvas scale and reused under a transform, so an icon stays crisp at any size or DPI, takes
/// the theme colour, and never needs a bitmap.
/// </summary>
public static class CouiIconPaths
{
    /// <summary>Canvas size in design units.</summary>
    public const float ViewBox = 24f;

    /// <summary>Stroke width in design units.</summary>
    public const float Stroke = 1.5f;

    /// <summary>Margin kept clear on every side, in design units.</summary>
    public const float Margin = 3f;

    private sealed class Shape
    {
        public GraphicsPath Stroke = new GraphicsPath();
        public GraphicsPath Fill = new GraphicsPath();
    }

    private static readonly Dictionary<CouiIconKind, Shape> Cache = new Dictionary<CouiIconKind, Shape>();
    private static readonly object Gate = new object();

    /// <summary>Returns the shared outline pair for <paramref name="kind"/>; do not dispose.</summary>
    public static (GraphicsPath Stroke, GraphicsPath Fill) Get(CouiIconKind kind)
    {
        lock (Gate)
        {
            if (!Cache.TryGetValue(kind, out Shape shape))
            {
                shape = Build(kind);
                Cache[kind] = shape;
            }
            return (shape.Stroke, shape.Fill);
        }
    }

    /// <summary>All kinds, in the order the gallery shows them.</summary>
    public static readonly CouiIconKind[] All =
    {
        CouiIconKind.Search, CouiIconKind.Close, CouiIconKind.Add, CouiIconKind.Minus, CouiIconKind.More,
        CouiIconKind.ChevronLeft, CouiIconKind.ChevronRight, CouiIconKind.ChevronUp, CouiIconKind.ChevronDown,
        CouiIconKind.ArrowBack, CouiIconKind.Check, CouiIconKind.Delete, CouiIconKind.Edit,
        CouiIconKind.Settings, CouiIconKind.Info, CouiIconKind.Warning, CouiIconKind.Refresh,
        CouiIconKind.Download, CouiIconKind.Copy, CouiIconKind.Share
    };

    private static Shape Build(CouiIconKind kind)
    {
        var s = new Shape();
        GraphicsPath st = s.Stroke;
        GraphicsPath fl = s.Fill;

        switch (kind)
        {
            case CouiIconKind.Search:
                st.AddEllipse(5.2f, 5.2f, 10.4f, 10.4f);
                Line(st, 15.1f, 15.1f, 19.2f, 19.2f);
                break;

            case CouiIconKind.Close:
                Line(st, 6.4f, 6.4f, 17.6f, 17.6f);
                Line(st, 17.6f, 6.4f, 6.4f, 17.6f);
                break;

            case CouiIconKind.Add:
                Line(st, 12f, 5f, 12f, 19f);
                Line(st, 5f, 12f, 19f, 12f);
                break;

            case CouiIconKind.Minus:
                Line(st, 5f, 12f, 19f, 12f);
                break;

            case CouiIconKind.More:
                Dot(fl, 6.2f, 12f, 1.35f);
                Dot(fl, 12f, 12f, 1.35f);
                Dot(fl, 17.8f, 12f, 1.35f);
                break;

            case CouiIconKind.ChevronLeft:
                Poly(st, false, 15f, 5f, 8.2f, 12f, 15f, 19f);
                break;

            case CouiIconKind.ChevronRight:
                Poly(st, false, 9f, 5f, 15.8f, 12f, 9f, 19f);
                break;

            case CouiIconKind.ChevronUp:
                Poly(st, false, 5f, 15f, 12f, 8.2f, 19f, 15f);
                break;

            case CouiIconKind.ChevronDown:
                Poly(st, false, 5f, 9f, 12f, 15.8f, 19f, 9f);
                break;

            case CouiIconKind.ArrowBack:
                Line(st, 19f, 12f, 6.2f, 12f);
                Poly(st, false, 11.8f, 6.2f, 6.2f, 12f, 11.8f, 17.8f);
                break;

            case CouiIconKind.Check:
                Poly(st, false, 5f, 12.6f, 9.8f, 17.4f, 19f, 6.6f);
                break;

            case CouiIconKind.Delete:
                // Lid, handle, tapered bin, one slot line.
                Line(st, 6.6f, 7.2f, 17.4f, 7.2f);
                Poly(st, false, 9.6f, 7.2f, 9.6f, 4.6f, 14.4f, 4.6f, 14.4f, 7.2f);
                Poly(st, false, 8.2f, 7.2f, 9.1f, 19.2f, 14.9f, 19.2f, 15.8f, 7.2f);
                Line(st, 12f, 10.1f, 12f, 16.3f);
                break;

            case CouiIconKind.Edit:
                // Pencil: body quad, tip split, eraser nib.
                Poly(st, true, 5.6f, 18.4f, 7.1f, 13.6f, 13.9f, 6.8f, 17.2f, 10.1f, 10.4f, 16.9f);
                Line(st, 7.1f, 13.6f, 10.4f, 16.9f);
                break;

            case CouiIconKind.Settings:
                // Eight spokes around two concentric rings: reads as a gear at any size.
                for (int i = 0; i < 8; i++)
                {
                    float a = (float)(i * Math.PI / 4);
                    float cx = 12f, cy = 12f;
                    Line(st,
                         cx + 8.3f * (float)Math.Cos(a), cy + 8.3f * (float)Math.Sin(a),
                         cx + 10.1f * (float)Math.Cos(a), cy + 10.1f * (float)Math.Sin(a));
                }
                st.AddEllipse(12f - 7.4f, 12f - 7.4f, 14.8f, 14.8f);
                st.AddEllipse(12f - 3.3f, 12f - 3.3f, 6.6f, 6.6f);
                break;

            case CouiIconKind.Info:
                st.AddEllipse(3.6f, 3.6f, 16.8f, 16.8f);
                Line(st, 12f, 11f, 12f, 16.6f);
                Dot(fl, 12f, 7.9f, 1.15f);
                break;

            case CouiIconKind.Warning:
                Poly(st, true, 12f, 4.4f, 20.2f, 19f, 3.8f, 19f);
                Line(st, 12f, 9.8f, 12f, 14.2f);
                Dot(fl, 12f, 16.5f, 1.1f);
                break;

            case CouiIconKind.Refresh:
                // Open ring with an arrowhead on the clockwise end.
                st.AddArc(4.8f, 4.8f, 14.4f, 14.4f, -46f, 292f);
                Poly(st, false, 19.4f, 5.6f, 16.3f, 11.6f, 14.2f, 6.2f);
                break;

            case CouiIconKind.Download:
                Line(st, 12f, 4.6f, 12f, 14.9f);
                Poly(st, false, 7.4f, 10.6f, 12f, 15.4f, 16.6f, 10.6f);
                Line(st, 5.4f, 18.2f, 18.6f, 18.2f);
                break;

            case CouiIconKind.Copy:
                Poly(st, true, 4.6f, 4.6f, 13.2f, 4.6f, 13.2f, 13.2f, 4.6f, 13.2f);
                Poly(st, true, 10.8f, 10.8f, 19.4f, 10.8f, 19.4f, 19.4f, 10.8f, 19.4f);
                break;

            case CouiIconKind.Share:
                Dot(fl, 18.2f, 5.6f, 2.1f);
                Dot(fl, 5.8f, 12f, 2.1f);
                Dot(fl, 18.2f, 18.4f, 2.1f);
                Line(st, 16.2f, 6.6f, 7.8f, 10.9f);
                Line(st, 7.8f, 13.1f, 16.2f, 17.4f);
                break;
        }

        return s;
    }

    private static void Line(GraphicsPath path, float x1, float y1, float x2, float y2)
    {
        // Each stroke must be its own figure. Consecutive AddLine calls continue the current
        // figure, which joined an "X" into a hexagon, a "+" into a triangle and turned every gear
        // spoke into a fan of connecting lines.
        path.StartFigure();
        path.AddLine(x1, y1, x2, y2);
    }

    private static void Poly(GraphicsPath path, bool closed, params float[] points)
    {
        var pts = new PointF[points.Length / 2];
        for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(points[i * 2], points[i * 2 + 1]);
        path.StartFigure();
        // AddPolygon always closes the figure, which would draw an unwanted return stroke on open
        // polyline icons such as ChevronLeft; AddLines leaves the figure open.
        if (closed) path.AddPolygon(pts);
        else path.AddLines(pts);
    }

    private static void Dot(GraphicsPath path, float cx, float cy, float r)
        => path.AddEllipse(cx - r, cy - r, r * 2, r * 2);
}
