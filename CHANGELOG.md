# Changelog

## 0.1.1 — 2026-11-20

Interaction pass over the demo: every switch, chip, checkbox, radio, slider and button was clicked
and the resulting states were captured.

### Fixed

- **Chip click crashed the demo** with `Index was outside the bounds of the array`. The handler
  captured the `for` loop counter, so by the time a chip was clicked `chipNames[i]` read past the
  end. The name is now captured per iteration.
- **Theme switches shrank every heading.** The base control reset `Font` to the 15sp body style on
  every theme/DPI change, discarding the header and caption fonts a host had assigned.
- **Choosing an accent in dark mode flipped the page to light tokens.** The accent override built a
  fresh `CouiColors`, and any token it did not repeat fell back to the light defaults. Colors are
  now copied from the active scheme (`with` on the record) before accents are replaced.
- **Chips allowed multiple selections.** Accent and filter chips were independent; both groups now
  uncheck siblings, with a re-entrancy guard so an uncheck does not bounce back.
- **Changing "display size" disposed the control being dragged.** The scale slider rebuilt the page
  inside its own `ValueChanged`, destroying the slider mid-drag and the status label. Relayout is
  now deferred until the drag ends, and the status label survives rebuilds.
- **Large display sizes clipped the chip row.** The window kept its pixel width while every dp
  grew, so cards overflowed. The demo window now grows with the display-size setting, clamped to
  the working area.
- `CouiProgressBar` no longer forces its height back to 4dp on a theme change.

### Added

- `--dump-layout=<path>` writes each control's screen rectangle, so clicks can be scripted against
  real coordinates during visual checks.

## 0.1.0 — 2026-11-20

First cut of an independent ColorOS 17 (COUI) styled WinForms library. Not derived from the
MiuixSharp codebase; only the engineering conventions carry over.

### Theme

- `CouiColors` token set transcribed from COUI's `Colors.kt`: accent `#0080FF`, page `#F0F1F2`,
  card `#FFFFFF`, label layers at 90/54/26%, divider 12%, semantic success/warning/error.
- `CouiColors.Dark()` for the night palette (`#1A8CFF` accent, `#1E1E1E` card).
- `CouiTheme`: light / dark / follow-system mode, accent seed, user display scale, `Changed`
  event, `SystemPrefersDark` through the Personalize registry key.
- `CouiTypography`: 10/12/13/14/15/18/20/24 sp styles from `TextStyles.kt`.
- `CouiShape`: card 12dp, button capsule 44dp, input 10dp, popup 16dp, sheet 28dp, dialog 33dp,
  switch 36×22, slider 4dp track, row 48dp.
- `Squircle`: rounded path with a 0.68 cubic control factor (fuller than a circle, approximating
  COUI's 1.25 curvature extension).

### Controls

- `CouiCard`, `CouiButton` (Primary / Tonal / Text / Danger), `CouiSwitch`, `CouiCheckBox`,
  `CouiRadioButton`, `CouiChip`, `CouiTextBox`, `CouiSlider`, `CouiProgressBar`, `CouiDivider`,
  `CouiLabel`.
- Keyboard access on every interactive control (Space/Enter, arrows on the slider, Home/End),
  focus rings using the 40% accent `focusOutline` token.

### Fixes made during bring-up

- Corners were aliasing because the base control declared `Opaque`; the base now fills a resolved
  backdrop and paints the shape instead of clipping a `Region`.
- Translucent label tokens rendered as solid dark text through GDI; text colors are composited
  with the surface first.
- Type came out double-scaled at 150% DPI because the point size was multiplied by the device
  ratio as well; fonts now follow only the user scale.
- The text field hint was invisible twice over: first because the hosted native `TextBox` covered
  the parent's paint, then because native `TextBox` does not raise `Paint`. It is now driven
  through the inner control's text + fore-color, keeping token colors.

### Demo

- `samples/CouiSharp.Demo`: ColorOS-style settings page (appearance, notifications, components,
  about) with theme/accent/scale controls; `--dark`, `--light`, `--accent=` switches for
  reproducible screenshots.
