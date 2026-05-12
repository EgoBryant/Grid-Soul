using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GridSoul.Controllers;
using GridSoul.Models;

namespace GridSoul.Views
{
    public class GameRenderer
    {
        private GameManager game;
        private Dictionary<string, Image> sprites = new Dictionary<string, Image>();
        private List<Rectangle> levelButtons = new List<Rectangle>();
        private Rectangle resumeButton;
        private Rectangle levelSelectButton;
        private Rectangle exitButton;
        private int renderWidth;
        private int renderHeight;

        private List<string> rain = new List<string>();
        private Random rand = new Random();

        public GameRenderer(GameManager gameManager)
        {
            game = gameManager;
            LoadSprites();
            InitializeRain();
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
                { "pilum_spear", new[] { "pilum_spear", "pilum" } },
                { "arrow_keys", new[] { "arrow_keys", "arrows" } },
                { "enemy_arrow", new[] { "enemy_arrow", "arrow" } }
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
                    {
                        try
                        {
                            return Image.FromFile(path);
                        }
                        catch { }
                    }
                }
            }

            return null;
        }

        private void InitializeRain()
        {
            rain.Clear();
            for (int i = 0; i < 30; i++)
                rain.Add(RandomString(20));
        }

        private string RandomString(int length)
        {
            const string chars = "01";
            char[] data = new char[length];
            for (int i = 0; i < length; i++)
                data[i] = chars[rand.Next(chars.Length)];
            return new string(data);
        }

        public void AnimateRain()
        {
            if (rand.Next(10) == 0)
            {
                int index = rand.Next(rain.Count);
                rain[index] = RandomString(20);
            }
        }

        public void Render(Graphics g, GameState state, int width, int height)
        {
            renderWidth = width;
            renderHeight = height;
            g.Clear(Color.Black);

            switch (state)
            {
                case GameState.Start:
                case GameState.Menu:
                    DrawMenu(g);
                    break;
                case GameState.Intro:
                    DrawStoryScreen(g, "ПРЕДАТЕЛЬСТВО");
                    break;
                case GameState.LevelSelect:
                    DrawLevelSelect(g);
                    break;
                case GameState.LevelTransition:
                    DrawStoryScreen(g, game.LevelTitle);
                    break;
                case GameState.Playing:
                    DrawGame(g);
                    break;
                case GameState.Pause:
                    DrawPause(g);
                    break;
                case GameState.Shop:
                    DrawShop(g);
                    break;
                case GameState.Outro:
                    DrawStoryScreen(g, "СВОБОДА РИМА");
                    break;
                case GameState.GameOver:
                    DrawGameOver(g);
                    break;
            }
        }

        public IReadOnlyList<Rectangle> LevelButtons => levelButtons;
        public Rectangle ResumeButton => resumeButton;
        public Rectangle LevelSelectButton => levelSelectButton;
        public Rectangle ExitButton => exitButton;

        private void DrawMenu(Graphics g)
        {
            DrawBackground(g, "start_bg");

            using (Font titleFont = new Font("Consolas", 52, FontStyle.Bold))
            using (Font subtitleFont = new Font("Consolas", 16, FontStyle.Bold))
            using (Font infoFont = new Font("Consolas", 13))
            using (Font controlsTitleFont = new Font("Palatino Linotype", 15, FontStyle.Bold))
            using (Font controlsFont = new Font("Palatino Linotype", 14, FontStyle.Bold))
            using (Pen goldPen = new Pen(Color.Gold, 4))
            using (Brush darkPanel = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            using (Brush textShadow = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
            using (Brush controlsBrush = new SolidBrush(Color.White))
            using (Brush controlsPanel = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                string title = "ГЛАДИАТОР";
                SizeF size = g.MeasureString(title, titleFont);
                float titleX = (renderWidth - size.Width) / 2;
                float titleY = renderHeight * 0.16f;

                for (int dx = -3; dx <= 3; dx++)
                {
                    for (int dy = -3; dy <= 3; dy++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        g.DrawString(title, titleFont, Brushes.Black, titleX + dx, titleY + dy);
                    }
                }

                g.DrawString(title, titleFont, Brushes.Gold, titleX, titleY);

                string subtitle = "СЮЖЕТНАЯ КАМПАНИЯ";
                SizeF subtitleSize = g.MeasureString(subtitle, subtitleFont);
                g.DrawString(subtitle, subtitleFont, Brushes.White,
                    (renderWidth - subtitleSize.Width) / 2,
                    titleY + size.Height - 8);

                Rectangle portrait = new Rectangle(
                    70,
                    80,
                    (int)(renderWidth * 0.28f),
                    renderHeight - 160);

                g.FillRectangle(darkPanel, portrait);
                g.DrawRectangle(goldPen, portrait);

                if (sprites.TryGetValue("hero", out Image? hero))
                    g.DrawImage(hero, portrait);
                else
                    g.FillEllipse(Brushes.Cyan, portrait);

                Rectangle enemyPortrait = new Rectangle(
                    renderWidth - portrait.Right,
                    portrait.Y,
                    portrait.Width,
                    portrait.Height);

                g.FillRectangle(darkPanel, enemyPortrait);
                g.DrawRectangle(goldPen, enemyPortrait);

                if (sprites.TryGetValue("boss", out Image? enemyImage) ||
                    sprites.TryGetValue("pretorian", out enemyImage))
                {
                    float scale = Math.Min(
                        enemyPortrait.Width / (float)enemyImage.Width,
                        enemyPortrait.Height / (float)enemyImage.Height);
                    int enemyWidth = (int)(enemyImage.Width * scale);
                    int enemyHeight = (int)(enemyImage.Height * scale);
                    Rectangle enemyDrawRect = new Rectangle(
                        enemyPortrait.Left + (enemyPortrait.Width - enemyWidth) / 2,
                        enemyPortrait.Top + (enemyPortrait.Height - enemyHeight) / 2,
                        enemyWidth,
                        enemyHeight);

                    var state = g.Save();
                    g.TranslateTransform(enemyDrawRect.Left + enemyDrawRect.Width, enemyDrawRect.Top);
                    g.ScaleTransform(-1, 1);
                    g.DrawImage(enemyImage, 0, 0, enemyDrawRect.Width, enemyDrawRect.Height);
                    g.Restore(state);
                }
                else
                {
                    g.FillEllipse(Brushes.DarkRed, enemyPortrait);
                }

                string info = "ENTER - НАЧАТЬ";
                SizeF size2 = g.MeasureString(info, infoFont);

                g.DrawString(info, infoFont, Brushes.White,
                    (renderWidth - size2.Width) / 2,
                    titleY + size.Height + 115);

                string[] controls =
                {
                    "WASD - движение",
                    "Стрелки - атака (направление броска)",
                    "ESC - выход"
                };
                string controlsTitle = "УПРАВЛЕНИЕ";
                SizeF controlsTitleSize = g.MeasureString(controlsTitle, controlsTitleFont);
                float controlsY = renderHeight * 0.70f;
                float controlsMaxWidth = 0;
                for (int i = 0; i < controls.Length; i++)
                    controlsMaxWidth = Math.Max(controlsMaxWidth, g.MeasureString(controls[i], controlsFont).Width);
                controlsMaxWidth = Math.Max(controlsMaxWidth, controlsTitleSize.Width);

                RectangleF controlsRect = new RectangleF(
                    (renderWidth - controlsMaxWidth) / 2 - 22,
                    controlsY - 42,
                    controlsMaxWidth + 44,
                    controls.Length * 25 + 56);
                g.FillRectangle(controlsPanel, controlsRect);

                float controlsTitleX = (renderWidth - controlsTitleSize.Width) / 2;
                g.DrawString(controlsTitle, controlsTitleFont, textShadow, controlsTitleX + 2, controlsY - 34);
                g.DrawString(controlsTitle, controlsTitleFont, controlsBrush, controlsTitleX, controlsY - 36);

                for (int i = 0; i < controls.Length; i++)
                {
                    SizeF controlsSize = g.MeasureString(controls[i], controlsFont);
                    float controlsX = (renderWidth - controlsSize.Width) / 2;
                    float lineY = controlsY + i * 25;
                    g.DrawString(controls[i], controlsFont, textShadow, controlsX + 2, lineY + 2);
                    g.DrawString(controls[i], controlsFont, controlsBrush, controlsX, lineY);
                }

                using (Font watermarkFont = new Font("Verdana", 11, FontStyle.Regular))
                using (Brush watermarkBrush = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
                {
                    string watermark = "Made by Egor Gabov";
                    SizeF watermarkSize = g.MeasureString(watermark, watermarkFont);
                    g.DrawString(watermark, watermarkFont, watermarkBrush,
                        renderWidth - watermarkSize.Width - 14,
                        renderHeight - watermarkSize.Height - 10);
                }

            }
        }

        private void DrawStoryScreen(Graphics g, string title)
        {
            string backgroundKey = game.State == GameState.Outro ? "outro_bg" : "intro_bg";
            DrawBackground(g, backgroundKey);

            using (Font titleFont = new Font("Consolas", 34, FontStyle.Bold))
            using (Font bodyFont = new Font("Consolas", 16))
            using (Font hintFont = new Font("Consolas", 12))
            using (Brush panel = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            using (Pen border = new Pen(Color.FromArgb(220, 215, 170, 85), 2))
            {
                Rectangle box = new Rectangle(
                    renderWidth / 2 - 430,
                    renderHeight / 2 - 190,
                    860,
                    380);

                g.FillRectangle(panel, box);
                g.DrawRectangle(border, box);
                DrawCenteredString(g, title, titleFont, Brushes.Gold, box.Top + 35);

                int lineHeight = 32;
                int y = box.Top + 110;

                string storyText = game.StoryText;

                string[] lines = WrapText(g, storyText, bodyFont, box.Width - 90);

                foreach (var line in lines)
                {
                    SizeF size = g.MeasureString(line, bodyFont);
                    g.DrawString(line, bodyFont, Brushes.White, box.Left + (box.Width - size.Width) / 2, y);
                    y += lineHeight;
                }

                string hint = game.State == GameState.Outro ? "ENTER - В МЕНЮ" : "ENTER - ПРОДОЛЖИТЬ";
                DrawCenteredString(g, hint, hintFont, Brushes.Gray, box.Bottom - 55);
            }
        }

        private string[] WrapText(Graphics g, string text, Font font, int maxWidth)
        {
            var lines = new List<string>();
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
                g.DrawImage(image, 0, 0, renderWidth, renderHeight);
                using (Brush shade = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                    g.FillRectangle(shade, 0, 0, renderWidth, renderHeight);
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
                int startX = (renderWidth - totalWidth) / 2;
                int y = renderHeight / 2 - size / 2;

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
            if (level == 1) return "tiger";
            if (level == 2) return "warrior";
            if (level == 3) return "chariot";
            if (level == 4) return "pretorian";
            return "boss";
        }

        private string GetWeaponStoryText(int level)
        {
            if (level == 2)
                return "Копье: легкое оружие для точных бросков и контроля дистанции. Это копье было выдано тебе тайными союзниками, чтобы пробить строй воинов и сохранить здоровье на арене.";
            if (level == 3)
                return "Пилум: тяжелое метательное копье, которое пробивает щиты и оставляет противника уязвимым. Оно отлито из прочного железа и передано тебе ветераном легиона.";
            if (level == 4)
                return "Фалкс: оружие ближнего боя с широким лезвием для мощных взмахов. Ты получил его от преторианца, который решил предать строй и помочь тебе прорваться к императору.";
            if (level == 5)
                return "Пилум и Фалкс: теперь у тебя есть комбинированный арсенал — дальний Пилум и ближний Фалкс. Эта пара стала твоим шансом пройти через империю к трону.";
            return "";
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
                g.FillRectangle(overlay, 0, 0, renderWidth, renderHeight);
                DrawCenteredString(g, "ПАУЗА", titleFont, Brushes.Gold, renderHeight / 2 - 160);

                int w = 310;
                int h = 52;
                int x = renderWidth / 2 - w / 2;
                int y = renderHeight / 2 - 70;

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

        private void DrawGame(Graphics g)
        {
            if (sprites.TryGetValue("arena_bg", out Image? arena))
            {
                g.DrawImage(arena, 0, 0, renderWidth, renderHeight);
                DrawGates(g);
            }
            else
            {
                g.Clear(Color.FromArgb(10, 10, 20));
                DrawMap(g);
            }

            foreach (var wall in game.walls)
            {
                if (wall.IsDestructible)
                    DrawDestructibleWall(g, wall);
                else
                    DrawSolidWall(g, wall);
            }

            DrawGladiator(g);

            foreach (var enemy in game.enemies)
                DrawEnemy(g, enemy);

            foreach (var slash in game.slashes)
                using (Brush slashBrush = new SolidBrush(Color.FromArgb(150, 255, 245, 170)))
                    g.FillEllipse(slashBrush, slash.Bounds);

            foreach (var b in game.bullets)
            {
                if (!b.IsEnemyBullet)
                {
                    RectangleF drawRect = b.Bounds;
                    
                    // Отрисовка снарядов игрока с ротацией
                    g.TranslateTransform(drawRect.X + drawRect.Width / 2, drawRect.Y + drawRect.Height / 2);
                    g.RotateTransform(b.RotationAngle);
                    
                    using (Pen goldPen = new Pen(Color.Gold, 5))
                        g.DrawLine(goldPen, -20, 0, 20, 0);
                    
                    g.ResetTransform();
                }
                else if (b.BulletType == 4)
                {
                    RectangleF drawRect = b.Bounds;

                    g.TranslateTransform(drawRect.X + drawRect.Width / 2, drawRect.Y + drawRect.Height / 2);
                    g.RotateTransform(b.RotationAngle);

                    using (Brush shaftBrush = new SolidBrush(Color.DarkRed))
                    using (Brush headBrush = new SolidBrush(Color.Purple))
                    {
                        g.FillRectangle(shaftBrush, -12, -2, 24, 4);
                        g.FillPolygon(headBrush, new Point[]
                        {
                            new Point(12, -5),
                            new Point(18, 0),
                            new Point(12, 5)
                        });
                    }

                    g.ResetTransform();
                }
                else if (b.BulletType == 3)
                {
                    RectangleF drawRect = b.Bounds;

                    g.TranslateTransform(drawRect.X + drawRect.Width / 2, drawRect.Y + drawRect.Height / 2);
                    g.RotateTransform(b.RotationAngle);

                    using (Pen arrowPen = new Pen(Color.SaddleBrown, 2))
                    {
                        g.DrawLine(arrowPen, -10, 0, 10, 0);
                        g.FillPolygon(Brushes.LightGray, new Point[]
                        {
                            new Point(10, -3),
                            new Point(15, 0),
                            new Point(10, 3)
                        });
                    }

                    g.ResetTransform();
                }
                else
                {
                    g.FillRectangle(Brushes.Magenta, b.Bounds);
                }
            }

            foreach (var p in game.particles)
                g.FillEllipse(Brushes.Orange, p.Position.X, p.Position.Y, 3, 3);

            foreach (var p in game.powerUps)
            {
                if (sprites.TryGetValue("heal_item", out Image? heal))
                {
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
            if (type == EnemyType.Tiger) return "tiger";
            if (type == EnemyType.Warrior) return "warrior";
            if (type == EnemyType.Praetorian) return "pretorian";
            if (type == EnemyType.Chariot) return "chariot";
            if (type == EnemyType.Emperor) return "boss";
            return "warrior";
        }

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
                g.FillRectangle(overlay, 0, 0, renderWidth, renderHeight);

                Rectangle panelRect = new Rectangle(
                    renderWidth / 2 - 260,
                    renderHeight / 2 - 190,
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
            g.DrawString(text, font, brush, (renderWidth - size.Width) / 2, y);
        }

        private void DrawShopLine(Graphics g, Font font, int key, string name, int cost, string status, int y)
        {
            string text = $"{key}. {name}  [{cost}]  {status}";
            SizeF size = g.MeasureString(text, font);
            Brush brush = game.Score >= cost ? Brushes.White : Brushes.Gray;
            g.DrawString(text, font, brush, (renderWidth - size.Width) / 2, y);
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
                Rectangle top = new Rectangle(renderWidth / 2 - 45, 0, 90, 18);
                Rectangle bottom = new Rectangle(renderWidth / 2 - 45, renderHeight - 18, 90, 18);
                Rectangle left = new Rectangle(0, renderHeight / 2 - 45, 18, 90);
                Rectangle right = new Rectangle(renderWidth - 18, renderHeight / 2 - 45, 18, 90);
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

        private void DrawGameOver(Graphics g)
        {
            DrawGame(g);
            using (Font font = new Font("Consolas", 32, FontStyle.Bold))
            using (Font small = new Font("Consolas", 14, FontStyle.Bold))
            {
                string text = "ИГРА ОКОНЧЕНА";
                SizeF size = g.MeasureString(text, font);
                g.DrawString(text, font, Brushes.White,
                    (renderWidth - size.Width) / 2,
                    renderHeight / 2 - 50);
                string restart = "Нажмите Enter для продолжения";
                SizeF size2 = g.MeasureString(restart, small);
                float restartX = (renderWidth - size2.Width) / 2;
                float restartY = renderHeight / 2 + 10;

                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        g.DrawString(restart, small, Brushes.Black, restartX + dx, restartY + dy);
                    }
                }

                g.DrawString(restart, small, Brushes.White, restartX, restartY);
            }
        }

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

            if (game.CampaignLevel == 5 && game.BossHP > 0)
            {
                int bossBarWidth = 400;
                int bossHpWidth = Math.Max(0, game.BossHP) * bossBarWidth / game.BossMaxHP;
                int bossBarX = (renderWidth - bossBarWidth) / 2;
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
            if (game.CurrentWeapon == WeaponType.Knife) key = "weapon_knife";
            else if (game.CurrentWeapon == WeaponType.Spear) key = "weapon_spear";
            else if (game.CurrentWeapon == WeaponType.Pilum) key = "pilum_spear";
            else if (game.CurrentWeapon == WeaponType.FireSword) key = "weapon_firesword";
            else if (game.CurrentWeapon == WeaponType.Falx) key = "weapon_falx";

            Rectangle rect = new Rectangle(renderWidth - 92, renderHeight - 92, 72, 72);
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
            if (game.CurrentWeapon == WeaponType.Knife) return "Нож";
            if (game.CurrentWeapon == WeaponType.Spear) return "Копье";
            if (game.CurrentWeapon == WeaponType.Pilum) return "Пилум";
            if (game.CurrentWeapon == WeaponType.FireSword) return "Огненный меч";
            if (game.CurrentWeapon == WeaponType.Falx) return "Фалкс";
            return "Меч";
        }

        private void DrawGradient(Graphics g)
        {
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, renderWidth, renderHeight), Color.Black, Color.DarkBlue, 90f))
            {
                g.FillRectangle(brush, new Rectangle(0, 0, renderWidth, renderHeight));
            }
        }

        private void DrawRain(Graphics g)
        {
            using (Font font = new Font("Consolas", 8))
            {
                for (int i = 0; i < rain.Count; i++)
                {
                    g.DrawString(rain[i], font, Brushes.Green, i * 25, (i * 20 + Environment.TickCount / 10) % renderHeight);
                }
            }
        }
    }
}
