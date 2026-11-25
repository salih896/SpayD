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
    /// <summary>Kule elementleri, geliştirme dalları ve eski kule türlerinin dönüşümü (kullanıcı kararı 25.11.2026).</summary>
    public class TowerElementTests
    {
        private static GridPos P(int x, int y) => new GridPos(x, y);

        /// <summary>Giriş (5,0), yol (5,1..6), depo (6,6); kule (4,2).</summary>
        private static DefenseSetup Setup(TowerSpec tower)
        {
            var layout = new VillageLayout(11, 20, 10);
            Assert.IsTrue(layout.ToggleEntrance(P(5, 0), out _));
            for (int y = 1; y <= 6; y++) Assert.IsTrue(layout.TryPlace(P(5, y), CellKind.Road, out _));
            Assert.IsTrue(layout.TryPlace(P(6, 6), CellKind.Building, out _));
            Assert.IsTrue(layout.TryPlace(P(4, 2), CellKind.Tower, out _));
            var s = new DefenseSetup(layout);
            s.Buildings[P(6, 6)] = new BuildingSpec(BuildingType.LumberDepot);
            s.Towers[P(4, 2)] = tower;
            return s;
        }

        private static BattleSim Start(DefenseSetup d, SoldierType t, int n)
        {
            var sim = new BattleSim(d, new Dictionary<SoldierType, int> { { t, n } }, null, 3);
            for (int i = 0; i < n; i++) Assert.IsTrue(sim.Deploy(t, 5, 1, out _));
            return sim;
        }

        private static TowerSpec Archer(TowerElement e) => new TowerSpec(TowerType.Archer) { Element = e };

        [Test]
        public void OnlyArcherAndWall_AreBuildable()
        {
            foreach (TowerType t in System.Enum.GetValues(typeof(TowerType)))
            {
                bool buildable = ContentCatalog.Tower(t).UnlockLevel <= 30;
                Assert.AreEqual(t == TowerType.Archer || t == TowerType.Wall, buildable, t.ToString());
            }
        }

        [Test]
        public void ElementDefs_HaveTheirSignatureEffects()
        {
            var basic = ContentCatalog.TowerFor(TowerType.Archer, TowerElement.None);
            var arrow = ContentCatalog.TowerFor(TowerType.Archer, TowerElement.Arrow);
            Assert.Greater(arrow.Attack, basic.Attack); Assert.Less(arrow.IntervalTicks, basic.IntervalTicks); Assert.Greater(arrow.CritPct, 0);
            var fire = ContentCatalog.TowerFor(TowerType.Archer, TowerElement.Fire);
            Assert.Greater(fire.SplashMilli, 0); Assert.Greater(fire.BurnTicks, 0); Assert.Greater(fire.IntervalTicks, 3 * basic.IntervalTicks, "çok yavaş");
            var ice = ContentCatalog.TowerFor(TowerType.Archer, TowerElement.Ice);
            Assert.Greater(ice.SlowTicks, 0); Assert.Greater(ice.FreezeStackPct, 0);
            var poison = ContentCatalog.TowerFor(TowerType.Archer, TowerElement.Poison);
            Assert.Greater(poison.PoisonTicks, 0); Assert.Greater(poison.WeakenPct, 0);
            Assert.AreEqual(100, ContentCatalog.TowerFor(TowerType.Archer, TowerElement.Magic).BonusVsAirPct);
            Assert.AreEqual(2, ContentCatalog.TowerFor(TowerType.Archer, TowerElement.Lightning).ChainCount);
        }

        [Test]
        public void IceElement_StacksFreezeDamage()
        {
            var sim = Start(Setup(Archer(TowerElement.Ice)), SoldierType.Giant, 1);
            int maxStacks = 0;
            for (int i = 0; i < 80; i++) { sim.Step(); maxStacks = System.Math.Max(maxStacks, sim.Soldiers[0].FreezeStacks); }
            Assert.GreaterOrEqual(maxStacks, 3, "üst üste vuruşla yığın artmalı");
            Assert.Greater(sim.Soldiers[0].SlowUntil, 0);
        }

        [Test]
        public void FireElement_Burns_PoisonElement_PoisonsAndWeakens()
        {
            var fire = Start(Setup(Archer(TowerElement.Fire)), SoldierType.Giant, 1);
            for (int i = 0; i < 30; i++) fire.Step();
            Assert.Greater(fire.Soldiers[0].BurnUntil, 0, "ateş yakar");
            var poison = Start(Setup(Archer(TowerElement.Poison)), SoldierType.Giant, 1);
            for (int i = 0; i < 30; i++) poison.Step();
            Assert.Greater(poison.Soldiers[0].PoisonUntil, 0, "zehirler");
            Assert.AreEqual(30, poison.Soldiers[0].WeakPct, "hasarını düşürür");
        }

        [Test]
        public void SpeedBranch_ShootsMoreOften_WallArmor_ReducesDamage()
        {
            int Shots(TowerSpec t)
            {
                var sim = Start(Setup(t), SoldierType.Giant, 1);
                var tower = sim.Structures[0].Kind == StructKind.Tower ? sim.Structures[0] : sim.Structures[1];
                int shots = 0, prev = tower.Cooldown;
                for (int i = 0; i < 60; i++) { sim.Step(); if (tower.Cooldown > prev) shots++; prev = tower.Cooldown; }
                return shots;
            }
            Assert.Greater(Shots(new TowerSpec(TowerType.Archer) { BranchA = 15 }), Shots(new TowerSpec(TowerType.Archer)));

            int WallLoss(TowerSpec w)
            {
                var sim = Start(Setup(w), SoldierType.Swordsman, 3);
                for (int i = 0; i < 120; i++) sim.Step();
                var wall = sim.Structures[0].Kind == StructKind.Tower ? sim.Structures[0] : sim.Structures[1];
                return wall.MaxHp - wall.Hp;
            }
            Assert.Less(WallLoss(new TowerSpec(TowerType.Wall) { BranchB = 20 }), WallLoss(new TowerSpec(TowerType.Wall)));
        }

        private static GameState Rich(int level)
        {
            var g = GameState.NewGame();
            long xp = 0;
            for (int l = 1; l < level; l++) xp += ProgressionRules.XpToNextLevel(l);
            g.TotalXp = xp;
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType))) { g.Wallet.SetCapacity(t, 100000000); g.Wallet.SetAmount(t, 1000000); }
            return g;
        }

        [Test]
        public void Element_NeedsResearch_ThenAppliesAfterShortJob_AndSurvivesSave()
        {
            var g = Rich(10);
            string err;
            Assert.IsTrue(g.TryPlaceTower(P(4, 1), TowerType.Archer, out err), err);
            Assert.IsFalse(g.TryApplyElement(P(4, 1), TowerElement.Fire, out err));
            StringAssert.Contains("araştırması", err);
            g.Research.ForceComplete(ResearchCatalog.FindSpecial(ResearchStat.TowerAttackElement, (int)TowerElement.Fire).Id);
            Assert.IsTrue(g.TryApplyElement(P(4, 1), TowerElement.Fire, out err), err);
            Assert.AreEqual(TowerElement.None, g.Towers[P(4, 1)].Element, "iş bitmeden değişmez");
            g.Advance(GameRules.ElementApplySeconds + 1, 0);
            Assert.AreEqual(TowerElement.Fire, g.Towers[P(4, 1)].Element);

            Assert.IsTrue(g.TryStartTowerUpgrade(P(4, 1), 2, out err), err);
            g.Advance(100000, 0);
            Assert.AreEqual(2, g.Towers[P(4, 1)].Level);
            Assert.AreEqual(1, g.Towers[P(4, 1)].BranchB);

            var h = GameSave.Load(GameSave.Save(g, 1), ResourceConfig.CreateDefault(), out _);
            Assert.AreEqual(TowerElement.Fire, h.Towers[P(4, 1)].Element);
            Assert.AreEqual(1, h.Towers[P(4, 1)].BranchB);
        }

        [Test]
        public void ElementCycle_WeakAttackerDealsLess_SameDefenseResists_NoBonus()
        {
            Assert.AreEqual(60, CombatMath.ElementPct(TowerElement.Poison, TowerElement.Fire), "Ateş zehre üstün: zehirle saldıran %40 az");
            Assert.AreEqual(60, CombatMath.ElementPct(TowerElement.Ice, TowerElement.Poison));
            Assert.AreEqual(60, CombatMath.ElementPct(TowerElement.Fire, TowerElement.Ice));
            Assert.AreEqual(100, CombatMath.ElementPct(TowerElement.Fire, TowerElement.Poison), "üstün olana ek hasar yok");
            Assert.AreEqual(65, CombatMath.ElementPct(TowerElement.Ice, TowerElement.Ice), "direnç");
            Assert.AreEqual(100, CombatMath.ElementPct(TowerElement.Lightning, TowerElement.Fire), "Şimşek nötr");
            Assert.AreEqual(100, CombatMath.ElementPct(TowerElement.Fire, TowerElement.None));
        }

        [Test]
        public void FireResistantSoldier_TakesLessFromFireTower()
        {
            int Loss(TowerElement soldierDef)
            {
                var sim = new BattleSim(Setup(Archer(TowerElement.Fire)), new Dictionary<SoldierType, int> { { SoldierType.Giant, 1 } }, null, 3);
                sim.AttackerElements[SoldierType.Giant] = (TowerElement.None, soldierDef);
                Assert.IsTrue(sim.Deploy(SoldierType.Giant, 5, 1, out _));
                for (int i = 0; i < 60; i++) sim.Step();
                return sim.Soldiers[0].MaxHp - sim.Soldiers[0].Hp;
            }
            Assert.Less(Loss(TowerElement.Fire), Loss(TowerElement.None));
        }

        [Test]
        public void SoldierElement_NeedsResearch_AndSaves()
        {
            var g = Rich(30);
            string err;
            Assert.IsFalse(g.TrySetSoldierElement(SoldierType.Giant, false, TowerElement.Ice, out err));
            g.Research.ForceComplete(ResearchCatalog.FindSpecial(ResearchStat.SoldierDefenseElement, (int)TowerElement.Ice).Id);
            Assert.IsTrue(g.TrySetSoldierElement(SoldierType.Giant, false, TowerElement.Ice, out err), err);
            var h = GameSave.Load(GameSave.Save(g, 1), ResourceConfig.CreateDefault(), out _);
            Assert.AreEqual(TowerElement.Ice, h.SoldierDefElement(SoldierType.Giant));
        }

        [Test]
        public void OldTowerTypes_LoadAsArcherWithElement()
        {
            var g = Rich(10);
            string err;
            Assert.IsTrue(g.TryPlaceTower(P(4, 1), TowerType.Archer, out err), err);
            string text = GameSave.Save(g, 1).Replace("tower 4 1 0 1 0 0 0", "tower 4 1 4 3"); // eski format: Top Kulesi Sv3
            var h = GameSave.Load(text, ResourceConfig.CreateDefault(), out _);
            Assert.AreEqual(TowerType.Archer, h.Towers[P(4, 1)].Type);
            Assert.AreEqual(TowerElement.Fire, h.Towers[P(4, 1)].Element);
            Assert.AreEqual(3, h.Towers[P(4, 1)].Level);
        }
    }
}
