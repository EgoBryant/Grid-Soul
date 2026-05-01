using System;
using System.Collections.Generic;
using System.Drawing;

namespace WinFormsApp1
{
    public enum GameState
    {
        Start,
        Menu,
        Intro,
        Playing,
        Shop,
        LevelSelect,
        Pause,
        LevelTransition,
        Outro,
        GameOver
    }

    public enum EnemyType
    {
        Tiger,
        Warrior,
        Chariot,
        Praetorian,
        Emperor
    }

    public enum WeaponType
    {
        Gladius,
        Knife,
        Spear,
        Pilum,
        Falx,
        FireSword
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
        public EnemyType Type;
        public int Armor;
    }

    public class Bullet
    {
        public RectangleF Bounds;
        public float dx, dy;
        public bool IsEnemyBullet;
        public int Damage = 1;
        public float SplashRadius = 0;
        public bool Pierces;
        public int BulletType = 0; // 0 - мяч, 1 - копье, 2 - пилум, 3 - дротик
    }

    public class Slash
    {
        public RectangleF Bounds;
        public int Life;
        public bool IsArc;
        public float Angle;
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
        public GameState State = GameState.Start;

        public Player player = new Player();
        public List<Enemy> enemies = new List<Enemy>();
        public List<Bullet> bullets = new List<Bullet>();
        public List<Particle> particles = new List<Particle>();
        public List<Slash> slashes = new List<Slash>();
        public List<Wall> walls = new List<Wall>();
        public List<RectangleF> mapAreas = new List<RectangleF>();
        public List<PowerUp> powerUps = new List<PowerUp>();

        public int Score = 0;
        public int TotalScore = 0;
        public int Level = 1;
        public int CampaignLevel = 1;
        public int EnemiesRemaining = 0;
        public int EnemiesTotal = 0;
        public string StoryText = "";
        public string LevelTitle = "";
        public WeaponType CurrentWeapon = WeaponType.Gladius;
        public int MaxUnlockedLevel = 1;
        public int BossHP = 0;
        public int BossMaxHP = 0;
        public bool IsPreLevelStory = false;
        public int PendingLevel => pendingLevel;

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
        private int enemiesSpawned = 0;
        private int spawnTarget = 0;
        private int levelSpawnTimer = 0;
        private EnemyType currentEnemyType = EnemyType.Tiger;
        private int pendingLevel = 1;

        private const float BasePlayerSpeed = 4f;
        private const float PlayerSize = 36f;
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
            State = GameState.Intro;

            player = new Player();
            enemies.Clear();
            bullets.Clear();
            particles.Clear();
            slashes.Clear();
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
            CampaignLevel = 1;
            MaxUnlockedLevel = 1;
            pendingLevel = 1;
            IsPreLevelStory = false;
            BossHP = 0;
            BossMaxHP = 0;
            CurrentWeapon = WeaponType.Gladius;
            EnemiesRemaining = 0;
            EnemiesTotal = 0;
            enemiesSpawned = 0;
            spawnTarget = 0;
            levelSpawnTimer = 0;
            StoryText = "Рим предал своего лучшего полководца. Его имя стерли из сената, семью отняли, а самого бросили на песок арены. Теперь он не генерал, а гладиатор. Но каждый бой приближает его к трону.";
            LevelTitle = "Предательство";

            GenerateMaze(width, height);

            PointF start = GetMapCenter();
            player.Bounds = new RectangleF(start.X - PlayerSize / 2, start.Y - PlayerSize / 2, PlayerSize, PlayerSize);
            player.Speed = BasePlayerSpeed;
        }

        public void ContinueStory(int width, int height)
        {
            if (State == GameState.Intro)
            {
                State = GameState.LevelSelect;
                return;
            }

            if (State == GameState.LevelTransition)
            {
                if (IsPreLevelStory)
                    StartLevel(pendingLevel, width, height);
                else
                    State = GameState.LevelSelect;

                return;
            }

            if (State == GameState.Outro)
                State = GameState.LevelSelect;
        }

        public void SelectLevel(int level, int width, int height)
        {
            if (State != GameState.LevelSelect || level < 1 || level > MaxUnlockedLevel)
                return;

            pendingLevel = level;
            IsPreLevelStory = true;
            LevelTitle = GetLevelTitle(level);
            StoryText = GetPreLevelStory(level);
            State = GameState.LevelTransition;
        }

        public void PauseGame()
        {
            if (State == GameState.Playing)
                State = GameState.Pause;
        }

        public void ResumeGame()
        {
            if (State == GameState.Pause)
                State = GameState.Playing;
        }

        public void ReturnToLevelSelect()
        {
            enemies.Clear();
            bullets.Clear();
            slashes.Clear();
            powerUps.Clear();
            State = GameState.LevelSelect;
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
            UpdateSlashes();
            UpdateParticles();

            spawnTimer++;
            powerUpTimer++;
            levelSpawnTimer++;

            int spawnRate = Math.Max(25, 90 - CampaignLevel * 8);
            if (enemiesSpawned < spawnTarget && levelSpawnTimer >= spawnRate)
            {
                levelSpawnTimer = 0;
                SpawnCampaignEnemy(width, height);
            }

            if (powerUpTimer >= 900)
            {
                powerUpTimer = 0;
                if (CampaignLevel >= 2)
                    SpawnPowerUp(width, height);
            }

            Level = CampaignLevel;
            EnemiesRemaining = Math.Max(0, spawnTarget - enemiesSpawned + enemies.Count);

            if (enemiesSpawned >= spawnTarget && enemies.Count == 0)
                CompleteLevel();

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
            for (int eIndex = enemies.Count - 1; eIndex >= 0; eIndex--)
            {
                var e = enemies[eIndex];
                PointF toPlayer = new PointF(
                    GetCenter(player.Bounds).X - GetCenter(e.Bounds).X,
                    GetCenter(player.Bounds).Y - GetCenter(e.Bounds).Y
                );
                Normalize(ref toPlayer);

                // SEPARATION
                PointF separation = new PointF(0, 0);
                for (int otherIndex = enemies.Count - 1; otherIndex >= 0; otherIndex--)
                {
                    var other = enemies[otherIndex];
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
            for (int eIndex = enemies.Count - 1; eIndex >= 0; eIndex--)
            {
                var e = enemies[eIndex];
                if (e.Type != EnemyType.Warrior &&
                    e.Type != EnemyType.Chariot &&
                    e.Type != EnemyType.Praetorian &&
                    e.Type != EnemyType.Emperor)
                    continue;

                e.ShootTimer--;

                if (e.ShootTimer <= 0)
                {
                    e.ShootTimer = GetEnemyShootInterval(e.Type);

                    PointF direction = new PointF(
                        player.Bounds.X + player.Bounds.Width / 2 - (e.Bounds.X + e.Bounds.Width / 2),
                        player.Bounds.Y + player.Bounds.Height / 2 - (e.Bounds.Y + e.Bounds.Height / 2));

                    if (e.Type == EnemyType.Emperor)
                    {
                        int attack = rand.Next(3);

                        if (attack == 0)
                        {
                            AddBullet(e.Bounds.X + e.Bounds.Width / 2, e.Bounds.Y + e.Bounds.Height / 2, direction.X - direction.Y * 0.55f, direction.Y + direction.X * 0.55f, true, 5f, 1, 0, false, 3);
                            AddBullet(e.Bounds.X + e.Bounds.Width / 2, e.Bounds.Y + e.Bounds.Height / 2, direction.X - direction.Y * 0.28f, direction.Y + direction.X * 0.28f, true, 5f, 1, 0, false, 3);
                            AddBullet(e.Bounds.X + e.Bounds.Width / 2, e.Bounds.Y + e.Bounds.Height / 2, direction.X, direction.Y, true, 5f, 1, 0, false, 3);
                            AddBullet(e.Bounds.X + e.Bounds.Width / 2, e.Bounds.Y + e.Bounds.Height / 2, direction.X + direction.Y * 0.28f, direction.Y - direction.X * 0.28f, true, 5f, 1, 0, false, 3);
                            AddBullet(e.Bounds.X + e.Bounds.Width / 2, e.Bounds.Y + e.Bounds.Height / 2, direction.X + direction.Y * 0.55f, direction.Y - direction.X * 0.55f, true, 5f, 1, 0, false, 3);
                        }
                        else if (attack == 1)
                        {
                            SummonBossWarriors(e);
                        }
                        else if (Distance(GetCenter(e.Bounds), GetCenter(player.Bounds)) < 125)
                        {
                            player.HP -= 18;
                        }
                        else
                        {
                            AddBullet(e.Bounds.X + e.Bounds.Width / 2, e.Bounds.Y + e.Bounds.Height / 2, direction.X, direction.Y, true, 5.4f, 2, 0, false, 3);
                        }
                    }
                    else
                    {
                        AddBullet(
                            e.Bounds.X + e.Bounds.Width / 2,
                            e.Bounds.Y + e.Bounds.Height / 2,
                            direction.X,
                            direction.Y,
                            true,
                            e.Type == EnemyType.Chariot ? 5.3f : 4.2f,
                            1,
                            0,
                            false,
                            3);
                    }
                }
            }
        }

        private int GetEnemyShootInterval(EnemyType type)
        {
            if (type == EnemyType.Chariot)
                return 112;
            if (type == EnemyType.Praetorian)
                return 120;
            if (type == EnemyType.Emperor)
                return 60;

            return 85;
        }

        private void SummonBossWarriors(Enemy boss)
        {
            PointF center = GetCenter(boss.Bounds);
            enemies.Add(CreateEnemy(EnemyType.Warrior, new PointF(center.X - 80, center.Y + 60)));
            enemies.Add(CreateEnemy(EnemyType.Warrior, new PointF(center.X + 55, center.Y + 60)));
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
                        int damage = Math.Max(1, bullets[j].Damage - e.Armor);

                        if (bullets[j].SplashRadius > 0)
                            ApplySplashDamage(bullets[j], i);

                        bullets.RemoveAt(j);
                        bool died = TakeDamageAt(i, damage);

                        if (died)
                        {
                            if (hasVampirism && rand.Next(100) < 7)
                                player.HP = Math.Min(100, player.HP + 3);

                            break;
                        }
                    }
                }
            }
        }

        public void ToggleWeapon()
        {
            if (CampaignLevel != 5)
                return;

            if (CurrentWeapon == WeaponType.Pilum)
                CurrentWeapon = WeaponType.Falx;
            else
                CurrentWeapon = WeaponType.Pilum;
        }

        public bool TakeDamage(Enemy enemy, int damage)
        {
            int index = enemies.IndexOf(enemy);
            if (index < 0)
                return false;

            return TakeDamageAt(index, damage);
        }

        public bool TakeDamageAt(int index, int damage)
        {
            if (index < 0 || index >= enemies.Count)
                return false;

            var enemy = enemies[index];
            enemy.HP -= damage;
            if (enemy.IsBoss)
                BossHP = Math.Max(0, enemy.HP);

            if (enemy.HP > 0)
                return false;

            CreateExplosion(enemy.Bounds.X, enemy.Bounds.Y);
            int bonus = enemy.IsBoss ? 1000 : 100;
            AddScore(bonus);
            enemies.RemoveAt(index);
            return true;
        }

        private void ApplySplashDamage(Bullet bullet, int directHitIndex)
        {
            PointF center = GetCenter(bullet.Bounds);

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i == directHitIndex)
                    continue;

                if (Distance(center, GetCenter(enemies[i].Bounds)) <= bullet.SplashRadius)
                    TakeDamageAt(i, Math.Max(1, bullet.Damage - enemies[i].Armor));
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
        private void StartLevel(int level, int width, int height)
        {
            CampaignLevel = level;
            State = GameState.Playing;
            enemies.Clear();
            bullets.Clear();
            slashes.Clear();
            powerUps.Clear();
            enemiesSpawned = 0;
            levelSpawnTimer = 999;
            powerUpTimer = 0;
            player.HP = 100;
            IsPreLevelStory = false;
            BossHP = 0;
            BossMaxHP = 0;

            switch (CampaignLevel)
            {
                case 1:
                    currentEnemyType = EnemyType.Tiger;
                    spawnTarget = 10;
                    CurrentWeapon = WeaponType.Knife;
                    LevelTitle = GetLevelTitle(1);
                    break;
                case 2:
                    currentEnemyType = EnemyType.Warrior;
                    spawnTarget = 15;
                    CurrentWeapon = WeaponType.Spear;
                    LevelTitle = GetLevelTitle(2);
                    break;
                case 3:
                    currentEnemyType = EnemyType.Chariot;
                    spawnTarget = 5;
                    CurrentWeapon = WeaponType.Pilum;
                    LevelTitle = GetLevelTitle(3);
                    break;
                case 4:
                    currentEnemyType = EnemyType.Praetorian;
                    spawnTarget = 20;
                    CurrentWeapon = WeaponType.Falx;
                    LevelTitle = GetLevelTitle(4);
                    break;
                default:
                    currentEnemyType = EnemyType.Emperor;
                    spawnTarget = 1;
                    CurrentWeapon = WeaponType.Pilum;
                    LevelTitle = GetLevelTitle(5);
                    break;
            }

            EnemiesTotal = spawnTarget;
            EnemiesRemaining = spawnTarget;
        }

        private string GetLevelTitle(int level)
        {
            if (level == 1)
                return "Уровень 1: Начало пути";
            if (level == 2)
                return "Уровень 2: Тайные союзники";
            if (level == 3)
                return "Уровень 3: Грохот колесниц";
            if (level == 4)
                return "Уровень 4: Преторианская стена";

            return "Уровень 5: Император";
        }

        private string GetPreLevelStory(int level)
        {
            if (level == 1)
                return "Уровень 1: Начало пути. На песок выпускают тигров. У тебя только нож, короткая сталь и желание выжить.";
            if (level == 2)
                return "Уровень 2: Тайные союзники. Твои тайные союзники — легионеры и слуги — подкупили охрану. Они будут тайно подбрасывать тебе сумки с лечебными травами прямо на арену. В руки тебе дают копье.";
            if (level == 3)
                return "Уровень 3: Грохот колесниц. Колесницы несутся по арене и бросают короткие копья. Держи дистанцию.";
            if (level == 4)
                return "Уровень 4: Преторианская стена. Против тебя броня императора. Огненный меч должен расколоть строй.";

            return "Уровень 5: Император. Тиран выходит сам: залпы, подмога и смертельный удар вблизи.\n\nНажми [Q] или [Цифру 1-2], чтобы переключиться между Пилумом (дальний бой) и Фалксом (ближний бой).";
        }

        private void CompleteLevel()
        {
            if (CampaignLevel == 1)
            {
                MaxUnlockedLevel = Math.Max(MaxUnlockedLevel, 2);
                StoryText = "Уровень 1 завершен. Толпа ревет. Тигры повержены, и путь к настоящей арене открыт.";
                IsPreLevelStory = false;
                State = GameState.LevelTransition;
                return;
            }

            if (CampaignLevel == 2)
            {
                MaxUnlockedLevel = Math.Max(MaxUnlockedLevel, 3);
                StoryText = "Уровень 2 завершен. Воины пали, а слух о тайных союзниках разлетелся по трибунам.";
                IsPreLevelStory = false;
                State = GameState.LevelTransition;
                return;
            }

            if (CampaignLevel == 3)
            {
                MaxUnlockedLevel = Math.Max(MaxUnlockedLevel, 4);
                StoryText = "Уровень 3 завершен. Колесницы разбиты. Народ впервые скандирует имя гладиатора громче имени императора.";
                IsPreLevelStory = false;
                State = GameState.LevelTransition;
                return;
            }

            if (CampaignLevel == 4)
            {
                MaxUnlockedLevel = Math.Max(MaxUnlockedLevel, 5);
                StoryText = "Уровень 4 завершен. Преторианцы отступили. Последние ворота открываются: за ними сам император.";
                IsPreLevelStory = false;
                State = GameState.LevelTransition;
                return;
            }

            StoryText = "Песок стих. Тиран пал, а гладиатор, прошедший через предательство и арену, стал правителем. Он вернул Риму честь и наконец обрел счастье.";
            State = GameState.Outro;
        }

        private void SpawnCampaignEnemy(int width, int height)
        {
            PointF pos = GetGatePosition(width, height, GetEnemySize(currentEnemyType).Width);
            Enemy enemy = CreateEnemy(currentEnemyType, pos);

            enemies.Add(enemy);
            enemiesSpawned++;
            EnemiesRemaining = Math.Max(0, spawnTarget - enemiesSpawned + enemies.Count);
        }

        private Enemy CreateEnemy(EnemyType type, PointF pos)
        {
            SizeF size = GetEnemySize(type);
            Enemy enemy = new Enemy
            {
                Type = type,
                Bounds = new RectangleF(pos.X, pos.Y, size.Width, size.Height),
                Speed = 1.8f,
                HP = 2,
                ShootTimer = rand.Next(40, 90)
            };

            if (type == EnemyType.Tiger)
            {
                enemy.Speed = 3.2f;
                enemy.HP = 2;
            }
            else if (type == EnemyType.Warrior)
            {
                // Уровень 2: Воины - редкие выстрелы дротиками (раз в 5-7 секунд)
                enemy.Speed = 1.5f;
                enemy.HP = 3;
                enemy.ShootTimer = rand.Next(300, 420);
            }
            else if (type == EnemyType.Chariot)
            {
                // Уровень 3: Колесницы - на 25% чаще (112 фреймов вместо 150)
                enemy.Speed = 4.1f;
                enemy.HP = 5;
                enemy.ShootTimer = 112;
            }
            else if (type == EnemyType.Praetorian)
            {
                enemy.Speed = 1.25f;
                enemy.HP = 6;
                enemy.Armor = 1;
            }
            else if (type == EnemyType.Emperor)
            {
                // Уровень 5: Босс - HP увеличен в 1.5 раза (45 * 1.5 = 68)
                enemy.Speed = 1.05f;
                enemy.HP = 68;
                enemy.IsBoss = true;
                enemy.ShootTimer = 60;
                BossHP = 68;
                BossMaxHP = 68;
            }

            return enemy;
        }

        private SizeF GetEnemySize(EnemyType type)
        {
            if (type == EnemyType.Tiger)
                return new SizeF(58, 38);

            if (type == EnemyType.Chariot)
                return new SizeF(74, 44);

            if (type == EnemyType.Praetorian)
                return new SizeF(48, 48);

            if (type == EnemyType.Emperor)
                return new SizeF(78, 78);

            return new SizeF(46, 46);
        }

        private PointF GetGatePosition(int width, int height, float size)
        {
            int gate = rand.Next(4);

            if (gate == 0)
                return new PointF(width / 2f - size / 2, 16);
            if (gate == 1)
                return new PointF(width / 2f - size / 2, height - size - 16);
            if (gate == 2)
                return new PointF(16, height / 2f - size / 2);

            return new PointF(width - size - 16, height / 2f - size / 2);
        }

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

            // Фалкс - ближний бой, не стреляет
            if (CurrentWeapon == WeaponType.Falx)
            {
                // Создание эффекта взмаха (будет отрисовано в DrawGame)
                AddFalxSlash(dx, dy);
                shootCooldown = 8;
                return;
            }

            if (CurrentWeapon == WeaponType.Gladius || CurrentWeapon == WeaponType.Knife)
            {
                AddSlash(dx, dy);
                shootCooldown = CurrentWeapon == WeaponType.Knife ? 6 : 9;
                return;
            }

            if (CurrentWeapon == WeaponType.Spear)
            {
                AddBullet(
                    player.Bounds.X + player.Bounds.Width / 2,
                    player.Bounds.Y + player.Bounds.Height / 2,
                    dx,
                    dy,
                    false,
                    4.9f,
                    4,
                    0,
                    false,
                    1);

                shootCooldown = 16;
            }
            else if (CurrentWeapon == WeaponType.Pilum)
            {
                AddBullet(
                    player.Bounds.X + player.Bounds.Width / 2,
                    player.Bounds.Y + player.Bounds.Height / 2,
                    dx,
                    dy,
                    false,
                    5.6f,
                    3,
                    0,
                    false,
                    2);

                shootCooldown = 18;
            }
            else
            {
                AddSlash(dx, dy);
                shootCooldown = 12;
            }
        }

        private void AddFalxSlash(float dx, float dy)
        {
            // Эффект взмаха фалкса - широкая дуга с полупрозрачным белым цветом
            float range = 50;
            float radius = 120; // Большой радиус поражения
            PointF direction = new PointF(dx, dy);
            Normalize(ref direction);

            RectangleF slashArea = new RectangleF(
                player.Bounds.X + player.Bounds.Width / 2 + direction.X * range - radius / 2,
                player.Bounds.Y + player.Bounds.Height / 2 + direction.Y * range - radius / 2,
                radius,
                radius);

            float angle = (float)(Math.Atan2(direction.Y, direction.X) * 180.0 / Math.PI);
            slashes.Add(new Slash { Bounds = slashArea, Life = 3, IsArc = true, Angle = angle });

            // Наносим урон всем врагам в радиусе
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (slashArea.IntersectsWith(enemies[i].Bounds) ||
                    Distance(GetCenter(slashArea), GetCenter(enemies[i].Bounds)) <= radius / 2)
                {
                    TakeDamageAt(i, 6);
                }
            }
        }

        private void AddSlash(float dx, float dy)
        {
            float range = CurrentWeapon == WeaponType.Knife ? 34 : 42;
            float size = CurrentWeapon == WeaponType.Knife ? 30 : 42;
            PointF direction = new PointF(dx, dy);
            Normalize(ref direction);

            RectangleF slash = new RectangleF(
                player.Bounds.X + player.Bounds.Width / 2 + direction.X * range - size / 2,
                player.Bounds.Y + player.Bounds.Height / 2 + direction.Y * range - size / 2,
                size,
                size);

            slashes.Add(new Slash { Bounds = slash, Life = 5, IsArc = false, Angle = 0 });

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (slashes[slashes.Count - 1].Bounds.IntersectsWith(enemies[i].Bounds))
                {
                    int damage = CurrentWeapon == WeaponType.Knife ? 2 : 1;
                    TakeDamageAt(i, damage);
                }
            }
        }

        private void UpdateSlashes()
        {
            for (int i = slashes.Count - 1; i >= 0; i--)
            {
                slashes[i].Life--;

                if (slashes[i].Life <= 0)
                    slashes.RemoveAt(i);
            }
        }

        private void AddBullet(float x, float y, float dx, float dy, bool isEnemyBullet, float speed, int damage, float splashRadius, bool pierces = false, int bulletType = 0)
        {
            PointF direction = new PointF(dx, dy);
            Normalize(ref direction);

            float width = 5;
            float height = 5;

            if (bulletType == 1)
            {
                width = 20;
                height = 8;
            }
            else if (bulletType == 2)
            {
                width = 22;
                height = 8;
            }
            else if (bulletType == 3)
            {
                width = 18;
                height = 5;
            }

            bullets.Add(new Bullet
            {
                Bounds = new RectangleF(
                    x - width / 2,
                    y - height / 2,
                    width,
                    height
                ),
                dx = direction.X * speed,
                dy = direction.Y * speed,
                IsEnemyBullet = isEnemyBullet,
                Damage = damage,
                SplashRadius = splashRadius,
                Pierces = pierces,
                BulletType = bulletType
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
