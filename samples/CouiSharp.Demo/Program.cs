using System;
using System.Drawing;
using System.Windows.Forms;
using CouiSharp.Controls;
using CouiSharp.Theme;

namespace CouiSharp.Demo;

internal static class Program
{
    internal static string LayoutDumpPath = string.Empty;

    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Register the bundled 霞鹜 fonts before any window exists: GDI text rendering silently
        // falls back to a system face when the family is not in the process font table.
        RegisterBundledFonts();

        bool dark = false;
        Color accent = Color.FromArgb(0xFF, 0x00, 0x80, 0xFF);
        CouiFontFace face = CouiFontFace.WenKai;

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
            if (arg.StartsWith("--face=", StringComparison.OrdinalIgnoreCase))
            {
                string name = arg.Substring("--face=".Length).Trim().ToLowerInvariant();
                if (name is "zhenkai" or "zk") face = CouiFontFace.ZhenKai;
            }
            if (arg.StartsWith("--dump-layout=", StringComparison.OrdinalIgnoreCase))
            {
                LayoutDumpPath = arg.Substring("--dump-layout=".Length).Trim();
            }
        }

        CouiTheme.Mode = dark ? CouiThemeMode.Dark : CouiThemeMode.Light;
        CouiTheme.SeedColor = accent;
        CouiTheme.FontFace = face;

        Application.Run(new DemoForm());
    }

    /// <summary>
    /// Loads fonts from a <c>fonts</c> folder next to the executable, falling back to a
    /// <c>fonts</c> folder beside the sources (developers run straight from the repo without
    /// copying 60 MB of typefaces into bin). Missing fonts are not fatal: the library falls back
    /// to Microsoft YaHei UI.
    /// </summary>
    private static void RegisterBundledFonts()
    {
        string local = System.IO.Path.Combine(AppContext.BaseDirectory, "fonts");
        int count = CouiFonts.Register(local);
        if (count > 0 || CouiFonts.IsAvailable(CouiFontFace.WenKai)) return;

        string besideSources = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "fonts"));
        CouiFonts.Register(besideSources);
    }
}
