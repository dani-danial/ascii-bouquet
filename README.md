# ASCII Bouquet (Mobile Web)

An interactive, spinning 3D ASCII flower bouquet built for phone screens (iOS Safari & Android Chrome) with zero dependencies.

## Features
- **3D Z-buffered ASCII Bouquet:** Roses, daisies, tulips, baby's breath, leafy stems, paper cone wrap, ribbon bow, and floating sparkles/hearts.
- **Touch Controls:** Drag to spin & tilt, pinch to zoom, tap anywhere for a sparkle/heart burst.
- **4 Color Palettes:** Classic Rose, Sunset Peach, Lavender Dream, Midnight Blue.
- **Personalize & Share:** Tap **Send** to set a recipient name and custom message, or pass URL params directly:
  ```
  ?to=Maya&msg=coded%20these%20flowers%20for%20you&theme=rose
  ```

## How to Launch on Your Phone

### Option 1: Share as a Real Link (Best for Sending to Someone)
Deploy the folder (`index.html`, `styles.css`, `bouquet.js`) to any free static host:
- **Netlify Drop:** Drag this folder onto https://app.netlify.com/drop — you get an instant `https://...` link to open or text to anyone.
- **GitHub Pages:** Push these files to a GitHub repo and enable **Settings → Pages → Deploy from branch**.

### Option 2: Preview on Your Phone via Local Wi-Fi (Same Network)
From this project folder on Windows, run a quick local HTTP server (for example using Node.js):
```powershell
npx --yes serve . -l 3000
```
Then open `http://<YOUR-PC-LOCAL-IP>:3000` in Safari or Chrome on your phone.
*(Tip: run `ipconfig` in PowerShell to find your PC's IPv4 address.)*
