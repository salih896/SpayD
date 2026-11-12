using NUnit.Framework;
using GameCore.Economy;

namespace GameCore.Tests
{
    public class ResourceRulesTests
    {
        private static readonly ResourceConfig Cfg = ResourceConfig.CreateDefault();

        [Test]
        public void UnlockLevels_AreWood1_Stone5_Gold15_Iron25()
        {
            Assert.AreEqual(1, Cfg.Get(ResourceType.Wood).UnlockLevel);
            Assert.AreEqual(5, Cfg.Get(ResourceType.Stone).UnlockLevel);
            Assert.AreEqual(15, Cfg.Get(ResourceType.Gold).UnlockLevel);
            Assert.AreEqual(25, Cfg.Get(ResourceType.Iron).UnlockLevel);
        }

        [Test]
        public void OnlyFourResources_NoFood()
        {
            Assert.AreEqual(4, System.Enum.GetValues(typeof(ResourceType)).Length);
        }

        [TestCase(1, 1)] [TestCase(4, 1)] [TestCase(5, 2)] [TestCase(14, 2)]
        [TestCase(15, 3)] [TestCase(24, 3)] [TestCase(25, 4)] [TestCase(40, 4)]
        public void UnlockedCount_PerLevel(int level, int count)
        {
            Assert.AreEqual(count, ResourceRules.Unlocked(Cfg, level).Count);
        }

        [Test]
        public void LockedResource_CannotProduceOrSpend()
        {
            Assert.IsFalse(ResourceRules.CanProduce(Cfg, ResourceType.Stone, 4));
            Assert.IsFalse(ResourceRules.CanSpend(Cfg, ResourceType.Gold, 14, SpendArea.Building));
            Assert.IsTrue(ResourceRules.CanSpend(Cfg, ResourceType.Gold, 15, SpendArea.Building));
            Assert.IsFalse(ResourceRules.CanSpend(Cfg, ResourceType.Iron, 24, SpendArea.Research));
            Assert.IsTrue(ResourceRules.CanSpend(Cfg, ResourceType.Wood, 1, SpendArea.Road));
        }

        [Test]
        public void IncompleteConfig_IsRejected()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new ResourceConfig(new[] { new ResourceDef { Type = ResourceType.Wood, UnlockLevel = 1 } }));
        }
    }
}
