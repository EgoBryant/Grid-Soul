using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinFormsApp1
{
    public enum GameState
    {
        Menu,
        Playing,
        Shop,
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
        public bool IsBoss;
        public int ShootTimer;
    }

    public class Bullet
    {
        public RectangleF Bounds;
        public float dx, dy;
        public bool IsEnemyBullet;
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
        public bool IsDestructible;
        public int HP = 3;
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
        public List<RectangleF> mapAreas = new List<RectangleF>();
        public List<PowerUp> powerUps = new List<PowerUp>();

        public int Score = 0;
        public int TotalScore = 0;
        public int Level = 1;

        private Random rand = new Random();
        private int spawnTimer = 0;
        private int powerUpTimer = 0;
        private int nextShopScore = 1000;
        private int nextBossScore = 1000;
        private int _shootInterval = 10;
        private int shootCooldown = 0;
        private bool hasMultiShot = false;
        private bool hasVampirism = false;
        private bool hasDash = false;
        private int dashTimer = 0;
        private int dashCooldown = 0;

        private const float BasePlayerSpeed = 4f;
        private const int DashDuration = 8;
        private const int DashCooldown = 130;
        private const int FireRateCost = 500;
        private const int MultiShotCost = 900;
        private const int VampirismCost = 800;
        private const int DashCost = 1000;

        public int ShootInterval => _shootInterval;
        public bool HasMultiShot => hasMultiShot;
        public bool HasVampirism => hasVampirism;
        public bool HasDash => hasDash;
        public bool DashReady => hasDash && dashCooldown <= 0 && dashTimer <= 0;
        public int FireRateUpgradeCost => FireRateCost;
        public int MultiShotUpgradeCost => MultiShotCost;
        public int VampirismUpgradeCost => VampirismCost;
        public int DashUpgradeCost => DashCost;

        // ================= START =================
        public void StartGame(int width, int height)
        {
            State = GameState.Playing;

            player = new Player();
            enemies.Clear();
            bullets.Clear();
            particles.Clear();
            walls.Clear();
            mapAreas.Clear();
            powerUps.Clear();

            Score = 0;
            TotalScore = 0;
            Level = 1;
            nextShopScore = 1000;
            nextBossScore = 1000;
            _shootInterval = 10;
            shootCooldown = 0;
            hasMultiShot = false;
            hasVampirism = false;
            hasDash = false;
            dashTimer = 0;
            dashCooldown = 0;

            GenerateMaze(width, height);

            PointF start = GetMapCenter();
            player.Bounds = new RectangleF(start.X - 10, start.Y - 10, 20, 20);
            player.Speed = BasePlayerSpeed;
        }

        // ================= UPDATE =================
        public void Update(bool up, bool down, bool left, bool right, int width, int height)
        {
            if (State != GameState.Playing)
                return;

            UpdateDash();
            if (shootCooldown > 0)
                shootCooldown--;

            MovePlayer(up, down, left, right, width, height);
            MoveEnemies();
            UpdateBosses();
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

            if (powerUpTimer >= 900)
            {
                powerUpTimer = 0;
                SpawnPowerUp(width, height);
            }

            Level = TotalScore / 500 + 1;

            if (player.HP <= 0)
                State = GameState.GameOver;
        }

        // ================= SHOP =================
        public void ToggleShop()
        {
            if (State == GameState.Playing)
                State = GameState.Shop;
            else if (State == GameState.Shop)
                State = GameState.Playing;
        }

        public bool BuyFireRate()
        {
            if (State != GameState.Shop || Score < FireRateCost)
                return false;

            Score -= FireRateCost;
            _shootInterval = Math.Max(4, (int)Math.Ceiling(_shootInterval * 0.93f));
            return true;
        }

        public bool BuyMultiShot()
        {
            if (State != GameState.Shop || hasMultiShot || Score < MultiShotCost)
                return false;

            Score -= MultiShotCost;
            hasMultiShot = true;
            return true;
        }

        public bool BuyVampirism()
        {
            if (State != GameState.Shop || hasVampirism || Score < VampirismCost)
                return false;

            Score -= VampirismCost;
            hasVampirism = true;
            return true;
        }

        public bool BuyDash()
        {
            if (State != GameState.Shop || hasDash || Score < DashCost)
                return false;

            Score -= DashCost;
            hasDash = true;
            return true;
        }

        private void OpenScoreShopIfNeeded()
        {
            while (TotalScore >= nextBossScore)
            {
                nextBossScore += 1000;
                SpawnBoss();
            }

            if (TotalScore >= nextShopScore)
            {
                nextShopScore += 1000;
                State = GameState.Shop;
            }
        }

        public void TryDash()
        {
            if (State != GameState.Playing || !hasDash || dashTimer > 0 || dashCooldown > 0)
                return;

            dashTimer = DashDuration;
            dashCooldown = DashCooldown;
            player.Speed = BasePlayerSpeed * 2.2f;
        }

        private void UpdateDash()
        {
            if (dashTimer > 0)
            {
                dashTimer--;

                if (dashTimer <= 0)
                    player.Speed = BasePlayerSpeed;
            }

            if (dashCooldown > 0)
                dashCooldown--;
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

            if (!CanStand(player.Bounds))
            {
                player.Bounds.X = oldX;
                player.Bounds.Y = oldY;
            }

            player.Bounds.X = Math.Max(0, Math.Min(width - player.Bounds.Width, player.Bounds.X));
            player.Bounds.Y = Math.Max(0, Math.Min(height - player.Bounds.Height, player.Bounds.Y));
        }

        // ================= ENEMIES =================
        private void MoveEnemies()
        {
            foreach (var e in enemies)
            {
                PointF toPlayer = new PointF(
                    GetCenter(player.Bounds).X - GetCenter(e.Bounds).X,
                    GetCenter(player.Bounds).Y - GetCenter(e.Bounds).Y
                );
                Normalize(ref toPlayer);

                // SEPARATION
                PointF separation = new PointF(0, 0);
                foreach (var other in enemies)
                {
                    if (other == e) continue;

                    float dx = e.Bounds.X - other.Bounds.X;
                    float dy = e.Bounds.Y - other.Bounds.Y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq < 900 && distSq > 0)
                    {
                        separation.X += dx / distSq;
                        separation.Y += dy / distSq;
                    }
                }

                // COMBINE
                PointF move = new PointF(
                    toPlayer.X * 1.5f + separation.X * 2f,
                    toPlayer.Y * 1.5f + separation.Y * 2f
                );

                Normalize(ref move);

                e.Velocity = new PointF(move.X * e.Speed, move.Y * e.Speed);
                MoveEnemyAroundWalls(e);
            }
        }

        private void MoveEnemyAroundWalls(Enemy enemy)
        {
            PointF desired = enemy.Velocity;
            PointF normalized = desired;
            Normalize(ref normalized);

            PointF[] directions =
            {
                normalized,
                new PointF(normalized.X, 0),
                new PointF(0, normalized.Y),
                new PointF(-normalized.Y, normalized.X),
                new PointF(normalized.Y, -normalized.X),
                new PointF(normalized.X + -normalized.Y * 0.7f, normalized.Y + normalized.X * 0.7f),
                new PointF(normalized.X + normalized.Y * 0.7f, normalized.Y - normalized.X * 0.7f),
                new PointF(-normalized.X, normalized.Y),
                new PointF(normalized.X, -normalized.Y)
            };

            RectangleF bestBounds = enemy.Bounds;
            float bestScore = float.MaxValue;
            PointF playerCenter = GetCenter(player.Bounds);

            foreach (var direction in directions)
            {
                PointF dir = direction;
                Normalize(ref dir);

                if (dir.X == 0 && dir.Y == 0)
                    continue;

                RectangleF future = new RectangleF(
                    enemy.Bounds.X + dir.X * enemy.Speed,
                    enemy.Bounds.Y + dir.Y * enemy.Speed,
                    enemy.Bounds.Width,
                    enemy.Bounds.Height);

                if (!CanStand(future))
                    continue;

                float distance = Distance(GetCenter(future), playerCenter);
                float turnPenalty = Math.Abs(dir.X - normalized.X) + Math.Abs(dir.Y - normalized.Y);
                float score = distance + turnPenalty * 18f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestBounds = future;
                }
            }

            enemy.Bounds = bestBounds;
        }

        private void UpdateBosses()
        {
            foreach (var e in enemies)
            {
                if (!e.IsBoss)
                    continue;

                e.ShootTimer--;

                if (e.ShootTimer <= 0)
                {
                    e.ShootTimer = 70;

                    PointF direction = new PointF(
                        player.Bounds.X + player.Bounds.Width / 2 - (e.Bounds.X + e.Bounds.Width / 2),
                        player.Bounds.Y + player.Bounds.Height / 2 - (e.Bounds.Y + e.Bounds.Height / 2));

                    AddBullet(
                        e.Bounds.X + e.Bounds.Width / 2,
                        e.Bounds.Y + e.Bounds.Height / 2,
                        direction.X,
                        direction.Y,
                        true,
                        4.5f);
                }
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
                    b.Bounds.X > width || b.Bounds.Y > height ||
                    !IsRectOnMap(b.Bounds))
                    remove = true;

                for (int w = walls.Count - 1; w >= 0; w--)
                {
                    var wall = walls[w];

                    if (b.Bounds.IntersectsWith(wall.Bounds))
                    {
                        remove = true;

                        if (wall.IsDestructible)
                        {
                            wall.HP--;
                            if (wall.HP <= 0)
                                walls.RemoveAt(w);
                        }

                        break;
                    }
                }

                if (!remove && b.IsEnemyBullet && b.Bounds.IntersectsWith(player.Bounds))
                {
                    player.HP -= 8;
                    remove = true;
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

                if (e.Bounds.IntersectsWith(player.Bounds) || DistanceToRect(GetCenter(e.Bounds), player.Bounds) < 8)
                    player.HP -= 1;

                for (int j = bullets.Count - 1; j >= 0; j--)
                {
                    if (bullets[j].IsEnemyBullet)
                        continue;

                    if (e.Bounds.IntersectsWith(bullets[j].Bounds))
                    {
                        e.HP--;
                        bullets.RemoveAt(j);

                        if (e.HP <= 0)
                        {
                            CreateExplosion(e.Bounds.X, e.Bounds.Y);
                            enemies.RemoveAt(i);
                            AddScore(e.IsBoss ? 500 : 100);

                            if (hasVampirism && rand.Next(100) < 7)
                                player.HP = Math.Min(100, player.HP + 3);

                            OpenScoreShopIfNeeded();
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

        private void AddScore(int amount)
        {
            Score += amount;
            TotalScore += amount;
        }

        private void SpawnBoss()
        {
            PointF pos = GetFreePosition(0, 0, 36);

            enemies.Add(new Enemy
            {
                Bounds = new RectangleF(pos.X, pos.Y, 36, 36),
                Speed = 0.8f + Level * 0.12f,
                HP = 12 + Level * 4,
                IsBoss = true,
                ShootTimer = 45
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

        public void GenerateMaze(int width, int height)
        {
            mapAreas.Clear();
            walls.Clear();

            AddMapArea(width, height, 0, 0, 1000, 700);

            AddWall(width, height, 180, 135, 110, 110, false);
            AddWall(width, height, 710, 135, 110, 110, false);
            AddWall(width, height, 180, 455, 110, 110, false);
            AddWall(width, height, 710, 455, 110, 110, false);

            AddWall(width, height, 390, 205, 220, 30, true);
            AddWall(width, height, 390, 465, 220, 30, true);
            AddWall(width, height, 320, 300, 30, 100, true);
            AddWall(width, height, 650, 300, 30, 100, true);
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
            if (shootCooldown > 0) return;

            AddBullet(
                player.Bounds.X + player.Bounds.Width / 2,
                player.Bounds.Y + player.Bounds.Height / 2,
                dx,
                dy,
                false,
                6f);

            if (hasMultiShot)
            {
                AddBullet(player.Bounds.X + player.Bounds.Width / 2, player.Bounds.Y + player.Bounds.Height / 2, dx - dy * 0.35f, dy + dx * 0.35f, false, 6f);
                AddBullet(player.Bounds.X + player.Bounds.Width / 2, player.Bounds.Y + player.Bounds.Height / 2, dx + dy * 0.35f, dy - dx * 0.35f, false, 6f);
            }

            shootCooldown = _shootInterval;
        }

        private void AddBullet(float x, float y, float dx, float dy, bool isEnemyBullet, float speed)
        {
            PointF direction = new PointF(dx, dy);
            Normalize(ref direction);

            bullets.Add(new Bullet
            {
                Bounds = new RectangleF(
                    x - 2,
                    y - 2,
                    5, 5
                ),
                dx = direction.X * speed,
                dy = direction.Y * speed,
                IsEnemyBullet = isEnemyBullet
            });
        }

        // ================= HELPERS =================
        private PointF GetFreePosition(int width, int height, float size)
        {
            for (int attempt = 0; attempt < 120; attempt++)
            {
                RectangleF area = mapAreas[rand.Next(mapAreas.Count)];
                float x = area.X + rand.Next(Math.Max(1, (int)(area.Width - size)));
                float y = area.Y + rand.Next(Math.Max(1, (int)(area.Height - size)));

                RectangleF rect = new RectangleF(x, y, size, size);

                if (CanStand(rect) && !rect.IntersectsWith(player.Bounds))
                    return new PointF(x, y);
            }

            PointF center = GetMapCenter();
            return new PointF(center.X, center.Y);
        }

        private PointF GetMapCenter()
        {
            if (mapAreas.Count == 0)
                return new PointF(300, 300);

            RectangleF area = mapAreas[0];
            return new PointF(area.X + area.Width / 2, area.Y + area.Height / 2);
        }

        private void AddMapArea(int width, int height, float x, float y, float w, float h)
        {
            mapAreas.Add(ScaleRect(width, height, x, y, w, h));
        }

        private void AddWall(int width, int height, float x, float y, float w, float h, bool destructible)
        {
            walls.Add(new Wall
            {
                Bounds = ScaleRect(width, height, x, y, w, h),
                IsDestructible = destructible,
                HP = destructible ? 3 : int.MaxValue
            });
        }

        private RectangleF ScaleRect(int width, int height, float x, float y, float w, float h)
        {
            float scaleX = width / 1000f;
            float scaleY = height / 700f;

            return new RectangleF(
                x * scaleX,
                y * scaleY,
                w * scaleX,
                h * scaleY);
        }

        private bool CanStand(RectangleF rect)
        {
            if (!IsRectOnMap(rect))
                return false;

            foreach (var wall in walls)
            {
                if (rect.IntersectsWith(wall.Bounds))
                    return false;
            }

            return true;
        }

        private bool IsRectOnMap(RectangleF rect)
        {
            return IsPointOnMap(rect.Left, rect.Top) &&
                   IsPointOnMap(rect.Right, rect.Top) &&
                   IsPointOnMap(rect.Left, rect.Bottom) &&
                   IsPointOnMap(rect.Right, rect.Bottom);
        }

        private bool IsPointOnMap(float x, float y)
        {
            foreach (var area in mapAreas)
            {
                if (area.Contains(x, y))
                    return true;
            }

            return false;
        }

        private PointF GetCenter(RectangleF rect)
        {
            return new PointF(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        }

        private float Distance(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;

            return (float)Math.Sqrt(dx * dx + dy * dy);
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
