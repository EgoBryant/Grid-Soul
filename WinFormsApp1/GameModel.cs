using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinFormsApp1
{
    public class Bullet
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float VX { get; set; }
        public float VY { get; set; }
        public bool IsAlive { get; set; } = true;
        public void Move() { X += VX; Y += VY; }
    }

    public class Player
    {
        public float X { get; set; } = 400;
        public float Y { get; set; } = 300;
        public float Speed = 5f;
        public int HP = 100;
        public void Move(float dx, float dy) { X += dx * Speed; Y += dy * Speed; }
    }

    public class Enemy
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Speed = 2f;
        public int HP = 30;
        public void MoveTowards(float tx, float ty)
        {
            float dx = tx - X;
            float dy = ty - Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist > 1) { X += (dx / dist) * Speed; Y += (dy / dist) * Speed; }
        }
    }

    public class GameModel
    {
        public Player Hero { get; } = new Player();
        public List<Bullet> Bullets { get; } = new List<Bullet>();
        public List<Enemy> Enemies { get; } = new List<Enemy>();
        private DateTime _lastShoot = DateTime.MinValue;
        private DateTime _lastSpawn = DateTime.Now;
        private Random _rnd = new Random();

        public void Update()
        {
            if (Hero.HP <= 0) return;

            // Спавн врагов каждые 3 секунды
            if ((DateTime.Now - _lastSpawn).TotalSeconds > 3)
            {
                Enemies.Add(new Enemy { X = _rnd.Next(800), Y = _rnd.Next(2) == 0 ? -30 : 630 });
                _lastSpawn = DateTime.Now;
            }

            foreach (var b in Bullets) b.Move();
            foreach (var e in Enemies)
            {
                e.MoveTowards(Hero.X, Hero.Y);
                if (new RectangleF(Hero.X, Hero.Y, 30, 30).IntersectsWith(new RectangleF(e.X, e.Y, 30, 30)))
                    Hero.HP -= 1;
            }

            foreach (var b in Bullets)
                foreach (var e in Enemies)
                    if (new RectangleF(b.X, b.Y, 8, 8).IntersectsWith(new RectangleF(e.X, e.Y, 30, 30)))
                    { e.HP -= 10; b.IsAlive = false; }

            Bullets.RemoveAll(b => !b.IsAlive || b.X < -100 || b.X > 1000 || b.Y < -100 || b.Y > 1000);
            Enemies.RemoveAll(e => e.HP <= 0);
        }

        public void Shoot(float vx, float vy)
        {
            if ((DateTime.Now - _lastShoot).TotalMilliseconds > 200)
            {
                Bullets.Add(new Bullet { X = Hero.X + 12, Y = Hero.Y + 12, VX = vx, VY = vy });
                _lastShoot = DateTime.Now;
            }
        }
    }
}