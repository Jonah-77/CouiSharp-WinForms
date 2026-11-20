using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CouiSharp.Theme;

/// <summary>The two typefaces a CouiSharp app may run in.</summary>
public enum CouiFontFace
{
    /// <summary>LXGW WenKai — the default, a text-weight kaiti with a real Medium cut.</summary>
    WenKai,

    /// <summary>LXGW ZhenKai — a heavier derived kaiti, for hosts that want bolder text.</summary>
    ZhenKai
}

/// <summary>
/// Registers the bundled 霞鹜 family with GDI and resolves it by family name.
/// <para>
/// Why not <c>PrivateFontCollection</c>: <see cref="TextRenderer"/> (GDI) does not see fonts owned
/// by a private collection and silently falls back to a system face. Adding the file through
/// <c>AddFontResourceEx</c> puts it in the process font table, where both GDI text rendering and
/// <c>Graphics.DrawString</c> find it and where the family cannot leak to other processes.
/// </para>
/// </summary>
public static class CouiFonts
{
    /// <summary>Family used when a 霞鹜 face is missing, so the UI never renders in a random fallback.</summary>
    public const string FallbackFamily = "Microsoft YaHei UI";

    private const uint FR_PRIVATE = 0x10;
    private const uint FR_NOT_ENUM = 0x20;

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int AddFontResourceEx(string file, uint flags, IntPtr res);

    private static readonly object Gate = new object();
    private static readonly HashSet<string> LoadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, bool> FamilyCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Family names per face. CJK releases ship each weight as its own family (WenKai Regular is
    /// "霞鹜文楷", WenKai Medium is "霞鹜文楷 Medium"), so the bold slot is a family, not a style.
    /// English aliases are listed too: some builds register only the Latin name.
    /// </summary>
    private static readonly Dictionary<CouiFontFace, (string[] Body, string[] Strong, FontStyle StrongStyle)> Faces =
        new Dictionary<CouiFontFace, (string[], string[], FontStyle)>
        {
            [CouiFontFace.WenKai] = (
                new[] { "霞鹜文楷", "LXGW WenKai" },
                new[] { "霞鹜文楷 Medium", "LXGW WenKai Medium" },
                FontStyle.Regular),
            [CouiFontFace.ZhenKai] = (
                new[] { "霞鹜臻楷 GB", "LXGW ZhenKai GB", "霞鹜臻楷", "LXGW ZhenKai" },
                new[] { "霞鹜臻楷 GB", "LXGW ZhenKai GB", "霞鹜臻楷", "LXGW ZhenKai" },
                FontStyle.Bold),
        };

    /// <summary>
    /// Registers every .ttf / .otf in <paramref name="directory"/> for this process only.
    /// Safe to call repeatedly; returns the number of newly registered files.
    /// </summary>
    public static int Register(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return 0;
        int count = 0;
        string[] files;
        try
        {
            files = Directory.GetFiles(directory, "*.ttf");
            files = files.Concat(Directory.GetFiles(directory, "*.otf")).ToArray();
        }
        catch (IOException)
        {
            return 0;
        }
        foreach (string file in files)
        {
            lock (Gate)
            {
                if (LoadedFiles.Contains(file)) continue;
                // FR_NOT_ENUM: keep the face out of the app-wide enumeration other apps can see.
                int added = AddFontResourceEx(file, FR_PRIVATE | FR_NOT_ENUM, IntPtr.Zero);
                if (added <= 0) continue;
                LoadedFiles.Add(file);
                FamilyCache.Clear();
                count++;
            }
        }
        return count;
    }

    /// <summary>Registers a single font file.</summary>
    public static bool RegisterFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        string full = Path.GetFullPath(path);
        lock (Gate)
        {
            if (LoadedFiles.Contains(full)) return true;
            int added = AddFontResourceEx(full, FR_PRIVATE | FR_NOT_ENUM, IntPtr.Zero);
            if (added <= 0) return false;
            LoadedFiles.Add(full);
            FamilyCache.Clear();
            return true;
        }
    }

    /// <summary>True when the resolved family for <paramref name="face"/> is actually installed.</summary>
    public static bool IsAvailable(CouiFontFace face) => Resolve(face, false) is not null;

    /// <summary>Display name used in settings rows ("霞鹜文楷" / "霞鹜臻楷").</summary>
    public static string DisplayName(CouiFontFace face) => face == CouiFontFace.ZhenKai ? "霞鹜臻楷" : "霞鹜文楷";

    /// <summary>
    /// Builds a font for <paramref name="face"/> at <paramref name="points"/>. Falls back to
    /// <see cref="FallbackFamily"/> when neither candidate family is present, so a host that ships
    /// without the bundled fonts still gets a usable UI.
    /// </summary>
    public static Font Create(CouiFontFace face, float points, bool strong)
    {
        string family = Resolve(face, strong);
        if (family is not null)
        {
            try
            {
                FontStyle style = strong ? Faces[face].StrongStyle : FontStyle.Regular;
                return new Font(family, points, style, GraphicsUnit.Point);
            }
            catch (ArgumentException)
            {
                // Some faces expose only one style; retry as regular before giving up on the family.
                try { return new Font(family, points, FontStyle.Regular, GraphicsUnit.Point); }
                catch (ArgumentException) { }
            }
        }
        return new Font(FallbackFamily, points, strong ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
    }

    /// <summary>Resolved family name for a face / weight slot, or null when unavailable.</summary>
    public static string Resolve(CouiFontFace face, bool strong)
    {
        var slot = Faces[face];
        string[] names = strong ? slot.Strong : slot.Body;
        foreach (string name in names)
        {
            if (Exists(name)) return name;
        }
        if (strong && slot.StrongStyle == FontStyle.Regular)
        {
            foreach (string name in slot.Body)
            {
                if (Exists(name)) return name;
            }
        }
        return null;
    }

    private static bool Exists(string familyName)
    {
        lock (Gate)
        {
            if (FamilyCache.TryGetValue(familyName, out bool cached)) return cached;
            bool found = false;
            try
            {
                // FontFamily.Families reflects the process font table, including FR_PRIVATE faces.
                foreach (FontFamily family in FontFamily.Families)
                {
                    if (string.Equals(family.Name, familyName, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
            }
            catch (ArgumentException)
            {
                found = false;
            }
            FamilyCache[familyName] = found;
            return found;
        }
    }
}


