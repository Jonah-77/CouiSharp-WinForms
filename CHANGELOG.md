# Changelog

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
