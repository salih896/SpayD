using System;
using System.Collections.Generic;
using GameCore.Economy;
using GameCore.Grid;
using GameCore.Progression;
using GameCore.Village;

namespace GameCore.Combat
{
    public enum StructKind { Tower = 0, Building = 1 }

    public sealed class SimStructure
    {
        public int Id; public GridPos Pos; public StructKind Kind;
        public TowerType TowerType; public BuildingType BuildingType;
        public int Level, Hp, MaxHp, StartHp;
        public bool Alive = true, Disabled, IsResource, IsDepot;
        public ResourceType Stores;
        public int Cooldown;
        public int CenterX => Pos.X * 1000;
        public int CenterY => Pos.Y * 1000;
    }

    public sealed class SimSoldier
    {
        public int Id; public SoldierType Type; public int Level;
        public int Hp, MaxHp, Attack, IntervalTicks, RangeMilli, SpeedMilli, ArmorPct, FireResistPct, SplashMilli;
        public bool Flying, Mounted, Alive = true, Idle;
        public int X, Y;                       // mili-hücre
        public int TargetId = -1;
        public int Phase;                      // 0: girişe yürüyor, 1: yolda / uçuyor
        public GridPos Entrance, LastCell;
        public List<GridPos> Path; public int PathIndex;
        public int Cooldown, SlowUntil, BurnUntil, BurnPerTick;
        public int RootUntil, AttackCount, NextHealTick;
        public SoldierPerks Perks = new SoldierPerks();
    }

    /// <summary>
    /// Deterministik savaş simülasyonu (10 tick/sn, tamsayı, float yok). Defans düzeni savaş başında değişmez kabul edilir.
    /// Köy sahibinin askerleri savaşmaz; yalnızca kuleler savunur (GDD Bölüm 13).
    /// Aynı girdi + aynı komutlar = aynı sonuç (sunucu otoritesi için).
    /// </summary>
    public sealed class BattleSim
    {
        public const int TicksPerSecond = 10;
        public const int TimeLimitTicks = 3 * 60 * TicksPerSecond;   // GEÇİCİ: 3 dakika
        public const int InjuredHpPct = 25;                          // GEÇİCİ: bu veya altı = yaralı = kayıp
        public const int RetreatLossPct = 25;                        // GDD Bölüm 15
        public const int LootCapPct = 70;                            // GDD Bölüm 18

        private readonly DefenseSetup setup;
        private readonly List<SimStructure> structures = new List<SimStructure>();
        private readonly List<SimSoldier> soldiers = new List<SimSoldier>();
        private readonly Dictionary<SoldierType, int> stock = new Dictionary<SoldierType, int>();
        private readonly Dictionary<SoldierType, int> levels;
        private readonly HashSet<GridPos> roads;
        private readonly List<GridPos> entrances;
        private readonly DeterministicRng rng;
        private readonly int size;

        public int Tick { get; private set; }
        public bool Finished { get; private set; }
        public bool Retreated { get; private set; }
        public IReadOnlyList<SimStructure> Structures => structures;
        public IReadOnlyList<SimSoldier> Soldiers => soldiers;
        public static int TowerTypeCount => System.Enum.GetValues(typeof(TowerType)).Length;
        public int StockOf(SoldierType t) => stock.TryGetValue(t, out var n) ? n : 0;

        /// <summary>Saldıranın araştırma bonusları (Deploy'dan önce atanır).</summary>
        public CombatMods AttackerMods = new CombatMods();

        public BattleSim(DefenseSetup defense, Dictionary<SoldierType, int> attackerStock,
                         Dictionary<SoldierType, int> soldierLevels, int seed)
        {
            setup = defense;
            size = defense.Layout.Size;
            levels = soldierLevels ?? new Dictionary<SoldierType, int>();
            rng = new DeterministicRng(seed);
            foreach (var kv in attackerStock) if (kv.Value > 0) stock[kv.Key] = kv.Value;

            roads = new HashSet<GridPos>(defense.Layout.CellsOf(CellKind.Road));
            entrances = defense.Layout.EntrancePositions();

            foreach (var p in defense.Layout.CellsOf(CellKind.Tower))
            {
                if (!defense.Towers.TryGetValue(p, out var spec)) spec = new TowerSpec(TowerType.Archer);
                var def = ContentCatalog.Tower(spec.Type);
                int max = CombatMods.Apply(ContentCatalog.Scaled(def.Hp, spec.Level), defense.Mods.TowerHp);
                int perm = Math.Max(0, Math.Min(100, spec.PermanentDamagePct));
                int start = Math.Max(1, max * (100 - perm) / 100);
                structures.Add(new SimStructure
                {
                    Id = structures.Count, Pos = p, Kind = StructKind.Tower, TowerType = spec.Type, Level = spec.Level,
                    MaxHp = max, Hp = start, StartHp = start, Disabled = perm >= 70
                });
            }
            foreach (var p in defense.Layout.CellsOf(CellKind.Building))
            {
                if (!defense.Buildings.TryGetValue(p, out var spec)) spec = new BuildingSpec(BuildingType.VillagerHouse);
                var def = ContentCatalog.Building(spec.Type);
                int max = CombatMods.Apply(ContentCatalog.Scaled(def.Hp, spec.Level), defense.Mods.BuildingHp);
                structures.Add(new SimStructure
                {
                    Id = structures.Count, Pos = p, Kind = StructKind.Building, BuildingType = spec.Type, Level = spec.Level,
                    MaxHp = max, Hp = max, StartHp = max, IsResource = def.IsResource, IsDepot = def.IsDepot, Stores = def.Stores
                });
            }
        }

        // ---------- Komutlar ----------
        public bool Deploy(SoldierType type, int x, int row, out string error)
        {
            error = null;
            if (Finished) { error = "Savaş bitti"; return false; }
            if (x < 0 || x >= size || row < 1 || row > VillageLayout.AttackZoneRows) { error = "Gönderim alanı dışı"; return false; }
            if (StockOf(type) <= 0) { error = "Bu türden asker kalmadı"; return false; }
            var def = ContentCatalog.Soldier(type);
            if (!def.Flying && entrances.Count == 0) { error = "Savunmada giriş yok"; return false; }

            int lvl = levels.TryGetValue(type, out var l) ? Math.Max(1, l) : 1;
            var perks = SoldierAbilities.PerksFor(type, lvl);
            var s = new SimSoldier
            {
                Id = soldiers.Count, Type = type, Level = lvl, Perks = perks,
                MaxHp = CombatMods.Apply(ContentCatalog.Scaled(def.Hp, lvl), AttackerMods.SoldierHp), Attack = CombatMods.Apply(ContentCatalog.Scaled(def.Attack, lvl), AttackerMods.SoldierDamage),
                IntervalTicks = def.IntervalTicks, RangeMilli = def.RangeMilli * (100 + perks.RangePct) / 100, SpeedMilli = def.SpeedMilli * (100 + perks.SpeedPct) / 100,
                ArmorPct = Math.Min(80, def.ArmorPct + perks.ArmorAdd), FireResistPct = def.FireResistPct, SplashMilli = def.SplashMilli + perks.SplashAdd,
                Flying = def.Flying, Mounted = def.Mounted,
                X = x * 1000, Y = -row * 1000, Phase = def.Flying ? 1 : 0
            };
            s.Hp = s.MaxHp;
            if (!def.Flying)
            {
                long best = long.MaxValue;
                foreach (var e in entrances)
                {
                    long d = CombatMath.DistSq(s.X, s.Y, e.X * 1000, 0);
                    if (d < best) { best = d; s.Entrance = e; }
                }
                s.LastCell = s.Entrance;
            }
            soldiers.Add(s);
            stock[type] = StockOf(type) - 1;
            return true;
        }

        /// <summary>Geri çekilme: savaş hemen biter; sağlıklı gönderilmiş askerlerin %25'i tohumlu rastgele seçimle feda edilir.</summary>
        public void Retreat()
        {
            if (Finished) return;
            Retreated = true;
            Finished = true;
        }

        // ---------- Adım ----------
        public void Step()
        {
            if (Finished) return;
            StepTowers();
            for (int i = 0; i < soldiers.Count; i++) StepSoldier(soldiers[i]);
            Tick++;
            CheckFinish();
        }

        private void CheckFinish()
        {
            bool anyStruct = false;
            for (int i = 0; i < structures.Count; i++) if (structures[i].Alive) { anyStruct = true; break; }
            if (!anyStruct || Tick >= TimeLimitTicks) { Finished = true; return; }

            int stockLeft = 0;
            foreach (var kv in stock) stockLeft += kv.Value;
            if (stockLeft > 0) return;

            bool anyActive = false;
            for (int i = 0; i < soldiers.Count; i++)
                if (soldiers[i].Alive && !soldiers[i].Idle) { anyActive = true; break; }
            if (!anyActive) Finished = true;
        }

        private void StepTowers()
        {
            for (int i = 0; i < structures.Count; i++)
            {
                var t = structures[i];
                if (!t.Alive || t.Kind != StructKind.Tower || t.Disabled) continue;
                if (t.Cooldown > 0) { t.Cooldown--; continue; }
                var def = ContentCatalog.Tower(t.TowerType);
                if (def.Attack <= 0) continue; // Duvar ateş etmez
                long r2 = (long)def.RangeMilli * def.RangeMilli;
                SimSoldier best = null; long bestD = long.MaxValue;
                for (int j = 0; j < soldiers.Count; j++)
                {
                    var s = soldiers[j];
                    if (!s.Alive) continue;
                    if (s.Flying && !def.CanHitAir) continue;
                    long d = CombatMath.DistSq(t.CenterX, t.CenterY, s.X, s.Y);
                    if (d > r2 || d >= bestD) continue;
                    best = s; bestD = d;
                }
                if (best == null) continue;
                int dmg = CombatMods.Apply(CombatMath.TowerDamage(def, t.Level, best.Flying, best.Mounted, best.ArmorPct), setup.Mods.TowerDamage);
                Hit(best, dmg);
                if (def.SlowTicks > 0 && best.Alive) best.SlowUntil = Math.Max(best.SlowUntil, Tick + def.SlowTicks);
                if (def.ChainCount > 0) Chain(best, def, dmg * 70 / 100);
                if (def.SplashMilli > 0)
                {
                    long sr2 = (long)def.SplashMilli * def.SplashMilli;
                    for (int j = 0; j < soldiers.Count; j++)
                    {
                        var o = soldiers[j];
                        if (o == best || !o.Alive) continue;
                        if (o.Flying && !def.CanHitAir) continue;
                        if (CombatMath.DistSq(best.X, best.Y, o.X, o.Y) <= sr2) Hit(o, Math.Max(1, dmg / 2));
                    }
                }
                t.Cooldown = Math.Max(0, def.IntervalTicks - 1);
            }
        }

        /// <summary>Şimşek sıçraması: son vurulandan 2 hücre içindeki en yakın canlı askere (uzaklık, sonra liste sırası; deterministik).</summary>
        private void Chain(SimSoldier from, TowerDef def, int dmg)
        {
            var hit = new HashSet<int> { from.Id };
            var cur = from;
            for (int n = 0; n < def.ChainCount; n++)
            {
                SimSoldier next = null; long bestD = 2000L * 2000L + 1;
                for (int j = 0; j < soldiers.Count; j++)
                {
                    var o = soldiers[j];
                    if (!o.Alive || hit.Contains(o.Id) || (o.Flying && !def.CanHitAir)) continue;
                    long d = CombatMath.DistSq(cur.X, cur.Y, o.X, o.Y);
                    if (d < bestD) { bestD = d; next = o; }
                }
                if (next == null) return;
                Hit(next, Math.Max(1, dmg));
                hit.Add(next.Id);
                cur = next;
            }
        }

        private void Hit(SimSoldier s, int dmg)
        {
            if (s.Perks.DmgTakenPermille > 0) dmg = Math.Max(1, dmg * (1000 - s.Perks.DmgTakenPermille) / 1000);
            s.Hp -= dmg;
            if (s.Hp <= 0) { s.Hp = 0; s.Alive = false; }
        }

        private void StepSoldier(SimSoldier s)
        {
            if (!s.Alive) return;
            if (Tick < s.BurnUntil) { Hit(s, s.BurnPerTick); if (!s.Alive) return; }
            // Yetenekler: yenilenme ve şifa (otomatik)
            if (s.Perks.RegenPermille > 0 && Tick % 10 == 0) s.Hp = Math.Min(s.MaxHp, s.Hp + Math.Max(1, s.MaxHp * s.Perks.RegenPermille / 1000));
            if (s.Perks.HealPermille > 0 && Tick >= s.NextHealTick)
            {
                s.NextHealTick = Tick + 20;
                for (int i = 0; i < soldiers.Count; i++)
                {
                    var o = soldiers[i];
                    if (o.Alive && CombatMath.DistSq(s.X, s.Y, o.X, o.Y) <= 2000L * 2000L)
                        o.Hp = Math.Min(o.MaxHp, o.Hp + Math.Max(1, o.MaxHp * s.Perks.HealPermille / 1000));
                }
            }

            SimStructure target = (s.TargetId >= 0 && structures[s.TargetId].Alive) ? structures[s.TargetId] : null;
            if (target == null)
            {
                target = ChooseTarget(s);
                if (target == null) { s.Idle = true; s.TargetId = -1; return; }
                s.Idle = false;
                s.TargetId = target.Id;
                PlanPath(s, target);
            }

            long r2 = (long)s.RangeMilli * s.RangeMilli;
            if (s.Phase == 1 && CombatMath.DistSq(s.X, s.Y, target.CenterX, target.CenterY) <= r2)
            {
                if (s.Cooldown > 0) { s.Cooldown--; return; }
                AttackStructure(s, target);
                s.Cooldown = Math.Max(0, s.IntervalTicks - 1);
                return;
            }
            Move(s, target);
        }

        private void AttackStructure(SimSoldier s, SimStructure target)
        {
            var pk = s.Perks;
            int pct = 100;
            if (target.Kind == StructKind.Tower) pct += pk.VsTowerPct;
            if (target.IsResource) pct += pk.VsResourcePct;
            if (s.Hp * 2 < s.MaxHp) pct += pk.LowHpAtkPct;
            int dmg = s.Attack * pct / 100;
            s.AttackCount++;
            if (pk.FirstHitMultPct > 0 && s.AttackCount == 1) dmg = dmg * pk.FirstHitMultPct / 100;
            if (pk.EveryN > 0 && s.AttackCount % pk.EveryN == 0) dmg = dmg * pk.EveryMultPct / 100;
            Damage(target, dmg);
            if (s.SplashMilli > 0)
            {
                long sr2 = (long)s.SplashMilli * s.SplashMilli;
                for (int i = 0; i < structures.Count; i++)
                {
                    var o = structures[i];
                    if (o == target || !o.Alive) continue;
                    if (CombatMath.DistSq(target.CenterX, target.CenterY, o.CenterX, o.CenterY) <= sr2)
                        Damage(o, Math.Max(1, dmg / 2));
                }
            }
        }

        private static void Damage(SimStructure st, int dmg)
        {
            st.Hp -= dmg;
            if (st.Hp <= 0) { st.Hp = 0; st.Alive = false; }
        }

        private void Move(SimSoldier s, SimStructure target)
        {
            if (!s.Perks.ImmuneSlow && Tick < s.RootUntil) return; // kapan/buz: yerinde
            int speed = (!s.Perks.ImmuneSlow && Tick < s.SlowUntil) ? s.SpeedMilli / 2 : s.SpeedMilli;
            if (speed < 1) speed = 1;
            if (s.Flying) { MoveToward(s, target.CenterX, target.CenterY, speed); return; }

            if (s.Phase == 0)
            {
                if (MoveToward(s, s.Entrance.X * 1000, 0, speed)) { s.Phase = 1; s.LastCell = s.Entrance; ApplyTrap(s, s.Entrance); }
                return;
            }
            if (s.Path == null || s.PathIndex >= s.Path.Count) return;
            var c = s.Path[s.PathIndex];
            if (!s.Perks.ImmuneSlow) speed = Math.Max(1, speed * (100 - RoadRules.SlowPct(setup.Layout.GetRoadTier(c))) / 100); // yol malzemesi yavaşlatır
            if (MoveToward(s, c.X * 1000, c.Y * 1000, speed)) { s.LastCell = c; s.PathIndex++; ApplyTrap(s, c); }
        }

        private static bool MoveToward(SimSoldier s, int tx, int ty, int speed)
        {
            long dx = tx - s.X, dy = ty - s.Y;
            long d2 = dx * dx + dy * dy;
            if (d2 <= (long)speed * speed) { s.X = tx; s.Y = ty; return true; }
            int d = CombatMath.ISqrt(d2);
            s.X += (int)(dx * speed / d);
            s.Y += (int)(dy * speed / d);
            return false;
        }

        private readonly HashSet<GridPos> usedTraps = new HashSet<GridPos>();

        /// <summary>Tuzak tetikleme (TrapCatalog). Uçanlar etkilenmez; yol malzemesi hasarı artırır; Patlayıcı savaş başına bir kez.</summary>
        private void ApplyTrap(SimSoldier s, GridPos cell)
        {
            if (s.Flying || !setup.Traps.TryGetValue(cell, out var trap)) return;
            var d = TrapCatalog.Get(trap);
            if (d == null || (d.OneShot && usedTraps.Contains(cell))) return;
            int bonus = RoadRules.TrapBonusPct(setup.Layout.GetRoadTier(cell));
            if (d.Damage > 0)
            {
                int dmg = d.Damage * bonus / 100;
                if (d.SplashMilli > 0)
                {
                    long r2 = (long)d.SplashMilli * d.SplashMilli;
                    for (int i = 0; i < soldiers.Count; i++)
                    {
                        var o = soldiers[i];
                        if (o.Alive && !o.Flying && CombatMath.DistSq(cell.X * 1000, cell.Y * 1000, o.X, o.Y) <= r2) Hit(o, CombatMath.ReduceByArmor(dmg, o.ArmorPct));
                    }
                }
                else Hit(s, CombatMath.ReduceByArmor(dmg, s.ArmorPct));
            }
            if (!s.Perks.ImmuneSlow)
            {
                if (d.RootTicks > 0) s.RootUntil = Math.Max(s.RootUntil, Tick + d.RootTicks);
                if (d.SlowTicks > 0) s.SlowUntil = Math.Max(s.SlowUntil, Tick + d.RootTicks + d.SlowTicks);
            }
            if (d.DotTicks > 0)
            {
                int per = d.DotPerTick * bonus / 100;
                if (d.FireDot) per = per * (100 - s.FireResistPct) / 100;
                s.BurnUntil = Tick + d.DotTicks; s.BurnPerTick = Math.Max(1, per);
            }
            if (d.ChainCount > 0)
            {
                var hit = new HashSet<int> { s.Id }; var cur = s;
                for (int n = 0; n < d.ChainCount; n++)
                {
                    SimSoldier next = null; long best = 2000L * 2000L + 1;
                    for (int j = 0; j < soldiers.Count; j++)
                    {
                        var o = soldiers[j];
                        if (!o.Alive || o.Flying || hit.Contains(o.Id)) continue;
                        long dd = CombatMath.DistSq(cur.X, cur.Y, o.X, o.Y);
                        if (dd < best) { best = dd; next = o; }
                    }
                    if (next == null) break;
                    Hit(next, CombatMath.ReduceByArmor(d.Damage * bonus / 100 * 70 / 100, next.ArmorPct));
                    hit.Add(next.Id); cur = next;
                }
            }
            if (d.OneShot) usedTraps.Add(cell);
        }

        // ---------- Hedef ve yol ----------
        private HashSet<GridPos> AccessCells(SimSoldier s, SimStructure target)
        {
            var goals = new HashSet<GridPos>();
            long r2 = (long)s.RangeMilli * s.RangeMilli;
            foreach (var c in roads)
                if (CombatMath.DistSq(c.X * 1000, c.Y * 1000, target.CenterX, target.CenterY) <= r2) goals.Add(c);
            return goals;
        }

        private int PriorityClass(SimSoldier s, SimStructure st)
        {
            // Duvar: yakındaki yer askerlerini üstüne çeker (en yüksek öncelik)
            if (!s.Flying && st.Kind == StructKind.Tower)
            {
                int taunt = ContentCatalog.Tower(st.TowerType).TauntMilli;
                if (taunt > 0 && CombatMath.DistSq(s.X, s.Y, st.CenterX, st.CenterY) <= (long)taunt * taunt) return -1;
            }
            // Uçanlar Duvar'ı yok sayar (approved #42): yalnızca başka yapı kalmazsa
            if (s.Flying && st.Kind == StructKind.Tower && ContentCatalog.Tower(st.TowerType).Attack <= 0) return 9;
            // Hedef öncelikleri (kullanıcı kararı 15.11.2026): Goblin önce kaynak depoları, Atlı önce kuleler, diğer herkes en yakın yapı
            switch (s.Type)
            {
                case SoldierType.Goblin: return st.IsDepot ? 0 : 1;
                case SoldierType.Cavalry: return st.Kind == StructKind.Tower ? 0 : 1;
                default: return 0;
            }
        }

        private SimStructure ChooseTarget(SimSoldier s)
        {
            var cands = new List<SimStructure>();
            for (int i = 0; i < structures.Count; i++) if (structures[i].Alive) cands.Add(structures[i]);
            if (cands.Count == 0) return null;

            var keyClass = new Dictionary<int, int>();
            var keyDist = new Dictionary<int, long>();
            foreach (var c in cands)
            {
                keyClass[c.Id] = PriorityClass(s, c);
                keyDist[c.Id] = CombatMath.DistSq(s.X, s.Y, c.CenterX, c.CenterY);
            }
            cands.Sort((a, b) =>
            {
                int r = keyClass[a.Id].CompareTo(keyClass[b.Id]);
                if (r != 0) return r;
                r = keyDist[a.Id].CompareTo(keyDist[b.Id]);
                return r != 0 ? r : a.Id.CompareTo(b.Id);
            });

            if (s.Flying) return cands[0];

            // Yer askeri: "en yakın" = yol üzerindeki yürüme mesafesi (kuş uçuşu değil; kullanıcı geri bildirimi 15.11.2026).
            // Eşitlikte kuş uçuşu, sonra kimlik (deterministik).
            GridPos start = s.Phase == 0 ? s.Entrance : s.LastCell;
            var dist = RoadDistances(start);
            var keyPath = new Dictionary<int, int>();
            var reach = new List<SimStructure>();
            foreach (var c in cands)
            {
                // Ulaşılabilir mi? (saldırı menzilindeki yol kareleri)
                int reachD = int.MaxValue;
                foreach (var g in AccessCells(s, c))
                    if (dist.TryGetValue(g, out int d) && d < reachD) reachD = d;
                if (reachD == int.MaxValue) continue;
                // Sıra: yapının yola değdiği (yan yana) kareye yürüme mesafesi; yalnızca çaprazdan değiyorsa +1
                int best = int.MaxValue;
                for (int i = 0; i < 4; i++)
                {
                    var n = new GridPos(c.Pos.X + BfsDx[i], c.Pos.Y + BfsDy[i]);
                    if (dist.TryGetValue(n, out int d) && d < best) best = d;
                }
                if (best == int.MaxValue) best = reachD + 1;
                keyPath[c.Id] = best;
                reach.Add(c);
            }
            if (reach.Count == 0) return null;
            reach.Sort((a, b) =>
            {
                int r = keyClass[a.Id].CompareTo(keyClass[b.Id]);
                if (r != 0) return r;
                r = keyPath[a.Id].CompareTo(keyPath[b.Id]);
                if (r != 0) return r;
                r = keyDist[a.Id].CompareTo(keyDist[b.Id]);
                return r != 0 ? r : a.Id.CompareTo(b.Id);
            });
            return reach[0];
        }

        private static readonly int[] BfsDx = { 0, 1, 0, -1 }, BfsDy = { 1, 0, -1, 0 };

        /// <summary>Başlangıç yol karesinden diğer yol karelerine adım sayısı (4 yönlü, sabit komşu sırası).</summary>
        private Dictionary<GridPos, int> RoadDistances(GridPos start)
        {
            var d = new Dictionary<GridPos, int>();
            if (!roads.Contains(start)) return d;
            var q = new Queue<GridPos>();
            d[start] = 0; q.Enqueue(start);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                int nd = d[c] + 1;
                for (int i = 0; i < 4; i++)
                {
                    var n = new GridPos(c.X + BfsDx[i], c.Y + BfsDy[i]);
                    if (roads.Contains(n) && !d.ContainsKey(n)) { d[n] = nd; q.Enqueue(n); }
                }
            }
            return d;
        }

        private void PlanPath(SimSoldier s, SimStructure target)
        {
            if (s.Flying) return;
            GridPos start = s.Phase == 0 ? s.Entrance : s.LastCell;
            s.Path = PathFinder.FindPath(size, roads, start, AccessCells(s, target));
            s.PathIndex = 1;
        }

        // ---------- Sonuç ----------
        /// <summary>Savaş bittikten sonra sonuç. defenderResources: savunmacının gerçek kaynakları (yağma hesabı için).</summary>
        public BattleResult Result(ResourceCost defenderResources, ResourceConfig cfg, int defenderLevel)
        {
            var res = new BattleResult { DurationTicks = Tick, Retreated = Retreated };

            for (int i = 0; i < structures.Count; i++)
            {
                var st = structures[i];
                int pct = st.Alive ? 100 - st.Hp * 100 / st.MaxHp : 100;
                res.StructureDamagePcts.Add(pct);
                if (st.Kind == StructKind.Tower)
                {
                    int taken = (st.StartHp - st.Hp) * 100 / st.MaxHp;
                    res.TowerDamages.Add(new TowerDamageRecord(st.Pos, Math.Max(0, taken)));
                }
            }
            res.OverallDamagePct = CombatMath.OverallDamagePct(res.StructureDamagePcts);
            res.Victory = CombatMath.IsVictory(res.OverallDamagePct);
            res.Stars = CombatMath.Stars(res.OverallDamagePct);
            res.Xp = ProgressionRules.BattleXp(res.OverallDamagePct, res.Stars);

            // Asker kayıpları
            foreach (var kv in stock) Add(res.Returned, kv.Key, kv.Value);
            var healthy = new List<SimSoldier>();
            for (int i = 0; i < soldiers.Count; i++)
            {
                var s = soldiers[i];
                if (!s.Alive) Add(res.LostDead, s.Type, 1);
                else if (s.Hp * 100 <= s.MaxHp * InjuredHpPct) Add(res.LostInjured, s.Type, 1);
                else healthy.Add(s);
            }
            int sacrificed = 0;
            var sacrificedIdx = new HashSet<int>();
            if (Retreated)
            {
                sacrificed = healthy.Count * RetreatLossPct / 100;
                var order = new List<int>();
                for (int i = 0; i < healthy.Count; i++) order.Add(i);
                for (int i = 0; i < sacrificed; i++)
                {
                    int j = i + rng.Next(order.Count - i);
                    int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
                    sacrificedIdx.Add(order[i]);
                }
            }
            for (int i = 0; i < healthy.Count; i++)
            {
                if (sacrificedIdx.Contains(i)) Add(res.LostSacrificed, healthy[i].Type, 1);
                else Add(res.Returned, healthy[i].Type, 1);
            }

            res.Loot = res.Victory ? ComputeLoot(defenderResources, cfg, defenderLevel) : new ResourceCost(0);
            return res;
        }

        /// <summary>Yağma: kaynağın %70'i × ilgili depo(lar)ın ortalama hasar yüzdesi; yalnızca açık kaynaklar (GDD Bölüm 18).</summary>
        private ResourceCost ComputeLoot(ResourceCost have, ResourceConfig cfg, int level)
        {
            int[] amt = new int[4];
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
            {
                if (!ResourceRules.IsUnlocked(cfg, t, level)) continue;
                long sum = 0; int n = 0;
                for (int i = 0; i < structures.Count; i++)
                {
                    var st = structures[i];
                    if (st.Kind != StructKind.Building || !st.IsDepot || st.Stores != t) continue;
                    sum += st.Alive ? 100 - st.Hp * 100 / st.MaxHp : 100;
                    n++;
                }
                if (n == 0) continue;
                int pct = (int)(sum / n);
                amt[(int)t] = (int)((long)have.Get(t) * LootCapPct * pct / 10000);
            }
            return new ResourceCost(amt[0], amt[1], amt[2], amt[3]);
        }

        private static void Add(Dictionary<SoldierType, int> d, SoldierType t, int n)
        {
            d[t] = (d.TryGetValue(t, out var v) ? v : 0) + n;
        }
    }
}
