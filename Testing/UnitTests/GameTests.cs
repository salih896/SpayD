using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Game;
using GameCore.Grid;
using GameCore.Progression;
using GameCore.Village;

namespace GameCore.Tests
{
    public class GameTests
    {
        private static GridPos P(int x, int y) => new GridPos(x, y);

        // ---------- Araştırma ----------
        [Test]
        public void ResearchCatalog_Has474UniqueEntries()
        {
            Assert.AreEqual(474, ResearchCatalog.All.Count);
            var ids = new HashSet<string>();
            foreach (var d in ResearchCatalog.All) Assert.IsTrue(ids.Add(d.Id), d.Id);
        }

        [Test]
        public void Research_RequiresPreviousInSameColumn()
        {
            var g = GameState.NewGame();
            g.TotalXp = 100000; // Sv30
            g.AddBuildingDirect(P(2, 2), BuildingType.University, 30);
            g.Wallet.SetCapacity(ResourceType.Wood, 100000000);
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType))) { g.Wallet.SetCapacity(t, 100000000); g.Wallet.SetAmount(t, 50000000); }
            var l2 = ResearchCatalog.Find(ResearchTree.Economy, 2, 1);
            Assert.IsNotNull(g.Research.LockReason(l2, g.Level, g.UniversityLevel));
            string err;
            Assert.IsTrue(g.TryResearch(ResearchCatalog.MakeId(0, 1, 1), out err), err);
            g.Advance(100000, 1);
            Assert.IsTrue(g.Research.IsDone(ResearchCatalog.MakeId(0, 1, 1)));
            Assert.IsNull(g.Research.LockReason(l2, g.Level, g.UniversityLevel));
        }

        [Test]
        public void Research_NeedsUniversityLevel()
        {
            var g = GameState.NewGame();
            g.TotalXp = 100000;
            g.AddBuildingDirect(P(2, 2), BuildingType.University, 1);
            var d5 = ResearchCatalog.Find(ResearchTree.Economy, 5, 1);
            StringAssert.Contains("Üniversite", g.Research.LockReason(d5, g.Level, g.UniversityLevel));
        }

        [Test]
        public void Research_ScienceCapsHold()
        {
            var r = new ResearchState();
            foreach (var d in ResearchCatalog.All) if (d.Tree == ResearchTree.Science) r.ForceComplete(d.Id);
            Assert.AreEqual(300, r.Total(ResearchStat.ResearchTime));
            Assert.AreEqual(200, r.Total(ResearchStat.ResearchCost));
            Assert.AreEqual(2, r.Slots);
        }

        // ---------- Görev ----------
        [Test]
        public void Quests_ChainProgressAndClaim()
        {
            var q = new QuestState();
            q.Report(QuestEvent.BattleWon, 1, 1);
            string err; QuestDef def;
            Assert.IsTrue(q.TryClaim("m_BattleWon_1", 1, out def, out err), err);
            Assert.IsFalse(q.TryClaim("m_BattleWon_1", 1, out def, out err));
            q.Report(QuestEvent.BattleWon, 2, 1);
            Assert.AreEqual(2, q.ProgressOf("m_BattleWon_2"));
            Assert.IsFalse(q.TryClaim("m_BattleWon_2", 1, out def, out err)); // hedef 3
            q.Report(QuestEvent.BattleWon, 1, 1);
            Assert.IsTrue(q.TryClaim("m_BattleWon_2", 1, out def, out err), err);
        }

        [Test]
        public void Quests_DailyDeterministicAndRolls()
        {
            var a = new QuestState(); var b = new QuestState();
            a.RollDaily(5, 10); b.RollDaily(5, 10);
            Assert.AreEqual(QuestState.DailyCount, a.DailyIds.Count);
            CollectionAssert.AreEqual(a.DailyIds, b.DailyIds);
            Assert.IsFalse(a.RollDaily(5, 10));
            Assert.IsTrue(a.RollDaily(6, 10));
        }

        // ---------- Oyun durumu ----------
        [Test]
        public void NewGame_IsValidVillage()
        {
            var g = GameState.NewGame();
            string err;
            Assert.IsTrue(g.VillageValid(out err), err);
            Assert.AreEqual(1, g.Level);
        }

        [Test]
        public void Advance_ProducesWood_RespectsCapacity()
        {
            var g = GameState.NewGame();
            int before = g.Wallet.Get(ResourceType.Wood);
            g.Advance(3600, 0);
            Assert.Greater(g.Wallet.Get(ResourceType.Wood), before);
            g.Advance(24 * 3600, 0);
            Assert.LessOrEqual(g.Wallet.Get(ResourceType.Wood), g.CapacityOf(ResourceType.Wood));
        }

        [Test]
        public void AddXp_LevelUp_RaisesRoadLimitAndRewards()
        {
            var g = GameState.NewGame();
            int need = ProgressionRules.XpToNextLevel(1);
            var ups = g.AddXp(need);
            Assert.AreEqual(1, ups.Count);
            Assert.AreEqual(2, g.Level);
            Assert.AreEqual(RoadRules.RoadLimitForLevel(2), g.Layout.RoadLimit);
            Assert.Greater(ups[0].Reward.Wood, 0);
        }

        [Test]
        public void PlaceTower_ChargesAndRespectsUnlock()
        {
            var g = GameState.NewGame();
            string err;
            Assert.IsFalse(g.TryPlaceTower(P(4, 5), TowerType.Archer, out err)); // Sv2'de açılır
            g.AddXp(ProgressionRules.XpToNextLevel(1));
            int wood = g.Wallet.Get(ResourceType.Wood);
            Assert.IsTrue(g.TryPlaceTower(P(4, 5), TowerType.Archer, out err), err);
            Assert.Less(g.Wallet.Get(ResourceType.Wood), wood);
        }

        [Test]
        public void Upgrade_NeedsLevelAndBuilder_ThenCompletes()
        {
            var g = GameState.NewGame();
            string err;
            var pos = P(4, 3); // Orman Sv1
            Assert.IsFalse(g.TryStartUpgrade(JobKind.Building, pos, SoldierType.Swordsman, out err)); // oyuncu Sv2 gerekli
            g.AddXp(ProgressionRules.XpToNextLevel(1));
            g.Wallet.SetAmount(ResourceType.Wood, 100000);
            Assert.IsTrue(g.TryStartUpgrade(JobKind.Building, pos, SoldierType.Swordsman, out err), err);
            Assert.IsFalse(g.TryStartUpgrade(JobKind.Building, pos, SoldierType.Swordsman, out err)); // zaten sürüyor
            g.Advance(10000, 0);
            Assert.AreEqual(2, g.Buildings[pos].Level);
        }

        [Test]
        public void Train_NeedsBarracksAndCapacity()
        {
            var g = GameState.NewGame();
            g.TotalXp = 3000;
            string err;
            Assert.IsFalse(g.TryTrain(SoldierType.Swordsman, out err)); // kışla yok
            g.AddBuildingDirect(P(6, 6), BuildingType.Barracks, 1);
            g.Wallet.SetAmount(ResourceType.Wood, 100000);
            for (int i = 0; i < g.ArmyCapacity; i++) Assert.IsTrue(g.TryTrain(SoldierType.Swordsman, out err), err);
            Assert.IsFalse(g.TryTrain(SoldierType.Swordsman, out err));
        }

        // ---------- Hedef üretici ----------
        [Test]
        public void TargetGenerator_AllLevelsProduceValidVillages()
        {
            for (int lvl = 1; lvl <= 30; lvl++)
                for (int diff = 0; diff < 3; diff++)
                    for (int seed = 0; seed < 4; seed++)
                    {
                        var t = TargetGenerator.Generate(lvl, diff, seed);
                        string err;
                        Assert.IsTrue(t.Defense.Layout.Validate(out err), "Sv" + lvl + " zorluk" + diff + " tohum" + seed + ": " + err);
                        Assert.GreaterOrEqual(t.Defense.Towers.Count, 1);
                    }
        }

        [Test]
        public void TargetGenerator_IsDeterministic()
        {
            var a = TargetGenerator.Generate(12, 1, 3);
            var b = TargetGenerator.Generate(12, 1, 3);
            Assert.AreEqual(a.Name, b.Name);
            Assert.AreEqual(a.Defense.Towers.Count, b.Defense.Towers.Count);
            CollectionAssert.AreEqual(a.Defense.Layout.CellsOf(CellKind.Road), b.Defense.Layout.CellsOf(CellKind.Road));
        }

        // ---------- Kayıt ----------
        [Test]
        public void SaveLoad_RoundTrips()
        {
            var g = GameState.NewGame();
            g.TotalXp = 2500;
            g.AddBuildingDirect(P(6, 6), BuildingType.Barracks, 2);
            g.AddTowerDirect(P(4, 5), TowerType.Archer, 3);
            g.Traps[P(5, 4)] = TrapType.Spikes;
            g.Inventory.Add(SoldierType.Archer, 7);
            g.SoldierLevels[SoldierType.Archer] = 2;
            g.Quests.RollDaily(3, g.Level);
            g.Quests.Report(QuestEvent.BattleWon, 1, g.Level);
            g.Research.ForceComplete("0-01-1");
            g.Research.ForceActive("0-01-2", 500);
            string a = GameSave.Save(g, 1234);
            long ts;
            var h = GameSave.Load(a, ResourceConfig.CreateDefault(), out ts);
            Assert.IsNotNull(h);
            Assert.AreEqual(1234, ts);
            Assert.AreEqual(a, GameSave.Save(h, 1234));
        }

        [Test]
        public void SaveLoad_BadInputReturnsNull()
        {
            long ts;
            Assert.IsNull(GameSave.Load("rubbish", ResourceConfig.CreateDefault(), out ts));
            Assert.IsNull(GameSave.Load("", ResourceConfig.CreateDefault(), out ts));
        }

        // ---------- Denge raporu (docs/balance-report.txt) ----------
        [Test]
        public void BalanceReport_WritesFile()
        {
            var sb = new StringBuilder();
            sb.AppendLine("NORMAL hedef, otomatik bot. satır: seviye | kule çarpanı % | ordu doluluk % | asker seviyesi=L-3 | kazanma/8 | ort hasar | ort yıldız | kule sayısı");
            int saved = TargetGenerator.TowerScalePct;
            foreach (int lvl in new[] { 3, 5, 8, 10, 15, 20, 30 })
                foreach (int ts in new[] { 100, 150, 200, 300 })
                    foreach (int fill in new[] { 50, 100 })
                    {
                        TargetGenerator.TowerScalePct = ts;
                        int wins = 0, dmg = 0, stars = 0, towers = 0;
                        for (int seed = 0; seed < 8; seed++)
                        {
                            var tgt = TargetGenerator.Generate(lvl, 1, seed);
                            var stock = IdealArmy(lvl, fill);
                            var levels = new Dictionary<SoldierType, int>();
                            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++) levels[(SoldierType)i] = System.Math.Max(1, lvl - 3);
                            var sim = BotBattle.Run(tgt.Defense, stock, levels, null, seed + 1);
                            var res = sim.Result(tgt.Pool, ResourceConfig.CreateDefault(), tgt.Level);
                            if (res.Victory) wins++;
                            dmg += res.OverallDamagePct; stars += res.Stars; towers += tgt.Defense.Towers.Count;
                        }
                        sb.AppendLine(lvl + " | " + ts + " | " + fill + " | " + wins + "/8 | " + dmg / 8 + " | " + (stars / 8.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " | " + towers / 8);
                    }
            TargetGenerator.TowerScalePct = saved;
            string path = Path.Combine(Directory.GetCurrentDirectory(), "..", "docs", "balance-report.txt");
            try { File.WriteAllText(path, sb.ToString()); } catch { /* yazılamazsa sessiz geç */ }
            Assert.Pass(sb.ToString());
        }

        private static Dictionary<SoldierType, int> IdealArmy(int lvl, int fillPct = 100)
        {
            int cap = GameRules.ArmyCapacity(lvl) * fillPct / 100;
            var unlocked = new List<SoldierType>();
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
                if (ContentCatalog.Soldier((SoldierType)i).UnlockLevel <= lvl) unlocked.Add((SoldierType)i);
            if (unlocked.Count == 0) unlocked.Add(SoldierType.Swordsman);
            var stock = new Dictionary<SoldierType, int>();
            for (int i = 0; i < cap; i++)
            {
                var t = unlocked[i % unlocked.Count];
                stock[t] = (stock.TryGetValue(t, out var n) ? n : 0) + 1;
            }
            return stock;
        }
    }
}
