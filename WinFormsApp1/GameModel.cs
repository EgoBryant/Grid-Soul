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

    public class Particle
    {
        public float X, Y, VX, VY;
        public int Life = 20; // Время жизни в кадрах
        public void Move() { X += VX; Y += VY; Life--; }
    }

    public class Player
    {
        public float X { get; set; } = 400;
        public float Y { get; set; } = 300;
        public float Speed = 5f;
        public int HP = 100;
        public int Score = 0;
        public void Move(float dx, float dy) { X += dx * Speed; Y += dy * Speed; }
    }

    public class Enemy
    {
        public float X, Y;
        public float Speed = 2f;
        public float HP = 30;
        public void MoveTowards(float tx, float ty)
        {
            float dx = tx - X, dy = ty - Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist > 1) { X += (dx / dist) * Speed; Y += (dy / dist) * Speed; }
        }
    }

    public class GameModel
    {
        public Player Hero { get; } = new Player();
        public List<Bullet> Bullets { get; } = new List<Bullet>();
        public List<Enemy> Enemies { get; } = new List<Enemy>();
        public List<Particle> Particles { get; } = new List<Particle>();

        public int Level = 1;
        private DateTime _lastShoot = DateTime.MinValue;
        private DateTime _lastSpawn = DateTime.Now;
        private Random _rnd = new Random();

        public void Update()
        {
            if (Hero.HP <= 0) return;

            // Повышение уровня каждые 500 очков
            Level = (Hero.Score / 500) + 1;

            // Спавн врагов (ускоряется с уровнем)
            double spawnInterval = Math.Max(0.5, 3.0 - (Level * 0.2));
            if ((DateTime.Now - _lastSpawn).TotalSeconds > spawnInterval)
            {
                Enemies.Add(new Enemy
                {
                    X = _rnd.Next(800),
                    Y = _rnd.Next(2) == 0 ? -30 : 630,
                    HP = 30 + (Level * 10) // Враги жирнее с каждым уровнем
                });
                _lastSpawn = DateTime.Now;
            }

            foreach (var b in Bullets) b.Move();
            foreach (var p in Particles) p.Move();
            foreach (var e in Enemies)
            {
                e.MoveTowards(Hero.X, Hero.Y);
                if (new RectangleF(Hero.X, Hero.Y, 30, 30).IntersectsWith(new RectangleF(e.X, e.Y, 30, 30)))
                    Hero.HP -= 1;
            }

            foreach (var b in Bullets)
                foreach (var e in Enemies)
                    if (new RectangleF(b.X, b.Y, 8, 8).IntersectsWith(new RectangleF(e.X, e.Y, 30, 30)))
                    {
                        e.HP -= 15;
                        b.IsAlive = false;
                        if (e.HP <= 0)
                        {
                            Hero.Score += 100;
                            CreateExplosion(e.X + 15, e.Y + 15);
                        }
                    }

            Bullets.RemoveAll(b => !b.IsAlive || b.X < -100 || b.X > 1000 || b.Y < -100 || b.Y > 1000);
            Enemies.RemoveAll(e => e.HP <= 0);
            Particles.RemoveAll(p => p.Life <= 0);
        }

        private void CreateExplosion(float x, float y)
        {
            for (int i = 0; i < 8; i++)
                Particles.Add(new Particle
                {
                    X = x,
                    Y = y,
                    VX = (float)(_rnd.NextDouble() * 6 - 3),
                    VY = (float)(_rnd.NextDouble() * 6 - 3)
                });
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