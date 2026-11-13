using System.Collections.Generic;
using NUnit.Framework;
using GameCore.Combat;
using GameCore.Progression;

namespace GameCore.Tests
{
    public class CombatMathTests
    {
        [Test]
        public void OverallDamage_GddExample_Is51_AndVictory()
        {
            var pcts = new List<int> { 100, 100, 100, 6, 0, 0 };
            Assert.AreEqual(51, CombatMath.OverallDamagePct(pcts));
            Assert.IsTrue(CombatMath.IsVictory(51));
            Assert.IsFalse(CombatMath.IsVictory(50));
        }

        [Test]
        public void Stars_Thresholds()
        {
            Assert.AreEqual(0, CombatMath.Stars(50));
            Assert.AreEqual(1, CombatMath.Stars(51));
            Assert.AreEqual(1, CombatMath.Stars(74));
            Assert.AreEqual(2, CombatMath.Stars(75));
            Assert.AreEqual(2, CombatMath.Stars(99));
            Assert.AreEqual(3, CombatMath.Stars(100));
        }

        [Test]
        public void ISqrt_IsExact()
        {
            Assert.AreEqual(0, CombatMath.ISqrt(0));
            Assert.AreEqual(1414, CombatMath.ISqrt(2000000));
            Assert.AreEqual(5000, CombatMath.ISqrt(25000000));
        }

        [Test]
        public void Armor_NeverReducesBelowOne()
        {
            Assert.AreEqual(1, CombatMath.ReduceByArmor(1, 90));
            Assert.AreEqual(90, CombatMath.ReduceByArmor(100, 10));
        }

        [Test]
        public void SpearTower_HasBonusVsMounted()
        {
            var spear = ContentCatalog.Tower(TowerType.Spear);
            int vsInfantry = CombatMath.TowerDamage(spear, 1, false, false, 10);   // 30 * 0.9
            int vsCavalry = CombatMath.TowerDamage(spear, 1, false, true, 20);     // 75 * 0.8
            Assert.AreEqual(27, vsInfantry);
            Assert.AreEqual(60, vsCavalry);
        }

        [Test]
        public void ElfTower_HasBonusVsAir()
        {
            var elf = ContentCatalog.Tower(TowerType.Elf);
            Assert.AreEqual(14, CombatMath.TowerDamage(elf, 1, true, false, 0));
            Assert.AreEqual(7, CombatMath.TowerDamage(elf, 1, false, false, 0));
        }

        [Test]
        public void MageTower_PiercesArmor()
        {
            var mage = ContentCatalog.Tower(TowerType.Mage);
            int pierced = CombatMath.TowerDamage(mage, 1, false, false, 30);   // zırh 30 -> 21
            Assert.AreEqual(20, pierced);
        }

        [Test]
        public void SpearTower_CannotHitAir_ByDefinition()
        {
            Assert.IsFalse(ContentCatalog.Tower(TowerType.Spear).CanHitAir);
            Assert.IsTrue(ContentCatalog.Tower(TowerType.Elf).CanHitAir);
        }

        [Test]
        public void LevelScaling_GrowsFasterEachLevel()
        {
            Assert.AreEqual(100, ContentCatalog.ScalePct(1));
            Assert.AreEqual(122, ContentCatalog.ScalePct(2));
            Assert.AreEqual(148, ContentCatalog.ScalePct(3));
            Assert.AreEqual(442, ContentCatalog.ScalePct(10));
            Assert.AreEqual(244, ContentCatalog.Scaled(200, 2));
        }

        [Test]
        public void Xp_CurveAndLevels()
        {
            Assert.AreEqual(100, ProgressionRules.XpToNextLevel(1));
            Assert.AreEqual(114, ProgressionRules.XpToNextLevel(2));
            Assert.AreEqual(1, ProgressionRules.LevelFromTotalXp(99, out int into));
            Assert.AreEqual(99, into);
            Assert.AreEqual(2, ProgressionRules.LevelFromTotalXp(100, out into));
            Assert.AreEqual(0, into);
            Assert.AreEqual(ProgressionRules.MaxLevel, ProgressionRules.LevelFromTotalXp(10000000, out _));
        }

        [Test]
        public void Rng_IsDeterministic()
        {
            var a = new DeterministicRng(42); var b = new DeterministicRng(42);
            for (int i = 0; i < 20; i++) Assert.AreEqual(a.Next(1000), b.Next(1000));
        }
    }
}
