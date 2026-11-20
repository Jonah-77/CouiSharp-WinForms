using System;
using System.Drawing;
using System.Windows.Forms;
using CouiSharp.Controls;
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
    private bool _rebuilding;

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
        _page.SizeChanged += (_, _) => Rebuild();
    }

    private int Px(float dp) => (int)Math.Round(dp * DeviceDpi / 96f * CouiTheme.Scale);

    private void OnThemeChanged(object sender, EventArgs e)
    {
        BackColor = CouiTheme.Colors.BackgroundWithCard;
        _page.BackColor = CouiTheme.Colors.BackgroundWithCard;
        if (Math.Abs(_builtDensity - Density) > 0.01f) Rebuild();
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
        var label = new CouiLabel
        {
            Text = text,
            Font = CouiTypography.Body(),
            Bounds = new Rectangle(Px(16), Px(44) + Px(CouiShape.RowHeight) * row,
                                   width, Px(CouiShape.RowHeight))
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
                if (!chip.Checked) { chip.Checked = true; return; }
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
            var chip = new CouiChip
            {
                Text = chipNames[i],
                Font = CouiTypography.BodySmall(),
                Checked = i == 0
            };
            chip.Size = chip.GetPreferredSize(Size.Empty);
            chip.Location = new Point(chipX, chipY);
            chip.CheckedChanged += (_, _) =>
            {
                foreach (Control sibling in card.Controls)
                {
                    if (sibling is CouiChip other && other != chip && other.Text != chipNames[0]) other.Checked = false;
                }
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
        text.Click += (_, _) => _status.Text = "CouiSharp 0.1.0";
        card.Controls.Add(text);

        return y;
    }

    private int BuildAbout(int left, int y, int width)
    {
        int height = Px(44) + Px(28) * 3 + Px(12);
        var card = Card(left, ref y, width, height, "关于");

        var lines = new[]
        {
            "CouiSharp 0.1.0 · ColorOS 17 视觉令牌",
            "12dp 卡片 / 44dp 胶囊按钮 / 10dp 输入框",
            "与 MiuixSharp 相互独立的另一个库"
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
