using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AsciiBouquetWidget
{
    internal sealed class BouquetPoint
    {
        public float X, Y, Z;
        public float Nx, Ny, Nz;
        public byte Role;
        public char FixedCh;
        public float Tint;
    }

    internal sealed class Particle
    {
        public float X, Y, Z;
        public float Vx, Vy;
        public float Phase;
        public char Ch;
        public byte Role;
        public float Life;
        public float Decay;
    }

    internal sealed class ThemePalette
    {
        public string Name;
        public Color Accent;
        public Color Glow;
        public int[] Rose, RoseInner, Tulip, Daisy, DaisyCenter, Baby, Ribbon, Heart;
    }

    public sealed class WidgetForm : Form
    {
        private const byte ROLE_ROSE = 0;
        private const byte ROLE_ROSE_INNER = 1;
        private const byte ROLE_TULIP = 2;
        private const byte ROLE_DAISY = 3;
        private const byte ROLE_DAISY_CENTER = 4;
        private const byte ROLE_BABY = 5;
        private const byte ROLE_RIBBON = 6;
        private const byte ROLE_STEM = 7;
        private const byte ROLE_LEAF = 8;
        private const byte ROLE_PAPER = 9;
        private const byte ROLE_HEART = 10;
        private const byte ROLE_SPARKLE = 11;

        private const string SHADE_CHARS = ".,-~:;=!*#$@";
        private static readonly string[] SINGLE_CHAR_CACHE = new string[128];

        private static readonly ThemePalette[] THEMES = new ThemePalette[]
        {
            new ThemePalette {
                Name = "Rose",
                Accent = Color.FromArgb(255, 92, 138),
                Glow = Color.FromArgb(42, 255, 88, 130),
                Rose = new[] {255, 77, 118}, RoseInner = new[] {255, 165, 190},
                Tulip = new[] {236, 112, 162}, Daisy = new[] {255, 246, 240},
                DaisyCenter = new[] {255, 206, 75}, Baby = new[] {255, 232, 242},
                Ribbon = new[] {230, 48, 84}, Heart = new[] {255, 92, 138}
            },
            new ThemePalette {
                Name = "Sunset",
                Accent = Color.FromArgb(255, 140, 90),
                Glow = Color.FromArgb(42, 255, 140, 90),
                Rose = new[] {255, 107, 74}, RoseInner = new[] {255, 194, 136},
                Tulip = new[] {255, 150, 95}, Daisy = new[] {255, 245, 224},
                DaisyCenter = new[] {244, 162, 54}, Baby = new[] {255, 234, 210},
                Ribbon = new[] {217, 68, 54}, Heart = new[] {255, 130, 92}
            },
            new ThemePalette {
                Name = "Lavender",
                Accent = Color.FromArgb(199, 141, 255),
                Glow = Color.FromArgb(42, 199, 141, 255),
                Rose = new[] {186, 115, 255}, RoseInner = new[] {232, 195, 255},
                Tulip = new[] {240, 148, 222}, Daisy = new[] {250, 245, 255},
                DaisyCenter = new[] {255, 214, 102}, Baby = new[] {238, 224, 255},
                Ribbon = new[] {148, 76, 228}, Heart = new[] {212, 150, 255}
            },
            new ThemePalette {
                Name = "Midnight",
                Accent = Color.FromArgb(110, 198, 255),
                Glow = Color.FromArgb(42, 110, 198, 255),
                Rose = new[] {82, 165, 255}, RoseInner = new[] {185, 226, 255},
                Tulip = new[] {130, 145, 255}, Daisy = new[] {238, 248, 255},
                DaisyCenter = new[] {255, 215, 110}, Baby = new[] {210, 238, 255},
                Ribbon = new[] {64, 128, 235}, Heart = new[] {120, 200, 255}
            }
        };

        private readonly List<BouquetPoint> _points;
        private readonly List<Particle> _particles = new List<Particle>(40);
        private readonly Random _rng = new Random();
        private readonly Timer _timer;
        private readonly Dictionary<int, SolidBrush> _brushCache = new Dictionary<int, SolidBrush>(256);

        private Font _asciiFont;
        private Font _kickerFont;
        private Font _titleFont;
        private Font _hintFont;
        private StringFormat _centerFormat;
        private StringFormat _charFormat;

        private float[] _zBuf = new float[0];
        private char[] _chBuf = new char[0];
        private int[] _colBuf = new int[0];
        private int _cols, _rows;
        private float _cellW, _cellH;

        private int _themeIdx = 0;
        private bool _autoSpin = true;
        private float _rotY = 0.25f;
        private float _rotX = 0.24f;
        private float _velY = 0f;
        private float _velX = 0f;
        private float _zoom = 1.0f;
        private long _lastTicks;

        private bool _draggingBouquet;
        private bool _movingWindow;
        private Point _lastMouse;
        private Point _windowDragOffset;
        private float _dragDist;

        private MenuItem _spinItem;
        private MenuItem _topMostItem;

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int wEllipse, int hEllipse);

        [DllImport("Gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public WidgetForm()
        {
            Text = "for pisya \u2665";
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(11, 9, 14);
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            ShowInTaskbar = false;

            _points = BuildBouquetPoints();
            for (int i = 0; i < 20; i++) _particles.Add(MakeAmbientParticle(true));

            _kickerFont = new Font("Consolas", 8f, FontStyle.Bold);
            _titleFont = new Font("Consolas", 13.5f, FontStyle.Bold);
            _hintFont = new Font("Consolas", 7.5f, FontStyle.Regular);
            _centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
            _charFormat = new StringFormat(StringFormat.GenericTypographic);

            SetWidgetSize(300, 360);
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(work.Right - Width - 22, work.Bottom - Height - 22);

            BuildContextMenu();

            _lastTicks = Environment.TickCount;
            _timer = new Timer { Interval = 42 }; // ~24 FPS keeps CPU & RAM very low
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void SetWidgetSize(int w, int h)
        {
            Size = new Size(w, h);
            IntPtr hrgn = CreateRoundRectRgn(0, 0, w, h, 28, 28);
            Region oldReg = Region;
            Region = Region.FromHrgn(hrgn);
            if (oldReg != null) oldReg.Dispose();
            if (hrgn != IntPtr.Zero) DeleteObject(hrgn);

            float fontPt = Math.Max(6.5f, Math.Min(9.5f, w / 41f));
            if (_asciiFont != null) _asciiFont.Dispose();
            _asciiFont = new Font("Consolas", fontPt, FontStyle.Bold);

            using (Graphics g = CreateGraphics())
            {
                SizeF sz = g.MeasureString("M", _asciiFont, 100, _charFormat);
                _cellW = Math.Max(5f, sz.Width * 0.92f);
                _cellH = Math.Max(8f, sz.Height * 0.92f);
            }

            _cols = Math.Max(28, (int)((w - 16) / _cellW));
            _rows = Math.Max(20, (int)((h - 64) / _cellH));
            int total = _cols * _rows;
            _zBuf = new float[total];
            _chBuf = new char[total];
            _colBuf = new int[total];
        }

        private void BuildContextMenu()
        {
            ContextMenu menu = new ContextMenu();
            _spinItem = new MenuItem("Pause Spin", (s, e) => {
                _autoSpin = !_autoSpin;
                _spinItem.Text = _autoSpin ? "Pause Spin" : "Resume Spin";
            });
            menu.MenuItems.Add(_spinItem);

            MenuItem themesMenu = new MenuItem("Palette");
            for (int i = 0; i < THEMES.Length; i++)
            {
                int idx = i;
                themesMenu.MenuItems.Add(new MenuItem(THEMES[i].Name, (s, e) => {
                    _themeIdx = idx;
                    Invalidate();
                }));
            }
            menu.MenuItems.Add(themesMenu);

            MenuItem sizeMenu = new MenuItem("Widget Size");
            sizeMenu.MenuItems.Add(new MenuItem("Small (240x290)", (s, e) => SetWidgetSize(240, 290)));
            sizeMenu.MenuItems.Add(new MenuItem("Medium (300x360)", (s, e) => SetWidgetSize(300, 360)));
            sizeMenu.MenuItems.Add(new MenuItem("Large (380x450)", (s, e) => SetWidgetSize(380, 450)));
            menu.MenuItems.Add(sizeMenu);

            _topMostItem = new MenuItem("Always on Top", (s, e) => {
                TopMost = !TopMost;
                _topMostItem.Checked = TopMost;
            });
            menu.MenuItems.Add(_topMostItem);

            menu.MenuItems.Add(new MenuItem("Reset View", (s, e) => {
                _rotY = 0.25f;
                _rotX = 0.24f;
                _velY = 0f;
                _velX = 0f;
                _zoom = 1.0f;
            }));
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(new MenuItem("Close Widget", (s, e) => Close()));

            ContextMenu = menu;
        }

        private Particle MakeAmbientParticle(bool randomY)
        {
            bool isHeart = _rng.NextDouble() < 0.38;
            double angle = _rng.NextDouble() * Math.PI * 2.0;
            double radius = 0.55 + _rng.NextDouble() * 0.62;
            return new Particle
            {
                X = (float)(Math.Cos(angle) * radius),
                Y = randomY ? (float)(-0.9 + _rng.NextDouble() * 1.9) : -0.95f,
                Z = (float)(Math.Sin(angle) * radius),
                Vy = (float)(0.14 + _rng.NextDouble() * 0.16),
                Vx = (float)((_rng.NextDouble() - 0.5) * 0.06),
                Phase = (float)(_rng.NextDouble() * Math.PI * 2.0),
                Ch = isHeart ? (_rng.NextDouble() < 0.65 ? '\u2665' : '\u2661') : (_rng.NextDouble() < 0.5 ? '*' : '+'),
                Role = isHeart ? ROLE_HEART : ROLE_SPARKLE,
                Life = 1f,
                Decay = 0f
            };
        }

        private void SpawnBurst(float wx, float wy, float wz)
        {
            for (int i = 0; i < 10; i++)
            {
                double a = (i / 10.0) * Math.PI * 2.0 + _rng.NextDouble() * 0.3;
                double speed = 0.35 + _rng.NextDouble() * 0.4;
                bool isHeart = (i % 2) == 0;
                _particles.Add(new Particle
                {
                    X = wx, Y = wy, Z = wz,
                    Vx = (float)(Math.Cos(a) * speed),
                    Vy = (float)(0.24 + Math.Sin(a) * speed * 0.7),
                    Phase = (float)(_rng.NextDouble() * Math.PI * 2.0),
                    Ch = isHeart ? '\u2665' : '*',
                    Role = isHeart ? ROLE_HEART : ROLE_SPARKLE,
                    Life = 1f,
                    Decay = (float)(0.55 + _rng.NextDouble() * 0.35)
                });
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) return;

            long now = Environment.TickCount;
            float dt = Math.Min(0.06f, Math.Max(0.01f, (now - _lastTicks) / 1000f));
            _lastTicks = now;

            if (!_draggingBouquet)
            {
                if (_autoSpin) _rotY += dt * 0.55f;
                _rotY += _velY * dt;
                _rotX = Math.Max(-0.55f, Math.Min(0.95f, _rotX + _velX * dt));
                float damp = (float)Math.Pow(0.04, dt);
                _velY *= damp;
                _velX *= damp;
            }

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                Particle p = _particles[i];
                p.X += p.Vx * dt;
                p.Y += p.Vy * dt;
                p.Phase += dt * 3f;
                if (p.Decay > 0f)
                {
                    p.Life -= p.Decay * dt;
                    if (p.Life <= 0f) _particles.RemoveAt(i);
                }
                else if (p.Y > 1.05f)
                {
                    _particles[i] = MakeAmbientParticle(false);
                }
            }

            Invalidate();
        }

        private SolidBrush GetCachedBrush(int argb)
        {
            SolidBrush b;
            if (!_brushCache.TryGetValue(argb, out b))
            {
                if (_brushCache.Count > 450)
                {
                    foreach (SolidBrush old in _brushCache.Values) old.Dispose();
                    _brushCache.Clear();
                }
                b = new SolidBrush(Color.FromArgb(argb));
                _brushCache[argb] = b;
            }
            return b;
        }

        private int ColorForRole(byte role, float shade, float tint)
        {
            ThemePalette t = THEMES[_themeIdx];
            int r0 = 240, g0 = 240, b0 = 240;
            switch (role)
            {
                case ROLE_ROSE: r0 = t.Rose[0]; g0 = t.Rose[1]; b0 = t.Rose[2]; break;
                case ROLE_ROSE_INNER: r0 = t.RoseInner[0]; g0 = t.RoseInner[1]; b0 = t.RoseInner[2]; break;
                case ROLE_TULIP: r0 = t.Tulip[0]; g0 = t.Tulip[1]; b0 = t.Tulip[2]; break;
                case ROLE_DAISY: r0 = t.Daisy[0]; g0 = t.Daisy[1]; b0 = t.Daisy[2]; break;
                case ROLE_DAISY_CENTER: r0 = t.DaisyCenter[0]; g0 = t.DaisyCenter[1]; b0 = t.DaisyCenter[2]; break;
                case ROLE_BABY: r0 = t.Baby[0]; g0 = t.Baby[1]; b0 = t.Baby[2]; break;
                case ROLE_RIBBON: r0 = t.Ribbon[0]; g0 = t.Ribbon[1]; b0 = t.Ribbon[2]; break;
                case ROLE_STEM: r0 = 66; g0 = 178; b0 = 92; break;
                case ROLE_LEAF: r0 = 92; g0 = 206; b0 = 128; break;
                case ROLE_PAPER: r0 = 228; g0 = 194; b0 = 152; break;
                case ROLE_HEART: r0 = t.Heart[0]; g0 = t.Heart[1]; b0 = t.Heart[2]; break;
                case ROLE_SPARKLE: r0 = 255; g0 = 240; b0 = 200; break;
            }
            float factor = Math.Min(1.2f, (0.38f + 0.72f * shade) * tint);
            // Quantize to multiples of 8 so the SolidBrush cache stays tiny
            int r = (Math.Min(255, (int)(r0 * factor)) & 0xF8);
            int g = (Math.Min(255, (int)(g0 * factor)) & 0xF8);
            int b = (Math.Min(255, (int)(b0 * factor)) & 0xF8);
            return unchecked((int)(0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | (uint)b));
        }

        private static string CharToString(char ch)
        {
            if (ch < 128)
            {
                string s = SINGLE_CHAR_CACHE[ch];
                if (s == null)
                {
                    s = ch.ToString();
                    SINGLE_CHAR_CACHE[ch] = s;
                }
                return s;
            }
            if (ch == '\u2665') return "\u2665";
            if (ch == '\u2661') return "\u2661";
            return ch.ToString();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            ThemePalette theme = THEMES[_themeIdx];

            // Subtle radial glow behind bouquet
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(20, 40, Width - 40, Height - 60);
                using (PathGradientBrush pgb = new PathGradientBrush(gp))
                {
                    pgb.CenterColor = theme.Glow;
                    pgb.SurroundColors = new[] { Color.FromArgb(0, 11, 9, 14) };
                    g.FillEllipse(pgb, 20, 40, Width - 40, Height - 60);
                }
            }

            // Subtle card border
            using (Pen borderPen = new Pen(Color.FromArgb(32, 255, 255, 255), 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            // Header (drag this area to move the widget on desktop)
            using (SolidBrush kickerBrush = new SolidBrush(theme.Accent))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(247, 242, 245)))
            using (SolidBrush hintBrush = new SolidBrush(Color.FromArgb(120, 247, 242, 245)))
            {
                g.DrawString("CODED WITH LOVE", _kickerFont, kickerBrush, Width * 0.5f, 14f, _centerFormat);
                SizeF nameSz = g.MeasureString("for pisya ", _titleFont, 400, _charFormat);
                SizeF heartSz = g.MeasureString("\u2665", _titleFont, 100, _charFormat);
                float totalTitleW = nameSz.Width + heartSz.Width;
                float startX = (Width - totalTitleW) * 0.5f;
                g.DrawString("for pisya ", _titleFont, titleBrush, startX, 27f, _charFormat);
                g.DrawString("\u2665", _titleFont, kickerBrush, startX + nameSz.Width, 27f, _charFormat);
                g.DrawString("drag header to move · right-click menu", _hintFont, hintBrush, Width * 0.5f, Height - 18f, _centerFormat);
            }

            // Project 3D bouquet into ASCII z-buffer
            int total = _cols * _rows;
            for (int i = 0; i < total; i++)
            {
                _zBuf[i] = float.NegativeInfinity;
                _chBuf[i] = '\0';
            }

            float cosY = (float)Math.Cos(_rotY);
            float sinY = (float)Math.Sin(_rotY);
            float cosX = (float)Math.Cos(_rotX);
            float sinX = (float)Math.Sin(_rotX);

            const float camDist = 3.1f;
            float basePxScale = Math.Min((Width - 16) * 1.84f, (Height - 64) * 1.18f) * _zoom;
            float cx = _cols * 0.5f;
            float cy = _rows * 0.54f;

            const float lx = 0.42f, ly = 0.76f, lz = 0.50f;

            for (int i = 0; i < _points.Count; i++)
            {
                BouquetPoint p = _points[i];
                ProjectPoint(p.X, p.Y, p.Z, p.Nx, p.Ny, p.Nz, p.Role, p.FixedCh, p.Tint,
                    cosY, sinY, cosX, sinX, camDist, basePxScale, cx, cy, lx, ly, lz);
            }

            for (int i = 0; i < _particles.Count; i++)
            {
                Particle p = _particles[i];
                float twinkle = 0.65f + 0.35f * (float)Math.Sin(p.Phase);
                ProjectPoint(p.X, p.Y, p.Z, 0f, 0.5f, 0.86f, p.Role, p.Ch, twinkle * p.Life,
                    cosY, sinY, cosX, sinX, camDist, basePxScale, cx, cy, lx, ly, lz);
            }

            float offsetX = (Width - _cols * _cellW) * 0.5f;
            float offsetY = 52f + ((Height - 72f) - _rows * _cellH) * 0.5f;

            for (int r = 0; r < _rows; r++)
            {
                float yPos = offsetY + r * _cellH;
                int rowOffset = r * _cols;
                for (int c = 0; c < _cols; c++)
                {
                    int idx = rowOffset + c;
                    char ch = _chBuf[idx];
                    if (ch == '\0') continue;
                    SolidBrush brush = GetCachedBrush(_colBuf[idx]);
                    g.DrawString(CharToString(ch), _asciiFont, brush, offsetX + c * _cellW, yPos, _charFormat);
                }
            }
        }

        private void ProjectPoint(
            float px, float py, float pz, float nx, float ny, float nz,
            byte role, char fixedCh, float tint,
            float cosY, float sinY, float cosX, float sinX,
            float camDist, float basePxScale, float cx, float cy,
            float lx, float ly, float lz)
        {
            float wy = py + 0.14f;
            float x1 = px * cosY + pz * sinY;
            float z1 = -px * sinY + pz * cosY;
            float y2 = wy * cosX - z1 * sinX;
            float z2 = wy * sinX + z1 * cosX;

            float depth = camDist - z2;
            if (depth <= 0.35f) return;
            float invZ = 1f / depth;

            int sx = (int)Math.Round(cx + (x1 * basePxScale * invZ) / _cellW);
            int sy = (int)Math.Round(cy - (y2 * basePxScale * invZ) / _cellH);
            if (sx < 0 || sx >= _cols || sy < 0 || sy >= _rows) return;

            int idx = sy * _cols + sx;
            if (invZ <= _zBuf[idx]) return;
            _zBuf[idx] = invZ;

            float nx1 = nx * cosY + nz * sinY;
            float nz1 = -nx * sinY + nz * cosY;
            float ny2 = ny * cosX - nz1 * sinX;
            float nz2 = ny * sinX + nz1 * cosX;

            float lum = Math.Max(0f, Math.Min(1f, nx1 * lx + ny2 * ly + nz2 * lz * 0.85f + 0.22f));
            char ch = fixedCh != '\0'
                ? fixedCh
                : SHADE_CHARS[Math.Min(SHADE_CHARS.Length - 1, (int)(lum * (SHADE_CHARS.Length - 0.01f)))];

            _chBuf[idx] = ch;
            _colBuf[idx] = ColorForRole(role, lum, tint);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            _lastMouse = e.Location;
            _dragDist = 0f;

            // Top header region moves the floating widget; bouquet region spins 3D
            if (e.Y <= 52)
            {
                _movingWindow = true;
                _windowDragOffset = e.Location;
            }
            else
            {
                _draggingBouquet = true;
                _velY = 0f;
                _velX = 0f;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_movingWindow)
            {
                Location = new Point(Location.X + e.X - _windowDragOffset.X, Location.Y + e.Y - _windowDragOffset.Y);
            }
            else if (_draggingBouquet)
            {
                int dx = e.X - _lastMouse.X;
                int dy = e.Y - _lastMouse.Y;
                _lastMouse = e.Location;
                _dragDist += (float)Math.Sqrt(dx * dx + dy * dy);
                _rotY += dx * 0.01f;
                _rotX = Math.Max(-0.55f, Math.Min(0.95f, _rotX + dy * 0.008f));
                _velY = dx * 0.04f;
                _velX = dy * 0.025f;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_draggingBouquet && _dragDist < 8f)
            {
                float nx = ((float)e.X / Width - 0.5f) * 1.4f;
                float ny = (0.5f - (float)e.Y / Height) * 1.4f;
                SpawnBurst(nx, ny, 0.2f);
            }
            _draggingBouquet = false;
            _movingWindow = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            float factor = e.Delta > 0 ? 1.08f : 0.92f;
            _zoom = Math.Max(0.65f, Math.Min(1.85f, _zoom * factor));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null) _timer.Dispose();
                if (_asciiFont != null) _asciiFont.Dispose();
                if (_kickerFont != null) _kickerFont.Dispose();
                if (_titleFont != null) _titleFont.Dispose();
                if (_hintFont != null) _hintFont.Dispose();
                if (_centerFormat != null) _centerFormat.Dispose();
                if (_charFormat != null) _charFormat.Dispose();
                foreach (SolidBrush b in _brushCache.Values) b.Dispose();
                _brushCache.Clear();
            }
            base.Dispose(disposing);
        }

        // --- 3D Parametric Bouquet Geometry ---
        private static void Norm3(float x, float y, float z, out float nx, out float ny, out float nz)
        {
            float len = (float)Math.Sqrt(x * x + y * y + z * z);
            if (len < 1e-6f) len = 1f;
            nx = x / len; ny = y / len; nz = z / len;
        }

        private struct Basis
        {
            public float Fx, Fy, Fz, Rx, Ry, Rz, Ux, Uy, Uz;
        }

        private static Basis BasisFromDir(float dx, float dy, float dz)
        {
            float fx, fy, fz;
            Norm3(dx, dy, dz, out fx, out fy, out fz);
            float upX = Math.Abs(fy) > 0.92f ? 1f : 0f;
            float upY = Math.Abs(fy) > 0.92f ? 0f : 1f;
            float rx, ry, rz;
            Norm3(upY * fz, -upX * fz, upX * fy - upY * fx, out rx, out ry, out rz);
            return new Basis
            {
                Fx = fx, Fy = fy, Fz = fz,
                Rx = rx, Ry = ry, Rz = rz,
                Ux = fy * rz - fz * ry,
                Uy = fz * rx - fx * rz,
                Uz = fx * ry - fy * rx
            };
        }

        private static List<BouquetPoint> BuildBouquetPoints()
        {
            List<BouquetPoint> pts = new List<BouquetPoint>(7500);
            Action<float, float, float, float, float, float, byte, char, float> push =
                (x, y, z, nx, ny, nz, role, ch, tint) =>
                {
                    float nnx, nny, nnz;
                    Norm3(nx, ny, nz, out nnx, out nny, out nnz);
                    pts.Add(new BouquetPoint { X = x, Y = y, Z = z, Nx = nnx, Ny = nny, Nz = nnz, Role = role, FixedCh = ch, Tint = tint });
                };

            // 1. Paper cone wrap
            const int coneY = 30, coneA = 48;
            for (int i = 0; i <= coneY; i++)
            {
                float t = (float)i / coneY;
                float y = -1.05f + t * 1.23f;
                float baseR = 0.06f + (float)Math.Pow(t, 0.92) * 0.54f;
                for (int j = 0; j < coneA; j++)
                {
                    float a = (float)(j / (double)coneA * Math.PI * 2.0);
                    float topWave = t > 0.7f ? (float)Math.Sin(a * 5 + 0.4) * 0.08f * ((t - 0.7f) / 0.3f) : 0f;
                    float fold = (float)Math.Sin(a * 7) * 0.015f * t;
                    float r = baseR + fold;
                    float backLift = Math.Sin(a) < 0 ? (float)(-Math.Sin(a) * 0.12 * t) : 0f;
                    char ch = i == coneY ? '~' : (j % 8 == 0 ? '|' : '\0');
                    push((float)Math.Cos(a) * r, y + topWave + backLift, (float)Math.Sin(a) * r,
                        (float)Math.Cos(a), 0.35f, (float)Math.Sin(a), ROLE_PAPER, ch, 0.85f + 0.15f * (float)Math.Cos(a * 3));
                }
            }

            // 2. Ribbon band, bow, tails
            const float waistY = -0.46f, waistR = 0.345f;
            for (int k = -2; k <= 2; k++)
            {
                float ry = waistY + k * 0.022f;
                for (int j = 0; j < 56; j++)
                {
                    float a = (float)(j / 56.0 * Math.PI * 2.0);
                    push((float)Math.Cos(a) * waistR, ry, (float)Math.Sin(a) * waistR,
                        (float)Math.Cos(a), 0.1f, (float)Math.Sin(a), ROLE_RIBBON, k == 0 ? '=' : '-', 1f);
                }
            }
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i <= 22; i++)
                {
                    float u = (float)(i / 22.0 * Math.PI * 2.0);
                    float lx = side * (0.04f + 0.16f * (float)Math.Abs(Math.Sin(u * 0.5)));
                    float ly = waistY + 0.055f * (float)Math.Sin(u);
                    float lz = waistR + 0.03f + 0.04f * (float)Math.Sin(u * 0.5);
                    for (int w = -1; w <= 1; w++)
                        push(lx, ly + w * 0.016f, lz, side * 0.3f, 0.4f, 0.9f, ROLE_RIBBON, '@', 1.05f);
                }
                for (int i = 0; i <= 16; i++)
                {
                    float t = i / 16f;
                    push(side * (0.03f + t * 0.13f + (float)Math.Sin(t * 4) * 0.015f),
                        waistY - 0.02f - t * 0.34f,
                        waistR + 0.02f + (float)Math.Sin(t * 3) * 0.02f,
                        0f, 0.2f, 1f, ROLE_RIBBON, side > 0 ? '\\' : '/', 0.95f);
                }
            }

            Action<float, float, float, float, float, float> addStem = (x1, y1, z1, x2, y2, z2) =>
            {
                for (int i = 0; i <= 20; i++)
                {
                    float t = i / 20f;
                    float bend = (float)Math.Sin(t * Math.PI) * 0.04f;
                    push(x1 * (1 - t) + x2 * t + bend, y1 * (1 - t) + y2 * t, z1 * (1 - t) + z2 * t + bend * 0.5f,
                        0.4f, 0.5f, 0.7f, ROLE_STEM, '|', 0.95f);
                }
            };

            Action<float, float, float, float, float, float, float> addRose = (ox, oy, oz, dx, dy, dz, scale) =>
            {
                Basis b = BasisFromDir(dx, dy, dz);
                for (int p = 0; p < 7; p++)
                {
                    float baseA = p * 2.39996f;
                    float layer = 0.35f + (p / 7f) * 0.65f;
                    for (int r = 0; r <= 8; r++)
                    {
                        float vr = r / 8f;
                        for (int c = -4; c <= 4; c++)
                        {
                            float vc = c / 4f;
                            float theta = baseA + vc * 0.65f;
                            float rad = (0.04f + vr * 0.23f * layer) * scale / 0.25f;
                            float cup = ((float)Math.Pow(vr, 1.35) * 0.22f - Math.Abs(vc) * 0.03f) * scale / 0.25f;
                            float lx = (float)Math.Cos(theta) * rad, ly = (float)Math.Sin(theta) * rad, lz = cup;
                            push(
                                ox + lx * b.Rx + ly * b.Ux + lz * b.Fx,
                                oy + lx * b.Ry + ly * b.Uy + lz * b.Fy,
                                oz + lx * b.Rz + ly * b.Uz + lz * b.Fz,
                                (float)Math.Cos(theta) * 0.6f * b.Rx + (float)Math.Sin(theta) * 0.6f * b.Ux + 0.7f * b.Fx,
                                (float)Math.Cos(theta) * 0.6f * b.Ry + (float)Math.Sin(theta) * 0.6f * b.Uy + 0.7f * b.Fy,
                                (float)Math.Cos(theta) * 0.6f * b.Rz + (float)Math.Sin(theta) * 0.6f * b.Uz + 0.7f * b.Fz,
                                layer < 0.6f ? ROLE_ROSE_INNER : ROLE_ROSE, '\0', 0.85f + vr * 0.25f);
                        }
                    }
                }
            };

            Action<float, float, float, float, float, float, float> addDaisy = (ox, oy, oz, dx, dy, dz, scale) =>
            {
                Basis b = BasisFromDir(dx, dy, dz);
                for (int i = 0; i < 32; i++)
                {
                    float a = i * 2.39996f;
                    float r = (float)Math.Sqrt(i / 32.0) * 0.095f * (scale / 0.24f);
                    float lz = 0.035f * (1f - (i / 32f) * 0.6f);
                    float lx = (float)Math.Cos(a) * r, ly = (float)Math.Sin(a) * r;
                    push(ox + lx * b.Rx + ly * b.Ux + lz * b.Fx, oy + lx * b.Ry + ly * b.Uy + lz * b.Fy, oz + lx * b.Rz + ly * b.Uz + lz * b.Fz,
                        b.Fx, b.Fy, b.Fz, ROLE_DAISY_CENTER, (i % 2 == 0) ? 'o' : '*', 1.05f);
                }
                for (int p = 0; p < 14; p++)
                {
                    float ang = (float)(p / 14.0 * Math.PI * 2.0);
                    float cosA = (float)Math.Cos(ang), sinA = (float)Math.Sin(ang);
                    for (int r = 2; r <= 9; r++)
                    {
                        float t = r / 9f;
                        float dist = (0.08f + t * 0.22f) * (scale / 0.24f);
                        float halfW = (float)Math.Sin(t * Math.PI) * 0.032f * (scale / 0.24f);
                        float curlZ = (float)Math.Sin(t * Math.PI) * 0.025f;
                        for (int w = -1; w <= 1; w++)
                        {
                            float lx = cosA * dist - sinA * (w * halfW);
                            float ly = sinA * dist + cosA * (w * halfW);
                            push(ox + lx * b.Rx + ly * b.Ux + curlZ * b.Fx, oy + lx * b.Ry + ly * b.Uy + curlZ * b.Fy, oz + lx * b.Rz + ly * b.Uz + curlZ * b.Fz,
                                b.Fx, b.Fy, b.Fz, ROLE_DAISY, t > 0.75f ? '*' : '\0', 0.95f + t * 0.1f);
                        }
                    }
                }
            };

            Action<float, float, float, float, float, float, float> addTulip = (ox, oy, oz, dx, dy, dz, scale) =>
            {
                Basis b = BasisFromDir(dx, dy, dz);
                for (int p = 0; p < 6; p++)
                {
                    float baseA = (float)(p / 6.0 * Math.PI * 2.0);
                    for (int h = 0; h <= 10; h++)
                    {
                        float t = h / 10f;
                        float r = (float)Math.Sin(t * Math.PI * 0.78) * 0.14f * (scale / 0.23f);
                        float lz = t * 0.28f * (scale / 0.23f);
                        for (int w = -2; w <= 2; w++)
                        {
                            float a = baseA + (w / 2f) * 0.42f * (float)Math.Sin(t * Math.PI * 0.85 + 0.2);
                            float lx = (float)Math.Cos(a) * r, ly = (float)Math.Sin(a) * r;
                            push(ox + lx * b.Rx + ly * b.Ux + lz * b.Fx, oy + lx * b.Ry + ly * b.Uy + lz * b.Fy, oz + lx * b.Rz + ly * b.Uz + lz * b.Fz,
                                (float)Math.Cos(a) * b.Rx + (float)Math.Sin(a) * b.Ux + 0.35f * b.Fx,
                                (float)Math.Cos(a) * b.Ry + (float)Math.Sin(a) * b.Uy + 0.35f * b.Fy,
                                (float)Math.Cos(a) * b.Rz + (float)Math.Sin(a) * b.Uz + 0.35f * b.Fz,
                                ROLE_TULIP, '\0', 0.8f + t * 0.3f);
                        }
                    }
                }
            };

            Action<float, float, float, float, float, float, float> addLeaf = (ox, oy, oz, dx, dy, dz, scale) =>
            {
                Basis b = BasisFromDir(dx, dy, dz);
                for (int i = 1; i <= 9; i++)
                {
                    float t = i / 9f;
                    float halfW = (float)Math.Sin(t * Math.PI) * 0.36f * scale;
                    float zFwd = t * scale;
                    float curveY = -t * t * 0.18f * scale;
                    for (int s = -3; s <= 3; s++)
                    {
                        float lx = (s / 3f) * halfW;
                        push(ox + lx * b.Rx + curveY * b.Ux + zFwd * b.Fx, oy + lx * b.Ry + curveY * b.Uy + zFwd * b.Fy, oz + lx * b.Rz + curveY * b.Uz + zFwd * b.Fz,
                            b.Ux, b.Uy, b.Uz, ROLE_LEAF, s == 0 ? '/' : '*', 0.9f + t * 0.2f);
                    }
                }
            };

            // 3. Place flowers & foliage
            float[][] roseSpecs = {
                new[] {0.00f, 0.66f, 0.06f, 0.00f, 0.92f, 0.38f, 0.29f},
                new[] {-0.34f, 0.48f, 0.24f, -0.45f, 0.80f, 0.40f, 0.25f},
                new[] {0.35f, 0.46f, 0.22f, 0.46f, 0.78f, 0.42f, 0.25f},
                new[] {-0.04f, 0.45f, -0.28f, -0.05f, 0.85f, -0.52f, 0.24f}
            };
            foreach (float[] f in roseSpecs)
            {
                addStem(0f, -0.35f, 0f, f[0], f[1] - 0.06f, f[2]);
                addRose(f[0], f[1], f[2], f[3], f[4], f[5], f[6]);
            }

            float[][] daisySpecs = {
                new[] {-0.46f, 0.36f, -0.08f, -0.62f, 0.75f, -0.15f, 0.23f},
                new[] {0.45f, 0.35f, -0.10f, 0.62f, 0.74f, -0.18f, 0.23f},
                new[] {0.02f, 0.28f, 0.42f, 0.05f, 0.62f, 0.78f, 0.22f}
            };
            foreach (float[] f in daisySpecs)
            {
                addStem(0f, -0.35f, 0f, f[0], f[1] - 0.04f, f[2]);
                addDaisy(f[0], f[1], f[2], f[3], f[4], f[5], f[6]);
            }

            float[][] tulipSpecs = {
                new[] {-0.24f, 0.56f, -0.26f, -0.32f, 0.88f, -0.34f, 0.22f},
                new[] {0.25f, 0.54f, -0.24f, 0.34f, 0.86f, -0.35f, 0.22f},
                new[] {-0.18f, 0.34f, 0.36f, -0.25f, 0.72f, 0.65f, 0.20f}
            };
            foreach (float[] f in tulipSpecs)
            {
                addStem(0f, -0.35f, 0f, f[0], f[1] - 0.04f, f[2]);
                addTulip(f[0], f[1], f[2], f[3], f[4], f[5], f[6]);
            }

            float[][] babySpots = {
                new[] {-0.50f, 0.22f, 0.16f}, new[] {0.50f, 0.21f, 0.15f},
                new[] {0.24f, 0.24f, 0.38f}, new[] {-0.28f, 0.24f, -0.38f},
                new[] {0.28f, 0.25f, -0.38f}, new[] {0.00f, 0.52f, -0.38f},
                new[] {-0.18f, 0.60f, 0.26f}, new[] {0.20f, 0.58f, 0.26f}
            };
            float[][] babyOffsets = {
                new[] {0f, 0f, 0f}, new[] {0.08f, 0.06f, 0.04f},
                new[] {-0.07f, 0.05f, 0.06f}, new[] {0.05f, -0.04f, -0.06f},
                new[] {-0.06f, 0.07f, -0.04f}, new[] {0.02f, 0.10f, 0.01f}
            };
            foreach (float[] s in babySpots)
                foreach (float[] d in babyOffsets)
                {
                    push(s[0] + d[0], s[1] + d[1], s[2] + d[2], 0f, 1f, 0f, ROLE_BABY, '*', 1.05f);
                    push(s[0] + d[0] + 0.02f, s[1] + d[1] + 0.015f, s[2] + d[2], 0.3f, 0.8f, 0.5f, ROLE_BABY, '+', 0.95f);
                }

            float[][] leafSpecs = {
                new[] {-0.36f, 0.14f, 0.22f, -0.85f, 0.45f, 0.35f, 0.26f},
                new[] {0.36f, 0.14f, 0.20f, 0.85f, 0.45f, 0.32f, 0.26f},
                new[] {0.00f, 0.12f, 0.38f, 0.05f, 0.40f, 0.90f, 0.25f},
                new[] {-0.30f, 0.16f, -0.28f, -0.70f, 0.50f, -0.60f, 0.25f},
                new[] {0.30f, 0.16f, -0.28f, 0.70f, 0.50f, -0.60f, 0.25f},
                new[] {0.00f, 0.18f, -0.38f, 0.00f, 0.55f, -0.82f, 0.24f}
            };
            foreach (float[] lf in leafSpecs)
                addLeaf(lf[0], lf[1], lf[2], lf[3], lf[4], lf[5], lf[6]);

            return pts;
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WidgetForm());
        }
    }
}
