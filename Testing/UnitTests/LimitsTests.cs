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
            var g = Rich(12); // 2. kopyalar Ekonomi araştırmasıyla
            string err;
            Assert.IsFalse(g.TryPlaceBuilding(P(6, 1), BuildingType.Forest, out err), "Başlangıçta zaten 1 orman var");
            StringAssert.Contains("sınırı 1", err);
            Assert.IsTrue(g.TryPlaceBuilding(P(6, 4), BuildingType.LumberDepot, out err), err);
            Assert.IsFalse(g.TryPlaceBuilding(P(6, 5), BuildingType.LumberDepot, out err));
            Assert.IsTrue(g.TryPlaceBuilding(P(6, 1), BuildingType.StoneQuarry, out err), err);
            Assert.IsFalse(g.TryPlaceBuilding(P(6, 2), BuildingType.StoneQuarry, out err));
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

        private static readonly string House1 = ResearchCatalog.MakeId(4, 1, 2), House2 = ResearchCatalog.MakeId(4, 3, 2),
            Barracks2 = ResearchCatalog.MakeId(4, 8, 2), Academy2 = ResearchCatalog.MakeId(4, 12, 2), Barracks3 = ResearchCatalog.MakeId(4, 18, 2);
        private static readonly string[] TowerR = { ResearchCatalog.MakeId(1, 3, 2), ResearchCatalog.MakeId(1, 6, 2), ResearchCatalog.MakeId(1, 10, 2), ResearchCatalog.MakeId(1, 15, 2), ResearchCatalog.MakeId(1, 20, 2) };
        private static string Tower1 => TowerR[0];

        [Test]
        public void SlotResearch_AreVillageColumn4_Chained()
        {
            Assert.AreEqual(ResearchStat.VillagerHouseSlot, ResearchCatalog.Get(House1).Stat);
            Assert.AreEqual(ResearchStat.VillagerHouseSlot, ResearchCatalog.Get(House2).Stat);
            foreach (var id in TowerR) Assert.AreEqual(ResearchStat.TowerSlot, ResearchCatalog.Get(id).Stat, id);
            Assert.AreEqual(ResearchStat.BarracksSlot, ResearchCatalog.Get(Barracks2).Stat);
            Assert.AreEqual(ResearchStat.BarracksSlot, ResearchCatalog.Get(Barracks3).Stat);
            Assert.AreEqual(ResearchStat.AcademySlot, ResearchCatalog.Get(Academy2).Stat);
            var r = new ResearchState();
            Assert.IsNull(r.LockReason(ResearchCatalog.Get(House1), 30, 30));
            StringAssert.Contains("Köylü Evi I", r.LockReason(ResearchCatalog.Get(House2), 30, 30));
            r.ForceComplete(House1);
            Assert.IsNull(r.LockReason(ResearchCatalog.Get(House2), 30, 30));
            Assert.IsNull(r.LockReason(ResearchCatalog.Get(Barracks2), 30, 30), "Kışla II başka aileye bağlı değil");
            StringAssert.Contains("Kışla II", r.LockReason(ResearchCatalog.Get(Barracks3), 30, 30));
            Assert.IsNull(r.LockReason(ResearchCatalog.Get(Tower1), 30, 30));
            StringAssert.Contains("Kule Hakkı I", r.LockReason(ResearchCatalog.Get(TowerR[1]), 30, 30));
        }

        [Test]
        public void VillagerHouses_StartAtOne_ResearchRaisesToThree()
        {
            var g = Rich(10);
            string err;
            Assert.AreEqual(1, g.MaxVillagerHouses);
            Assert.IsFalse(g.TryPlaceBuilding(P(6, 1), BuildingType.VillagerHouse, out err));
            StringAssert.Contains("Akademi", err);
            g.Research.ForceComplete(House1);
            Assert.AreEqual(2, g.MaxVillagerHouses);
            Assert.IsTrue(g.TryPlaceBuilding(P(6, 1), BuildingType.VillagerHouse, out err), err);
            g.Research.ForceComplete(House2);
            Assert.AreEqual(3, g.MaxVillagerHouses);
            Assert.IsTrue(g.TryPlaceBuilding(P(6, 3), BuildingType.VillagerHouse, out err), err);
            Assert.IsFalse(g.TryPlaceBuilding(P(6, 4), BuildingType.VillagerHouse, out err), "En çok 3");
        }

        [Test]
        public void Towers_StartAtThree_GrowWithLevel_ResearchRaisesToEight()
        {
            Assert.AreEqual(3, Rich(1).MaxTowers, "Başta 3");
            var g = Rich(30);
            Assert.AreEqual(3, g.MaxTowers, "seviye kule hakkı vermez");
            for (int i = 0; i < TowerR.Length; i++) { g.Research.ForceComplete(TowerR[i]); Assert.AreEqual(4 + i, g.MaxTowers); }
            Assert.AreEqual(8, g.MaxTowers);
            Assert.AreEqual(GameRules.MaxTowerSlots, g.MaxTowers);
        }

        [Test]
        public void TowerPlacement_RespectsSlotLimit()
        {
            var g = Rich(10);
            string err;
            var cells = new[] { P(4, 1), P(6, 1), P(4, 2), P(6, 2), P(4, 4), P(6, 4) };
            for (int i = 0; i < 3; i++) Assert.IsTrue(g.TryPlaceTower(cells[i], TowerType.Archer, out err), err);
            Assert.IsFalse(g.TryPlaceTower(cells[3], TowerType.Archer, out err));
            StringAssert.Contains("Kule sınırı 3", err);
        }

        [Test]
        public void Barracks_FirstAtLevel3_SecondAndThirdByResearch_AcademySecondByResearch()
        {
            string err;
            var low = Rich(2);
            Assert.IsFalse(low.TryPlaceBuilding(P(6, 1), BuildingType.Barracks, out err), "Kışla Sv3'te");
            var g = Rich(30);
            Assert.AreEqual(1, g.MaxCountOf(BuildingType.Barracks), "seviye kışla hakkı vermez");
            Assert.IsTrue(g.TryPlaceBuilding(P(6, 1), BuildingType.Barracks, out err), err);
            Assert.IsFalse(g.TryPlaceBuilding(P(6, 2), BuildingType.Barracks, out err));
            StringAssert.Contains("Kışla II", err);
            g.Research.ForceComplete(Barracks2);
            Assert.IsTrue(g.TryPlaceBuilding(P(6, 2), BuildingType.Barracks, out err), err);
            g.Research.ForceComplete(Barracks3);
            Assert.AreEqual(3, g.MaxCountOf(BuildingType.Barracks));
            Assert.AreEqual(1, g.MaxCountOf(BuildingType.University));
            g.Research.ForceComplete(Academy2);
            Assert.AreEqual(2, g.MaxCountOf(BuildingType.University));
        }

        [Test]
        public void ResourceBuildings_SecondCopyByEconomyResearch_MaxTwo()
        {
            var g = Rich(30);
            Assert.AreEqual(1, g.MaxCountOf(BuildingType.Forest), "seviye 2. kopya vermez");
            g.Research.ForceComplete(ResearchCatalog.FindSpecial(ResearchStat.WoodSlot, 1).Id);
            Assert.AreEqual(2, g.MaxCountOf(BuildingType.Forest));
            Assert.AreEqual(2, g.MaxCountOf(BuildingType.LumberDepot));
            Assert.AreEqual(1, g.MaxCountOf(BuildingType.StoneQuarry));
            foreach (var st in new[] { ResearchStat.StoneSlot, ResearchStat.GoldSlot, ResearchStat.IronSlot }) g.Research.ForceComplete(ResearchCatalog.FindSpecial(st, 1).Id);
            Assert.AreEqual(2, g.MaxCountOf(BuildingType.IronDepot));
            Assert.AreEqual(2, g.MaxCountOf(BuildingType.Bank));
        }

        [Test]
        public void RoadResearch_AddsEightRoads()
        {
            var g = Rich(10);
            int before = g.Layout.RoadLimit;
            foreach (var d in ResearchCatalog.All) if (d.Stat == ResearchStat.RoadSlot) g.Research.ForceActive(d.Id, 1);
            g.Advance(5, 0); // araştırmalar biter → yol hakkı eklenir
            Assert.AreEqual(before + 8, g.Layout.RoadLimit);
        }

        [Test]
        public void Buildings_MustTouchRoad()
        {
            var g = Rich(10);
            string err;
            Assert.IsFalse(g.TryPlaceBuilding(P(1, 1), BuildingType.LumberDepot, out err), "Yola bağlı değil");
            StringAssert.Contains("yola bağlı", err);
            Assert.IsTrue(g.TryPlaceBuilding(P(6, 6), BuildingType.LumberDepot, out err), err);
        }

        [Test]
        public void Housing_StrongerSoldiersTakeMoreSpace()
        {
            Assert.AreEqual(1, GameRules.Housing(SoldierType.Swordsman));
            Assert.AreEqual(2, GameRules.Housing(SoldierType.Spearman));
            Assert.AreEqual(3, GameRules.Housing(SoldierType.Cavalry));
            Assert.AreEqual(5, GameRules.Housing(SoldierType.Giant));
            Assert.AreEqual(6, GameRules.Housing(SoldierType.Catapult));
            Assert.AreEqual(10, GameRules.Housing(SoldierType.DragonRider));
            var g = Rich(30);
            g.AddBuildingDirect(P(6, 1), BuildingType.Barracks, 1); // kapasite 24
            string err;
            Assert.AreEqual(24, g.ArmyCapacity);
            Assert.IsTrue(g.TryTrain(SoldierType.DragonRider, out err), err);
            Assert.IsTrue(g.TryTrain(SoldierType.DragonRider, out err), err);
            Assert.AreEqual(20, g.ArmySize, "2 ejderha = 20 yer");
            Assert.IsFalse(g.TryTrain(SoldierType.Giant, out err), "5 yer kalmadı (4 boş)");
            Assert.IsTrue(g.TryTrain(SoldierType.Spearman, out err), err);
            Assert.IsTrue(g.TryTrain(SoldierType.Spearman, out err), err);
            Assert.IsFalse(g.TryTrain(SoldierType.Swordsman, out err), "Dolu");
        }

        [Test]
        public void RoadUpgrade_NeedsResearch_ThenTiersGatedByResources()
        {
            var g = Rich(10);
            string err;
            var road = P(5, 3);
            Assert.IsFalse(g.TryUpgradeRoad(road, out err), "Araştırma yok");
            StringAssert.Contains("Yol Geliştirme 1", err);
            foreach (var d in ResearchCatalog.All) if (d.Stat == ResearchStat.RoadTier && d.Permille <= 1) g.Research.ForceComplete(d.Id); // yalnızca 1. basamak (Taş)

            // Sv10: Taş basamağı açık (işçi + süre)
            Assert.IsTrue(g.TryUpgradeRoad(road, out err), err);
            Assert.AreEqual(RoadTier.Wood, g.Layout.GetRoadTier(road), "Süre dolmadan değişmez");
            Assert.IsFalse(g.TryUpgradeRoad(road, out err), "Zaten geliştiriliyor");
            g.Advance(RoadRules.UpgradeSeconds(RoadTier.Stone), 0);
            Assert.AreEqual(RoadTier.Stone, g.Layout.GetRoadTier(road));
            Assert.IsFalse(g.TryUpgradeRoad(road, out err), "Altın+Tahta Sv15");
            StringAssert.Contains("Sv 15", err);
            g.TotalXp = XpFor(30);
            Assert.IsFalse(g.TryUpgradeRoad(road, out err), "Sv30 olsa da 2. basamak araştırması yok");
            StringAssert.Contains("Yol Geliştirme 2", err);
            foreach (var d in ResearchCatalog.All) if (d.Stat == ResearchStat.RoadTier && d.Permille <= 4) g.Research.ForceComplete(d.Id);
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
        public void SaveLoad_KeepsSlotResearchAndRoadResearch_MigratesOldAcademyClaims()
        {
            var g = Rich(10);
            g.AddBuildingDirect(P(2, 2), BuildingType.University, 1);
            string err;
            g.Research.ForceComplete(House1);
            foreach (var d in ResearchCatalog.All) if (d.Stat == ResearchStat.RoadTier && d.Permille <= 1) g.Research.ForceComplete(d.Id);
            g.Advance(100, 0);
            string text = GameSave.Save(g, 9);
            var h = GameSave.Load(text, ResourceConfig.CreateDefault(), out _);
            Assert.IsTrue(h.Research.IsDone(House1));
            Assert.IsTrue(h.RoadTierResearched(RoadTier.Stone));
            Assert.IsFalse(h.RoadTierResearched(RoadTier.Reinforced));
            // 27.11.2026 öncesi kayıt: tek yol/tuzak araştırması (yapılmış ya da süren) → ağaçtaki tüm basamaklar / tuzak hakları
            var mig = GameSave.Load(text + "rr 1 -1\ntrr 0 500\n", ResourceConfig.CreateDefault(), out _);
            Assert.IsTrue(mig.RoadTierResearched(RoadTier.Crystal));
            Assert.AreEqual(6, mig.MaxTraps);
            Assert.AreEqual(2, h.MaxVillagerHouses);
            Assert.AreEqual(text, GameSave.Save(h, 9));

            // eski kayıt: alınmış Akademi görevleri aynı hakkı veren araştırmaya çevrilir
            var old = GameSave.Load(text + "aq a_house2\naq a_tower1\n", ResourceConfig.CreateDefault(), out _);
            Assert.IsTrue(old.Research.IsDone(House2));
            Assert.IsTrue(old.Research.IsDone(TowerR[1]), "a_tower1 → Kule Hakkı II (Sv6)");
            // eski katalog (rv 1) kimliği: 4-15-4 → Savunma Kule Hakkı IV (güncel katalogda 4-15-4 artık Yol Geliştirme II)
            var v1 = GameSave.Load(text.Replace("rv 2", "rv 1") + "rdone 4-15-4\n", ResourceConfig.CreateDefault(), out _);
            Assert.IsTrue(v1.Research.IsDone(TowerR[3]), "eski 4-15-4 → Savunma Kule Hakkı IV");
            Assert.AreEqual(3, old.MaxVillagerHouses);
        }
    }
}
