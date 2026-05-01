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

        // эффект "дожд€"
        private List<string> rain = new List<string>();
        private Random rand = new Random();

        public Form1()
        {
            InitializeComponent();

            DoubleBuffered = true;
            KeyPreview = true;

            timer.Interval = 20;
            timer.Tick += GameLoop;
            timer.Start();

            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;

            // инициализаци€ "дожд€"
            for (int i = 0; i < 30; i++)
                rain.Add(RandomString(20));
        }

        private void GameLoop(object sender, EventArgs e)
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

            if (game.State == GameState.Menu)
                DrawMenu(g);
            else if (game.State == GameState.Playing)
                DrawGame(g);
            else if (game.State == GameState.GameOver)
                DrawGameOver(g);
        }

        // ===================== MENU =====================
        private void DrawMenu(Graphics g)
        {
            DrawGradient(g);
            DrawRain(g);

            using (Font titleFont = new Font("Consolas", 36, FontStyle.Bold))
            using (Font infoFont = new Font("Consolas", 14))
            {
                string title = "GRID SOUL";
                SizeF size = g.MeasureString(title, titleFont);

                g.DrawString(title, titleFont, Brushes.Cyan,
                    (ClientSize.Width - size.Width) / 2, 150);

                string info = "PRESS ENTER TO START";
                SizeF size2 = g.MeasureString(info, infoFont);

                g.DrawString(info, infoFont, Brushes.White,
                    (ClientSize.Width - size2.Width) / 2, 250);
            }
        }

        // ===================== GAME =====================
        private void DrawGame(Graphics g)
        {
            g.Clear(Color.FromArgb(10, 10, 20));

            // стены (grid стиль)
            foreach (var wall in game.walls)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(40, 40, 40)), wall.Bounds);
                g.DrawRectangle(Pens.Cyan,
                    wall.Bounds.X,
                    wall.Bounds.Y,
                    wall.Bounds.Width,
                    wall.Bounds.Height);
            }

            // игрок
            g.FillEllipse(Brushes.Blue, game.player.Bounds);

            // враги
            foreach (var enemy in game.enemies)
            {
                g.FillRectangle(Brushes.Red, enemy.Bounds);
                g.DrawRectangle(Pens.White,
                    enemy.Bounds.X,
                    enemy.Bounds.Y,
                    enemy.Bounds.Width,
                    enemy.Bounds.Height);
            }

            // пули
            foreach (var b in game.bullets)
                g.FillRectangle(Brushes.Yellow, b.Bounds);

            // частицы
            foreach (var p in game.particles)
                g.FillEllipse(Brushes.Orange, p.Position.X, p.Position.Y, 3, 3);

            // бонусы
            foreach (var p in game.powerUps)
                g.FillEllipse(Brushes.Lime, p.Bounds);

            DrawHUD(g);
        }

        // ===================== GAME OVER =====================
        private void DrawGameOver(Graphics g)
        {
            DrawGame(g);

            using (Font font = new Font("Consolas", 32, FontStyle.Bold))
            using (Font small = new Font("Consolas", 14))
            {
                string text = "GAME OVER";
                SizeF size = g.MeasureString(text, font);

                g.DrawString(text, font, Brushes.White,
                    (ClientSize.Width - size.Width) / 2,
                    ClientSize.Height / 2 - 50);

                string restart = "PRESS ENTER TO RESTART";
                SizeF size2 = g.MeasureString(restart, small);

                g.DrawString(restart, small, Brushes.Gray,
                    (ClientSize.Width - size2.Width) / 2,
                    ClientSize.Height / 2 + 10);
            }
        }

        // ===================== HUD =====================
        private void DrawHUD(Graphics g)
        {
            g.DrawString($"Score: {game.Score}", Font, Brushes.White, 10, 10);
            g.DrawString($"Level: {game.Level}", Font, Brushes.White, 10, 30);

            int maxHP = 100;
            int barWidth = 200;
            int hpWidth = Math.Max(0, game.player.HP) * barWidth / maxHP;

            g.DrawRectangle(Pens.White, 10, 55, barWidth, 12);
            g.FillRectangle(Brushes.Red, 10, 55, hpWidth, 12);
        }

        // ===================== INPUT =====================
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // запуск / рестарт
            if (e.KeyCode == Keys.Enter)
            {
                if (game.State == GameState.Menu || game.State == GameState.GameOver)
                {
                    game.StartGame(ClientSize.Width, ClientSize.Height);
                }
            }

            if (game.State != GameState.Playing) return;

            if (e.KeyCode == Keys.W) up = true;
            if (e.KeyCode == Keys.S) down = true;
            if (e.KeyCode == Keys.A) left = true;
            if (e.KeyCode == Keys.D) right = true;

            if (e.KeyCode == Keys.Up) game.Shoot(0, -1);
            if (e.KeyCode == Keys.Down) game.Shoot(0, 1);
            if (e.KeyCode == Keys.Left) game.Shoot(-1, 0);
            if (e.KeyCode == Keys.Right) game.Shoot(1, 0);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
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