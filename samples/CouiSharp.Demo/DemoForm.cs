using System;
using System.Drawing;
using System.Windows.Forms;
using CouiSharp.Controls;
using CouiSharp.Drawing;
using CouiSharp.Theme;

namespace CouiSharp.Demo;

/// <summary>
/// Showcase page laid out like a ColorOS 17 settings screen: gray page, 12dp white cards, 48dp
/// rows, capsule actions. The whole page is rebuilt when density changes, so every dp stays true.
/// </summary>
public sealed class DemoForm : Form
{
    private readonly Panel _page;
    private readonly CouiLabel _status;
    private float _builtDensity;
    private int _pageWidth = -1;
    private bool _rebuilding;
    private bool _pendingLayout;

    public DemoForm()
    {
        Text = "CouiSharp 演示 · ColorOS 17 风格";
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = CouiTheme.Colors.BackgroundWithCard;
        Font = CouiTypography.Body();
        ClientSize = new Size(Px(468), Px(900));
        MinimumSize = new Size(Px(360), Px(480));

        _page = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = CouiTheme.Colors.BackgroundWithCard
        };
        Controls.Add(_page);

        _status = new CouiLabel
        {
            Text = "就绪",
            Tone = CouiTextTone.Secondary,
            Font = CouiTypography.Note(),
            Height = Px(24)
        };

        CouiTheme.Changed += OnThemeChanged;
        // Rebuild only on a width change: the page also raises SizeChanged when a scrollbar appears
        // or disappears, and rebuilding on that reset the scroll position after a font switch.
        _page.SizeChanged += (_, _) =>
        {
            int width = _page.ClientSize.Width;
            if (width == _pageWidth) return;
            _pageWidth = width;
            Rebuild();
        };
    }

    private int Px(float dp) => (int)Math.Round(dp * DeviceDpi / 96f * CouiTheme.Scale);

    private void OnThemeChanged(object sender, EventArgs e)
    {
        BackColor = CouiTheme.Colors.BackgroundWithCard;
        _page.BackColor = CouiTheme.Colors.BackgroundWithCard;
        if (Math.Abs(_builtDensity - Density) > 0.01f)
        {
            // Never rebuild inside a control's own event: the scale slider raises ValueChanged
            // while dragging, and disposing it there killed the drag (and its status label).
            _pendingLayout = true;
        }
        Invalidate(true);
    }

    private float Density => DeviceDpi / 96f * CouiTheme.Scale;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Rebuild();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        Rebuild();
    }

    private void ClearPage()
    {
        foreach (Control child in _page.Controls)
        {
            if (ReferenceEquals(child, _status)) continue;
            child.Dispose();
        }
        _page.Controls.Clear();
    }

    private void Rebuild()
    {
        if (_rebuilding || !IsHandleCreated) return;
        _rebuilding = true;
        _page.SuspendLayout();
        try
        {
            // "Display size" grows the whole window in dp, otherwise the fixed window would keep
            // the old pixel width while every control grew and the chip rows overflowed the cards.
            if (Math.Abs(_builtDensity - Density) > 0.01f && IsHandleCreated)
            {
                Rectangle area = Screen.FromControl(this).WorkingArea;
                int targetWidth = Math.Min(Px(468), (int)(area.Width * 0.92));
                int targetHeight = Math.Min(Px(900), (int)(area.Height * 0.92));
                ClientSize = new Size(targetWidth, targetHeight);
            }

            ClearPage();
            _builtDensity = Density;

            int margin = Px(16);
            int cardWidth = Math.Min(Px(428), Math.Max(Px(240), _page.ClientSize.Width - margin * 2));
            int left = Math.Max(margin, (_page.ClientSize.Width - cardWidth) / 2);
            int y = Px(10);

            // page title
            var title = new CouiLabel
            {
                Text = "设置",
                Font = CouiTypography.Title(),
                Tone = CouiTextTone.Primary,
                Bounds = new Rectangle(left + Px(4), y, cardWidth, Px(40))
            };
            _page.Controls.Add(title);
            y += Px(44);

            y = BuildAppearance(left, y, cardWidth);
            y = BuildNotifications(left, y, cardWidth);
            y = BuildComponents(left, y, cardWidth);
            y = BuildIcons(left, y, cardWidth);
            y = BuildAbout(left, y, cardWidth);

            _status.Bounds = new Rectangle(left + Px(4), y + Px(4), cardWidth, Px(24));
            if (!_page.Controls.Contains(_status)) _page.Controls.Add(_status);
            y += Px(36);
            _page.AutoScrollMinSize = new Size(0, y + Px(24));
        }
        finally
        {
            _page.ResumeLayout(true);
            _rebuilding = false;
            _pendingLayout = false;
        }

        DumpLayout();
    }

    /// <summary>Writes every control's screen rectangle, for scripted click checks.</summary>
    private void DumpLayout()
    {
        string path = Program.LayoutDumpPath;
        if (string.IsNullOrEmpty(path)) return;
        var builder = new System.Text.StringBuilder();
        foreach (Control control in Enumerate(_page))
        {
            Point origin = control.PointToScreen(Point.Empty);
            string text = (control.Text ?? string.Empty).Replace("|", "/");
            builder.AppendLine($"{control.GetType().Name}|{text}|{origin.X}|{origin.Y}|{control.Width}|{control.Height}");
        }
        try
        {
            System.IO.File.WriteAllText(path, builder.ToString(), new System.Text.UTF8Encoding(false));
        }
        catch
        {
            // Layout dumping is a test aid; never let it break the demo.
        }
    }

    private static System.Collections.Generic.IEnumerable<Control> Enumerate(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control nested in Enumerate(child)) yield return nested;
        }
    }

    private CouiCard Card(int left, ref int y, int width, int height, string heading)
    {
        var card = new CouiCard
        {
            Bounds = new Rectangle(left, y, width, height)
        };
        var label = new CouiLabel
        {
            Text = heading,
            Font = CouiTypography.Header(),
            Tone = CouiTextTone.Primary,
            Bounds = new Rectangle(Px(16), Px(12), width - Px(32), Px(28))
        };
        card.Controls.Add(label);
        _page.Controls.Add(card);
        y += height + Px(12);
        return card;
    }

    private CouiLabel RowText(CouiCard card, string text, int row, int width)
    {
        // Never let a label grow past its own text. A label added before a chip or a slider sits
        // above it in z-order, so an over-wide label paints card colour over the neighbour and
        // clips its left cap, which reads as a chopped-off edge.
        Font font = CouiTypography.Body();
        int measured = TextRenderer.MeasureText(text, font).Width + Px(8);
        var label = new CouiLabel
        {
            Text = text,
            Font = font,
            Bounds = new Rectangle(Px(16), Px(44) + Px(CouiShape.RowHeight) * row,
                                   Math.Min(width, measured), Px(CouiShape.RowHeight))
        };
        card.Controls.Add(label);
        return label;
    }

    private CouiSwitch RowSwitch(CouiCard card, int row, bool value)
    {
        var toggle = new CouiSwitch
        {
            Checked = value,
            Bounds = new Rectangle(
                card.Width - Px(16) - Px(CouiShape.SwitchWidth),
                Px(44) + Px(CouiShape.RowHeight) * row + (Px(CouiShape.RowHeight) - Px(CouiShape.SwitchHeight)) / 2,
                Px(CouiShape.SwitchWidth),
                Px(CouiShape.SwitchHeight))
        };
        card.Controls.Add(toggle);
        return toggle;
    }

    private bool _syncChips;

    private void SelectOnly(CouiCard card, CouiChip selected)
    {
        _syncChips = true;
        foreach (Control child in card.Controls)
        {
            if (child is CouiChip other && other != selected && other.Checked) other.Checked = false;
        }
        _syncChips = false;
    }

    private int BuildAppearance(int left, int y, int width)
    {
        int height = Px(44) + Px(CouiShape.RowHeight) * 3 + Px(8);
        var card = Card(left, ref y, width, height, "外观");

        RowText(card, "深色模式", 0, width - Px(100));
        var darkSwitch = RowSwitch(card, 0, CouiTheme.IsDark);
        darkSwitch.CheckedChanged += (_, _) =>
        {
            CouiTheme.Mode = darkSwitch.Checked ? CouiThemeMode.Dark : CouiThemeMode.Light;
            _status.Text = darkSwitch.Checked ? "已切换到深色" : "已切换到浅色";
        };

        RowText(card, "主题色", 1, Px(100));
        (Color color, string name)[] accents =
        {
            (Color.FromArgb(0xFF, 0x00, 0x80, 0xFF), "蓝"),
            (Color.FromArgb(0xFF, 0x00, 0xBD, 0x13), "绿"),
            (Color.FromArgb(0xFF, 0x8B, 0x5C, 0xF6), "紫"),
            (Color.FromArgb(0xFF, 0xFF, 0x77, 0x00), "橙")
        };

        int chipX = Px(110);
        int chipY = Px(44) + Px(CouiShape.RowHeight) + (Px(CouiShape.RowHeight) - Px(CouiShape.ChipHeight)) / 2;
        foreach ((Color color, string name) in accents)
        {
            var chip = new CouiChip
            {
                Text = name,
                Font = CouiTypography.BodySmall(),
                Checked = color.ToArgb() == CouiTheme.SeedColor.ToArgb()
            };
            chip.Size = chip.GetPreferredSize(Size.Empty);
            chip.Location = new Point(chipX, chipY);
            chip.CheckedChanged += (_, _) =>
            {
                if (_syncChips) return;
                if (!chip.Checked) { chip.Checked = true; return; }
                SelectOnly(card, chip);
                CouiTheme.SeedColor = color;
                _status.Text = $"主题色：{name}";
            };
            card.Controls.Add(chip);
            chipX += chip.Width + Px(8);
        }

        RowText(card, "界面大小", 2, Px(100));
        var scale = new CouiSlider
        {
            Minimum = 75,
            Maximum = 175,
            Value = (int)Math.Round(CouiTheme.Scale * 100),
            Bounds = new Rectangle(Px(110), Px(44) + Px(CouiShape.RowHeight) * 2,
                                   width - Px(126), Px(48))
        };
        scale.ValueChanged += (_, _) =>
        {
            if (Math.Abs(CouiTheme.Scale - scale.Value / 100f) < 0.005f) return;
            CouiTheme.Scale = scale.Value / 100f;
            _status.Text = $"界面大小：{scale.Value}%";
        };
        // Apply the pending relayout only after the drag ends, so the slider is not disposed
        // underneath its own mouse capture.
        scale.MouseUp += (_, _) => { if (_pendingLayout) Rebuild(); };
        card.Controls.Add(scale);
        return y;
    }

    private int BuildNotifications(int left, int y, int width)
    {
        int height = Px(44) + Px(CouiShape.RowHeight) * 3 + Px(8);
        var card = Card(left, ref y, width, height, "通知与声音");

        RowText(card, "允许通知", 0, width - Px(100));
        RowSwitch(card, 0, true);

        RowText(card, "响铃", 1, width - Px(100));
        RowSwitch(card, 1, true);

        RowText(card, "锁屏显示详情", 2, width - Px(100));
        RowSwitch(card, 2, false);
        return y;
    }

    private int BuildComponents(int left, int y, int width)
    {
        int height = Px(44) + Px(CouiShape.RowHeight) * 7 + Px(16);
        var card = Card(left, ref y, width, height, "组件");

        var check = new CouiCheckBox
        {
            Text = "记住我的选择",
            Font = CouiTypography.Body(),
            Checked = true,
            Location = new Point(Px(16), Px(48))
        };
        check.Size = check.GetPreferredSize(Size.Empty);
        card.Controls.Add(check);

        int row2 = Px(44) + Px(CouiShape.RowHeight);
        var radioA = new CouiRadioButton
        {
            Text = "标准",
            GroupName = "mode",
            Font = CouiTypography.Body(),
            Checked = true,
            Location = new Point(Px(16), row2 + Px(10))
        };
        radioA.Size = radioA.GetPreferredSize(Size.Empty);
        card.Controls.Add(radioA);

        var radioB = new CouiRadioButton
        {
            Text = "省电",
            GroupName = "mode",
            Font = CouiTypography.Body(),
            Location = new Point(Px(120), row2 + Px(10))
        };
        radioB.Size = radioB.GetPreferredSize(Size.Empty);
        card.Controls.Add(radioB);

        int chipRow = 2;
        var chipY = Px(44) + Px(CouiShape.RowHeight) * chipRow
                    + (Px(CouiShape.RowHeight) - Px(CouiShape.ChipHeight)) / 2;
        string[] chipNames = { "全部", "图片", "视频" };
        int chipX = Px(16);
        for (int i = 0; i < chipNames.Length; i++)
        {
            // Capture the name, not the loop counter: the handler runs after the loop has ended,
            // so referencing i there reads chipNames[chipNames.Length] and throws.
            string chipName = chipNames[i];
            var chip = new CouiChip
            {
                Text = chipName,
                Font = CouiTypography.BodySmall(),
                Checked = i == 0
            };
            chip.Size = chip.GetPreferredSize(Size.Empty);
            chip.Location = new Point(chipX, chipY);
            chip.CheckedChanged += (_, _) =>
            {
                if (_syncChips) return;
                if (!chip.Checked) { chip.Checked = true; return; }
                SelectOnly(card, chip);
                _status.Text = "筛选：" + chipName;
            };
            card.Controls.Add(chip);
            chipX += chip.Width + Px(8);
        }

        var field = new CouiTextBox
        {
            PlaceholderText = "输入设备名称",
            Bounds = new Rectangle(Px(16), Px(44) + Px(CouiShape.RowHeight) * 3 + Px(2),
                                   width - Px(32), Px(CouiShape.InputHeight))
        };
        field.TextSubmitted += (_, _) => _status.Text = "已保存：" + field.Text;
        card.Controls.Add(field);

        var volumeLabel = RowText(card, "音量 60%", 4, Px(110));
        var volume = new CouiSlider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 60,
            Bounds = new Rectangle(Px(110), Px(44) + Px(CouiShape.RowHeight) * 4,
                                   width - Px(126), Px(48))
        };
        volume.ValueChanged += (_, _) => volumeLabel.Text = "音量 " + volume.Value + "%";
        card.Controls.Add(volume);

        var progress = new CouiProgressBar
        {
            Value = 45,
            Bounds = new Rectangle(Px(16), Px(44) + Px(CouiShape.RowHeight) * 5 + Px(18),
                                   width - Px(32), Px(6))
        };
        card.Controls.Add(progress);

        int buttonY = Px(44) + Px(CouiShape.RowHeight) * 6 + Px(4);
        var primary = new CouiButton
        {
            Text = "保存",
            Variant = CouiButtonVariant.Primary,
            Bounds = new Rectangle(Px(16), buttonY, Px(104), Px(CouiShape.ButtonHeight))
        };
        primary.Click += (_, _) => _status.Text = "已保存设置";
        card.Controls.Add(primary);

        var tonal = new CouiButton
        {
            Text = "重置",
            Variant = CouiButtonVariant.Tonal,
            Bounds = new Rectangle(Px(132), buttonY, Px(104), Px(CouiShape.ButtonHeight))
        };
        tonal.Click += (_, _) => _status.Text = "已重置";
        card.Controls.Add(tonal);

        var text = new CouiButton
        {
            Text = "了解更多",
            Variant = CouiButtonVariant.Text,
            Bounds = new Rectangle(Px(236), buttonY, Px(148), Px(CouiShape.ButtonHeight))
        };
        text.Click += (_, _) => _status.Text = "CouiSharp 0.2.2";
        card.Controls.Add(text);

        return y;
    }

    private int BuildIcons(int left, int y, int width)
    {
        const int columns = 5;
        int gridTop = Px(44);
        int cell = (width - Px(32)) / columns;
        int iconSize = Px(28);
        int rowHeight = iconSize + Px(14);
        int rows = (CouiIconPaths.All.Length + columns - 1) / columns;
        int height = gridTop + rowHeight * rows + Px(6);
        var card = Card(left, ref y, width, height, "图标（自绘）");

        for (int i = 0; i < CouiIconPaths.All.Length; i++)
        {
            int row = i / columns;
            int column = i % columns;
            var icon = new CouiIcon
            {
                Kind = CouiIconPaths.All[i],
                IconSize = 28f,
                Size = new Size(iconSize, iconSize),
                Location = new Point(
                    Px(16) + cell * column + (cell - iconSize) / 2,
                    gridTop + rowHeight * row + (rowHeight - iconSize) / 2)
            };
            card.Controls.Add(icon);
        }
        return y;
    }

    private int BuildAbout(int left, int y, int width)
    {
        int height = Px(44) + Px(28) * 3 + Px(12);
        var card = Card(left, ref y, width, height, "关于");

        var lines = new[]
        {
            "CouiSharp 0.2.2 · ColorOS 17 视觉令牌",
            "20 个自绘图标 · 纯 GDI+ 自绘",
            "与 MiuixSharp 相互独立"
        };

        for (int i = 0; i < lines.Length; i++)
        {
            var label = new CouiLabel
            {
                Text = lines[i],
                Font = CouiTypography.Caption(),
                Tone = i == 0 ? CouiTextTone.Primary : CouiTextTone.Secondary,
                Bounds = new Rectangle(Px(16), Px(44) + Px(28) * i, width - Px(32), Px(26))
            };
            card.Controls.Add(label);
        }
        return y;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) CouiTheme.Changed -= OnThemeChanged;
        base.Dispose(disposing);
    }
}














