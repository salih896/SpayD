using System;
using System.Collections.Generic;
using GameCore.Economy;

namespace GameCore.Progression
{
    public enum QuestEvent { BattleFought, BattleWon, StarsEarned, SoldiersTrained, BuildingsUpgraded, TowersBuilt, ResearchDone, TowersDestroyed, LootTaken }
    public enum QuestKind { Daily, Milestone }

    public sealed class QuestDef
    {
        public string Id, Name;
        public QuestKind Kind;
        public QuestEvent Event;
        public int Target;
        public int MinLevel;
        public int Xp;
        public ResourceCost Reward;
    }

    /// <summary>
    /// Görev kataloğu (GEÇİCİ sayılar). Amaç: oyuncuya her gün dönecek bir sebep (günlük görevler) ve uzun vadeli hedefler
    /// (kilometre taşı zincirleri) vermek. Ödül kaynakları yalnızca açık kaynaklardır (ödül verirken filtrelenir).
    /// </summary>
    public static class QuestCatalog
    {
        private static readonly List<QuestDef> all = new List<QuestDef>();
        private static readonly Dictionary<string, QuestDef> byId = new Dictionary<string, QuestDef>();
        public static IReadOnlyList<QuestDef> All => all;
        public static QuestDef Get(string id) => byId.TryGetValue(id, out var q) ? q : null;

        static QuestCatalog()
        {
            // Kilometre taşı zincirleri: her basamak bir öncekinden büyük hedef ve ödül
            Chain(QuestEvent.BattleWon, "Zafer", "{0} savaş kazan", new[] { 1, 3, 10, 25, 50, 100, 250, 500, 1000 }, 1);
            Chain(QuestEvent.BattleFought, "Savaşçı", "{0} savaşa katıl", new[] { 3, 15, 50, 150, 400 }, 1);
            Chain(QuestEvent.StarsEarned, "Yıldız Avcısı", "{0} yıldız topla", new[] { 3, 15, 45, 120, 300, 750 }, 1);
            Chain(QuestEvent.SoldiersTrained, "Komutan", "{0} asker eğit", new[] { 10, 50, 200, 600, 1500 }, 3);
            Chain(QuestEvent.BuildingsUpgraded, "Mimar", "{0} yapı yükselt", new[] { 3, 10, 30, 75, 150, 300 }, 1);
            Chain(QuestEvent.TowersBuilt, "Kule Ustası", "{0} kule kur", new[] { 2, 6, 15, 30 }, 2);
            Chain(QuestEvent.ResearchDone, "Bilge", "{0} araştırma tamamla", new[] { 1, 5, 15, 40, 100, 250 }, 5);
            Chain(QuestEvent.LootTaken, "Yağmacı", "{0} kaynak yağmala", new[] { 500, 3000, 15000, 60000, 200000 }, 1);
            Chain(QuestEvent.TowersDestroyed, "Yıkıcı", "{0} kule yık", new[] { 5, 25, 100, 400 }, 2);

            // Günlük havuz (her gün 3 tanesi seçilir)
            Daily("d_win2", "Bugün 2 savaş kazan", QuestEvent.BattleWon, 2, 1, 40, 60);
            Daily("d_fight4", "Bugün 4 savaşa katıl", QuestEvent.BattleFought, 4, 1, 35, 50);
            Daily("d_star5", "Bugün 5 yıldız topla", QuestEvent.StarsEarned, 5, 1, 45, 70);
            Daily("d_train10", "Bugün 10 asker eğit", QuestEvent.SoldiersTrained, 10, 3, 30, 60);
            Daily("d_upg2", "Bugün 2 yapı yükselt", QuestEvent.BuildingsUpgraded, 2, 1, 30, 60);
            Daily("d_tower2", "Bugün 2 kule yık", QuestEvent.TowersDestroyed, 2, 2, 35, 55);
            Daily("d_loot", "Bugün 800 kaynak yağmala", QuestEvent.LootTaken, 800, 1, 40, 60);
            Daily("d_research", "Bugün 1 araştırma tamamla", QuestEvent.ResearchDone, 1, 5, 60, 90);
        }

        private static void Add(QuestDef q) { all.Add(q); byId[q.Id] = q; }

        private static void Chain(QuestEvent ev, string title, string fmt, int[] targets, int minLevel)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                int tier = i + 1;
                int baseRes = 80 * tier * tier + 60 * tier;
                Add(new QuestDef
                {
                    Id = "m_" + ev + "_" + tier, Kind = QuestKind.Milestone, Event = ev, Target = targets[i], MinLevel = minLevel,
                    Name = title + " " + ToRoman(tier) + ": " + string.Format(fmt, targets[i]),
                    Xp = 40 * tier * tier + 30 * tier,
                    Reward = new ResourceCost(baseRes, baseRes / 2, baseRes / 4, baseRes / 8)
                });
            }
        }

        private static void Daily(string id, string name, QuestEvent ev, int target, int minLevel, int xp, int res)
        {
            Add(new QuestDef { Id = id, Name = name, Kind = QuestKind.Daily, Event = ev, Target = target, MinLevel = minLevel, Xp = xp, Reward = new ResourceCost(res, res / 2, res / 4, res / 8) });
        }

        private static string ToRoman(int n)
        {
            string[] r = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return n < r.Length ? r[n] : n.ToString();
        }

        /// <summary>Ödülü oyuncu seviyesinde açık kaynaklarla sınırla; seviye arttıkça ödül de büyür (%5/seviye).</summary>
        public static ResourceCost RewardFor(QuestDef q, ResourceConfig cfg, int playerLevel)
        {
            int k = 100 + 5 * (playerLevel - 1);
            Func<ResourceType, int, int> f = (t, v) => ResourceRules.IsUnlocked(cfg, t, playerLevel) ? (int)((long)v * k / 100) : 0;
            return new ResourceCost(f(ResourceType.Wood, q.Reward.Wood), f(ResourceType.Stone, q.Reward.Stone), f(ResourceType.Gold, q.Reward.Gold), f(ResourceType.Iron, q.Reward.Iron));
        }
    }

    /// <summary>Oyuncunun görev ilerlemesi. Günlükler gün numarasına göre deterministik seçilir (sunucu doğrular).</summary>
    public sealed class QuestState
    {
        public const int DailyCount = 3;
        private readonly Dictionary<string, int> progress = new Dictionary<string, int>();
        private readonly HashSet<string> claimed = new HashSet<string>();
        private readonly List<string> dailyIds = new List<string>();
        public int DailyDay { get; private set; } = -1;
        public IReadOnlyList<string> DailyIds => dailyIds;
        public IReadOnlyDictionary<string, int> Progress => progress;
        public IReadOnlyCollection<string> Claimed => claimed;

        public void Restore(string id, int value, bool isClaimed) { progress[id] = value; if (isClaimed) claimed.Add(id); }
        public void RestoreDaily(int day, IEnumerable<string> ids) { DailyDay = day; dailyIds.Clear(); dailyIds.AddRange(ids); }

        public int ProgressOf(string id) => progress.TryGetValue(id, out var v) ? v : 0;
        public bool IsClaimed(string id) => claimed.Contains(id);
        public bool IsComplete(QuestDef q) => ProgressOf(q.Id) >= q.Target;

        /// <summary>Yeni gün başladıysa günlükleri yeniden seç ve günlük ilerlemeyi sıfırla.
        /// Gün yalnızca ileri gider: saat geri alınırsa (cihaz saati, test aracı) günlükler yeniden seçilmez.</summary>
        public bool RollDaily(int dayIndex, int playerLevel)
        {
            if (dayIndex <= DailyDay) return false;
            foreach (var id in dailyIds) { progress.Remove(id); claimed.Remove(id); }
            dailyIds.Clear();
            var pool = new List<QuestDef>();
            foreach (var q in QuestCatalog.All) if (q.Kind == QuestKind.Daily && q.MinLevel <= playerLevel) pool.Add(q);
            var rng = new GameCore.Combat.DeterministicRng(7919 + dayIndex * 31);
            while (dailyIds.Count < DailyCount && pool.Count > 0)
            {
                int i = rng.Next(pool.Count);
                dailyIds.Add(pool[i].Id); pool.RemoveAt(i);
            }
            DailyDay = dayIndex;
            return true;
        }

        public bool IsActive(QuestDef q, int playerLevel)
        {
            if (q.MinLevel > playerLevel) return false;
            if (q.Kind == QuestKind.Daily) return dailyIds.Contains(q.Id);
            return true;
        }

        public void Report(QuestEvent ev, int amount, int playerLevel)
        {
            if (amount <= 0) return;
            foreach (var q in QuestCatalog.All)
            {
                if (q.Event != ev || !IsActive(q, playerLevel) || claimed.Contains(q.Id)) continue;
                // Zincirde yalnızca açık (önceki basamağı alınmış) kilometre taşı ilerler
                if (q.Kind == QuestKind.Milestone && !PreviousClaimed(q)) continue;
                progress[q.Id] = Math.Min(q.Target, ProgressOf(q.Id) + amount);
            }
        }

        private bool PreviousClaimed(QuestDef q)
        {
            int us = q.Id.LastIndexOf('_');
            int tier = int.Parse(q.Id.Substring(us + 1));
            if (tier <= 1) return true;
            return claimed.Contains(q.Id.Substring(0, us + 1) + (tier - 1));
        }

        /// <summary>Aktif ve görünür görevler (günlükler + her zincirin sıradaki basamağı).</summary>
        public List<QuestDef> Visible(int playerLevel)
        {
            var list = new List<QuestDef>();
            foreach (var q in QuestCatalog.All)
            {
                if (!IsActive(q, playerLevel) || claimed.Contains(q.Id)) continue;
                if (q.Kind == QuestKind.Milestone && !PreviousClaimed(q)) continue;
                list.Add(q);
            }
            return list;
        }

        public bool TryClaim(string id, int playerLevel, out QuestDef quest, out string error)
        {
            error = null; quest = QuestCatalog.Get(id);
            if (quest == null) { error = "Görev yok"; return false; }
            if (claimed.Contains(id)) { error = "Zaten alındı"; return false; }
            if (!IsActive(quest, playerLevel)) { error = "Görev aktif değil"; return false; }
            if (!IsComplete(quest)) { error = "Görev tamamlanmadı"; return false; }
            claimed.Add(id);
            return true;
        }
    }
}
