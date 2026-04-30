using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly GameModel _model = new GameModel();
        private readonly Timer _gameTimer = new Timer();
        private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();

        public Form1()
        {
            InitializeComponent();

            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Color.FromArgb(10, 12, 28);

            _gameTimer.Interval = 20;
            _gameTimer.Tick += GameTick;
            _gameTimer.Start();

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
        }

        private void GameTick(object sender, EventArgs e)
        {
            HandleInput();
            _model.Update(0.02f, ClientSize.Width, ClientSize.Height);
            Invalidate();
        }

        private void HandleInput()
        {
            float dx = 0f;
            float dy = 0f;

            if (_pressedKeys.Contains(Keys.W)) dy -= 1f;
            if (_pressedKeys.Contains(Keys.S)) dy += 1f;
            if (_pressedKeys.Contains(Keys.A)) dx -= 1f;
            if (_pressedKeys.Contains(Keys.D)) dx += 1f;

            _model.MovePlayer(dx, dy, ClientSize.Width, ClientSize.Height);

            const float bulletSpeed = 11f;
            if (_pressedKeys.Contains(Keys.Up)) _model.Shoot(0f, -bulletSpeed);
            if (_pressedKeys.Contains(Keys.Down)) _model.Shoot(0f, bulletSpeed);
            if (_pressedKeys.Contains(Keys.Left)) _model.Shoot(-bulletSpeed, 0f);
            if (_pressedKeys.Contains(Keys.Right)) _model.Shoot(bulletSpeed, 0f);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            _pressedKeys.Add(e.KeyCode);
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            _pressedKeys.Remove(e.KeyCode);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(8, 10, 22));

            DrawParticles(g);
            DrawBullets(g);
            DrawEnemies(g);
            DrawHero(g);
            DrawHud(g);

            if (_model.IsGameOver)
            {
                DrawGameOver(g);
            }
        }

        private void DrawHero(Graphics g)
        {
            g.FillEllipse(Brushes.DodgerBlue, _model.Hero.X, _model.Hero.Y, _model.Hero.Size, _model.Hero.Size);
        }

        private void DrawEnemies(Graphics g)
        {
            using Pen borderPen = new Pen(Color.White, 1.5f);
            foreach (Enemy enemy in _model.Enemies)
            {
                g.FillRectangle(Brushes.IndianRed, enemy.X, enemy.Y, enemy.Size, enemy.Size);
                g.DrawRectangle(borderPen, enemy.X, enemy.Y, enemy.Size, enemy.Size);
            }
        }

        private void DrawBullets(Graphics g)
        {
            foreach (Bullet bullet in _model.Bullets)
            {
                g.FillEllipse(Brushes.Gold, bullet.X, bullet.Y, bullet.Size, bullet.Size);
            }
        }

        private void DrawParticles(Graphics g)
        {
            foreach (Particle particle in _model.Particles)
            {
                int alpha = (int)(255f * Math.Max(0f, particle.Life / particle.MaxLife));
                using Brush brush = new SolidBrush(Color.FromArgb(alpha, 255, 170, 40));
                g.FillEllipse(brush, particle.X, particle.Y, particle.Size, particle.Size);
            }
        }

        private void DrawHud(Graphics g)
        {
            using Font hudFont = new Font("Segoe UI", 11f, FontStyle.Bold);
            using Brush textBrush = new SolidBrush(Color.WhiteSmoke);

            g.DrawString($"Level: {_model.Level}", hudFont, textBrush, 12, 10);
            g.DrawString($"Score: {_model.Score}", hudFont, textBrush, 12, 34);

            int barX = 12;
            int barY = 62;
            int barW = 220;
            int barH = 18;

            g.FillRectangle(Brushes.DarkSlateGray, barX, barY, barW, barH);
            float hpRatio = _model.Hero.MaxHP <= 0 ? 0f : _model.Hero.HP / (float)_model.Hero.MaxHP;
            g.FillRectangle(Brushes.LimeGreen, barX, barY, (int)(barW * hpRatio), barH);
            g.DrawRectangle(Pens.White, barX, barY, barW, barH);
        }

        private void DrawGameOver(Graphics g)
        {
            using Font overFont = new Font("Segoe UI", 36f, FontStyle.Bold);
            using Font subFont = new Font("Segoe UI", 14f, FontStyle.Regular);
            using StringFormat format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            Rectangle bounds = ClientRectangle;
            g.DrawString("GAME OVER", overFont, Brushes.White, bounds, format);
            g.DrawString($"Final Score: {_model.Score}", subFont, Brushes.Gainsboro, bounds.X + bounds.Width / 2f - 80f, bounds.Y + bounds.Height / 2f + 55f);
        }
    }
}
