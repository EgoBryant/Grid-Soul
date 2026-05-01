using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private GameModel game = new GameModel();
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        private bool up, down, left, right;
        private Dictionary<string, Image> sprites = new Dictionary<string, Image>();
        private List<Rectangle> levelButtons = new List<Rectangle>();
        private Rectangle resumeButton;
        private Rectangle levelSelectButton;
        private Rectangle exitButton;

        // эффект дождя
        private List<string> rain = new List<string>();
        private Random rand = new Random();

        public Form1()
        {
            InitializeComponent();

            DoubleBuffered = true;
            KeyPreview = true;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            LoadSprites();

            timer.Interval = 20;
            timer.Tick += GameLoop;
            timer.Start();

            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
            this.MouseDown += Form1_MouseDown;

            // инициализация дождя
            for (int i = 0; i < 30; i++)
                rain.Add(RandomString(20));
        }

        private void LoadSprites()
        {
            sprites.Clear();

            string assetsPath = Path.Combine(Application.StartupPath, "Assets");
            if (!Directory.Exists(assetsPath))
                assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets");

            Dictionary<string, string[]> aliases = new Dictionary<string, string[]>
            {
                { "start_bg", new[] { "start_bg", "arena", "sand" } },
                { "intro_bg", new[] { "intro_bg", "prison", "General forward beat" } },
                { "map_bg", new[] { "map_bg", "arena", "sand" } },
                { "arena_bg", new[] { "arena_bg", "arena", "sand" } },
                { "outro_bg", new[] { "outro_bg", "victory" } },
                { "hero", new[] { "hero", "gladiator" } },
                { "tiger", new[] { "tiger", "tigr" } },
                { "warrior", new[] { "warrior", "rimskivoin" } },
                { "chariot", new[] { "chariot", "colesnica" } },
                { "pretorian", new[] { "pretorian", "pretorianec" } },
                { "boss", new[] { "boss", "imperator" } },
                { "lock", new[] { "lock", "shit" } },
                { "weapon_gladius", new[] { "fireknife", "piece", "weapon_gladius" } },
                { "weapon_knife", new[] { "knife", "weapon_knife" } },
                { "weapon_spear", new[] { "copie", "weapon_spear" } },
                { "weapon_firesword", new[] { "firesword", "fire_sword" } },
                { "weapon_falx", new[] { "falx", "weapon_falx" } },
                { "ball", new[] { "ball" } },
                { "heal_item", new[] { "heal_item", "heal" } },
                { "pilum_spear", new[] { "pilum_spear", "pilum" } }
            };

            foreach (var item in aliases)
            {
                Image? image = TryLoadAsset(assetsPath, item.Value);
                if (image != null)
                    sprites[item.Key] = image;
            }
        }

        private Image? TryLoadAsset(string assetsPath, string[] names)
        {
            string[] extensions = { ".png", ".jpg", ".jpeg", ".bmp" };

            foreach (string name in names)
            {
                foreach (string extension in extensions)
                {
                    string path = Path.Combine(assetsPath, name + extension);

                    if (File.Exists(path))
                        return Image.FromFile(path);
                }
            }

            return null;
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (game.State == GameState.Playing)
            {
                game.Update(up, down, left, right, ClientSize.Width, ClientSize.Height);
            }

            AnimateRain();
            Invalidate();
        }

        // ===================== DRAW =====================
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (game.State == GameState.Start || game.State == GameState.Menu)
                DrawMenu(g);
            else if (game.State == GameState.Intro)
                DrawStoryScreen(g, "ПРЕДАТЕЛЬСТВО");
            else if (game.State == GameState.Playing)
                DrawGame(g);
            else if (game.State == GameState.Shop)
                DrawShop(g);
            else if (game.State == GameState.LevelSelect)
                DrawLevelSelect(g);
            else if (game.State == GameState.Pause)
                DrawPause(g);
            else if (game.State == GameState.LevelTransition)
                DrawStoryScreen(g, game.LevelTitle);
            else if (game.State == GameState.Outro)
                DrawStoryScreen(g, "СВОБОДА РИМА");
            else if (game.State == GameState.GameOver)
                DrawGameOver(g);
        }

        // ===================== MENU =====================
        private void DrawMenu(Graphics g)
        {
            DrawBackground(g, "start_bg");

            using (Font titleFont = new Font("Consolas", 56, FontStyle.Bold))
            using (Font subtitleFont = new Font("Consolas", 16, FontStyle.Bold))
            using (Font infoFont = new Font("Consolas", 13))
            using (Font smallFont = new Font("Consolas", 11))
            using (Font controlsFont = new Font("Consolas", 18, FontStyle.Bold))
            using (Pen cyanPen = new Pen(Color.FromArgb(180, 0, 230, 255), 2))
            using (Brush darkPanel = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            {
                string title = "ГЛАДИАТОР";
                SizeF size = g.MeasureString(title, titleFont);
                float titleX = ClientSize.Width * 0.44f;
                float titleY = ClientSize.Height * 0.16f;

                for (int i = 5; i >= 1; i--)
                {
                    using (Brush glow = new SolidBrush(Color.FromArgb(18, 0, 220, 255)))
                        g.DrawString(title, titleFont, glow, titleX - i, titleY - i);
                }

                g.DrawString(title, titleFont, Brushes.Cyan, titleX, titleY);

                string subtitle = "СЮЖЕТНАЯ КАМПАНИЯ";
                SizeF subtitleSize = g.MeasureString(subtitle, subtitleFont);
                g.DrawString(subtitle, subtitleFont, Brushes.White,
                    titleX + (size.Width - subtitleSize.Width) / 2,
                    titleY + size.Height - 8);

                Rectangle portrait = new Rectangle(
                    70,
                    80,
                    (int)(ClientSize.Width * 0.28f),
                    ClientSize.Height - 160);

                g.FillRectangle(darkPanel, portrait);
                g.DrawRectangle(cyanPen, portrait);

                if (sprites.TryGetValue("hero", out Image? hero))
                    g.DrawImage(hero, portrait);
                else
                    g.FillEllipse(Brushes.Cyan, portrait);

                string info = "ENTER - НАЧАТЬ";
                SizeF size2 = g.MeasureString(info, infoFont);

                g.DrawString(info, infoFont, Brushes.White,
                    titleX + (size.Width - size2.Width) / 2,
                    titleY + size.Height + 70);

                string controls = "WASD - движение    стрелки - атака    ESC - выход";
                SizeF controlsSize = g.MeasureString(controls, controlsFont);
                float controlsY = ClientSize.Height - 60;
                g.DrawString(controls, controlsFont, Brushes.White,
                    titleX + (size.Width - controlsSize.Width) / 2,
                    controlsY);

                // Добавим hero.png слева и справа от текста управления
                if (sprites.TryGetValue("hero", out Image? heroIcon))
                {
                    float heroSize = 40;
                    float heroY = controlsY - 10;
                    float leftHeroX = titleX + (size.Width - controlsSize.Width) / 2 - heroSize - 10;
                    float rightHeroX = titleX + (size.Width + controlsSize.Width) / 2 + 10;
                    g.DrawImage(heroIcon, leftHeroX, heroY, heroSize, heroSize);
                    g.DrawImage(heroIcon, rightHeroX, heroY, heroSize, heroSize);
                }
            }
        }

        private void DrawMenuMapPreview(Graphics g, Rectangle bounds)
        {
            using (Brush floor = new SolidBrush(Color.FromArgb(150, 120, 58)))
            using (Pen border = new Pen(Color.FromArgb(230, 190, 70), 2))
            using (Brush wall = new SolidBrush(Color.FromArgb(45, 45, 45)))
            using (Pen wallBorder = new Pen(Color.Cyan, 1))
            using (Brush playerBrush = new SolidBrush(Color.Cyan))
            {
                float sx = bounds.Width / 1000f;
                float sy = bounds.Height / 700f;

                DrawPreviewRect(g, floor, border, bounds, sx, sy, 0, 0, 1000, 700);
                DrawPreviewRect(g, wall, wallBorder, bounds, sx, sy, 180, 135, 110, 110);
                DrawPreviewRect(g, wall, wallBorder, bounds, sx, sy, 710, 135, 110, 110);
                DrawPreviewRect(g, wall, wallBorder, bounds, sx, sy, 180, 455, 110, 110);
                DrawPreviewRect(g, wall, wallBorder, bounds, sx, sy, 710, 455, 110, 110);
                DrawPreviewRect(g, wall, wallBorder, bounds, sx, sy, 390, 205, 220, 30);
                DrawPreviewRect(g, wall, wallBorder, bounds, sx, sy, 390, 465, 220, 30);
                DrawPreviewRect(g, wall, wallBorder, bounds, sx, sy, 320, 300, 30, 100);
                DrawPreviewRect(g, wall, wallBorder, bounds, sx, sy, 650, 300, 30, 100);

                g.FillEllipse(playerBrush,
                    bounds.Left + 500 * sx - 5,
                    bounds.Top + 330 * sy - 5,
                    10,
                    10);
            }
        }

        private void DrawPreviewRect(Graphics g, Brush brush, Pen pen, Rectangle bounds, float sx, float sy, float x, float y, float w, float h)
        {
            RectangleF rect = new RectangleF(
                bounds.Left + x * sx,
                bounds.Top + y * sy,
                w * sx,
                h * sy);

            g.FillRectangle(brush, rect);
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        private void DrawStoryScreen(Graphics g, string title)
        {
            string backgroundKey = game.State == GameState.Outro ? "outro_bg" : "intro_bg";
            DrawBackground(g, backgroundKey);

            using (Font titleFont = new Font("Consolas", 34, FontStyle.Bold))
            using (Font textFont = new Font("Consolas", 16))
            using (Font hintFont = new Font("Consolas", 12))
            using (Brush panel = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            using (Pen border = new Pen(Color.FromArgb(220, 215, 170, 85), 2))
            {
                Rectangle box = new Rectangle(
                    ClientSize.Width / 2 - 430,
                    ClientSize.Height / 2 - 190,
                    860,
                    380);

                g.FillRectangle(panel, box);
                g.DrawRectangle(border, box);
                DrawCenteredString(g, title, titleFont, Brushes.Gold, box.Top + 35);

                int y = box.Top + 115;
            string? storyImageKey = null;

            if (game.State == GameState.LevelTransition)
            {
                if (game.PendingLevel == 3)
                    storyImageKey = "pilum_spear";
                else if (game.PendingLevel == 4 || game.PendingLevel == 5)
                    storyImageKey = "weapon_falx";
            }

            if (storyImageKey != null && sprites.TryGetValue(storyImageKey, out Image? storyImage))
            {
                Rectangle imageRect = new Rectangle(box.Left + box.Width / 2 - 120, box.Top + 80, 240, 120);
                g.DrawImage(storyImage, imageRect);
                y = imageRect.Bottom + 20;
            }

            string[] lines = WrapText(g, game.StoryText, textFont, box.Width - 90);

            foreach (var line in lines)
            {
                SizeF size = g.MeasureString(line, textFont);
                g.DrawString(line, textFont, Brushes.White, box.Left + (box.Width - size.Width) / 2, y);
                y += 32;
            }

                string hint = game.State == GameState.Outro ? "ENTER - В МЕНЮ" : "ENTER - ПРОДОЛЖИТЬ";
                DrawCenteredString(g, hint, hintFont, Brushes.Gray, box.Bottom - 55);
            }
        }

        private string[] WrapText(Graphics g, string text, Font font, int maxWidth)
        {
            List<string> lines = new List<string>();
            string[] paragraphs = text.Split(new[] { '\n' }, StringSplitOptions.None);

            for (int p = 0; p < paragraphs.Length; p++)
            {
                string paragraph = paragraphs[p];
                string current = "";

                foreach (string word in paragraph.Split(' '))
                {
                    string next = current.Length == 0 ? word : current + " " + word;

                    if (g.MeasureString(next, font).Width > maxWidth && current.Length > 0)
                    {
                        lines.Add(current);
                        current = word;
                    }
                    else
                    {
                        current = next;
                    }
                }

                if (current.Length > 0)
                    lines.Add(current);

                if (p < paragraphs.Length - 1)
                    lines.Add("");
            }

            return lines.ToArray();
        }

        private void DrawBackground(Graphics g, string key)
        {
            if (sprites.TryGetValue(key, out Image? image))
            {
                g.DrawImage(image, 0, 0, ClientSize.Width, ClientSize.Height);
                using (Brush shade = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                    g.FillRectangle(shade, ClientRectangle);
            }
            else
            {
                DrawGradient(g);
                DrawRain(g);
            }
        }

        private void DrawLevelSelect(Graphics g)
        {
            DrawBackground(g, "map_bg");
            levelButtons.Clear();

            using (Font titleFont = new Font("Consolas", 34, FontStyle.Bold))
            using (Font labelFont = new Font("Consolas", 14, FontStyle.Bold))
            using (Brush lockedBrush = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            using (Pen activePen = new Pen(Color.Gold, 3))
            using (Pen lockedPen = new Pen(Color.Gray, 2))
            {
                DrawCenteredString(g, "ВЫБОР УРОВНЯ", titleFont, Brushes.Gold, 90);

                int size = 150;
                int gap = 28;
                int totalWidth = size * 5 + gap * 4;
                int startX = (ClientSize.Width - totalWidth) / 2;
                int y = ClientSize.Height / 2 - size / 2;

                for (int i = 0; i < 5; i++)
                {
                    Rectangle rect = new Rectangle(startX + i * (size + gap), y, size, size);
                    levelButtons.Add(rect);
                    bool unlocked = i + 1 <= game.MaxUnlockedLevel;

                    string iconKey = GetLevelIconKey(i + 1);
                    if (sprites.TryGetValue(iconKey, out Image? icon))
                        g.DrawImage(icon, rect);
                    else
                        g.FillRectangle(Brushes.SaddleBrown, rect);

                    if (!unlocked)
                    {
                        g.FillRectangle(lockedBrush, rect);

                        if (sprites.TryGetValue("lock", out Image? lockImage))
                        {
                            Rectangle lockRect = Rectangle.Inflate(rect, -42, -42);
                            g.DrawImage(lockImage, lockRect);
                        }
                    }

                    g.DrawRectangle(unlocked ? activePen : lockedPen, rect);

                    string text = unlocked ? $"УРОВЕНЬ {i + 1}" : "ЗАКРЫТО";
                    SizeF textSize = g.MeasureString(text, labelFont);
                    g.DrawString(text, labelFont, unlocked ? Brushes.White : Brushes.Gray,
                        rect.Left + (rect.Width - textSize.Width) / 2,
                        rect.Bottom + 14);
                }
            }
        }

        private string GetLevelIconKey(int level)
        {
            if (level == 1)
                return "tiger";
            if (level == 2)
                return "warrior";
            if (level == 3)
                return "chariot";
            if (level == 4)
                return "pretorian";

            return "boss";
        }

        private void DrawPause(Graphics g)
        {
            DrawGame(g);

            using (Brush overlay = new SolidBrush(Color.FromArgb(185, 0, 0, 0)))
            using (Brush buttonBrush = new SolidBrush(Color.FromArgb(230, 42, 34, 28)))
            using (Pen border = new Pen(Color.Gold, 2))
            using (Font titleFont = new Font("Consolas", 30, FontStyle.Bold))
            using (Font buttonFont = new Font("Consolas", 16, FontStyle.Bold))
            {
                g.FillRectangle(overlay, ClientRectangle);
                DrawCenteredString(g, "ПАУЗА", titleFont, Brushes.Gold, ClientSize.Height / 2 - 160);

                int w = 310;
                int h = 52;
                int x = ClientSize.Width / 2 - w / 2;
                int y = ClientSize.Height / 2 - 70;

                resumeButton = new Rectangle(x, y, w, h);
                levelSelectButton = new Rectangle(x, y + 72, w, h);
                exitButton = new Rectangle(x, y + 144, w, h);

                DrawMenuButton(g, resumeButton, "Продолжить", buttonBrush, border, buttonFont);
                DrawMenuButton(g, levelSelectButton, "В меню уровней", buttonBrush, border, buttonFont);
                DrawMenuButton(g, exitButton, "Выход из игры", buttonBrush, border, buttonFont);
            }
        }

        private void DrawMenuButton(Graphics g, Rectangle rect, string text, Brush brush, Pen pen, Font font)
        {
            g.FillRectangle(brush, rect);
            g.DrawRectangle(pen, rect);

            SizeF size = g.MeasureString(text, font);
            g.DrawString(text, font, Brushes.White,
                rect.Left + (rect.Width - size.Width) / 2,
                rect.Top + (rect.Height - size.Height) / 2);
        }

        // ===================== GAME =====================
        private void DrawGame(Graphics g)
        {
            if (sprites.TryGetValue("arena_bg", out Image? arena))
            {
                g.DrawImage(arena, 0, 0, ClientSize.Width, ClientSize.Height);
                DrawGates(g);
            }
            else
            {
                g.Clear(Color.FromArgb(10, 10, 20));
                DrawMap(g);
            }

            // стены
            foreach (var wall in game.walls)
            {
                if (wall.IsDestructible)
                    DrawDestructibleWall(g, wall);
                else
                    DrawSolidWall(g, wall);
            }

            // игрок
            DrawGladiator(g);

            // враги
            foreach (var enemy in game.enemies)
            {
                DrawEnemy(g, enemy);
            }

            foreach (var slash in game.slashes)
            {
                using (Brush slashBrush = new SolidBrush(Color.FromArgb(150, 255, 245, 170)))
                    g.FillEllipse(slashBrush, slash.Bounds);
            }

            // пули
            foreach (var b in game.bullets)
            {
                if (!b.IsEnemyBullet)
                {
                    if (b.BulletType == 1 && sprites.TryGetValue("weapon_spear", out Image? spear))
                    {
                        g.DrawImage(spear, b.Bounds);
                    }
                    else if (b.BulletType == 2 && sprites.TryGetValue("pilum_spear", out Image? pilum))
                    {
                        g.DrawImage(pilum, b.Bounds);
                    }
                    else if (sprites.TryGetValue("ball", out Image? ball))
                    {
                        g.DrawImage(ball, b.Bounds);
                    }
                    else
                    {
                        g.FillEllipse(Brushes.Yellow, b.Bounds);
                    }
                }
                else if (b.BulletType == 3)
                {
                    using (Brush dartBrush = new SolidBrush(Color.SaddleBrown))
                    {
                        g.FillRectangle(dartBrush, b.Bounds);
                        float dx = b.dx;
                        float dy = b.dy;
                        float length = (float)Math.Sqrt(dx * dx + dy * dy);

                        if (length > 0.1f)
                        {
                            PointF dir = new PointF(dx / length, dy / length);
                            PointF perp = new PointF(-dir.Y, dir.X);
                            PointF tip = new PointF(b.Bounds.Left + b.Bounds.Width / 2 + dir.X * b.Bounds.Width, b.Bounds.Top + b.Bounds.Height / 2 + dir.Y * b.Bounds.Width);
                            PointF p1 = tip;
                            PointF p2 = new PointF(b.Bounds.Left + b.Bounds.Width / 2 + perp.X * 4, b.Bounds.Top + b.Bounds.Height / 2 + perp.Y * 4);
                            PointF p3 = new PointF(b.Bounds.Left + b.Bounds.Width / 2 - perp.X * 4, b.Bounds.Top + b.Bounds.Height / 2 - perp.Y * 4);
                            g.FillPolygon(dartBrush, new[] { p1, p2, p3 });
                        }
                    }
                }
                else
                {
                    g.FillRectangle(Brushes.Magenta, b.Bounds);
                }
            }

            // частицы
            foreach (var p in game.particles)
                g.FillEllipse(Brushes.Orange, p.Position.X, p.Position.Y, 3, 3);

            // аптечки
            foreach (var p in game.powerUps)
            {
                if (sprites.TryGetValue("heal_item", out Image? heal))
                {
                    // Увеличиваем размер heal_item в 2 раза
                    RectangleF healRect = new RectangleF(p.Bounds.X - p.Bounds.Width / 2, p.Bounds.Y - p.Bounds.Height / 2, p.Bounds.Width * 2, p.Bounds.Height * 2);
                    g.DrawImage(heal, (int)healRect.X, (int)healRect.Y, (int)healRect.Width, (int)healRect.Height);
                }
                else
                {
                    g.FillEllipse(Brushes.Lime, p.Bounds);
                }
            }

            DrawHUD(g);
        }

        private void DrawGladiator(Graphics g)
        {
            RectangleF b = game.player.Bounds;

            if (sprites.TryGetValue("hero", out Image? playerImage))
            {
                g.DrawImage(playerImage, b);
                return;
            }

            using (Brush skin = new SolidBrush(Color.FromArgb(210, 160, 105)))
            using (Brush armor = new SolidBrush(Color.FromArgb(170, 45, 35)))
            using (Pen helmet = new Pen(Color.Gold, 2))
            using (Pen blade = new Pen(Color.Silver, 3))
            {
                g.FillEllipse(skin, b);
                g.FillRectangle(armor, b.X + b.Width * 0.2f, b.Y + b.Height * 0.45f, b.Width * 0.6f, b.Height * 0.45f);
                g.DrawArc(helmet, b.X - 2, b.Y - 4, b.Width + 4, b.Height, 195, 150);
                g.DrawLine(blade, b.Right - 2, b.Y + b.Height / 2, b.Right + 14, b.Y + b.Height / 2 - 8);
            }
        }

        private void DrawEnemy(Graphics g, Enemy enemy)
        {
            string key = GetEnemyImageKey(enemy.Type);
            if (sprites.TryGetValue(key, out Image? image))
            {
                g.DrawImage(image, enemy.Bounds);
                return;
            }

            if (enemy.Type == EnemyType.Tiger)
            {
                using (Brush body = new SolidBrush(Color.Orange))
                using (Pen stripes = new Pen(Color.Black, 2))
                {
                    g.FillEllipse(body, enemy.Bounds);
                    for (int i = 0; i < 4; i++)
                    {
                        float x = enemy.Bounds.Left + 5 + i * 6;
                        g.DrawLine(stripes, x, enemy.Bounds.Top + 4, x + 5, enemy.Bounds.Bottom - 4);
                    }
                }
            }
            else if (enemy.Type == EnemyType.Chariot)
            {
                using (Brush cart = new SolidBrush(Color.SaddleBrown))
                using (Pen wheel = new Pen(Color.Gold, 2))
                {
                    g.FillRectangle(cart, enemy.Bounds);
                    g.DrawEllipse(wheel, enemy.Bounds.Left + 3, enemy.Bounds.Bottom - 10, 10, 10);
                    g.DrawEllipse(wheel, enemy.Bounds.Right - 13, enemy.Bounds.Bottom - 10, 10, 10);
                }
            }
            else if (enemy.Type == EnemyType.Praetorian)
            {
                using (Brush armor = new SolidBrush(Color.DarkSlateGray))
                using (Pen gold = new Pen(Color.Gold, 2))
                {
                    g.FillRectangle(armor, enemy.Bounds);
                    g.DrawRectangle(gold, enemy.Bounds.X, enemy.Bounds.Y, enemy.Bounds.Width, enemy.Bounds.Height);
                }
            }
            else if (enemy.Type == EnemyType.Emperor)
            {
                using (Brush robe = new SolidBrush(Color.Purple))
                using (Pen crown = new Pen(Color.Gold, 3))
                {
                    g.FillEllipse(robe, enemy.Bounds);
                    g.DrawLine(crown, enemy.Bounds.Left + 8, enemy.Bounds.Top, enemy.Bounds.Right - 8, enemy.Bounds.Top);
                    g.DrawRectangle(Pens.White, enemy.Bounds.X, enemy.Bounds.Y, enemy.Bounds.Width, enemy.Bounds.Height);
                }
            }
            else
            {
                using (Brush tunic = new SolidBrush(Color.Firebrick))
                {
                    g.FillRectangle(tunic, enemy.Bounds);
                    g.DrawLine(Pens.Silver, enemy.Bounds.Left, enemy.Bounds.Top, enemy.Bounds.Right, enemy.Bounds.Bottom);
                }
            }
        }

        private string GetEnemyImageKey(EnemyType type)
        {
            if (type == EnemyType.Tiger)
                return "tiger";

            if (type == EnemyType.Warrior)
                return "warrior";

            if (type == EnemyType.Praetorian)
                return "pretorian";

            if (type == EnemyType.Chariot)
                return "chariot";

            if (type == EnemyType.Emperor)
                return "boss";

            return "warrior";
        }

        // ===================== SHOP =====================
        private void DrawShop(Graphics g)
        {
            DrawGame(g);

            using (Brush overlay = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
            using (Brush panel = new SolidBrush(Color.FromArgb(230, 15, 18, 28)))
            using (Pen border = new Pen(Color.Cyan, 2))
            using (Font title = new Font("Consolas", 28, FontStyle.Bold))
            using (Font item = new Font("Consolas", 14))
            using (Font small = new Font("Consolas", 11))
            {
                g.FillRectangle(overlay, ClientRectangle);

                Rectangle panelRect = new Rectangle(
                    ClientSize.Width / 2 - 260,
                    ClientSize.Height / 2 - 190,
                    520,
                    380);

                g.FillRectangle(panel, panelRect);
                g.DrawRectangle(border, panelRect);

                DrawCenteredString(g, "МАГАЗИН", title, Brushes.Cyan, panelRect.Top + 25);
                DrawCenteredString(g, $"Очки: {game.Score}", item, Brushes.White, panelRect.Top + 75);

                int y = panelRect.Top + 120;
                DrawShopLine(g, item, 1, "Скорострельность -7%", game.FireRateUpgradeCost, $"интервал {game.ShootInterval}", y);
                DrawShopLine(g, item, 2, "Мульти-выстрел", game.MultiShotUpgradeCost, game.HasMultiShot ? "куплено" : "3 пули", y + 42);
                DrawShopLine(g, item, 3, "Вампиризм", game.VampirismUpgradeCost, game.HasVampirism ? "куплено" : "7% лечение", y + 84);
                DrawShopLine(g, item, 4, "Рывок", game.DashUpgradeCost, game.HasDash ? "куплено" : "Пробел", y + 126);

                DrawCenteredString(g, "P - ПРОДОЛЖИТЬ", small, Brushes.Gray, panelRect.Bottom - 45);
            }
        }

        private void DrawCenteredString(Graphics g, string text, Font font, Brush brush, float y)
        {
            SizeF size = g.MeasureString(text, font);
            g.DrawString(text, font, brush, (ClientSize.Width - size.Width) / 2, y);
        }

        private void DrawShopLine(Graphics g, Font font, int key, string name, int cost, string status, int y)
        {
            string text = $"{key}. {name}  [{cost}]  {status}";
            SizeF size = g.MeasureString(text, font);
            Brush brush = game.Score >= cost ? Brushes.White : Brushes.Gray;

            g.DrawString(text, font, brush, (ClientSize.Width - size.Width) / 2, y);
        }

        private void DrawSolidWall(Graphics g, Wall wall)
        {
            using (Brush brush = new SolidBrush(Color.FromArgb(28, 30, 34)))
            using (Pen glow = new Pen(Color.FromArgb(220, 0, 235, 255), 2))
            using (Pen inner = new Pen(Color.FromArgb(120, 180, 255, 255), 1))
            {
                g.FillRectangle(brush, wall.Bounds);
                g.DrawRectangle(glow, wall.Bounds.X, wall.Bounds.Y, wall.Bounds.Width, wall.Bounds.Height);

                RectangleF innerRect = RectangleF.Inflate(wall.Bounds, -5, -5);
                g.DrawRectangle(inner, innerRect.X, innerRect.Y, innerRect.Width, innerRect.Height);
            }
        }

        private void DrawDestructibleWall(Graphics g, Wall wall)
        {
            int alpha = Math.Max(70, Math.Min(230, 70 + wall.HP * 50));

            using (Brush brush = new SolidBrush(Color.FromArgb(alpha / 2, 115, 62, 20)))
            using (Pen dashed = new Pen(Color.FromArgb(alpha, 255, 145, 35), 3))
            {
                dashed.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                g.FillRectangle(brush, wall.Bounds);
                g.DrawRectangle(dashed, wall.Bounds.X, wall.Bounds.Y, wall.Bounds.Width, wall.Bounds.Height);
            }
        }

        private void DrawMap(Graphics g)
        {
            using (Brush floor = new SolidBrush(Color.FromArgb(130, 112, 55)))
            using (Pen border = new Pen(Color.FromArgb(205, 178, 72), 2))
            using (Pen grid = new Pen(Color.FromArgb(60, 40, 25)))
            {
                foreach (var area in game.mapAreas)
                {
                    g.FillRectangle(floor, area);

                    for (float x = area.Left + 16; x < area.Right; x += 16)
                        g.DrawLine(grid, x, area.Top, x, area.Bottom);

                    for (float y = area.Top + 16; y < area.Bottom; y += 16)
                        g.DrawLine(grid, area.Left, y, area.Right, y);

                    g.DrawRectangle(border, area.X, area.Y, area.Width - 1, area.Height - 1);
                }

                DrawGates(g);
            }
        }

        private void DrawGates(Graphics g)
        {
            using (Brush gate = new SolidBrush(Color.FromArgb(60, 25, 15)))
            using (Pen gold = new Pen(Color.Gold, 3))
            {
                int w = ClientSize.Width;
                int h = ClientSize.Height;

                Rectangle top = new Rectangle(w / 2 - 45, 0, 90, 18);
                Rectangle bottom = new Rectangle(w / 2 - 45, h - 18, 90, 18);
                Rectangle left = new Rectangle(0, h / 2 - 45, 18, 90);
                Rectangle right = new Rectangle(w - 18, h / 2 - 45, 18, 90);

                g.FillRectangle(gate, top);
                g.FillRectangle(gate, bottom);
                g.FillRectangle(gate, left);
                g.FillRectangle(gate, right);

                g.DrawRectangle(gold, top);
                g.DrawRectangle(gold, bottom);
                g.DrawRectangle(gold, left);
                g.DrawRectangle(gold, right);
            }
        }

        // ===================== GAME OVER =====================
        private void DrawGameOver(Graphics g)
        {
            DrawGame(g);

            using (Font font = new Font("Consolas", 32, FontStyle.Bold))
            using (Font small = new Font("Consolas", 14))
            {
                string text = "ИГРА ОКОНЧЕНА";
                SizeF size = g.MeasureString(text, font);

                g.DrawString(text, font, Brushes.White,
                    (ClientSize.Width - size.Width) / 2,
                    ClientSize.Height / 2 - 50);

                string restart = "ENTER - НАЧАТЬ ЗАНОВО";
                SizeF size2 = g.MeasureString(restart, small);

                g.DrawString(restart, small, Brushes.Gray,
                    (ClientSize.Width - size2.Width) / 2,
                    ClientSize.Height / 2 + 10);
            }
        }

        // ===================== HUD =====================
        private void DrawHUD(Graphics g)
        {
            using (Font hudFont = new Font("Consolas", 12, FontStyle.Bold))
            {
                g.DrawString($"Очки: {game.Score}", hudFont, Brushes.White, 10, 10);
                g.DrawString($"Глава: {game.CampaignLevel}/5", hudFont, Brushes.White, 10, 30);
                g.DrawString($"Врагов осталось: {game.EnemiesRemaining}", hudFont, Brushes.White, 10, 50);
                g.DrawString($"Оружие: {GetWeaponName()}", hudFont, Brushes.White, 10, 95);
            }

            int maxHP = 100;
            int barWidth = 200;
            int hpWidth = Math.Max(0, game.player.HP) * barWidth / maxHP;

            g.DrawRectangle(Pens.White, 10, 75, barWidth, 12);
            g.FillRectangle(Brushes.Red, 10, 75, hpWidth, 12);

            DrawWeaponIcon(g);

            // Здоровье Босса на Level 5
            if (game.CampaignLevel == 5 && game.BossHP > 0)
            {
                int bossBarWidth = 400;
                int bossHpWidth = Math.Max(0, game.BossHP) * bossBarWidth / game.BossMaxHP;
                int bossBarX = (ClientSize.Width - bossBarWidth) / 2;
                int bossBarY = 20;

                g.DrawRectangle(Pens.White, bossBarX, bossBarY, bossBarWidth, 20);
                g.FillRectangle(Brushes.Red, bossBarX, bossBarY, bossHpWidth, 20);

                using (Font bossFont = new Font("Consolas", 10, FontStyle.Bold))
                {
                    string bossText = $"Босс: {game.BossHP}/{game.BossMaxHP}";
                    SizeF bossSize = g.MeasureString(bossText, bossFont);
                    g.DrawString(bossText, bossFont, Brushes.White, bossBarX + (bossBarWidth - bossSize.Width) / 2, bossBarY + 22);
                }
            }
        }

        private void DrawWeaponIcon(Graphics g)
        {
            string key = "weapon_gladius";

            if (game.CurrentWeapon == WeaponType.Knife)
                key = "weapon_knife";
            else if (game.CurrentWeapon == WeaponType.Spear)
                key = "weapon_spear";
            else if (game.CurrentWeapon == WeaponType.Pilum)
                key = "pilum_spear";
            else if (game.CurrentWeapon == WeaponType.FireSword)
                key = "weapon_firesword";
            else if (game.CurrentWeapon == WeaponType.Falx)
                key = "weapon_falx";

            Rectangle rect = new Rectangle(ClientSize.Width - 92, ClientSize.Height - 92, 72, 72);

            using (Brush bg = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            using (Pen border = new Pen(Color.Gold, 2))
            using (Font font = new Font("Consolas", 8, FontStyle.Bold))
            {
                g.FillRectangle(bg, rect);
                g.DrawRectangle(border, rect);

                if (sprites.TryGetValue(key, out Image? icon))
                    g.DrawImage(icon, Rectangle.Inflate(rect, -6, -6));
                else
                    g.DrawString(GetWeaponName(), font, Brushes.White, rect.Left + 6, rect.Top + 28);
            }
        }

        private string GetWeaponName()
        {
            if (game.CurrentWeapon == WeaponType.Knife)
                return "Нож";

            if (game.CurrentWeapon == WeaponType.Spear)
                return "Копье";

            if (game.CurrentWeapon == WeaponType.Pilum)
                return "Пилум";

            if (game.CurrentWeapon == WeaponType.FireSword)
                return "Огненный меч";

            if (game.CurrentWeapon == WeaponType.Falx)
                return "Фалкс";

            return "Меч";
        }

        // ===================== INPUT =====================
        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (game.State == GameState.Playing)
                {
                    game.PauseGame();
                    timer.Stop();
                    Invalidate();
                }
                else if (game.State == GameState.Pause)
                {
                    game.ResumeGame();
                    timer.Start();
                    Invalidate();
                }
                return;
            }

            // запуск / рестарт
            if (e.KeyCode == Keys.Enter)
            {
                if (game.State == GameState.Start || game.State == GameState.Menu)
                {
                    game.StartGame(ClientSize.Width, ClientSize.Height);
                }
                else if (game.State == GameState.GameOver)
                {
                    game.ReturnToLevelSelect();
                }
                else if (game.State == GameState.Intro || game.State == GameState.LevelTransition || game.State == GameState.Outro)
                {
                    game.ContinueStory(ClientSize.Width, ClientSize.Height);
                }
            }

            if (e.KeyCode == Keys.P)
            {
                return;
            }

            if (game.State == GameState.Shop)
            {
                if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1) game.BuyFireRate();
                if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2) game.BuyMultiShot();
                if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3) game.BuyVampirism();
                if (e.KeyCode == Keys.D4 || e.KeyCode == Keys.NumPad4) game.BuyDash();
                return;
            }

            if (game.State != GameState.Playing) return;

            if (game.CampaignLevel == 5)
            {
                if (e.KeyCode == Keys.Q)
                    game.ToggleWeapon();

                if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
                    game.CurrentWeapon = WeaponType.Pilum;

                if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
                    game.CurrentWeapon = WeaponType.Falx;
            }

            if (e.KeyCode == Keys.W) up = true;
            if (e.KeyCode == Keys.S) down = true;
            if (e.KeyCode == Keys.A) left = true;
            if (e.KeyCode == Keys.D) right = true;

            if (e.KeyCode == Keys.Up) game.Shoot(0, -1);
            if (e.KeyCode == Keys.Down) game.Shoot(0, 1);
            if (e.KeyCode == Keys.Left) game.Shoot(-1, 0);
            if (e.KeyCode == Keys.Right) game.Shoot(1, 0);
            if (e.KeyCode == Keys.Space) game.TryDash();
        }

        private void Form1_MouseDown(object? sender, MouseEventArgs e)
        {
            if (game.State == GameState.LevelSelect)
            {
                for (int i = 0; i < levelButtons.Count; i++)
                {
                    if (levelButtons[i].Contains(e.Location) && i + 1 <= game.MaxUnlockedLevel)
                    {
                        game.SelectLevel(i + 1, ClientSize.Width, ClientSize.Height);
                        timer.Start();
                        Invalidate();
                        return;
                    }
                }
            }

            if (game.State != GameState.Pause)
                return;

            if (resumeButton.Contains(e.Location))
            {
                game.ResumeGame();
                timer.Start();
                Invalidate();
            }
            else if (levelSelectButton.Contains(e.Location))
            {
                game.ReturnToLevelSelect();
                timer.Start();
                Invalidate();
            }
            else if (exitButton.Contains(e.Location))
            {
                Close();
            }
        }

        private void Form1_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W) up = false;
            if (e.KeyCode == Keys.S) down = false;
            if (e.KeyCode == Keys.A) left = false;
            if (e.KeyCode == Keys.D) right = false;
        }

        // ===================== EFFECTS =====================
        private void DrawGradient(Graphics g)
        {
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                ClientRectangle,
                Color.Black,
                Color.DarkBlue,
                90f))
            {
                g.FillRectangle(brush, ClientRectangle);
            }
        }

        private void DrawRain(Graphics g)
        {
            using (Font font = new Font("Consolas", 8))
            {
                for (int i = 0; i < rain.Count; i++)
                {
                    g.DrawString(rain[i], font, Brushes.Green,
                        i * 25,
                        (i * 20 + Environment.TickCount / 10) % ClientSize.Height);
                }
            }
        }

        private void AnimateRain()
        {
            if (rand.Next(10) == 0)
            {
                int index = rand.Next(rain.Count);
                rain[index] = RandomString(20);
            }
        }

        private string RandomString(int length)
        {
            const string chars = "01";
            char[] data = new char[length];

            for (int i = 0; i < length; i++)
                data[i] = chars[rand.Next(chars.Length)];

            return new string(data);
        }
    }
}
