using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private GameModel _model = new GameModel();
        private System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
        private HashSet<Keys> _keys = new HashSet<Keys>();

        public Form1()
        {
            this.DoubleBuffered = true;
            this.ClientSize = new Size(800, 600);
            this.BackColor = Color.FromArgb(30, 30, 30);
            _timer.Interval = 20;
            _timer.Tick += (s, e) => { UpdateInput(); _model.Update(); Invalidate(); };
            _timer.Start();
            this.KeyDown += (s, e) => _keys.Add(e.KeyCode);
            this.KeyUp += (s, e) => _keys.Remove(e.KeyCode);
        }

        private void UpdateInput()
        {
            float dx = 0, dy = 0;
            if (_keys.Contains(Keys.W)) dy--; if (_keys.Contains(Keys.S)) dy++;
            if (_keys.Contains(Keys.A)) dx--; if (_keys.Contains(Keys.D)) dx++;
            _model.Hero.Move(dx, dy);

            if (_keys.Contains(Keys.Up)) _model.Shoot(0, -12);
            else if (_keys.Contains(Keys.Down)) _model.Shoot(0, 12);
            else if (_keys.Contains(Keys.Left)) _model.Shoot(-12, 0);
            else if (_keys.Contains(Keys.Right)) _model.Shoot(12, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            foreach (var b in _model.Bullets) g.FillEllipse(Brushes.Gold, b.X, b.Y, 8, 8);
            foreach (var en in _model.Enemies)
            {
                g.FillRectangle(Brushes.Crimson, en.X, en.Y, 30, 30);
                g.FillRectangle(Brushes.Lime, en.X, en.Y - 10, (en.HP / 30f) * 30, 5);
            }
            g.FillEllipse(Brushes.DeepSkyBlue, _model.Hero.X, _model.Hero.Y, 30, 30);

            // Èםעונפויס חהמנמגüÿ
            g.FillRectangle(Brushes.DarkRed, 10, 10, 200, 20);
            g.FillRectangle(Brushes.Red, 10, 10, Math.Max(0, _model.Hero.HP * 2), 20);

            if (_model.Hero.HP <= 0)
                g.DrawString("GAME OVER", new Font("Arial", 40), Brushes.White, 250, 250);
        }
    }
}