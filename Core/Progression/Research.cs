using System;
using System.Collections.Generic;
using GameCore.Combat;
using GameCore.Economy;

namespace GameCore.Progression
{
    public enum ResearchTree { Economy = 0, Defense, Military, Architecture, Village, Science, Element }

    public enum ResearchStat
    {
        ProductionPct, StoragePct, TowerDamage, TowerHp, SoldierDamage, SoldierHp, BuildingHp,
        UpgradeCost, BuildTime, TrainTime, ResearchTime, ResearchCost, ElementMastery
    }

    public sealed class ResearchDef
    {
        public string Id;
        public ResearchTree Tree;
        public int Level;      // araştırma seviyesi (1..30) = gereken Üniversite seviyesi
        public int Column;     // 1..3
        public string Name;
        public ResearchStat Stat;
        public int Permille;   // bonus (‰)
    }

    /// <summary>
    /// 474 araştırmanın kataloğu (GameDesign/TechTrees.md'den üretilen ResearchData). Bonus değerleri GEÇİCİDİR (dengeleme aşaması).
    /// Ön koşul: aynı sütundaki bir önceki araştırma; 3. sütun ayrıca aynı seviyedeki 1. ve 2. sütunu gerektirir (öneri, onay bekler).
    /// Element ağacının diğer ağaçlarla ortak kilitleri henüz uygulanmadı (docs/open-questions.md).
    /// </summary>
    public static class ResearchCatalog
    {
        public const int MaxLevel = 30;
        private static readonly List<ResearchDef> all = new List<ResearchDef>();
        private static readonly Dictionary<string, ResearchDef> byId = new Dictionary<string, ResearchDef>();

        static ResearchCatalog()
        {
            foreach (var row in ResearchData.Rows)
            {
                var p = row.Split('|');
                int tree = int.Parse(p[0]), lvl = int.Parse(p[1]), col = int.Parse(p[2]);
                var d = new ResearchDef { Tree = (ResearchTree)tree, Level = lvl, Column = col, Name = p[3], Id = MakeId(tree, lvl, col) };
                AssignStat(d);
                all.Add(d); byId[d.Id] = d;
            }
        }

        public static string MakeId(int tree, int level, int col) => tree + "-" + level.ToString("00") + "-" + col;
        public static IReadOnlyList<ResearchDef> All => all;
        public static ResearchDef Get(string id) => byId.TryGetValue(id, out var d) ? d : null;
        public static ResearchDef Find(ResearchTree tree, int level, int col) => Get(MakeId((int)tree, level, col));

        private static void AssignStat(ResearchDef d)
        {
            bool c3 = d.Column == 3;
            switch (d.Tree)
            {
                case ResearchTree.Economy: d.Stat = d.Column == 2 ? ResearchStat.StoragePct : ResearchStat.ProductionPct; d.Permille = c3 ? 25 : 15; break;
                case ResearchTree.Defense: d.Stat = d.Column == 2 ? ResearchStat.TowerHp : ResearchStat.TowerDamage; d.Permille = c3 ? 18 : 10; break;
                case ResearchTree.Military: d.Stat = d.Column == 2 ? ResearchStat.SoldierHp : ResearchStat.SoldierDamage; d.Permille = c3 ? 18 : 10; break;
                case ResearchTree.Architecture: d.Stat = d.Column == 2 ? ResearchStat.UpgradeCost : ResearchStat.BuildingHp; d.Permille = d.Column == 2 ? 10 : (c3 ? 20 : 15); break;
                case ResearchTree.Village: d.Stat = d.Column == 2 ? ResearchStat.TrainTime : ResearchStat.BuildTime; d.Permille = c3 ? 15 : 10; break;
                case ResearchTree.Science: d.Stat = d.Column == 2 ? ResearchStat.ResearchCost : ResearchStat.ResearchTime; d.Permille = c3 ? 20 : (d.Column == 2 ? 10 : 14); break;
                default: d.Stat = ResearchStat.ElementMastery; d.Permille = 3; break;
            }
        }

        /// <summary>Üst sınırlar (‰): Bilim süre -%30, maliyet -%20 (onaylı); diğerleri GEÇİCİ.</summary>
        public static int CapOf(ResearchStat s)
        {
            switch (s)
            {
                case ResearchStat.ResearchTime: return 300;
                case ResearchStat.ResearchCost: return 200;
                case ResearchStat.BuildTime: return 400;
                case ResearchStat.TrainTime: return 400;
                case ResearchStat.UpgradeCost: return 300;
                default: return int.MaxValue;
            }
        }

        public static int BaseSeconds(ResearchDef d) => (60 * d.Level + 20 * d.Level * d.Level) * (d.Column == 3 ? 3 : 2) / 2;

        /// <summary>Yalnızca oyuncu seviyesinde AÇIK kaynaklarla ödenir (onaylı kural). Odun tabanı, diğerleri oranla.</summary>
        public static ResourceCost BaseCost(ResearchDef d, ResourceConfig cfg, int playerLevel)
        {
            int w = (60 * d.Level + 8 * d.Level * d.Level) * (d.Column == 3 ? 3 : 2) / 2;
            return new ResourceCost(w,
                ResourceRules.IsUnlocked(cfg, ResourceType.Stone, playerLevel) ? w / 2 : 0,
                ResourceRules.IsUnlocked(cfg, ResourceType.Gold, playerLevel) ? w / 4 : 0,
                ResourceRules.IsUnlocked(cfg, ResourceType.Iron, playerLevel) ? w / 6 : 0);
        }

        public static ResourceCost Scale(ResourceCost c, int reductionPermille)
        {
            int k = 1000 - Math.Max(0, Math.Min(900, reductionPermille));
            return new ResourceCost((int)((long)c.Wood * k / 1000), (int)((long)c.Stone * k / 1000), (int)((long)c.Gold * k / 1000), (int)((long)c.Iron * k / 1000));
        }
    }

    public sealed class ActiveResearch
    {
        public string Id;
        public int RemainingSeconds;
    }

    /// <summary>Oyuncunun araştırma durumu: tamamlananlar + sürenler. Üniversite Sv5'te açılır; araştırma seviyesi ≤ üniversite seviyesi.</summary>
    public sealed class ResearchState
    {
        public const int UniversityUnlockLevel = 5;
        private readonly HashSet<string> done = new HashSet<string>();
        private readonly List<ActiveResearch> active = new List<ActiveResearch>();

        public IReadOnlyCollection<string> Done => done;
        public IReadOnlyList<ActiveResearch> Active => active;
        public bool IsDone(string id) => done.Contains(id);
        public bool IsActive(string id) { foreach (var a in active) if (a.Id == id) return true; return false; }
        public void ForceComplete(string id) { done.Add(id); }
        public void ForceActive(string id, int remaining) { active.Add(new ActiveResearch { Id = id, RemainingSeconds = remaining }); }

        /// <summary>2. araştırma yuvası Bilim seviye 30 araştırmalarından biri bitince açılır.</summary>
        public int Slots
        {
            get
            {
                for (int c = 1; c <= 3; c++) if (done.Contains(ResearchCatalog.MakeId((int)ResearchTree.Science, 30, c))) return 2;
                return 1;
            }
        }

        public int Total(ResearchStat s)
        {
            int sum = 0;
            foreach (var id in done) { var d = ResearchCatalog.Get(id); if (d != null && d.Stat == s) sum += d.Permille; }
            return Math.Min(sum, ResearchCatalog.CapOf(s));
        }

        public CombatMods Mods()
        {
            int m = Total(ResearchStat.ElementMastery);
            return new CombatMods
            {
                SoldierDamage = Total(ResearchStat.SoldierDamage) + m, SoldierHp = Total(ResearchStat.SoldierHp),
                TowerDamage = Total(ResearchStat.TowerDamage) + m, TowerHp = Total(ResearchStat.TowerHp),
                BuildingHp = Total(ResearchStat.BuildingHp)
            };
        }

        /// <summary>Kilit durumu: null = başlatılabilir, aksi halde sebep.</summary>
        public string LockReason(ResearchDef d, int playerLevel, int universityLevel)
        {
            if (done.Contains(d.Id)) return "Tamamlandı";
            if (IsActive(d.Id)) return "Sürüyor";
            if (playerLevel < UniversityUnlockLevel || universityLevel < 1) return "Üniversite Sv" + UniversityUnlockLevel + "'te açılır";
            if (universityLevel < d.Level) return "Üniversite Sv " + d.Level + " gerekli";
            if (d.Column == 3)
            {
                if (!done.Contains(ResearchCatalog.MakeId((int)d.Tree, d.Level, 1)) || !done.Contains(ResearchCatalog.MakeId((int)d.Tree, d.Level, 2)))
                    return "Aynı seviyedeki 1. ve 2. araştırma gerekli";
                for (int l = d.Level - 1; l >= 1; l--)
                {
                    var prev = ResearchCatalog.Find(d.Tree, l, 3);
                    if (prev == null) continue;
                    if (!done.Contains(prev.Id)) return "Önce: " + prev.Name;
                    break;
                }
            }
            else if (d.Level > 1)
            {
                var prev = ResearchCatalog.Find(d.Tree, d.Level - 1, d.Column);
                if (prev != null && !done.Contains(prev.Id)) return "Önce: " + prev.Name;
            }
            return null;
        }

        public int SecondsFor(ResearchDef d) => Math.Max(10, (int)((long)ResearchCatalog.BaseSeconds(d) * (1000 - Total(ResearchStat.ResearchTime)) / 1000));
        public ResourceCost CostFor(ResearchDef d, ResourceConfig cfg, int playerLevel) =>
            ResearchCatalog.Scale(ResearchCatalog.BaseCost(d, cfg, playerLevel), Total(ResearchStat.ResearchCost));

        public bool TryStart(ResearchDef d, Wallet wallet, ResourceConfig cfg, int playerLevel, int universityLevel, out string error)
        {
            error = LockReason(d, playerLevel, universityLevel);
            if (error != null) return false;
            if (active.Count >= Slots) { error = "Araştırma yuvası dolu"; return false; }
            if (!wallet.TrySpend(CostFor(d, cfg, playerLevel), playerLevel, SpendArea.Research, out error)) return false;
            active.Add(new ActiveResearch { Id = d.Id, RemainingSeconds = SecondsFor(d) });
            return true;
        }

        /// <summary>Süreyi ilerletir; biten araştırmaların listesini döndürür.</summary>
        public List<ResearchDef> Advance(int seconds)
        {
            var finished = new List<ResearchDef>();
            for (int i = active.Count - 1; i >= 0; i--)
            {
                active[i].RemainingSeconds -= seconds;
                if (active[i].RemainingSeconds <= 0)
                {
                    done.Add(active[i].Id);
                    finished.Add(ResearchCatalog.Get(active[i].Id));
                    active.RemoveAt(i);
                }
            }
            return finished;
        }
    }
}
