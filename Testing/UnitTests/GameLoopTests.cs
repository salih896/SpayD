using System.Collections.Generic;
using NUnit.Framework;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Game;
using GameCore.Grid;
using GameCore.Progression;
using GameCore.Village;

namespace GameCore.Tests
{
    /// <summary>
    /// HANDOFF Bölüm 4'te "elle denenmedi" olan akışların Core tarafı:
    /// zafer yolu, günlük baskın + rapor, kayıt-yükleme, araştırma başlat/bitir, seviye atlama sırası.
    /// </summary>
    public class GameLoopTests
    {
        private static GridPos P(int x, int y) => new GridPos(x, y);

        /// <summary>Seviye L'ye ulaşmak için gereken toplam XP.</summary>
        private static long XpFor(int level)
        {
            long s = 0;
            for (int l = 1; l < level; l++) s += ProgressionRules.XpToNextLevel(l);
            return s;
        }

        private static void FillWallet(GameState g, int amount)
        {
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
            {
                g.Wallet.SetCapacity(t, 100000000);
                g.Wallet.SetAmount(t, amount);
            }
        }

        /// <summary>İstemcideki gibi: oyuncu askerleri alana dağıtır, savaş sonuna kadar oynar.</summary>
        private static void DeployAllAndRun(BattleSim sim, int size)
        {
            int i = 0;
            for (int k = 0; k < ContentCatalog.SoldierTypeCount; k++)
            {
                var t = (SoldierType)k;
                int n = sim.StockOf(t);
                for (int j = 0; j < n; j++, i++) sim.Deploy(t, (i * 3) % size, 1 + (i % VillageLayout.AttackZoneRows), out _);
            }
            int guard = BattleSim.TimeLimitTicks + 5;
            while (!sim.Finished && guard-- > 0) sim.Step();
        }

        /// <summary>Sv5, geçerli köy (Sv1 yol düzeni korunur) + 1 okçu kulesi: baskın şartları sağlanır.</summary>
        private static GameState RaidableVillage()
        {
            var g = GameState.NewGame();
            g.TotalXp = XpFor(5);
            g.AddTowerDirect(P(6, 5), TowerType.Archer, 1);
            string err;
            Assert.IsTrue(g.VillageValid(out err), err);
            Assert.AreEqual(5, g.Level);
            return g;
        }

        // ---------- Zafer yolu ----------
        [Test]
        public void Victory_UpdatesStatsLootXpAndReturnsSoldiers()
        {
            var g = GameState.NewGame();
            g.TotalXp = XpFor(10);
            FillWallet(g, 0);
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++) g.SoldierLevels[(SoldierType)i] = 10;
            g.Inventory.Add(SoldierType.Swordsman, 40);
            g.Inventory.Add(SoldierType.Archer, 20);
            var tgt = TargetGenerator.Generate(3, 0, 1); // kolay, düşük seviyeli hedef

            string err;
            var sim = g.StartAttack(tgt.Defense, 77, out err);
            Assert.IsNotNull(sim, err);
            Assert.AreEqual(0, g.Inventory.Get(SoldierType.Swordsman), "Saldırıya çıkan askerler envanterden düşmeli");
            DeployAllAndRun(sim, tgt.Defense.Layout.Size);
            var res = sim.Result(tgt.Pool, g.Config, tgt.Level);
            Assert.IsTrue(res.Victory, "Güçlü ordu kolay hedefi yenmeli (hasar %" + res.OverallDamagePct + ")");
            Assert.GreaterOrEqual(res.Stars, 1);

            long xpBefore = g.TotalXp;
            int woodBefore = g.Wallet.Get(ResourceType.Wood);
            var o = g.FinishAttack(sim, res, tgt.Level);

            Assert.AreEqual(1, g.BattlesFought);
            Assert.AreEqual(1, g.BattlesWon);
            Assert.AreEqual(res.Stars, g.TotalStars);
            Assert.Greater(o.XpGained, 0);
            Assert.AreEqual(xpBefore + o.XpGained, g.TotalXp);
            Assert.AreEqual(woodBefore + res.Loot.Wood, g.Wallet.Get(ResourceType.Wood), "Odun yağması cüzdana eklenmeli");
            Assert.AreEqual(0, g.Wallet.Get(ResourceType.Gold), "Sv10'da altın kilitli: yağmada gelse bile eklenmemeli");
            int returned = BattleResult.Total(res.Returned);
            Assert.AreEqual(returned, g.Inventory.Get(SoldierType.Swordsman) + g.Inventory.Get(SoldierType.Archer));
            Assert.AreEqual(1, g.Quests.ProgressOf("m_BattleWon_1"));
        }

        [Test]
        public void Attack_WithoutSoldiers_IsRefused()
        {
            var g = GameState.NewGame();
            string err;
            Assert.IsNull(g.StartAttack(TargetGenerator.Generate(1, 0, 0).Defense, 1, out err));
            Assert.IsNotNull(err);
        }

        // ---------- Günlük baskın ----------
        [Test]
        public void Raid_OncePerDay_ReportAndLootCap()
        {
            var g = RaidableVillage();
            g.Wallet.SetAmount(ResourceType.Wood, 1000);
            g.Advance(0, 100);
            Assert.IsNotNull(g.LastRaid, "Sv5 + geçerli köy + kule: yeni günde baskın olmalı");
            Assert.AreEqual(100, g.LastRaid.Day);
            Assert.AreEqual(100, g.LastRaidDay);
            Assert.LessOrEqual(g.LastRaid.LootLost.Wood, 1000 * RaidSimulator.MaxLootLossPct / 100);
            Assert.AreEqual(1000 - g.LastRaid.LootLost.Wood, g.Wallet.Get(ResourceType.Wood));
            if (!g.LastRaid.Result.Victory) Assert.AreEqual(0, g.LastRaid.LootLost.Wood, "Savunma tutarsa kayıp olmaz");

            var first = g.LastRaid;
            g.Advance(60, 100);
            Assert.AreSame(first, g.LastRaid, "Aynı gün ikinci baskın olmamalı");
        }

        [Test]
        public void Raid_ClockGoingBack_DoesNotRaidOrRerollDailies()
        {
            var g = RaidableVillage();
            g.Advance(0, 100);
            var first = g.LastRaid;
            g.Advance(0, 101); // test aracı "+1 gün"
            var second = g.LastRaid;
            Assert.AreNotSame(first, second);
            var dailies101 = new List<string>(g.Quests.DailyIds);
            g.Advance(1, 100); // gerçek saat bir gün geride
            Assert.AreSame(second, g.LastRaid, "Saat geri gidince yeni baskın olmamalı");
            Assert.AreEqual(101, g.LastRaidDay);
            Assert.AreEqual(101, g.Quests.DailyDay, "Günlükler eski güne geri sarılmamalı");
            CollectionAssert.AreEqual(dailies101, g.Quests.DailyIds);
        }

        [Test]
        public void Raid_SkippedBelowLevel3_WithoutTower_OrInvalidVillage()
        {
            var low = GameState.NewGame();
            low.TotalXp = XpFor(2);
            low.AddTowerDirect(P(6, 5), TowerType.Archer, 1);
            low.Advance(0, 50);
            Assert.IsNull(low.LastRaid, "Sv2: baskın yok");

            var noTower = GameState.NewGame();
            noTower.TotalXp = XpFor(5);
            noTower.Advance(0, 50);
            Assert.IsNull(noTower.LastRaid, "Kule yok: baskın yok");

            var g = RaidableVillage();
            g.AddXp(ProgressionRules.XpToNextLevel(g.Level)); // yol hakkı arttı → köy geçersiz
            string err;
            Assert.IsFalse(g.VillageValid(out err));
            g.Advance(0, 50);
            Assert.IsNull(g.LastRaid, "Geçersiz savunmaya baskın yapılmaz");
        }

        // ---------- Kayıt / yükleme ----------
        [Test]
        public void SaveLoad_KeepsRaidReportJobsAndResourcesAboveBaseCapacity()
        {
            var g = RaidableVillage();
            g.Advance(0, 200);
            Assert.IsNotNull(g.LastRaid);
            g.Wallet.SetCapacity(ResourceType.Wood, 200000);
            g.Wallet.SetAmount(ResourceType.Wood, 100000);
            string err;
            Assert.IsTrue(g.TryStartUpgrade(JobKind.Building, P(4, 3), SoldierType.Swordsman, out err), err);
            int cap = g.CapacityOf(ResourceType.Wood);
            g.Wallet.SetAmount(ResourceType.Wood, cap); // depo binası sayesinde taban kapasitenin üstünde

            string text = GameSave.Save(g, 5000);
            var h = GameSave.Load(text, ResourceConfig.CreateDefault(), out long ts);
            Assert.IsNotNull(h);
            Assert.AreEqual(5000, ts);
            Assert.AreEqual(cap, h.Wallet.Get(ResourceType.Wood), "Yüklemede kaynak kırpılmamalı");
            Assert.AreEqual(1, h.Jobs.Count);
            Assert.IsNotNull(h.LastRaid, "Baskın raporu kayıttan sonra da görünmeli");
            Assert.AreEqual(g.LastRaid.Day, h.LastRaid.Day);
            Assert.AreEqual(g.LastRaid.Result.Victory, h.LastRaid.Result.Victory);
            Assert.AreEqual(g.LastRaid.Result.OverallDamagePct, h.LastRaid.Result.OverallDamagePct);
            Assert.AreEqual(g.LastRaid.LootLost.Wood, h.LastRaid.LootLost.Wood);
            Assert.AreEqual(g.LastRaid.TowersDamaged, h.LastRaid.TowersDamaged);
            Assert.AreEqual(text, GameSave.Save(h, 5000));
        }

        [Test]
        public void SaveLoad_ThenAdvance_MatchesNoReload()
        {
            var g = RaidableVillage();
            g.AddBuildingDirect(P(2, 2), BuildingType.University, 3);
            g.AddBuildingDirect(P(6, 7), BuildingType.Barracks, 2);
            FillWallet(g, 20000);
            string err;
            Assert.IsTrue(g.TryResearch(ResearchCatalog.MakeId(0, 1, 1), out err), err);
            Assert.IsTrue(g.TryTrain(SoldierType.Swordsman, out err), err);
            Assert.IsTrue(g.TryStartUpgrade(JobKind.Tower, P(6, 5), SoldierType.Swordsman, out err), err);
            g.Advance(5, 300);

            var h = GameSave.Load(GameSave.Save(g, 1), g.Config, out _);
            Assert.IsNotNull(h);
            g.Advance(8 * 3600, 301);
            h.Advance(8 * 3600, 301);
            Assert.AreEqual(GameSave.Save(g, 2), GameSave.Save(h, 2), "Yüklenmiş oyun, hiç kapanmamış oyunla aynı ilerlemeli");
        }

        [Test]
        public void NewGame_SaveLoadTwice_KeepsEntranceAndValidity()
        {
            var g = GameState.NewGame();
            g.Advance(0, 20770);
            string err;
            Assert.IsTrue(g.VillageValid(out err), err);
            StringAssert.Contains("ent 5 0", GameSave.Save(g, 1));
            var h = GameSave.Load(GameSave.Save(g, 1), g.Config, out _);
            h.Advance(1200, 20770);
            var k = GameSave.Load(GameSave.Save(h, 2), g.Config, out _);
            Assert.AreEqual(1, k.Layout.EntranceCount, GameSave.Save(h, 2));
            Assert.IsTrue(k.VillageValid(out err), err);
        }

        [Test]
        public void Load_OldSaveWithoutRaidLine_StillWorks()
        {
            var g = GameState.NewGame();
            string text = GameSave.Save(g, 10);
            StringAssert.DoesNotContain("raid ", text);
            Assert.IsNotNull(GameSave.Load(text, ResourceConfig.CreateDefault(), out _));
        }

        // ---------- Araştırma ----------
        [Test]
        public void Research_StartChargesBlocksSlotThenFinishesAndApplies()
        {
            var g = GameState.NewGame();
            g.TotalXp = XpFor(5);
            g.AddBuildingDirect(P(2, 2), BuildingType.University, 1);
            FillWallet(g, 0);
            g.Wallet.SetAmount(ResourceType.Wood, 50000);
            g.Wallet.SetAmount(ResourceType.Stone, 50000);
            var d = ResearchCatalog.Get(ResearchCatalog.MakeId(0, 1, 1)); // Ekonomi Sv1 sütun 1: üretim
            var cost = g.Research.CostFor(d, g.Config, g.Level);
            int secs = g.Research.SecondsFor(d);
            int perHourBefore = g.PerHour(ResourceType.Wood);

            string err;
            Assert.IsTrue(g.TryResearch(d.Id, out err), err);
            Assert.AreEqual(50000 - cost.Wood, g.Wallet.Get(ResourceType.Wood));
            Assert.AreEqual(50000 - cost.Stone, g.Wallet.Get(ResourceType.Stone));
            Assert.AreEqual(0, cost.Gold, "Sv5'te altın kilitli: maliyette olamaz (onaylı #28)");
            Assert.IsTrue(g.Research.IsActive(d.Id));
            Assert.IsFalse(g.TryResearch(ResearchCatalog.MakeId(1, 1, 1), out err), "Tek yuva dolu");
            StringAssert.Contains("yuva", err);

            g.Advance(secs - 1, 0);
            Assert.IsFalse(g.Research.IsDone(d.Id));
            var ev = g.Advance(1, 0);
            Assert.IsTrue(g.Research.IsDone(d.Id));
            Assert.IsTrue(ev.Exists(e => e.Contains("Araştırma tamamlandı")));
            Assert.AreEqual(1, g.Quests.ProgressOf("m_ResearchDone_1"));
            Assert.Greater(g.PerHour(ResourceType.Wood), perHourBefore, "Üretim bonusu uygulanmalı");
            Assert.IsTrue(g.TryResearch(ResearchCatalog.MakeId(1, 1, 1), out err), "Yuva boşaldı: " + err);
        }

        [Test]
        public void Research_LockedBeforeLevel5_AndWithoutMoney()
        {
            var g = GameState.NewGame();
            g.AddBuildingDirect(P(2, 2), BuildingType.University, 1);
            string err;
            Assert.IsFalse(g.TryResearch(ResearchCatalog.MakeId(0, 1, 1), out err));
            g.TotalXp = XpFor(5);
            FillWallet(g, 0);
            Assert.IsFalse(g.TryResearch(ResearchCatalog.MakeId(0, 1, 1), out err));
            StringAssert.Contains("Yetersiz", err);
            Assert.AreEqual(0, g.Research.Active.Count, "Para yetmezse araştırma başlamamalı");
        }

        // ---------- Seviye atlama ----------
        [Test]
        public void LevelUp_MultipleLevels_InOrderWithUnlocksAndFinalRoadLimit()
        {
            var g = GameState.NewGame();
            var ups = g.AddXp((int)XpFor(6));
            Assert.AreEqual(6, g.Level);
            Assert.AreEqual(5, ups.Count);
            for (int i = 0; i < ups.Count; i++) Assert.AreEqual(i + 2, ups[i].NewLevel);
            Assert.AreEqual(RoadRules.RoadLimitForLevel(6), g.Layout.RoadLimit);
            var l5 = ups.Find(u => u.NewLevel == 5);
            Assert.IsTrue(l5.Unlocks.Contains("Araştırma ağaçları"));
            Assert.IsTrue(l5.Unlocks.Contains("Kaynak: Taş"));
            foreach (var u in ups)
            {
                if (u.NewLevel < 5) Assert.AreEqual(0, u.Reward.Stone, "Taş Sv5'ten önce ödül olamaz");
                Assert.AreEqual(0, u.Reward.Gold, "Altın Sv15'te açılır");
            }
            string err;
            Assert.IsFalse(g.VillageValid(out err), "Yol hakkı arttı: yeni yol konana kadar savunma geçersiz (onaylı #21)");
        }

        [Test]
        public void LevelUp_FromPlacement_GoesToPendingOnce()
        {
            var g = GameState.NewGame();
            g.TotalXp = XpFor(3) - 1; // Sv2, bir sonraki XP ile Sv3
            g.Wallet.SetCapacity(ResourceType.Wood, 200000);
            g.Wallet.SetAmount(ResourceType.Wood, 100000);
            string err;
            Assert.IsTrue(g.TryPlaceTower(P(6, 5), TowerType.Archer, out err), err);
            Assert.AreEqual(1, g.PendingLevelUps.Count);
            Assert.AreEqual(3, g.PendingLevelUps[0].NewLevel);
            g.PendingLevelUps.Clear();
            g.Advance(1, 0);
            Assert.AreEqual(0, g.PendingLevelUps.Count, "Aynı seviye atlama iki kez bildirilmemeli");
        }
    }
}
