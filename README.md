# for pisya ♥ — ASCII Bouquet (PWA & Windows Widget)

An interactive, spinning 3D ASCII flower bouquet built as an installable Progressive Web App (PWA) for iPhone & Android, plus a lightweight native Windows desktop widget.

**Live Web/Phone App:** https://dani-danial.github.io/ascii-bouquet/

## 1. Windows Desktop Widget (Low RAM)
- **File:** `windows-widget/BouquetWidget.exe` (28 KB native `.NET Framework 4` WinForms app, ~24 MB private RAM — no browser/Electron required).
- **Run:** Double-click `windows-widget/BouquetWidget.exe` (works out of the box on Windows 10/11).
- **Controls:**
  - Drag the top header (`for pisya ♥`) to move the widget anywhere on your desktop.
  - Drag the bouquet to spin/tilt, scroll wheel to zoom, click for sparkles.
  - **Right-click** anywhere on the widget to pause spin, switch color palettes, resize (`Small`, `Medium`, `Large`), toggle **Always on Top**, reset view, or close.
- **Rebuild from source:** Run `windows-widget/build.bat` (uses the built-in Windows `csc.exe` compiler).

## 2. Install as an App on Phone (iPhone & Android)
- **iPhone (Safari):** Open the link → tap **Share** → **Add to Home Screen** (or tap **📷 Save** to export a square bouquet card for iPhone's **Photos** home-screen widget).
- **Android (Chrome):** Open the link → tap **⋮** → **Install app** (or **Add to Home screen**).

## Web Controls
- **Drag** to spin & tilt in 3D, **Pinch** to zoom, **Tap** for extra hearts & sparkles.
- **Bottom bar:** Toggle spin, cycle 4 palettes (`Rose`, `Sunset`, `Lavender`, `Midnight`), reset view, or save a `1080×1080` PNG card.

*(Note: A customizable copy with the **Send** button is preserved on the `template-with-send` branch.)*
