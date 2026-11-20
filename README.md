# CouiSharp

A ColorOS 17 (COUI) styled control library for **WinForms**, written in pure C#. It is a
**separate library** from [MiuixSharp-WinForms](https://github.com/Jonah-77/MiuixSharp-WinForms):
different namespace (`CouiSharp`), different repository, different version line. The two only
share engineering conventions (DPI maths, self-painted surfaces), not code or design tokens.

![Light theme](docs/screenshots/light.png)
![Dark theme](docs/screenshots/dark.png)

## Why it looks the way it does

The visual language is taken from **COUI**, the Compose Multiplatform ColorOS 17 theme
([suqi8/coui](https://github.com/suqi8/coui), Apache-2.0). The tokens implemented here were read
from that project's sources and re-theme spec:

| Area | Value | Token / source |
| --- | --- | --- |
| Accent | `#0080FF` light / `#1A8CFF` dark | `primary`, `containerTheme` |
| Page | `#F0F1F2` light / `#000000` dark | `backgroundWithCard` |
| Card | `#FFFFFF` light / `#1E1E1E` dark | `card` |
| Text | 90% / 54% / 26% black (inverse in dark) | `labelPrimary`, `labelSecondary`, `labelTertiary` |
| Dividers | 12% black, 15% white in dark | `divider` |
| Card radius | **12dp** | card token |
| Button | **capsule**, 44dp tall (radius 21.5dp) | button spec |
| Input | **10dp** radius, 44dp tall | text field spec |
| Switch | **36 × 22dp**, 18dp thumb | switch spec |
| Slider | 20dp min height, 4dp track | slider spec |
| Type | 15sp body, 13sp caption, 18sp header, 20sp title | `TextStyles.kt` |
| Shape | squircle, continuous curvature | `coui-squircle` |

Success / warning / error use `#00BD13`, `#FF7700` and `#E82A18`; the semantic set also carries
`red` `#EB3B2F`, `orange`, `yellow` and `green` so a host can express state without inventing
colors.

## Controls

| Control | What it is |
| --- | --- |
| `CouiCard` | 12dp squircle container on the page color; optional hairline and press feedback |
| `CouiButton` | Capsule action: `Primary`, `Tonal`, `Text`, `Danger` |
| `CouiSwitch` | 36 × 22dp switch with an 18dp thumb |
| `CouiCheckBox` | 18dp squircle box with a drawn tick |
| `CouiRadioButton` | 18dp ring, `GroupName` unchecks siblings |
| `CouiChip` | 32dp checkable filter chip |
| `CouiTextBox` | 10dp field with a real `TextBox` inside (IME, selection, caret stay native) |
| `CouiSlider` | 4dp track, accent fill, keyboard support |
| `CouiProgressBar` | 4dp capsule, determinate and indeterminate |
| `CouiDivider` | 1dp rule with the 16dp leading inset |
| `CouiLabel` | Text primitive so labels never leave stale pixels on a painted card |
| `CouiIcon` | Vector icon from the built-in set, stroked on a 24dp canvas in the theme colour |
| `CouiGlassPanel` | Frosted panel: the page behind it is re-rendered, blurred and clipped to a squircle |

## Typeface

The whole UI runs in **霞鹜文楷** (LXGW WenKai, OFL-1.1) — a text-weight kaiti — and switches to
**霞鹜臻楷** (LXGW ZhenKai) when a host wants heavier text. There is exactly one family in the
program, so the two faces are the only typographic switch:

```csharp
CouiFonts.Register(Path.Combine(AppContext.BaseDirectory, "fonts")); // process-local, FR_PRIVATE
CouiTheme.FontFace = CouiFontFace.ZhenKai;                          // default: WenKai
```

CJK 霞鹜 releases ship each weight as its own family (`霞鹜文楷` vs `霞鹜文楷 Medium`), so the
library maps *roles* to family names instead of asking for `FontStyle.Bold`: headers, titles and
buttons use the Medium family, body text the regular one. When no 霞鹜 face is installed the
library falls back to `Microsoft YaHei UI` rather than rendering in a random default.

Fonts are loaded with `AddFontResourceEx(FR_PRIVATE | FR_NOT_ENUM)`, not `PrivateFontCollection`:
`TextRenderer` is GDI-based and silently ignores private-collection fonts, which made every label
fall back to a system face.

## Icons

20 icons ship as `GraphicsPath` outlines — no bitmaps, no traced system assets:

`Search  Close  Add  Minus  More  ChevronLeft/Right/Up/Down  ArrowBack  Check  Delete  Edit
Settings  Info  Warning  Refresh  Download  Copy  Share`

Every icon uses a 24dp canvas, a 1.5dp stroke with round caps and a 2dp safety margin, so it stays
crisp at any DPI and re-colours with the theme (`CouiIcon.Tint` overrides). Icons paint no backdrop
(`FillsBackdrop = false`), which is also what lets them sit on top of a glass panel without a
rectangle of page colour behind them.

## Glass (rendering plan "B")

`CouiGlassPanel` blurs the **app's own page**, not the desktop: WinForms cannot read back rendered
pixels of a self-painted control (`DrawToBitmap` returns blank, `PrintWindow` does not match the
real frame), so the panel re-renders the control tree into a bitmap, downscales it, runs a box blur
on the CPU and draws the result clipped to a squircle. A wallpaper or photo behind the window will
**not** show through; cards, buttons and text of the page will.

```csharp
var bar = new CouiGlassPanel { Radius = 22, BlurStrength = 10, TintStrength = 0.14f };
Controls.Add(bar);
bar.BringToFront();   // call Refresh() after the page under it changes
```


## Quick start

```csharp
using CouiSharp.Controls;
using CouiSharp.Theme;

CouiTheme.Mode = CouiThemeMode.Dark;          // or System / Light
CouiTheme.SeedColor = Color.FromArgb(0xFF, 0x00, 0xBD, 0x13); // optional accent override

var card = new CouiCard { Bounds = new Rectangle(16, 16, 400, 160) };
var title = new CouiLabel { Text = "外观", Font = CouiTypography.Header(), Bounds = new Rectangle(16, 12, 360, 28) };
var toggle = new CouiSwitch { Checked = true, Location = new Point(340, 60) };
var save = new CouiButton { Text = "保存", Variant = CouiButtonVariant.Primary, Location = new Point(16, 100) };

card.Controls.AddRange(new Control[] { title, toggle, save });
Controls.Add(card);
```

The demo in `samples/CouiSharp.Demo` is the same page shape as a ColorOS settings screen and
accepts `--dark`, `--light`, `--accent=green|purple|orange|teal` and `--face=zhenkai` so visual
states can be reproduced without clicking. Put the `.ttf` files under `fonts/` at the repository
root and they are copied next to the executable on build.

## Design rules the controls follow

1. **No `Region` clipping for rounded corners.** At 125–150% DPI a clipped region aliases into a
   dashed edge. Every control paints its own squircle path instead.
2. **Paint the backdrop first.** `CouiBackdrop.Resolve` walks the parent chain (including custom
   surfaces exposed through `SurfaceColor`), so a rounded corner never shows a foreign color.
3. **Never declare `ControlStyles.Opaque`.** It would skip the backdrop fill; the library fills a
   double-buffered surface instead.
4. **Flatten translucent text colors.** GDI's text renderer does not blend alpha reliably, so
   tokens such as a 26% label are composited with the surface before drawing.
5. **Metrics are recomputed, not baked.** `ApplyMetrics()` runs on handle creation, DPI change,
   theme change and resize.
6. **dp everywhere.** `CouiTheme.Dp(control, dp)` = dp × (monitor DPI / 96) × user scale; fonts use
   points, which GDI already scales, so they only follow the user scale.

## Build

```
dotnet build CouiSharp.sln
dotnet run --project samples/CouiSharp.Demo
```

Targets `net8.0-windows` and `net6.0-windows`. The demo targets `net8.0-windows` and is
PerMonitorV2 aware.

## Known limits

- Controls are **self-painted**, not native wrappers; a `CouiTextBox` hosts a borderless native
  `TextBox` but everything else is drawn by the library.
- Glass is a **CPU blur of the library's own paint tree**. A window shadow, the desktop wallpaper or
  another app's pixels never appear through a `CouiGlassPanel`; a real backdrop blur needs a
  compositor (Direct2D / Composition), which is not part of this release. Glass also does not
  animate: refresh it explicitly after the page under it changes.
- No shader-based squircle, no metaball/ Lottie effects, no navigation stack — those are COUI
  modules that need effects WinForms cannot cheaply provide.
- Icon set is the 20 hand-drawn outlines above; COUI's `coui-icons` pack is larger and is not
  redistributed here (its glyphs are traced from ColorOS system packages).
- High contrast mode is not wired into the token layer yet; `CouiTheme` exposes the switch point.

## Credits and license

Design tokens are derived from **COUI** (Copyright the COUI contributors, Apache-2.0) and from the
ColorOS 17 design language. Typefaces are **霞鹜文楷 / 霞鹜臻楷** by LXGW, SIL Open Font License 1.1.
Icons are drawn for this library, not traced from ColorOS system assets. This library ships under
the MIT license; see `LICENSE` and `NOTICE`.

Not affiliated with OPPO / ColorOS. ColorOS is a trademark of its owner.
