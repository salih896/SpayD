using System.Collections.Generic;
using NUnit.Framework;
using GameCore.Combat;
using GameCore.Progression;

namespace GameCore.Tests
{
    public class CombatMathTests
    {
        [Test]
        public void OverallDamage_GddExample_Is51_VictoryNeeds75()
        {
            var pcts = new List<int> { 100, 100, 100, 6, 0, 0 };
            Assert.AreEqual(51, CombatMath.OverallDamagePct(pcts));
            Assert.IsFalse(CombatMath.IsVictory(51), "Zafer %75 (kullanıcı kararı 15.11.2026)");
            Assert.IsTrue(CombatMath.IsVictory(75));
            Assert.IsFalse(CombatMath.IsVictory(74));

        }

        [Test]
        public void Stars_Thresholds()
        {
            Assert.AreEqual(0, CombatMath.Stars(74));
            Assert.AreEqual(1, CombatMath.Stars(75));
            Assert.AreEqual(1, CombatMath.Stars(89));
            Assert.AreEqual(2, CombatMath.Stars(90));
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
        public void Wall_DoesNotAttack_HasHighHpAndTaunt()
        {
            var wall = ContentCatalog.Tower(TowerType.Wall);
            Assert.AreEqual(0, wall.Attack);
            Assert.Greater(wall.Hp, ContentCatalog.Tower(TowerType.Archer).Hp * 2);
            Assert.Greater(wall.TauntMilli, 0);
        }

        [Test]
        public void MountedBonusFormula_StillWorks()
        {
            var spear = new TowerDef { Attack = 75, BonusVsMountedPct = 150 };
            int vsInfantry = CombatMath.TowerDamage(spear, 1, false, false, 10);   // 75 * 0.9
            int vsCavalry = CombatMath.TowerDamage(spear, 1, false, true, 20);     // 187 * 0.8
            Assert.AreEqual(67, vsInfantry);
            Assert.AreEqual(149, vsCavalry); // tamsayı yuvarlama
        }

        [Test]
        public void ElfTower_HasBonusVsAir()
        {
            var elf = ContentCatalog.Tower(TowerType.Elf);
            Assert.AreEqual(40, CombatMath.TowerDamage(elf, 1, true, false, 0));
            Assert.AreEqual(20, CombatMath.TowerDamage(elf, 1, false, false, 0));
        }

        [Test]
        public void MageTower_PiercesArmor()
        {
            var mage = ContentCatalog.Tower(TowerType.Mage);
            int pierced = CombatMath.TowerDamage(mage, 1, false, false, 30);   // zırh 30 -> 21
            Assert.AreEqual(15, pierced); // 20 * (1 - 0.21) (tüm kuleler 20 hasar, 15.11.2026)
        }

        [Test]
        public void SpearTower_CannotHitAir_ByDefinition()
        {
            Assert.IsFalse(ContentCatalog.Tower(TowerType.Wall).CanHitAir);
            Assert.IsFalse(ContentCatalog.Tower(TowerType.Cannon).CanHitAir);
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
            Assert.AreEqual(203, ProgressionRules.XpToNextLevel(2)); // 100 + 85l + 18l², l = 1
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
