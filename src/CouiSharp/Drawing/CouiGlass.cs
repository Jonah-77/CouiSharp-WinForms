using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CouiSharp.Controls;

namespace CouiSharp.Drawing;

/// <summary>
/// Backdrop sampling for glass surfaces (the "B" rendering plan).
/// <para>
/// Windows gives a WinForms app no reliable way to read the pixels already on screen:
/// <c>DrawToBitmap</c> comes back blank for a self-painted control, and <c>PrintWindow</c> does
/// not match what is rendered. So instead of capturing the desktop, CouiSharp re-renders its own
/// control tree into a bitmap and blurs it on the CPU. Glass therefore blurs the app's own page and
/// cards — it does not show a photo or wallpaper behind the window. That is the honest limit of
/// GDI+ glass without a compositor, and it is enough for toolbars, sheets and dialogs.
/// </para>
/// </summary>
public static class CouiGlass
{
    /// <summary>
    /// Renders the page behind <paramref name="area"/> (in <paramref name="root"/> client
    /// coordinates) into a new bitmap. Glass panels in the tree are skipped so a panel never
    /// samples itself.
    /// </summary>
    public static Bitmap Capture(Control root, Rectangle area)
    {
        if (area.Width <= 0 || area.Height <= 0) return new Bitmap(1, 1);

        var bitmap = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppPArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(CouiBackdrop.Resolve(root));
            PaintChildren(root, g, new Point(-area.X, -area.Y));
        }
        return bitmap;
    }

    /// <summary>Renders the page behind <paramref name="area"/> and returns it blurred.</summary>
    public static Bitmap CaptureBlurred(Control root, Rectangle area, float strength = 9f, int passes = 3)
    {
        Bitmap raw = Capture(root, area);
        try
        {
            return Blur(raw, strength, passes);
        }
        finally
        {
            raw.Dispose();
        }
    }

    /// <summary>
    /// Box blur done on a downscaled copy: shrink by <paramref name="strength"/>, blur the small
    /// image, scale back up with bilinear filtering. Three passes of a box blur approximate a
    /// Gaussian well enough for a frosted panel, and the whole thing costs a few milliseconds for
    /// a toolbar-sized region.
    /// </summary>
    public static Bitmap Blur(Bitmap source, float strength = 9f, int passes = 3)
    {
        int sw = Math.Max(1, source.Width);
        int sh = Math.Max(1, source.Height);
        int factor = (int)Math.Max(1f, strength);
        int dw = Math.Max(1, sw / factor);
        int dh = Math.Max(1, sh / factor);

        Bitmap small = new Bitmap(dw, dh, PixelFormat.Format32bppPArgb);
        using (Graphics g = Graphics.FromImage(small))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, new Rectangle(0, 0, dw, dh), new Rectangle(0, 0, sw, sh), GraphicsUnit.Pixel);
        }

        int[] pixels = ReadArgb(small);
        int radius = Math.Max(1, factor / 3);
        for (int i = 0; i < passes; i++)
        {
            BoxBlurPass(pixels, dw, dh, radius, horizontal: true);
            BoxBlurPass(pixels, dw, dh, radius, horizontal: false);
        }
        WriteArgb(small, pixels);

        var result = new Bitmap(sw, sh, PixelFormat.Format32bppPArgb);
        using (Graphics g = Graphics.FromImage(result))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(small, new Rectangle(0, 0, sw, sh), new Rectangle(0, 0, dw, dh), GraphicsUnit.Pixel);
        }
        small.Dispose();
        return result;
    }

    private static void PaintChildren(Control parent, Graphics g, Point offset)
    {
        // Controls[0] is the topmost child, so paint back to front.
        for (int i = parent.Controls.Count - 1; i >= 0; i--)
        {
            Control child = parent.Controls[i];
            if (!child.Visible) continue;
            // A glass panel must never appear in its own backdrop, or it would sample stale glass.
            if (child is CouiGlassPanel) continue;

            Point location = new Point(offset.X + child.Left, offset.Y + child.Top);
            if (child is CouiControl coui)
            {
                coui.PaintSnapshot(g, location);
            }
            else
            {
                Color back = child.BackColor;
                if (back.A > 0)
                {
                    using var brush = new SolidBrush(back);
                    g.FillRectangle(brush, new Rectangle(location, child.Size));
                }
            }
            PaintChildren(child, g, location);
        }
    }

    private static int[] ReadArgb(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        var pixels = new int[width * height];
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
        try
        {
            if (data.Stride == width * 4)
            {
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    IntPtr row = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(row, pixels, y * width, width);
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
        return pixels;
    }

    private static void WriteArgb(Bitmap bitmap, int[] pixels)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        try
        {
            if (data.Stride == width * 4)
            {
                Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    IntPtr row = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(pixels, y * width, row, width);
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static void BoxBlurPass(int[] pixels, int width, int height, int radius, bool horizontal)
    {
        int length = horizontal ? width : height;
        int lines = horizontal ? height : width;
        if (radius <= 0) return;
        var line = new int[length];
        var blurred = new int[length];
        for (int l = 0; l < lines; l++)
        {
            for (int i = 0; i < length; i++)
                line[i] = horizontal ? pixels[l * width + i] : pixels[i * width + l];

            BlurLine(line, blurred, radius);

            for (int i = 0; i < length; i++)
            {
                int v = blurred[i];
                if (horizontal) pixels[l * width + i] = v;
                else pixels[i * width + l] = v;
            }
        }
    }

    /// <summary>Sliding-window box blur that keeps alpha in the average, so edges stay clean.</summary>
    private static void BlurLine(int[] line, int[] target, int radius)
    {
        int n = line.Length;
        int window = radius * 2 + 1;
        int a = 0, r = 0, gg = 0, b = 0;
        // Seed the window at index 0, clamping the left edge so the ends are not darkened.
        for (int k = -radius; k <= radius; k++)
        {
            int p = line[Math.Clamp(k, 0, n - 1)];
            a += (p >> 24) & 0xFF; r += (p >> 16) & 0xFF; gg += (p >> 8) & 0xFF; b += p & 0xFF;
        }
        for (int i = 0; i < n; i++)
        {
            target[i] = ((a / window) << 24) | ((r / window) << 16) | ((gg / window) << 8) | (b / window);
            int outIdx = Math.Clamp(i - radius, 0, n - 1);
            int inIdx = Math.Clamp(i + radius + 1, 0, n - 1);
            int outP = line[outIdx];
            int inP = line[inIdx];
            a += ((inP >> 24) & 0xFF) - ((outP >> 24) & 0xFF);
            r += ((inP >> 16) & 0xFF) - ((outP >> 16) & 0xFF);
            gg += ((inP >> 8) & 0xFF) - ((outP >> 8) & 0xFF);
            b += (inP & 0xFF) - (outP & 0xFF);
        }
    }
}
