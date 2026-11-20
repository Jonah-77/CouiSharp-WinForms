using System;
using System.Drawing;
using System.Windows.Forms;
using CouiSharp.Controls;
using CouiSharp.Theme;

namespace CouiSharp.Demo;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        bool dark = false;
        Color accent = Color.FromArgb(0xFF, 0x00, 0x80, 0xFF);

        foreach (string arg in args)
        {
            if (arg.Equals("--dark", StringComparison.OrdinalIgnoreCase)) dark = true;
            if (arg.Equals("--light", StringComparison.OrdinalIgnoreCase)) dark = false;
            if (arg.StartsWith("--accent=", StringComparison.OrdinalIgnoreCase))
            {
                string name = arg.Substring("--accent=".Length).Trim();
                accent = name.ToLowerInvariant() switch
                {
                    "green" => Color.FromArgb(0xFF, 0x00, 0xBD, 0x13),
                    "purple" => Color.FromArgb(0xFF, 0x8B, 0x5C, 0xF6),
                    "orange" => Color.FromArgb(0xFF, 0xFF, 0x77, 0x00),
                    "teal" => Color.FromArgb(0xFF, 0x00, 0xB3, 0xA8),
                    _ => accent
                };
            }
        }

        CouiTheme.Mode = dark ? CouiThemeMode.Dark : CouiThemeMode.Light;
        CouiTheme.SeedColor = accent;

        Application.Run(new DemoForm());
    }
}
