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
        public float Size { get; set; } = 8f;

        public void Move()
        {
            X += VX;
            Y += VY;
        }
    }

    public class Player
    {
        public float X { get; set; } = 385f;
        public float Y { get; set; } = 285f;
        public float Speed { get; set; } = 4.5f;
        public float Size { get; set; } = 30f;
        public int HP { get; set; } = 100;
        public int MaxHP { get; set; } = 100;

        public void Move(float dx, float dy)
        {
            X += dx * Speed;
            Y += dy * Speed;
        }
    }

    public class Enemy
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Speed { get; set; } = 1.8f;
        public float Size { get; set; } = 28f;
        public int HP { get; set; } = 30;
        public int MaxHP { get; set; } = 30;

        public void MoveTowards(float tx, float ty)
        {
            float dx = tx - X;
            float dy = ty - Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist > 0.001f)
            {
                X += (dx / dist) * Speed;
                Y += (dy / dist) * Speed;
            }
        }
    }

    public class Particle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float VX { get; set; }
        public float VY { get; set; }
        public float Life { get; set; } = 0.5f;
        public float MaxLife { get; set; } = 0.5f;
        public float Size { get; set; } = 4f;

        public bool IsAlive => Life > 0f;

        public void Update(float dt)
        {
            X += VX;
            Y += VY;
            VX *= 0.97f;
            VY *= 0.97f;
            Life -= dt;
        }
    }

    public class GameModel
    {
        private readonly Random _random = new Random();
        private DateTime _lastShotTime = DateTime.MinValue;
        private DateTime _lastSpawnTime = DateTime.MinValue;

        public Player Hero { get; } = new Player();
        public List<Bullet> Bullets { get; } = new List<Bullet>();
        public List<Enemy> Enemies { get; } = new List<Enemy>();
        public List<Particle> Particles { get; } = new List<Particle>();

        public int Score { get; private set; }
        public int Level => (Score / 500) + 1;
        public bool IsGameOver => Hero.HP <= 0;

        public float SpawnIntervalSeconds => Math.Max(0.35f, 2.0f - (Level - 1) * 0.12f);
        public int EnemyBaseHP => 30 + (Level - 1) * 6;
        public float EnemyBaseSpeed => 1.6f + (Level - 1) * 0.08f;

        public void Update(float deltaTime, int worldWidth, int worldHeight)
        {
            if (IsGameOver)
            {
                UpdateParticles(deltaTime);
                return;
            }

            SpawnEnemies(worldWidth, worldHeight);
            MoveBullets(worldWidth, worldHeight);
            MoveEnemies();
            ResolveCollisions();
            UpdateParticles(deltaTime);
            ClampPlayer(worldWidth, worldHeight);
        }

        public void MovePlayer(float dx, float dy, int worldWidth, int worldHeight)
        {
            if (IsGameOver)
            {
                return;
            }

            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len > 0f)
            {
                dx /= len;
                dy /= len;
            }

            Hero.Move(dx, dy);
            ClampPlayer(worldWidth, worldHeight);
        }

        public void Shoot(float vx, float vy)
        {
            if (IsGameOver)
            {
                return;
            }

            if ((DateTime.Now - _lastShotTime).TotalMilliseconds < 140)
            {
                return;
            }

            Bullets.Add(new Bullet
            {
                X = Hero.X + Hero.Size * 0.5f - 4f,
                Y = Hero.Y + Hero.Size * 0.5f - 4f,
                VX = vx,
                VY = vy
            });

            _lastShotTime = DateTime.Now;
        }

        private void SpawnEnemies(int worldWidth, int worldHeight)
        {
            if ((DateTime.Now - _lastSpawnTime).TotalSeconds < SpawnIntervalSeconds)
            {
                return;
            }

            int side = _random.Next(4);
            float x;
            float y;

            switch (side)
            {
                case 0:
                    x = _random.Next(0, worldWidth);
                    y = -35;
                    break;
                case 1:
                    x = worldWidth + 35;
                    y = _random.Next(0, worldHeight);
                    break;
                case 2:
                    x = _random.Next(0, worldWidth);
                    y = worldHeight + 35;
                    break;
                default:
                    x = -35;
                    y = _random.Next(0, worldHeight);
                    break;
            }

            int hp = EnemyBaseHP;
            Enemies.Add(new Enemy
            {
                X = x,
                Y = y,
                HP = hp,
                MaxHP = hp,
                Speed = EnemyBaseSpeed + (float)_random.NextDouble() * 0.35f
            });

            _lastSpawnTime = DateTime.Now;
        }

        private void MoveBullets(int worldWidth, int worldHeight)
        {
            foreach (Bullet bullet in Bullets)
            {
                bullet.Move();
                if (bullet.X < -40 || bullet.X > worldWidth + 40 || bullet.Y < -40 || bullet.Y > worldHeight + 40)
                {
                    bullet.IsAlive = false;
                }
            }

            Bullets.RemoveAll(b => !b.IsAlive);
        }

        private void MoveEnemies()
        {
            float heroCenterX = Hero.X + Hero.Size * 0.5f;
            float heroCenterY = Hero.Y + Hero.Size * 0.5f;

            foreach (Enemy enemy in Enemies)
            {
                enemy.MoveTowards(heroCenterX - enemy.Size * 0.5f, heroCenterY - enemy.Size * 0.5f);
            }
        }

        private void ResolveCollisions()
        {
            RectangleF heroRect = new RectangleF(Hero.X, Hero.Y, Hero.Size, Hero.Size);

            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy enemy = Enemies[i];
                RectangleF enemyRect = new RectangleF(enemy.X, enemy.Y, enemy.Size, enemy.Size);

                if (heroRect.IntersectsWith(enemyRect))
                {
                    Hero.HP = Math.Max(0, Hero.HP - 1);
                }
            }

            for (int b = 0; b < Bullets.Count; b++)
            {
                Bullet bullet = Bullets[b];
                if (!bullet.IsAlive)
                {
                    continue;
                }

                RectangleF bulletRect = new RectangleF(bullet.X, bullet.Y, bullet.Size, bullet.Size);
                for (int e = 0; e < Enemies.Count; e++)
                {
                    Enemy enemy = Enemies[e];
                    RectangleF enemyRect = new RectangleF(enemy.X, enemy.Y, enemy.Size, enemy.Size);

                    if (bulletRect.IntersectsWith(enemyRect))
                    {
                        enemy.HP -= 10;
                        bullet.IsAlive = false;

                        if (enemy.HP <= 0)
                        {
                            Score += 100;
                            CreateExplosion(enemy.X + enemy.Size * 0.5f, enemy.Y + enemy.Size * 0.5f);
                        }

                        break;
                    }
                }
            }

            Bullets.RemoveAll(b => !b.IsAlive);
            Enemies.RemoveAll(e => e.HP <= 0);
        }

        private void CreateExplosion(float centerX, float centerY)
        {
            int count = _random.Next(8, 11);
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(Math.PI * 2 * i / count);
                float speed = 2f + (float)_random.NextDouble() * 3f;

                Particles.Add(new Particle
                {
                    X = centerX,
                    Y = centerY,
                    VX = (float)Math.Cos(angle) * speed,
                    VY = (float)Math.Sin(angle) * speed,
                    Life = 0.45f + (float)_random.NextDouble() * 0.25f,
                    MaxLife = 0.7f,
                    Size = 3f + (float)_random.NextDouble() * 2f
                });
            }
        }

        private void UpdateParticles(float deltaTime)
        {
            foreach (Particle particle in Particles)
            {
                particle.Update(deltaTime);
            }

            Particles.RemoveAll(p => !p.IsAlive);
        }

        private void ClampPlayer(int worldWidth, int worldHeight)
        {
            Hero.X = Math.Max(0, Math.Min(worldWidth - Hero.Size, Hero.X));
            Hero.Y = Math.Max(0, Math.Min(worldHeight - Hero.Size, Hero.Y));
        }
    }
}
