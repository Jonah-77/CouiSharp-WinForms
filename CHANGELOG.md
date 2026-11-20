# Changelog

## 0.1.5 — 2026-11-20

Aligns the state layers with COUI after its motion pass (`library: Bring motion and touch in line
with COUI 17`).

### Fixed

- **Press and hover were a shade too light.** `Press` held `#14000000` and `Hover` `#0A000000`, which
  are COUI's `pressBackground` slot and a dimmer hover; the components paint the `press` and `hover`
  tokens instead — `#1F000000` and `#14000000` in light, `#33FFFFFF` and `#26FFFFFF` in dark.
  `CouiButton` and `CouiSwitch` had been papering over it with inline literals; both now read the
  palette, so a pressed button, switch or chip darkens by the same amount as COUI's.
- **A pressed card in the dark theme was too dark.** `CardPressed` was `#2C2C2C`. COUI presses a card
  to the `#33FFFFFF` layer, which replaces the fill and composites over the page, so the flattened
  value is `#333333`.

### New

- `CouiColors.Focus`, COUI's `couiColorFocus` (`#1F0080FF` in light, `#331A8CFF` in dark). It follows
  the accent seed like the other accent slots. The controls here still draw focus as a ring, so the
  token is for host apps that paint list rows.

### Note

- COUI's other motion changes are out of reach of WinForms chrome: spring state layers, the
  glass thumb, glitter gradients, haptics and page transitions have no GDI counterpart. The icon
  rewrite in the same upstream pass is not part of this port.

## 0.1.4 — 2026-11-20

### Fixed

- **The square remnant at the corners of cards and input fields was a square backdrop, not a
  stroke mismatch.** `CouiBackdrop.Resolve` began its walk at the control itself, so every control
  with an opaque `SurfaceColor` (cards, outlined text fields) flooded its whole client rectangle
  with its own surface colour before drawing the rounded shape. The radius then only existed in the
  hairline on top: the four corners kept the square fill and showed as an L-shaped ghost outside
  the curve. The walk now starts at the parent (`CouiBackdrop.Resolve`), so the corner shows the
  surface behind the control, and `CouiBackdrop.ResolveSurface` resolves the control's own surface
  for flattening translucent tokens.

### Changed

- `CouiBackdrop.ResolveSurface` is new; it keeps `CouiControl.Solid` and `CouiTextBox.FieldFill`
  compositing onto the fill they actually paint on.

### Note

- The 0.1.3 entry below diagnosed this defect as a fill/hairline curve mismatch. That change
  removed the seam between fill and line, but it could not remove the square fill underneath; the
  ghost survived because the fill itself never followed the radius.

## 0.1.3 — 2026-11-20

### Fixed

- **Chips lost the left cap of their capsule.** The demo placed a `CouiLabel` of a fixed width in
  front of the chip row; the label ended nine pixels inside the first chip, and because it was
  added before the chips it sat above them and clipped that strip, leaving a flat vertical edge
  where a round cap should be. Row labels now size themselves to their own text, so they stop
  short of the next column. The same overlap was trimming the left end of the display-size slider.
- **Outlined fields showed square remnants at their rounded corners.** The fill was a 0.68
  squircle while the inset hairline was built with the 0.5523 circle factor, so the two curves
  diverged at every corner and left an L-shaped ghost. `StrokeInset`/`DrawFocusRing` now take the
  corner factor of the shape they trace, and the inset no longer leaves a gap that showed the fill
  as a lighter band outside the line.

## 0.1.2 — 2026-11-20

Edge pass: buttons were clicked, hovered and focused at a 150% scale factor and the resulting
bitmaps were read pixel by pixel.

### Fixed

- **A translucent row along capsule edges.** Shapes were filled into a rectangle offset by half a
  pixel and shortened by one pixel, so the outermost row along a button's top and left edge was
  only half covered. At 150% scaling this showed up as a lighter rim (a `#255A8F` row sitting above
  the `#2B95FF` fill on the primary button). Shape bounds now hug the client box exactly.
- **Focus rings were cut into flat segments at the ends of a capsule.** The ring stroked the button
  silhouette with a 2dp pen, so half of the stroke fell outside the control, got clipped, and left
  a pale seam between ring and fill. Rings are now drawn on an inward-offset path, producing a
  continuous outline just inside the shape.
- **Hairline borders were clipped the same way** on cards, input fields and check boxes; they now
  stroke an inset path so the full pen width is visible.
- `CouiSlider`'s focus ring could exceed the control bounds when the slider was short; it is now
  clamped to the control and inset like the other rings.

### Changed

- `CouiControl.DrawFocusRing` takes the shape bounds and corner radius instead of a prepared path,
  so it can inset the ring itself. A new `StrokeInset` helper does the same for outline strokes.

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
