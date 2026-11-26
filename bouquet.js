(() => {
  "use strict";

  // Color themes for the bouquet petals, ribbon, and UI glow.
  const THEMES = {
    rose: {
      label: "Rose",
      accent: "#ff5c8a",
      glow: "rgba(255, 88, 130, 0.14)",
      rose: [255, 77, 118],
      roseInner: [255, 165, 190],
      tulip: [236, 112, 162],
      daisy: [255, 246, 240],
      daisyCenter: [255, 206, 75],
      baby: [255, 232, 242],
      ribbon: [230, 48, 84],
      heart: [255, 92, 138],
    },
    sunset: {
      label: "Sunset",
      accent: "#ff8c5a",
      glow: "rgba(255, 140, 90, 0.15)",
      rose: [255, 107, 74],
      roseInner: [255, 194, 136],
      tulip: [255, 150, 95],
      daisy: [255, 245, 224],
      daisyCenter: [244, 162, 54],
      baby: [255, 234, 210],
      ribbon: [217, 68, 54],
      heart: [255, 130, 92],
    },
    lavender: {
      label: "Lavender",
      accent: "#c78dff",
      glow: "rgba(199, 141, 255, 0.15)",
      rose: [186, 115, 255],
      roseInner: [232, 195, 255],
      tulip: [240, 148, 222],
      daisy: [250, 245, 255],
      daisyCenter: [255, 214, 102],
      baby: [238, 224, 255],
      ribbon: [148, 76, 228],
      heart: [212, 150, 255],
    },
    midnight: {
      label: "Midnight",
      accent: "#6ec6ff",
      glow: "rgba(110, 198, 255, 0.14)",
      rose: [82, 165, 255],
      roseInner: [185, 226, 255],
      tulip: [130, 145, 255],
      daisy: [238, 248, 255],
      daisyCenter: [255, 215, 110],
      baby: [210, 238, 255],
      ribbon: [64, 128, 235],
      heart: [120, 200, 255],
    },
  };

  const THEME_ORDER = ["rose", "sunset", "lavender", "midnight"];
  const SHADE_CHARS = ".,-~:;=!*#$@";

  // Vector helpers
  function norm3(x, y, z) {
    const len = Math.hypot(x, y, z) || 1;
    return [x / len, y / len, z / len];
  }

  function basisFromDir(dx, dy, dz) {
    const [fx, fy, fz] = norm3(dx, dy, dz);
    const upX = Math.abs(fy) > 0.92 ? 1 : 0;
    const upY = Math.abs(fy) > 0.92 ? 0 : 1;
    const upZ = 0;
    // right = normalize(cross(up, forward))
    const rx0 = upY * fz - upZ * fy;
    const ry0 = upZ * fx - upX * fz;
    const rz0 = upX * fy - upY * fx;
    const [rx, ry, rz] = norm3(rx0, ry0, rz0);
    // localUp = cross(forward, right)
    const ux = fy * rz - fz * ry;
    const uy = fz * rx - fx * rz;
    const uz = fx * ry - fy * rx;
    return { fx, fy, fz, rx, ry, rz, ux, uy, uz };
  }

  function toWorld(b, lx, ly, lz, ox, oy, oz) {
    return [
      ox + lx * b.rx + ly * b.ux + lz * b.fx,
      oy + lx * b.ry + ly * b.uy + lz * b.fy,
      oz + lx * b.rz + ly * b.uz + lz * b.fz,
    ];
  }

  function rotVec(b, lx, ly, lz) {
    return norm3(
      lx * b.rx + ly * b.ux + lz * b.fx,
      lx * b.ry + ly * b.uy + lz * b.fy,
      lx * b.rz + ly * b.uz + lz * b.fz
    );
  }

  // Build the 3D point cloud representing the entire bouquet.
  function buildBouquetPoints() {
    const pts = [];

    function pushPt(x, y, z, nx, ny, nz, role, ch = null, tint = 1) {
      const [nnx, nny, nnz] = norm3(nx, ny, nz);
      pts.push({ x, y, z, nx: nnx, ny: nny, nz: nnz, role, ch, tint });
    }

    // 1. Paper cone wrap (lower portion: y from -1.05 to +0.18)
    const coneStepsY = 34;
    const coneStepsA = 56;
    for (let i = 0; i <= coneStepsY; i++) {
      const t = i / coneStepsY;
      const y = -1.05 + t * 1.23;
      const baseR = 0.06 + Math.pow(t, 0.92) * 0.54;
      for (let j = 0; j < coneStepsA; j++) {
        const a = (j / coneStepsA) * Math.PI * 2;
        // Scalloped paper rim + gentle fold creases
        const topWave = t > 0.7 ? Math.sin(a * 5 + 0.4) * 0.08 * ((t - 0.7) / 0.3) : 0;
        const fold = Math.sin(a * 7) * 0.015 * t;
        const r = baseR + fold;
        const py = y + topWave;
        const x = Math.cos(a) * r;
        const z = Math.sin(a) * r;
        // Paper cone tilts slightly back on the back side for a classic florist wrap
        const backLift = Math.sin(a) < 0 ? -Math.sin(a) * 0.12 * t : 0;
        const ch = i === coneStepsY ? "~" : (j % 8 === 0 ? "|" : null);
        const shadeTint = 0.85 + 0.15 * Math.cos(a * 3);
        pushPt(x, py + backLift, z, Math.cos(a), 0.35, Math.sin(a), "paper", ch, shadeTint);
      }
    }

    // 2. Ribbon waist band + bow + trailing tails around y = -0.45
    const waistY = -0.46;
    const waistR = 0.345;
    for (let k = -2; k <= 2; k++) {
      const ry = waistY + k * 0.022;
      for (let j = 0; j < 64; j++) {
        const a = (j / 64) * Math.PI * 2;
        pushPt(
          Math.cos(a) * waistR,
          ry,
          Math.sin(a) * waistR,
          Math.cos(a),
          0.1,
          Math.sin(a),
          "ribbon",
          k === 0 ? "=" : "-",
          1
        );
      }
    }

    // Bow loops on the front (+z)
    for (const side of [-1, 1]) {
      for (let i = 0; i <= 26; i++) {
        const u = (i / 26) * Math.PI * 2;
        const lx = side * (0.04 + 0.16 * Math.abs(Math.sin(u * 0.5)));
        const ly = waistY + 0.055 * Math.sin(u);
        const lz = waistR + 0.03 + 0.04 * Math.sin(u * 0.5);
        for (let w = -1; w <= 1; w++) {
          pushPt(lx, ly + w * 0.016, lz, side * 0.3, 0.4, 0.9, "ribbon", "@", 1.05);
        }
      }
      // Hanging ribbon tail
      for (let i = 0; i <= 18; i++) {
        const t = i / 18;
        const tx = side * (0.03 + t * 0.13 + Math.sin(t * 4) * 0.015);
        const ty = waistY - 0.02 - t * 0.34;
        const tz = waistR + 0.02 + Math.sin(t * 3) * 0.02;
        pushPt(tx, ty, tz, 0, 0.2, 1, "ribbon", side > 0 ? "\\" : "/", 0.95);
      }
    }

    // Helpers for flowers
    function addStem(x1, y1, z1, x2, y2, z2) {
      const steps = 22;
      for (let i = 0; i <= steps; i++) {
        const t = i / steps;
        const cx = x1 * (1 - t) + x2 * t;
        const cy = y1 * (1 - t) + y2 * t;
        const cz = z1 * (1 - t) + z2 * t;
        // Slight organic bow in stem
        const bend = Math.sin(t * Math.PI) * 0.04;
        pushPt(cx + bend, cy, cz + bend * 0.5, 0.4, 0.5, 0.7, "stem", "|", 0.95);
      }
    }

    function addLeaf(ox, oy, oz, dirX, dirY, dirZ, scale = 0.2) {
      const b = basisFromDir(dirX, dirY, dirZ);
      for (let i = 1; i <= 10; i++) {
        const t = i / 10;
        const halfW = Math.sin(t * Math.PI) * 0.36 * scale;
        const zFwd = t * scale;
        const curveY = -Math.pow(t, 2) * 0.18 * scale;
        for (let s = -4; s <= 4; s++) {
          const u = s / 4;
          const [wx, wy, wz] = toWorld(b, u * halfW, curveY, zFwd, ox, oy, oz);
          const [nx, ny, nz] = rotVec(b, u * 0.25, 0.95, 0.15);
          pushPt(wx, wy, wz, nx, ny, nz, "leaf", s === 0 ? "/" : "*", 0.9 + t * 0.2);
        }
      }
    }

    function addRose(ox, oy, oz, dx, dy, dz, scale = 0.25) {
      const b = basisFromDir(dx, dy, dz);
      // Fibonacci spiral petals cupped like a rose/peony
      const petalCount = 7;
      for (let p = 0; p < petalCount; p++) {
        const baseAngle = p * 2.39996; // golden angle
        const radialLayer = 0.35 + (p / petalCount) * 0.65;
        const rows = 9;
        const cols = 10;
        for (let r = 0; r <= rows; r++) {
          const vr = r / rows;
          for (let c = -cols / 2; c <= cols / 2; c++) {
            const vc = c / (cols / 2);
            const theta = baseAngle + vc * 0.65;
            const rad = (0.04 + vr * 0.23 * radialLayer) * scale / 0.25;
            const cup = (Math.pow(vr, 1.35) * 0.22 - Math.abs(vc) * 0.03) * scale / 0.25;
            const lx = Math.cos(theta) * rad;
            const ly = Math.sin(theta) * rad;
            const lz = cup;
            const [wx, wy, wz] = toWorld(b, lx, ly, lz, ox, oy, oz);
            const [nx, ny, nz] = rotVec(b, Math.cos(theta) * 0.6, Math.sin(theta) * 0.6, 0.7);
            const role = radialLayer < 0.6 ? "roseInner" : "rose";
            pushPt(wx, wy, wz, nx, ny, nz, role, null, 0.85 + vr * 0.25);
          }
        }
      }
    }

    function addDaisy(ox, oy, oz, dx, dy, dz, scale = 0.24) {
      const b = basisFromDir(dx, dy, dz);
      // Golden textured center dome
      for (let i = 0; i < 36; i++) {
        const a = i * 2.39996;
        const r = Math.sqrt(i / 36) * 0.095 * (scale / 0.24);
        const lz = 0.035 * (1 - (i / 36) * 0.6);
        const [wx, wy, wz] = toWorld(b, Math.cos(a) * r, Math.sin(a) * r, lz, ox, oy, oz);
        const [nx, ny, nz] = rotVec(b, Math.cos(a) * 0.3, Math.sin(a) * 0.3, 0.95);
        pushPt(wx, wy, wz, nx, ny, nz, "daisyCenter", i % 2 === 0 ? "o" : "*", 1.05);
      }
      // Radiating white petals
      const petals = 14;
      for (let p = 0; p < petals; p++) {
        const ang = (p / petals) * Math.PI * 2;
        const cosA = Math.cos(ang);
        const sinA = Math.sin(ang);
        for (let r = 2; r <= 10; r++) {
          const t = r / 10;
          const dist = (0.08 + t * 0.22) * (scale / 0.24);
          const halfW = Math.sin(t * Math.PI) * 0.032 * (scale / 0.24);
          const curlZ = Math.sin(t * Math.PI) * 0.025;
          for (let w = -1; w <= 1; w++) {
            const lx = cosA * dist - sinA * (w * halfW);
            const ly = sinA * dist + cosA * (w * halfW);
            const [wx, wy, wz] = toWorld(b, lx, ly, curlZ, ox, oy, oz);
            const [nx, ny, nz] = rotVec(b, cosA * 0.2, sinA * 0.2, 0.95);
            pushPt(wx, wy, wz, nx, ny, nz, "daisy", t > 0.75 ? "*" : null, 0.95 + t * 0.1);
          }
        }
      }
    }

    function addTulip(ox, oy, oz, dx, dy, dz, scale = 0.23) {
      const b = basisFromDir(dx, dy, dz);
      // Cupped tulip chalice made of 6 overlapping petals
      const petals = 6;
      for (let p = 0; p < petals; p++) {
        const baseA = (p / petals) * Math.PI * 2;
        for (let h = 0; h <= 11; h++) {
          const t = h / 11;
          const r = Math.sin(t * Math.PI * 0.78) * 0.14 * (scale / 0.23);
          const lz = t * 0.28 * (scale / 0.23);
          for (let w = -3; w <= 3; w++) {
            const a = baseA + (w / 3) * 0.42 * Math.sin(t * Math.PI * 0.85 + 0.2);
            const lx = Math.cos(a) * r;
            const ly = Math.sin(a) * r;
            const [wx, wy, wz] = toWorld(b, lx, ly, lz, ox, oy, oz);
            const [nx, ny, nz] = rotVec(b, Math.cos(a), Math.sin(a), 0.35);
            pushPt(wx, wy, wz, nx, ny, nz, "tulip", null, 0.8 + t * 0.3);
          }
        }
      }
    }

    function addBabysBreath(ox, oy, oz) {
      const offsets = [
        [0, 0, 0],
        [0.08, 0.06, 0.04],
        [-0.07, 0.05, 0.06],
        [0.05, -0.04, -0.06],
        [-0.06, 0.07, -0.04],
        [0.02, 0.1, 0.01],
      ];
      for (const [dx, dy, dz] of offsets) {
        pushPt(ox + dx, oy + dy, oz + dz, 0, 1, 0, "baby", "*", 1.05);
        pushPt(ox + dx + 0.02, oy + dy + 0.015, oz + dz, 0.3, 0.8, 0.5, "baby", "+", 0.95);
      }
    }

    // 3. Place stems, leaves, and flower heads in a full 3D dome
    const stemsOrigin = [0, -0.35, 0];

    const roses = [
      { pos: [0.0, 0.66, 0.06], dir: [0.0, 0.92, 0.38], s: 0.29 },
      { pos: [-0.34, 0.48, 0.24], dir: [-0.45, 0.8, 0.4], s: 0.25 },
      { pos: [0.35, 0.46, 0.22], dir: [0.46, 0.78, 0.42], s: 0.25 },
      { pos: [-0.04, 0.45, -0.28], dir: [-0.05, 0.85, -0.52], s: 0.24 },
    ];
    for (const f of roses) {
      addStem(stemsOrigin[0], stemsOrigin[1], stemsOrigin[2], f.pos[0], f.pos[1] - 0.06, f.pos[2]);
      addRose(f.pos[0], f.pos[1], f.pos[2], f.dir[0], f.dir[1], f.dir[2], f.s);
    }

    const daisies = [
      { pos: [-0.46, 0.36, -0.08], dir: [-0.62, 0.75, -0.15], s: 0.23 },
      { pos: [0.45, 0.35, -0.1], dir: [0.62, 0.74, -0.18], s: 0.23 },
      { pos: [0.02, 0.28, 0.42], dir: [0.05, 0.62, 0.78], s: 0.22 },
    ];
    for (const f of daisies) {
      addStem(stemsOrigin[0], stemsOrigin[1], stemsOrigin[2], f.pos[0], f.pos[1] - 0.04, f.pos[2]);
      addDaisy(f.pos[0], f.pos[1], f.pos[2], f.dir[0], f.dir[1], f.dir[2], f.s);
    }

    const tulips = [
      { pos: [-0.24, 0.56, -0.26], dir: [-0.32, 0.88, -0.34], s: 0.22 },
      { pos: [0.25, 0.54, -0.24], dir: [0.34, 0.86, -0.35], s: 0.22 },
      { pos: [-0.18, 0.34, 0.36], dir: [-0.25, 0.72, 0.65], s: 0.2 },
    ];
    for (const f of tulips) {
      addStem(stemsOrigin[0], stemsOrigin[1], stemsOrigin[2], f.pos[0], f.pos[1] - 0.04, f.pos[2]);
      addTulip(f.pos[0], f.pos[1], f.pos[2], f.dir[0], f.dir[1], f.dir[2], f.s);
    }

    // Baby's breath tufts around the rim
    const babySpots = [
      [-0.5, 0.22, 0.16],
      [0.5, 0.21, 0.15],
      [0.24, 0.24, 0.38],
      [-0.28, 0.24, -0.38],
      [0.28, 0.25, -0.38],
      [0.0, 0.52, -0.38],
      [-0.18, 0.6, 0.26],
      [0.2, 0.58, 0.26],
    ];
    for (const [bx, by, bz] of babySpots) {
      addBabysBreath(bx, by, bz);
    }

    // Leaves poking over the paper rim
    const leaves = [
      [[-0.36, 0.14, 0.22], [-0.85, 0.45, 0.35], 0.26],
      [[0.36, 0.14, 0.2], [0.85, 0.45, 0.32], 0.26],
      [[0.0, 0.12, 0.38], [0.05, 0.4, 0.9], 0.25],
      [[-0.3, 0.16, -0.28], [-0.7, 0.5, -0.6], 0.25],
      [[0.3, 0.16, -0.28], [0.7, 0.5, -0.6], 0.25],
      [[0.0, 0.18, -0.38], [0.0, 0.55, -0.82], 0.24],
    ];
    for (const [orig, dir, sc] of leaves) {
      addLeaf(orig[0], orig[1], orig[2], dir[0], dir[1], dir[2], sc);
    }

    return pts;
  }

  // Ambient sparkles and floating hearts
  class ParticleSystem {
    constructor() {
      this.items = [];
      for (let i = 0; i < 24; i++) {
        this.items.push(this.makeAmbient(true));
      }
    }

    makeAmbient(randomY = false) {
      const isHeart = Math.random() < 0.36;
      const angle = Math.random() * Math.PI * 2;
      const radius = 0.55 + Math.random() * 0.65;
      return {
        x: Math.cos(angle) * radius,
        y: randomY ? -0.9 + Math.random() * 1.9 : -0.95,
        z: Math.sin(angle) * radius,
        vy: 0.14 + Math.random() * 0.18,
        vx: (Math.random() - 0.5) * 0.06,
        phase: Math.random() * Math.PI * 2,
        ch: isHeart ? (Math.random() < 0.65 ? "♥" : "♡") : (Math.random() < 0.5 ? "✦" : "*"),
        role: isHeart ? "heart" : "sparkle",
        life: 1,
        decay: 0,
      };
    }

    spawnBurst(wx, wy, wz) {
      for (let i = 0; i < 12; i++) {
        const a = (i / 12) * Math.PI * 2 + Math.random() * 0.3;
        const speed = 0.35 + Math.random() * 0.45;
        const isHeart = i % 2 === 0;
        this.items.push({
          x: wx,
          y: wy,
          z: wz,
          vx: Math.cos(a) * speed,
          vy: 0.25 + Math.sin(a) * speed * 0.7,
          phase: Math.random() * Math.PI * 2,
          ch: isHeart ? "♥" : "✦",
          role: isHeart ? "heart" : "sparkle",
          life: 1,
          decay: 0.55 + Math.random() * 0.35,
        });
      }
    }

    update(dt) {
      for (let i = this.items.length - 1; i >= 0; i--) {
        const p = this.items[i];
        p.x += p.vx * dt;
        p.y += p.vy * dt;
        p.phase += dt * 3;
        if (p.decay > 0) {
          p.life -= p.decay * dt;
          if (p.life <= 0) {
            this.items.splice(i, 1);
          }
        } else if (p.y > 1.05) {
          this.items[i] = this.makeAmbient(false);
        }
      }
    }
  }

  // DOM references
  const canvas = document.getElementById("bouquet-canvas");
  const ctx = canvas.getContext("2d", { alpha: true });

  const elKicker = document.getElementById("dedication-kicker");
  const elTitle = document.getElementById("dedication-title");
  const elMsg = document.getElementById("dedication-msg");

  const btnPlay = document.getElementById("btn-play");
  const playIcon = document.getElementById("play-icon");
  const playLabel = document.getElementById("play-label");
  const btnTheme = document.getElementById("btn-theme");
  const themeLabel = document.getElementById("theme-label");
  const btnReset = document.getElementById("btn-reset");
  const btnSave = document.getElementById("btn-save");
  const saveLabel = document.getElementById("save-label");

  // State
  const bouquetPoints = buildBouquetPoints();
  const particles = new ParticleSystem();

  let currentThemeKey = "rose";
  let currentTo = "";
  let currentMsg = "";

  let autoSpin = true;
  let rotY = 0.25;
  let rotX = 0.24;
  let velY = 0;
  let velX = 0;
  let zoom = 1.0;

  let cssW = 0;
  let cssH = 0;
  let cols = 80;
  let rows = 44;
  let cellW = 8;
  let cellH = 14;
  let fontSize = 13;

  let zBuf = new Float32Array(0);
  let chBuf = [];
  let colBuf = [];

  function applyTheme(key) {
    if (!THEMES[key]) key = "rose";
    currentThemeKey = key;
    const t = THEMES[key];
    document.documentElement.style.setProperty("--accent", t.accent);
    document.documentElement.style.setProperty(
      "--bg-glow",
      `radial-gradient(circle at 50% 42%, ${t.glow}, rgba(11, 9, 14, 0) 65%)`
    );
    themeLabel.textContent = t.label;
  }

  const DEFAULT_TO = "pisya";

  function applyDedication(to, msg) {
    currentTo = (to || "").trim();
    currentMsg = (msg || "").trim();
    elKicker.textContent = currentTo ? "coded with love" : "coded for you";

    const label = currentTo ? `for ${currentTo}` : "a digital bouquet";
    const hasHeart = /[♥♡❤]/.test(currentTo);
    elTitle.textContent = label + (hasHeart ? "" : " ");
    if (!hasHeart && typeof document.createElement === "function") {
      const heart = document.createElement("span");
      heart.className = "heart-icon";
      heart.textContent = "♥";
      elTitle.appendChild(heart);
    }

    elMsg.textContent = currentMsg || "drag to spin · tap for sparkles";
  }

  function loadFromURL() {
    const params = new URLSearchParams(window.location.search);
    applyTheme(params.get("theme") || "rose");
    const toParam = params.has("to") ? params.get("to") : DEFAULT_TO;
    applyDedication(toParam, params.get("msg") || "");
  }

  function resize() {
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    cssW = window.innerWidth;
    cssH = window.innerHeight;
    canvas.width = Math.floor(cssW * dpr);
    canvas.height = Math.floor(cssH * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

    // Choose font size so mobile portrait fits ~72-86 columns comfortably
    const minDim = Math.min(cssW, cssH);
    fontSize = Math.max(9, Math.min(15, Math.floor(minDim / 42)));
    ctx.font = `600 ${fontSize}px "JetBrains Mono", "Fira Code", "Cascadia Code", ui-monospace, Menlo, Consolas, monospace`;
    cellW = ctx.measureText("M").width || fontSize * 0.6;
    cellH = Math.round(fontSize * 1.18);

    cols = Math.max(36, Math.floor(cssW / cellW));
    rows = Math.max(24, Math.floor(cssH / cellH));

    const total = cols * rows;
    zBuf = new Float32Array(total);
    chBuf = new Array(total);
    colBuf = new Array(total);
  }

  function colorForRole(role, shade, tint) {
    const t = THEMES[currentThemeKey];
    let rgb;
    switch (role) {
      case "rose":
        rgb = t.rose;
        break;
      case "roseInner":
        rgb = t.roseInner;
        break;
      case "tulip":
        rgb = t.tulip;
        break;
      case "daisy":
        rgb = t.daisy;
        break;
      case "daisyCenter":
        rgb = t.daisyCenter;
        break;
      case "baby":
        rgb = t.baby;
        break;
      case "ribbon":
        rgb = t.ribbon;
        break;
      case "stem":
        rgb = [66, 178, 92];
        break;
      case "leaf":
        rgb = [92, 206, 128];
        break;
      case "paper":
        rgb = [228, 194, 152];
        break;
      case "heart":
        rgb = t.heart;
        break;
      case "sparkle":
        rgb = [255, 240, 200];
        break;
      default:
        rgb = [240, 240, 240];
    }
    const factor = Math.min(1.2, (0.38 + 0.72 * shade) * tint);
    const r = Math.min(255, Math.round(rgb[0] * factor));
    const g = Math.min(255, Math.round(rgb[1] * factor));
    const b = Math.min(255, Math.round(rgb[2] * factor));
    return `rgb(${r},${g},${b})`;
  }

  const MONO_FONT = '"JetBrains Mono", "Fira Code", "Cascadia Code", ui-monospace, Menlo, Consolas, monospace';

  function renderScene(
    targetCtx,
    w,
    h,
    gCols,
    gRows,
    gFontSize,
    gCellW,
    gCellH,
    bufZ,
    bufCh,
    bufCol,
    basePxScale,
    cyRatio
  ) {
    bufZ.fill(-Infinity);
    bufCh.fill(null);

    const cosY = Math.cos(rotY);
    const sinY = Math.sin(rotY);
    const cosX = Math.cos(rotX);
    const sinX = Math.sin(rotX);

    const camDist = 3.1;
    const cx = gCols * 0.5;
    const cy = gRows * cyRatio;

    // Light vector (normalized)
    const lx = 0.42;
    const ly = 0.76;
    const lz = 0.5;

    function projectPoint(px, py, pz, nx, ny, nz, role, fixedCh, tint) {
      const wy = py + 0.14;
      const x1 = px * cosY + pz * sinY;
      const z1 = -px * sinY + pz * cosY;
      const y2 = wy * cosX - z1 * sinX;
      const z2 = wy * sinX + z1 * cosX;

      const depth = camDist - z2;
      if (depth <= 0.35) return;
      const invZ = 1 / depth;

      const sx = Math.round(cx + (x1 * basePxScale * invZ) / gCellW);
      const sy = Math.round(cy - (y2 * basePxScale * invZ) / gCellH);
      if (sx < 0 || sx >= gCols || sy < 0 || sy >= gRows) return;

      const idx = sy * gCols + sx;
      if (invZ <= bufZ[idx]) return;
      bufZ[idx] = invZ;

      const nx1 = nx * cosY + nz * sinY;
      const nz1 = -nx * sinY + nz * cosY;
      const ny2 = ny * cosX - nz1 * sinX;
      const nz2 = ny * sinX + nz1 * cosX;

      const lum = Math.max(0, Math.min(1, nx1 * lx + ny2 * ly + nz2 * lz * 0.85 + 0.22));
      const ch =
        fixedCh ||
        SHADE_CHARS[Math.min(SHADE_CHARS.length - 1, Math.floor(lum * (SHADE_CHARS.length - 0.01)))];

      bufCh[idx] = ch;
      bufCol[idx] = colorForRole(role, lum, tint);
    }

    for (let i = 0; i < bouquetPoints.length; i++) {
      const p = bouquetPoints[i];
      projectPoint(p.x, p.y, p.z, p.nx, p.ny, p.nz, p.role, p.ch, p.tint);
    }

    for (let i = 0; i < particles.items.length; i++) {
      const p = particles.items[i];
      const twinkle = 0.65 + 0.35 * Math.sin(p.phase);
      projectPoint(p.x, p.y, p.z, 0, 0.5, 0.86, p.role, p.ch, twinkle * p.life);
    }

    targetCtx.textBaseline = "top";
    targetCtx.font = `600 ${gFontSize}px ${MONO_FONT}`;

    const offsetX = (w - gCols * gCellW) * 0.5;
    const offsetY = (h - gRows * gCellH) * 0.5;

    let lastColor = "";
    for (let r = 0; r < gRows; r++) {
      const yPos = offsetY + r * gCellH;
      const rowOffset = r * gCols;
      for (let c = 0; c < gCols; c++) {
        const idx = rowOffset + c;
        const ch = bufCh[idx];
        if (!ch) continue;
        const col = bufCol[idx];
        if (col !== lastColor) {
          targetCtx.fillStyle = col;
          lastColor = col;
        }
        targetCtx.fillText(ch, offsetX + c * gCellW, yPos);
      }
    }
  }

  function renderFrame() {
    ctx.clearRect(0, 0, cssW, cssH);
    const basePxScale = Math.min(cssW * 1.84, cssH * 1.03) * zoom;
    renderScene(ctx, cssW, cssH, cols, rows, fontSize, cellW, cellH, zBuf, chBuf, colBuf, basePxScale, 0.53);
  }

  function exportWidgetCard() {
    const size = 1080;
    const out = document.createElement("canvas");
    out.width = size;
    out.height = size;
    const octx = out.getContext("2d");

    // Solid dark backdrop + theme glow
    octx.fillStyle = "#0b090e";
    octx.fillRect(0, 0, size, size);
    const theme = THEMES[currentThemeKey];
    const grad = octx.createRadialGradient(size * 0.5, size * 0.5, 20, size * 0.5, size * 0.5, size * 0.55);
    grad.addColorStop(0, theme.glow);
    grad.addColorStop(1, "rgba(11, 9, 14, 0)");
    octx.fillStyle = grad;
    octx.fillRect(0, 0, size, size);

    // Header text matching live view
    octx.textAlign = "center";
    octx.textBaseline = "top";
    octx.fillStyle = theme.accent;
    octx.font = `600 24px ${MONO_FONT}`;
    octx.fillText((elKicker.textContent || "CODED WITH LOVE").toUpperCase(), size * 0.5, 58);

    const titleBase = currentTo ? `for ${currentTo}` : "a digital bouquet";
    octx.fillStyle = "#f7f2f5";
    octx.font = `600 46px ${MONO_FONT}`;
    octx.fillText(`${titleBase} ♥`, size * 0.5, 96);

    octx.textAlign = "left";
    const expFont = 16;
    octx.font = `600 ${expFont}px ${MONO_FONT}`;
    const expCellW = octx.measureText("M").width || expFont * 0.6;
    const expCellH = Math.round(expFont * 1.18);
    const expCols = Math.floor(size / expCellW);
    const expRows = Math.floor(size / expCellH);
    const total = expCols * expRows;

    renderScene(
      octx,
      size,
      size,
      expCols,
      expRows,
      expFont,
      expCellW,
      expCellH,
      new Float32Array(total),
      new Array(total),
      new Array(total),
      size * 1.12,
      0.57
    );

    return out;
  }

  // Pointer & Touch Gestures (drag to spin, pinch to zoom, tap to sparkle)
  const activePointers = new Map();
  let lastPinchDist = null;
  let dragDistance = 0;
  let downPos = null;

  canvas.addEventListener("pointerdown", (e) => {
    canvas.setPointerCapture(e.pointerId);
    activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
    if (activePointers.size === 1) {
      dragDistance = 0;
      downPos = { x: e.clientX, y: e.clientY };
      velY = 0;
      velX = 0;
    } else if (activePointers.size === 2) {
      const [a, b] = [...activePointers.values()];
      lastPinchDist = Math.hypot(a.x - b.x, a.y - b.y);
    }
  });

  canvas.addEventListener("pointermove", (e) => {
    if (!activePointers.has(e.pointerId)) return;
    const prev = activePointers.get(e.pointerId);
    const dx = e.clientX - prev.x;
    const dy = e.clientY - prev.y;
    activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });

    if (activePointers.size === 1) {
      dragDistance += Math.hypot(dx, dy);
      rotY += dx * 0.0085;
      rotX = Math.max(-0.55, Math.min(0.95, rotX + dy * 0.0065));
      velY = dx * 0.04;
      velX = dy * 0.025;
    } else if (activePointers.size === 2) {
      const [a, b] = [...activePointers.values()];
      const dist = Math.hypot(a.x - b.x, a.y - b.y);
      if (lastPinchDist && dist > 0) {
        zoom = Math.max(0.65, Math.min(1.85, zoom * (dist / lastPinchDist)));
      }
      lastPinchDist = dist;
    }
  });

  function releasePointer(e) {
    if (!activePointers.has(e.pointerId)) return;
    activePointers.delete(e.pointerId);
    if (activePointers.size < 2) lastPinchDist = null;

    if (activePointers.size === 0 && downPos && dragDistance < 10) {
      // Convert tap position roughly to world space for burst
      const nx = ((e.clientX / cssW) - 0.5) * 1.4;
      const ny = (0.5 - (e.clientY / cssH)) * 1.4;
      particles.spawnBurst(nx, ny, 0.2);
    }
  }

  canvas.addEventListener("pointerup", releasePointer);
  canvas.addEventListener("pointercancel", releasePointer);

  canvas.addEventListener(
    "wheel",
    (e) => {
      e.preventDefault();
      const delta = e.deltaY > 0 ? 0.92 : 1.08;
      zoom = Math.max(0.65, Math.min(1.85, zoom * delta));
    },
    { passive: false }
  );

  // Control buttons
  btnPlay.addEventListener("click", () => {
    autoSpin = !autoSpin;
    btnPlay.setAttribute("aria-pressed", String(autoSpin));
    playIcon.textContent = autoSpin ? "❚❚" : "▶";
    playLabel.textContent = autoSpin ? "Spin" : "Paused";
  });

  btnTheme.addEventListener("click", () => {
    const idx = THEME_ORDER.indexOf(currentThemeKey);
    const next = THEME_ORDER[(idx + 1) % THEME_ORDER.length];
    applyTheme(next);
  });

  btnReset.addEventListener("click", () => {
    rotY = 0.25;
    rotX = 0.24;
    velY = 0;
    velX = 0;
    zoom = 1.0;
  });

  btnSave.addEventListener("click", () => {
    const out = exportWidgetCard();
    if (!out || typeof out.toBlob !== "function") return;
    out.toBlob(async (blob) => {
      if (!blob) return;
      const file = typeof File === "function"
        ? new File([blob], "for-pisya-bouquet.png", { type: "image/png" })
        : null;

      if (file && navigator.canShare && navigator.canShare({ files: [file] })) {
        try {
          await navigator.share({ files: [file], title: "for pisya ♥" });
          saveLabel.textContent = "Saved!";
          setTimeout(() => { saveLabel.textContent = "Save"; }, 1600);
          return;
        } catch (err) {
          if (err && err.name === "AbortError") return;
        }
      }

      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "for-pisya-bouquet.png";
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      saveLabel.textContent = "Saved!";
      setTimeout(() => { saveLabel.textContent = "Save"; }, 1600);
    }, "image/png");
  });

  // Animation loop
  let lastTime = performance.now();
  function tick(now) {
    const dt = Math.min(0.05, (now - lastTime) / 1000);
    lastTime = now;

    if (activePointers.size === 0) {
      if (autoSpin) {
        rotY += dt * 0.55;
      }
      rotY += velY * dt;
      rotX = Math.max(-0.55, Math.min(0.95, rotX + velX * dt));
      velY *= Math.pow(0.04, dt);
      velX *= Math.pow(0.04, dt);
    }

    particles.update(dt);
    renderFrame();
    requestAnimationFrame(tick);
  }

  window.addEventListener("resize", resize);
  loadFromURL();
  resize();
  requestAnimationFrame(tick);

  if ("serviceWorker" in navigator && window.location.protocol.startsWith("http")) {
    const registerSW = () => navigator.serviceWorker.register("./sw.js").catch(() => {});
    if (document.readyState === "complete") {
      registerSW();
    } else {
      window.addEventListener("load", registerSW);
    }
  }
})();
