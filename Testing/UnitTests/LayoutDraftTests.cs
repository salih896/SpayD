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
    /// <summary>Köy Düzeni taslağı: sürükle-bırak, onayda kural denetimi ve köye aktarma (kullanıcı kararı 15.11.2026).</summary>
    public class LayoutDraftTests
    {
        private static GridPos P(int x, int y) => new GridPos(x, y);

        private static GameState Rich(int level)
        {
            var g = GameState.NewGame();
            long xp = 0;
            for (int l = 1; l < level; l++) xp += ProgressionRules.XpToNextLevel(l);
            g.TotalXp = xp;
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType))) { g.Wallet.SetCapacity(t, 100000000); g.Wallet.SetAmount(t, 1000000); }
            return g;
        }

        private static void Move(LayoutDraft d, GridPos from, GridPos to)
        {
            string err;
            var it = d.PickUp(from, out err);
            Assert.IsNotNull(it, err);
            Assert.IsTrue(d.Place(it, to, out err), err);
        }

        /// <summary>Yeni yol haklarını ana yolun sağına (6,y) dizer ki "tüm yol haklarını kullan" kuralı geçsin.</summary>
        private static void FillRoads(LayoutDraft d)
        {
            string err;
            int y = 1;
            while (true)
            {
                DraftItem road = null;
                foreach (var i in d.Inventory) if (i.Kind == DraftKind.Road) { road = i; break; }
                if (road == null) break;
                while (d.At(P(6, y)) != null) y++;
                Assert.IsTrue(d.Place(road, P(6, y), out err), err);
            }
        }

        [Test]
        public void MoveBuilding_KeepsLevelAndUpgradeJob()
        {
            var g = Rich(10);
            string err;
            Assert.IsTrue(g.TryStartUpgrade(JobKind.Building, P(4, 3), SoldierType.Swordsman, out err), err); // orman Sv1→2
            var d = new LayoutDraft(g);
            FillRoads(d);
            Move(d, P(4, 3), P(4, 8));
            Assert.AreEqual(BuildingType.Forest, g.Buildings[P(4, 3)].Type, "Onaydan önce köy değişmez");
            Assert.IsTrue(g.TryApplyLayout(d, out err), err);
            Assert.IsFalse(g.Buildings.ContainsKey(P(4, 3)));
            Assert.AreEqual(BuildingType.Forest, g.Buildings[P(4, 8)].Type);
            Assert.IsTrue(g.IsUpgrading(JobKind.Building, P(4, 8), SoldierType.Swordsman), "Yükseltme işi yapıyla taşınır");
            g.Advance(3600, 0);
            Assert.AreEqual(2, g.Buildings[P(4, 8)].Level);
        }

        [Test]
        public void DisconnectedRoad_IsRejected_VillageUnchanged()
        {
            var g = Rich(10);
            var d = new LayoutDraft(g);
            FillRoads(d);
            Move(d, P(5, 9), P(9, 9));
            string err;
            Assert.IsFalse(g.TryApplyLayout(d, out err));
            StringAssert.Contains("Yollar birbirine bağlı değil", err);
            Assert.AreEqual(CellKind.Road, g.Layout.Get(P(5, 9)), "Reddedilince köy aynen kalır");
        }

        [Test]
        public void BuildingNotTouchingRoad_IsRejected()
        {
            var g = Rich(10);
            var d = new LayoutDraft(g);
            FillRoads(d);
            Move(d, P(4, 6), P(1, 6));
            string err;
            Assert.IsFalse(g.TryApplyLayout(d, out err));
            StringAssert.Contains("yola bağlı değil", err);
        }

        [Test]
        public void TowerNotTouchingRoad_IsRejected()
        {
            var g = Rich(10);
            var d = new LayoutDraft(g);
            FillRoads(d);
            string err;
            var t = d.AddNew(DraftKind.Tower, (int)TowerType.Archer, out err);
            Assert.IsNotNull(t, err);
            Assert.IsTrue(d.Place(t, P(1, 1), out err), "Taslakta serbest: " + err);
            Assert.IsFalse(g.TryApplyLayout(d, out err));
            StringAssert.Contains("yola bağlı değil", err);
        }

        [Test]
        public void NewPurchase_PaidOnConfirm_LumberDepotGrantsLevel2()
        {
            var g = GameState.NewGame();
            var d = new LayoutDraft(g);
            string err;
            var it = d.AddNew(DraftKind.Building, (int)BuildingType.LumberDepot, out err);
            Assert.IsNotNull(it, err);
            int wood = g.Wallet.Get(ResourceType.Wood);
            Assert.IsTrue(d.Place(it, P(6, 3), out err), err);
            Assert.AreEqual(wood, g.Wallet.Get(ResourceType.Wood), "Bedel onayda ödenir");
            Assert.IsTrue(g.TryApplyLayout(d, out err), err);
            Assert.AreEqual(BuildingType.LumberDepot, g.Buildings[P(6, 3)].Type);
            Assert.AreEqual(2, g.Level, "approved #4");
            Assert.Less(g.Wallet.Get(ResourceType.Wood), wood + 1000, "Bedel ödendi (seviye ödülü eklense de)");
        }

        [Test]
        public void ItemsLeftInInventory_AreRejected()
        {
            var g = Rich(10);
            var d = new LayoutDraft(g);
            FillRoads(d);
            string err;
            Assert.IsNotNull(d.PickUp(P(4, 6), out err), err);
            Assert.IsFalse(g.TryApplyLayout(d, out err));
            StringAssert.Contains("Envanterde", err);
        }

        [Test]
        public void Limits_AreEnforcedInDraft()
        {
            var g = Rich(10);
            var d = new LayoutDraft(g);
            string err;
            Assert.IsNull(d.AddNew(DraftKind.Building, (int)BuildingType.Forest, out err), "Orman sınırı 1");
            Assert.IsNotNull(d.AddNew(DraftKind.Building, (int)BuildingType.Barracks, out err), err);
            Assert.IsNull(d.AddNew(DraftKind.Building, (int)BuildingType.Barracks, out err), "Sv10'da 1 kışla");
            StringAssert.Contains("Sv13", err);
            for (int i = 0; i < 4; i++) Assert.IsNotNull(d.AddNew(DraftKind.Tower, (int)TowerType.Archer, out err), err);
            Assert.IsNull(d.AddNew(DraftKind.Tower, (int)TowerType.Archer, out err), "Sv10'da 4 kule");
            Assert.IsNull(d.AddNew(DraftKind.Building, (int)BuildingType.GoldMine, out err), "Altın Sv15");
        }

        [Test]
        public void NewRoadRights_AppearInInventory_TrapsMoveWithDraft()
        {
            var g = Rich(10);
            g.Layout = g.Layout.WithLimits(RoadRules.RoadLimitForLevel(10), RoadRules.MaxEntrances); // Rich XP'yi doğrudan yazar; yol hakkını elle güncelle
            var d = new LayoutDraft(g);
            int extra = g.Layout.RoadLimit - g.Layout.RoadCount;
            Assert.Greater(extra, 0, "Sv10'da yeni yol hakları var");
            int inv = 0; foreach (var i in d.Inventory) if (i.Kind == DraftKind.Road) inv++;
            Assert.AreEqual(extra, inv);
            FillRoads(d);
            string err;
            var trap = d.AddNew(DraftKind.Trap, (int)TrapType.Spikes, out err);
            Assert.IsNotNull(trap, err);
            Assert.IsFalse(d.Place(trap, P(1, 1), out err), "Tuzak yalnızca yola");
            Assert.IsTrue(d.Place(trap, P(5, 4), out err), err);
            Assert.IsTrue(g.TryApplyLayout(d, out err), err);
            Assert.AreEqual(TrapType.Spikes, g.Traps[P(5, 4)]);
            Assert.AreEqual(g.Layout.RoadLimit, g.Layout.RoadCount);
        }

        [Test]
        public void Sell_RefundsHalfOnConfirm()
        {
            var g = Rich(10);
            var d = new LayoutDraft(g);
            FillRoads(d);
            string err;
            int wood = g.Wallet.Get(ResourceType.Wood);
            Assert.IsTrue(d.Sell(d.At(P(4, 6)), out err), err);
            Assert.IsFalse(d.Sell(d.At(P(5, 3)), out err), "Yol satılamaz");
            Assert.IsTrue(g.TryApplyLayout(d, out err), err);
            Assert.AreEqual(0, g.CountBuildings(BuildingType.VillagerHouse));
            Assert.Greater(g.Wallet.Get(ResourceType.Wood), wood);
        }

        [Test]
        public void PickUpAll_EmptiesVillage_KeepsLockedRoad_AndCanRebuild()
        {
            var g = Rich(10);
            g.RoadResearchDone = true;
            string err;
            Assert.IsTrue(g.TryUpgradeRoad(P(5, 3), out err), err);
            var d = new LayoutDraft(g);
            int inv0 = d.Inventory.Count;
            int before = inv0 + d.Cells.Count + d.Traps.Count;
            int n = d.PickUpAll();
            Assert.AreEqual(before - inv0 - 1, n, "Geliştirilen yol dışında her şey alındı");
            Assert.AreEqual(1, d.Cells.Count, "Yalnızca geliştirilen yol yerinde kalır");
            Assert.IsTrue(d.Cells.ContainsKey(P(5, 3)));
            Assert.AreEqual(0, d.Entrances.Count, "Girişler kalkar");
            Assert.AreEqual(before - 1, d.Inventory.Count);
            Assert.IsFalse(g.TryApplyLayout(d, out err), "Boş köy onaylanmaz");
            Assert.AreEqual(CellKind.Road, g.Layout.Get(P(5, 1)), "Köy değişmedi");
        }

        private static DefenseSetup TargetVillage()
        {
            var layout = new VillageLayout(11, 20, 10);
            layout.ToggleEntrance(P(5, 0), out _);
            for (int y = 1; y <= 8; y++) layout.TryPlace(P(5, y), CellKind.Road, out _);
            layout.TryPlace(P(4, 1), CellKind.Building, out _);   // en yakın: Akademi (depo değil, kule değil)
            layout.TryPlace(P(6, 5), CellKind.Tower, out _);      // orta: devre dışı okçu kulesi
            layout.TryPlace(P(6, 8), CellKind.Building, out _);   // en uzak: kereste deposu
            var d = new DefenseSetup(layout);
            d.Buildings[P(4, 1)] = new BuildingSpec(BuildingType.University);
            d.Towers[P(6, 5)] = new TowerSpec(TowerType.Archer, 1, 70);
            d.Buildings[P(6, 8)] = new BuildingSpec(BuildingType.LumberDepot);
            return d;
        }

        private static GridPos FirstTarget(SoldierType t)
        {
            var sim = new BattleSim(TargetVillage(), new Dictionary<SoldierType, int> { { t, 1 } }, new Dictionary<SoldierType, int> { { t, 1 } }, 3);
            sim.Deploy(t, 5, 1, out _);
            for (int i = 0; i < 5; i++) sim.Step();
            var s = sim.Soldiers[0];
            Assert.GreaterOrEqual(s.TargetId, 0, t.ToString());
            return sim.Structures[s.TargetId].Pos;
        }

        /// <summary>Kullanıcının haritası (15.11.2026 kaydı): yol girişten sola kıvrılıp dolaşıyor; köylü evi ve kışla yolun başında, kuleler kuş uçuşu yakın ama yolun sonunda.</summary>
        [Test]
        public void Swordsman_FollowsRoadOrder_HouseThenBarracks_BeforeTowers()
        {
            var L = new VillageLayout(11, 11, 10);
            L.ToggleEntrance(P(5, 0), out _);
            foreach (var r in new[] { P(5, 1), P(4, 1), P(4, 2), P(7, 2), P(4, 3), P(7, 3), P(4, 4), P(5, 4), P(6, 4), P(7, 4) }) L.TryPlace(r, CellKind.Road, out _);
            var d = new DefenseSetup(L);
            foreach (var t in new[] { P(6, 3), P(6, 2), P(7, 1) }) { L.TryPlace(t, CellKind.Tower, out _, RoadTier.Wood, false); d.Towers[t] = new TowerSpec(TowerType.Archer, 1, 70); } // devre dışı: asker ölmesin
            L.TryPlace(P(5, 2), CellKind.Building, out _); d.Buildings[P(5, 2)] = new BuildingSpec(BuildingType.VillagerHouse);
            L.TryPlace(P(5, 3), CellKind.Building, out _); d.Buildings[P(5, 3)] = new BuildingSpec(BuildingType.Barracks);
            L.TryPlace(P(8, 2), CellKind.Building, out _); d.Buildings[P(8, 2)] = new BuildingSpec(BuildingType.Forest);
            L.TryPlace(P(8, 1), CellKind.Building, out _); d.Buildings[P(8, 1)] = new BuildingSpec(BuildingType.LumberDepot);
            var sim = new BattleSim(d, new Dictionary<SoldierType, int> { { SoldierType.Swordsman, 1 } }, new Dictionary<SoldierType, int> { { SoldierType.Swordsman, 10 } }, 5);
            sim.Deploy(SoldierType.Swordsman, 5, 1, out _);
            var order = new List<GridPos>();
            for (int i = 0; i < 3000 && !sim.Finished && order.Count < 3; i++)
            {
                sim.Step();
                int id = sim.Soldiers[0].TargetId;
                if (id >= 0 && (order.Count == 0 || !order[order.Count - 1].Equals(sim.Structures[id].Pos))) order.Add(sim.Structures[id].Pos);
            }
            Assert.GreaterOrEqual(order.Count, 2, "En az iki hedef");
            Assert.AreEqual(P(5, 2), order[0], "Önce yolun başındaki köylü evi");
            Assert.AreEqual(P(5, 3), order[1], "Sonra kışla");
        }

        [Test]
        public void Diagonal_DoesNotConnect_MeleeAndArcherCannotHitFromDiagonal_CatapultCan()
        {
            var L = new VillageLayout(11, 20, 10);
            L.ToggleEntrance(P(5, 0), out _);
            for (int y = 1; y <= 4; y++) L.TryPlace(P(5, y), CellKind.Road, out _);
            Assert.IsFalse(L.HasAdjacentRoad(P(6, 5)), "Çapraz yola bağlı sayılmaz");
            Assert.IsTrue(L.HasAdjacentRoad(P(6, 4)), "Yan kare bağlı");
            L.TryPlace(P(6, 5), CellKind.Building, out _);
            var d = new DefenseSetup(L);
            d.Buildings[P(6, 5)] = new BuildingSpec(BuildingType.University);
            foreach (var t in new[] { SoldierType.Swordsman, SoldierType.Archer, SoldierType.Catapult })
            {
                var sim = new BattleSim(d, new Dictionary<SoldierType, int> { { t, 1 } }, null, 2);
                sim.Deploy(t, 5, 1, out _);
                for (int i = 0; i < 300; i++) sim.Step();
                if (t == SoldierType.Catapult) Assert.Less(sim.Structures[0].Hp, sim.Structures[0].MaxHp, "Mancınık (2 kare) çaprazdan vurur");
                else Assert.AreEqual(sim.Structures[0].MaxHp, sim.Structures[0].Hp, t + " çaprazdan vuramaz (Kılıçlı yakın dövüş, Okçu 1 kare)");
            }
        }

        [Test]
        public void Targeting_NearestForMost_GoblinDepotFirst_CavalryTowerFirst()
        {
            foreach (var t in new[] { SoldierType.Swordsman, SoldierType.Archer, SoldierType.Giant, SoldierType.Spearman, SoldierType.Fairy, SoldierType.Catapult, SoldierType.DragonRider, SoldierType.Mage })
                Assert.AreEqual(P(4, 1), FirstTarget(t), t + " en yakın yapıya saldırır");
            Assert.AreEqual(P(6, 8), FirstTarget(SoldierType.Goblin), "Goblin önce kaynak deposuna");
            Assert.AreEqual(P(6, 5), FirstTarget(SoldierType.Cavalry), "Atlı önce kuleye");
        }
    }
}
