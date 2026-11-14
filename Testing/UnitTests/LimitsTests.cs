using NUnit.Framework;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Game;
using GameCore.Grid;
using GameCore.Progression;
using GameCore.Village;

namespace GameCore.Tests
{
    /// <summary>Yapı sınırları, Akademi görevleri ve yol geliştirme (kullanıcı kararları 14.11.2026).</summary>
    public class LimitsTests
    {
        private static GridPos P(int x, int y) => new GridPos(x, y);

        private static long XpFor(int level)
        {
            long s = 0;
            for (int l = 1; l < level; l++) s += ProgressionRules.XpToNextLevel(l);
            return s;
        }

        private static GameState Rich(int level)
        {
            var g = GameState.NewGame();
            g.TotalXp = XpFor(level);
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType))) { g.Wallet.SetCapacity(t, 100000000); g.Wallet.SetAmount(t, 1000000); }
            return g;
        }

        [Test]
        public void ProductionAndDepots_AreLimitedToOne()
        {
            var g = Rich(30);
            string err;
            Assert.IsFalse(g.TryPlaceBuilding(P(1, 1), BuildingType.Forest, out err), "Başlangıçta zaten 1 orman var");
            StringAssert.Contains("sınırı 1", err);
            Assert.IsTrue(g.TryPlaceBuilding(P(1, 4), BuildingType.LumberDepot, out err), err);
            Assert.IsFalse(g.TryPlaceBuilding(P(1, 5), BuildingType.LumberDepot, out err));
            Assert.IsTrue(g.TryPlaceBuilding(P(1, 1), BuildingType.StoneQuarry, out err), err);
            Assert.IsFalse(g.TryPlaceBuilding(P(1, 2), BuildingType.StoneQuarry, out err));
            Assert.IsTrue(g.TryPlaceBuilding(P(1, 2), BuildingType.GoldMine, out err), err);
            Assert.IsFalse(g.TryPlaceBuilding(P(1, 3), BuildingType.GoldMine, out err));
        }

        [Test]
        public void NewGame_LumberDepotFromGuide_GrantsLevel2()
        {
            var g = GameState.NewGame();
            Assert.AreEqual(0, g.CountBuildings(BuildingType.LumberDepot), "approved #3: başta yalnızca orman + köylü");
            Assert.AreEqual(1, g.Level);
            string err;
            Assert.IsTrue(g.TryPlaceBuilding(P(6, 3), BuildingType.LumberDepot, out err), err);
            Assert.AreEqual(2, g.Level, "approved #4: kereste deposu Sv2 garanti");
            Assert.AreEqual(1, g.PendingLevelUps.Count);
        }

        [Test]
        public void VillagerHouses_StartAtOne_AcademyTasksRaiseToThree()
        {
            var g = Rich(10);
            string err;
            Assert.AreEqual(1, g.MaxVillagerHouses);
            Assert.IsFalse(g.TryPlaceBuilding(P(1, 1), BuildingType.VillagerHouse, out err));
            StringAssert.Contains("Akademi", err);

            Assert.IsFalse(g.TryClaimAcademy("a_house1", out err), "Akademi yokken alınamaz");
            Assert.IsTrue(g.TryPlaceBuilding(P(2, 2), BuildingType.University, out err), err);
            Assert.IsTrue(g.TryClaimAcademy("a_house1", out err), err);
            Assert.IsFalse(g.TryClaimAcademy("a_house1", out err), "İki kez alınamaz");
            Assert.AreEqual(2, g.MaxVillagerHouses);
            Assert.IsTrue(g.TryPlaceBuilding(P(1, 1), BuildingType.VillagerHouse, out err), err);

            Assert.IsFalse(g.TryClaimAcademy("a_house2", out err), "3 araştırma yapılmadı");
            foreach (var id in new[] { "0-01-1", "0-01-2", "1-01-1" }) g.Research.ForceComplete(id);
            Assert.IsTrue(g.TryClaimAcademy("a_house2", out err), err);
            Assert.AreEqual(3, g.MaxVillagerHouses);
            Assert.IsTrue(g.TryPlaceBuilding(P(1, 2), BuildingType.VillagerHouse, out err), err);
            Assert.IsFalse(g.TryPlaceBuilding(P(1, 3), BuildingType.VillagerHouse, out err), "En çok 3");
        }

        [Test]
        public void Towers_FiveFixed_ThreeAcademyTasksRaiseToEight_NotLevel()
        {
            var g = Rich(30);
            Assert.AreEqual(5, g.MaxTowers, "Seviye 30 olsa da başta 5");
            g.AddBuildingDirect(P(2, 2), BuildingType.University, 3);
            g.BattlesWon = 5;
            for (int i = 0; i < 15; i++) g.Research.ForceComplete(ResearchCatalog.All[i].Id);
            string err;
            foreach (var id in new[] { "a_tower1", "a_tower2", "a_tower3" }) Assert.IsTrue(g.TryClaimAcademy(id, out err), id + ": " + err);
            Assert.AreEqual(8, g.MaxTowers);
            Assert.AreEqual(GameRules.MaxTowerSlots, g.MaxTowers);
        }

        [Test]
        public void TowerPlacement_RespectsSlotLimit()
        {
            var g = Rich(10);
            string err;
            var cells = new[] { P(4, 1), P(6, 1), P(4, 2), P(6, 2), P(4, 4), P(6, 4) };
            for (int i = 0; i < 5; i++) Assert.IsTrue(g.TryPlaceTower(cells[i], TowerType.Archer, out err), err);
            Assert.IsFalse(g.TryPlaceTower(cells[5], TowerType.Archer, out err));
            StringAssert.Contains("Kule sınırı 5", err);
        }

        [Test]
        public void RoadUpgrade_NeedsResearch_ThenTiersGatedByResources()
        {
            var g = Rich(10);
            string err;
            var road = P(5, 3);
            Assert.IsFalse(g.TryUpgradeRoad(road, out err), "Araştırma yok");
            Assert.IsFalse(g.TryStartRoadResearch(out err), "Akademi yok");
            g.AddBuildingDirect(P(2, 2), BuildingType.University, 1);
            int wood = g.Wallet.Get(ResourceType.Wood);
            Assert.IsTrue(g.TryStartRoadResearch(out err), err);
            Assert.AreEqual(wood - RoadRules.ResearchCost.Wood, g.Wallet.Get(ResourceType.Wood));
            g.Advance(RoadRules.ResearchDurationSeconds - 1, 0);
            Assert.IsFalse(g.RoadResearchDone);
            var ev = g.Advance(1, 0);
            Assert.IsTrue(g.RoadResearchDone);
            Assert.IsTrue(ev.Exists(e => e.Contains("Yol geliştirme")));

            // Sv10: Taş basamağı açık (işçi + süre)
            Assert.IsTrue(g.TryUpgradeRoad(road, out err), err);
            Assert.AreEqual(RoadTier.Wood, g.Layout.GetRoadTier(road), "Süre dolmadan değişmez");
            Assert.IsFalse(g.TryUpgradeRoad(road, out err), "Zaten geliştiriliyor");
            g.Advance(RoadRules.UpgradeSeconds(RoadTier.Stone), 0);
            Assert.AreEqual(RoadTier.Stone, g.Layout.GetRoadTier(road));
            Assert.IsFalse(g.TryUpgradeRoad(road, out err), "Altın+Tahta Sv15");
            StringAssert.Contains("Sv 15", err);
            g.TotalXp = XpFor(30);
            foreach (var next in new[] { RoadTier.Reinforced, RoadTier.Iron, RoadTier.Crystal })
            {
                Assert.IsTrue(g.TryUpgradeRoad(road, out err), next + ": " + err);
                g.Advance(RoadRules.UpgradeSeconds(next), 0);
                Assert.AreEqual(next, g.Layout.GetRoadTier(road));
            }
            Assert.IsFalse(g.TryUpgradeRoad(road, out err), "Kristal en üst");
            Assert.IsFalse(g.TryUpgradeRoad(P(0, 10), out err), "Yol değil");
        }

        [Test]
        public void SaveLoad_KeepsAcademyClaimsAndRoadResearch()
        {
            var g = Rich(10);
            g.AddBuildingDirect(P(2, 2), BuildingType.University, 1);
            string err;
            Assert.IsTrue(g.TryClaimAcademy("a_house1", out err), err);
            Assert.IsTrue(g.TryStartRoadResearch(out err), err);
            g.Advance(100, 0);
            string text = GameSave.Save(g, 9);
            var h = GameSave.Load(text, ResourceConfig.CreateDefault(), out _);
            Assert.IsTrue(h.AcademyClaimed.Contains("a_house1"));
            Assert.AreEqual(g.RoadResearchRemaining, h.RoadResearchRemaining);
            Assert.AreEqual(2, h.MaxVillagerHouses);
            Assert.AreEqual(text, GameSave.Save(h, 9));
        }
    }
}
