using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinFormsApp1
{
    public enum GameState
    {
        Menu,
        Playing,
        GameOver
    }

    public class Player
    {
        public RectangleF Bounds;
        public float Speed = 4f;
        public int HP = 100;
    }

    public class Enemy
    {
        public RectangleF Bounds;
        public float Speed;
        public int HP;
        public PointF Velocity;
    }

    public class Bullet
    {
        public RectangleF Bounds;
        public float dx, dy;
    }

    public class Particle
    {
        public PointF Position;
        public float dx, dy;
        public int Life;
    }

    public class Wall
    {
        public RectangleF Bounds;
    }

    public enum PowerUpType
    {
        Heal
    }

    public class PowerUp
    {
        public RectangleF Bounds;
        public PowerUpType Type;
    }

    public class GameModel
    {
        public GameState State = GameState.Menu;

        public Player player = new Player();
        public List<Enemy> enemies = new List<Enemy>();
        public List<Bullet> bullets = new List<Bullet>();
        public List<Particle> particles = new List<Particle>();
        public List<Wall> walls = new List<Wall>();
        public List<PowerUp> powerUps = new List<PowerUp>();

        public int Score = 0;
        public int Level = 1;

        private Random rand = new Random();
        private int spawnTimer = 0;
        private int powerUpTimer = 0;

        // ================= START =================
        public void StartGame(int width, int height)
        {
            State = GameState.Playing;

            player = new Player();
            enemies.Clear();
            bullets.Clear();
            particles.Clear();
            walls.Clear();
            powerUps.Clear();

            Score = 0;
            Level = 1;

            player.Bounds = new RectangleF(width / 2f, height / 2f, 20, 20);

            GenerateWalls(width, height);
        }

        // ================= UPDATE =================
        public void Update(bool up, bool down, bool left, bool right, int width, int height)
        {
            if (State != GameState.Playing)
                return;

            MovePlayer(up, down, left, right, width, height);
            MoveEnemies();
            MoveBullets(width, height);

            HandleCollisions();
            HandlePowerUps();
            UpdateParticles();

            spawnTimer++;
            powerUpTimer++;

            int spawnRate = Math.Max(20, 100 - Level * 5);

            if (spawnTimer >= spawnRate)
            {
                spawnTimer = 0;
                SpawnEnemy(width, height);
            }

            if (powerUpTimer >= 500)
            {
                powerUpTimer = 0;
                SpawnPowerUp(width, height);
            }

            Level = Score / 500 + 1;

            if (player.HP <= 0)
                State = GameState.GameOver;
        }

        // ================= PLAYER =================
        private void MovePlayer(bool up, bool down, bool left, bool right, int width, int height)
        {
            float oldX = player.Bounds.X;
            float oldY = player.Bounds.Y;

            if (up) player.Bounds.Y -= player.Speed;
            if (down) player.Bounds.Y += player.Speed;
            if (left) player.Bounds.X -= player.Speed;
            if (right) player.Bounds.X += player.Speed;

            foreach (var wall in walls)
            {
                if (player.Bounds.IntersectsWith(wall.Bounds))
                {
                    player.Bounds.X = oldX;
                    player.Bounds.Y = oldY;
                    break;
                }
            }

            player.Bounds.X = Math.Max(0, Math.Min(width - player.Bounds.Width, player.Bounds.X));
            player.Bounds.Y = Math.Max(0, Math.Min(height - player.Bounds.Height, player.Bounds.Y));
        }

        // ================= ENEMIES =================
        private void MoveEnemies()
        {
            foreach (var e in enemies)
            {
                PointF pos = new PointF(e.Bounds.X, e.Bounds.Y);

                // SEEK
                PointF toPlayer = new PointF(
                    player.Bounds.X - pos.X,
                    player.Bounds.Y - pos.Y
                );
                Normalize(ref toPlayer);

                // SEPARATION
                PointF separation = new PointF(0, 0);
                foreach (var other in enemies)
                {
                    if (other == e) continue;

                    float dx = pos.X - other.Bounds.X;
                    float dy = pos.Y - other.Bounds.Y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq < 900 && distSq > 0)
                    {
                        separation.X += dx / distSq;
                        separation.Y += dy / distSq;
                    }
                }

                // WALL AVOIDANCE
                PointF avoid = new PointF(0, 0);
                foreach (var wall in walls)
                {
                    if (DistanceToRect(pos, wall.Bounds) < 40)
                    {
                        float dx = pos.X - wall.Bounds.X;
                        float dy = pos.Y - wall.Bounds.Y;

                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (dist > 0)
                        {
                            avoid.X += dx / dist;
                            avoid.Y += dy / dist;
                        }
                    }
                }

                // COMBINE
                PointF move = new PointF(
                    toPlayer.X * 1.5f + separation.X * 2f + avoid.X * 2.5f,
                    toPlayer.Y * 1.5f + separation.Y * 2f + avoid.Y * 2.5f
                );

                Normalize(ref move);

                e.Velocity = new PointF(move.X * e.Speed, move.Y * e.Speed);

                float newX = e.Bounds.X + e.Velocity.X;
                float newY = e.Bounds.Y + e.Velocity.Y;

                RectangleF future = new RectangleF(newX, newY, e.Bounds.Width, e.Bounds.Height);

                bool collision = false;

                foreach (var wall in walls)
                {
                    if (future.IntersectsWith(wall.Bounds))
                    {
                        collision = true;
                        break;
                    }
                }

                if (!collision)
                    e.Bounds = future;
            }
        }

        // ================= BULLETS =================
        private void MoveBullets(int width, int height)
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                var b = bullets[i];

                b.Bounds.X += b.dx;
                b.Bounds.Y += b.dy;

                bool remove = false;

                if (b.Bounds.X < 0 || b.Bounds.Y < 0 ||
                    b.Bounds.X > width || b.Bounds.Y > height)
                    remove = true;

                foreach (var wall in walls)
                {
                    if (b.Bounds.IntersectsWith(wall.Bounds))
                    {
                        remove = true;
                        break;
                    }
                }

                if (remove)
                    bullets.RemoveAt(i);
            }
        }

        // ================= COLLISIONS =================
        private void HandleCollisions()
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i];

                if (e.Bounds.IntersectsWith(player.Bounds))
                    player.HP -= 1;

                for (int j = bullets.Count - 1; j >= 0; j--)
                {
                    if (e.Bounds.IntersectsWith(bullets[j].Bounds))
                    {
                        e.HP--;
                        bullets.RemoveAt(j);

                        if (e.HP <= 0)
                        {
                            CreateExplosion(e.Bounds.X, e.Bounds.Y);
                            enemies.RemoveAt(i);
                            Score += 100;
                            break;
                        }
                    }
                }
            }
        }

        // ================= POWERUPS =================
        private void HandlePowerUps()
        {
            for (int i = powerUps.Count - 1; i >= 0; i--)
            {
                if (powerUps[i].Bounds.IntersectsWith(player.Bounds))
                {
                    player.HP = Math.Min(100, player.HP + 20);
                    powerUps.RemoveAt(i);
                }
            }
        }

        // ================= PARTICLES =================
        private void UpdateParticles()
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                p.Position = new PointF(p.Position.X + p.dx, p.Position.Y + p.dy);
                p.Life--;

                if (p.Life <= 0)
                    particles.RemoveAt(i);
            }
        }

        // ================= SPAWN =================
        private void SpawnEnemy(int width, int height)
        {
            PointF pos = GetFreePosition(width, height, 20);

            enemies.Add(new Enemy
            {
                Bounds = new RectangleF(pos.X, pos.Y, 20, 20),
                Speed = 1f + Level * 0.3f,
                HP = 1 + Level
            });
        }

        private void SpawnPowerUp(int width, int height)
        {
            PointF pos = GetFreePosition(width, height, 15);

            powerUps.Add(new PowerUp
            {
                Bounds = new RectangleF(pos.X, pos.Y, 15, 15),
                Type = PowerUpType.Heal
            });
        }

        private void GenerateWalls(int width, int height)
        {
            int grid = 40;

            for (int i = 0; i < 20; i++)
            {
                float x = rand.Next(width / grid) * grid;
                float y = rand.Next(height / grid) * grid;

                RectangleF wall = new RectangleF(x, y, grid, grid);

                RectangleF center = new RectangleF(width / 2 - 60, height / 2 - 60, 120, 120);

                if (!wall.IntersectsWith(center))
                    walls.Add(new Wall { Bounds = wall });
            }
        }

        private void CreateExplosion(float x, float y)
        {
            int count = rand.Next(8, 11);

            for (int i = 0; i < count; i++)
            {
                float angle = (float)(rand.NextDouble() * Math.PI * 2);

                particles.Add(new Particle
                {
                    Position = new PointF(x, y),
                    dx = (float)Math.Cos(angle) * 2,
                    dy = (float)Math.Sin(angle) * 2,
                    Life = 20
                });
            }
        }

        public void Shoot(float dx, float dy)
        {
            if (State != GameState.Playing) return;

            bullets.Add(new Bullet
            {
                Bounds = new RectangleF(
                    player.Bounds.X + player.Bounds.Width / 2 - 2,
                    player.Bounds.Y + player.Bounds.Height / 2 - 2,
                    5, 5
                ),
                dx = dx * 6,
                dy = dy * 6
            });
        }

        // ================= HELPERS =================
        private PointF GetFreePosition(int width, int height, float size)
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                float x = rand.Next((int)(width - size));
                float y = rand.Next((int)(height - size));

                RectangleF rect = new RectangleF(x, y, size, size);

                bool collides = false;

                foreach (var wall in walls)
                {
                    if (rect.IntersectsWith(wall.Bounds))
                    {
                        collides = true;
                        break;
                    }
                }

                if (!collides && rect.IntersectsWith(player.Bounds))
                    collides = true;

                if (!collides)
                    return new PointF(x, y);
            }

            return new PointF(width / 2, height / 2);
        }

        private void Normalize(ref PointF v)
        {
            float len = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y);
            if (len > 0)
            {
                v.X /= len;
                v.Y /= len;
            }
        }

        private float DistanceToRect(PointF p, RectangleF r)
        {
            float dx = Math.Max(r.Left - p.X, 0);
            dx = Math.Max(dx, p.X - r.Right);

            float dy = Math.Max(r.Top - p.Y, 0);
            dy = Math.Max(dy, p.Y - r.Bottom);

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}