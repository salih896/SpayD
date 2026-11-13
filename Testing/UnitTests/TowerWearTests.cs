using NUnit.Framework;
using GameCore.Combat;
using GameCore.Economy;

namespace GameCore.Tests
{
    public class TowerWearTests
    {
        [Test]
        public void FiveBattles_FollowLimits_AndReach70()
        {
            var w = new TowerWear();
            Assert.AreEqual(30, w.ApplyBattle(100));
            Assert.AreEqual(20, w.ApplyBattle(100));
            Assert.AreEqual(10, w.ApplyBattle(100));
            Assert.AreEqual(5, w.ApplyBattle(100));
            Assert.AreEqual(5, w.ApplyBattle(100));
            Assert.AreEqual(70, w.PermanentDamagePct);
            Assert.IsTrue(w.IsDisabled);
            Assert.AreEqual(0, w.ApplyBattle(100));          // tavan
            Assert.AreEqual(70, w.PermanentDamagePct);
        }

        [Test]
        public void SmallDamage_IsKeptAsIs_UnderLimit()
        {
            var w = new TowerWear();
            Assert.AreEqual(12, w.ApplyBattle(12));
            Assert.AreEqual(12, w.PermanentDamagePct);
            Assert.IsFalse(w.IsDisabled);
        }

        [Test]
        public void NoDamage_DoesNotConsumeAnAttackSlot()
        {
            var w = new TowerWear();
            w.ApplyBattle(0);
            Assert.AreEqual(0, w.AttackCount);
            Assert.AreEqual(30, w.ApplyBattle(100));
        }

        [Test]
        public void Repair_GddExample_Level1_70Pct_Is7Minutes()
        {
            var w = new TowerWear(70);
            Assert.AreEqual(14, w.RepairUnits);
            Assert.AreEqual(14 * 30, w.RepairSeconds(1));
            Assert.AreEqual(14 * 60, w.RepairSeconds(2));
            Assert.AreEqual(14 * 180, w.RepairSeconds(3));
        }

        [Test]
        public void RepairCost_UsesOnlyUnlockedResources()
        {
            var cfg = ResourceConfig.CreateDefault();
            var w = new TowerWear(70);
            var early = w.RepairCost(1, cfg, 3);
            Assert.AreEqual(0, early.Gold);
            Assert.Greater(early.Wood, 0);
            var late = w.RepairCost(1, cfg, 15);
            Assert.Greater(late.Gold, 0);
        }

        [Test]
        public void RepairAll_ResetsWearAndAttackCount()
        {
            var w = new TowerWear();
            w.ApplyBattle(100);
            w.RepairAll();
            Assert.AreEqual(0, w.PermanentDamagePct);
            Assert.AreEqual(0, w.AttackCount);
            Assert.AreEqual(30, w.ApplyBattle(100));
        }
    }
}
