using NUnit.Framework;
using GameCore.Economy;

namespace GameCore.Tests
{
    public class WalletTests
    {
        private static Wallet New() => new Wallet(ResourceConfig.CreateDefault());

        [Test]
        public void Wood_CanBeAdded_AtLevelOne()
        {
            var w = New();
            Assert.IsTrue(w.TryAdd(ResourceType.Wood, 100, 1, out int added, out _));
            Assert.AreEqual(100, added);
            Assert.AreEqual(100, w.Get(ResourceType.Wood));
        }

        [Test]
        public void LockedResource_CannotBeProduced()
        {
            var w = New();
            Assert.IsFalse(w.TryAdd(ResourceType.Stone, 10, 4, out _, out var err));
            StringAssert.Contains("açılmadı", err);
            Assert.AreEqual(0, w.Get(ResourceType.Stone));
            Assert.IsTrue(w.TryAdd(ResourceType.Stone, 10, 5, out _, out _));
        }

        [Test]
        public void NegativeAdd_IsRejected()
        {
            Assert.IsFalse(New().TryAdd(ResourceType.Wood, -5, 1, out _, out _));
        }

        [Test]
        public void Spend_NeedsEnough_AndIsAtomic()
        {
            var w = New();
            w.TryAdd(ResourceType.Wood, 100, 1, out _, out _);
            w.TryAdd(ResourceType.Stone, 10, 5, out _, out _);
            var cost = new ResourceCost(wood: 50, stone: 20); // taş yetmez
            Assert.IsFalse(w.TrySpend(cost, 5, SpendArea.Building, out var err));
            StringAssert.Contains("Yetersiz Taş", err);
            Assert.AreEqual(100, w.Get(ResourceType.Wood)); // odun da düşmedi
            Assert.AreEqual(10, w.Get(ResourceType.Stone));
        }

        [Test]
        public void Spend_Succeeds_WhenEnough()
        {
            var w = New();
            w.TryAdd(ResourceType.Wood, 100, 1, out _, out _);
            Assert.IsTrue(w.TrySpend(new ResourceCost(wood: 40), 1, SpendArea.Road, out _));
            Assert.AreEqual(60, w.Get(ResourceType.Wood));
        }

        [Test]
        public void Spend_OfLockedResource_IsRejected_EvenIfBalanceExists()
        {
            // Seviye düşse/kilit açılmamış olsa bile açılmamış kaynak harcanamaz.
            var w = New();
            w.TryAdd(ResourceType.Gold, 500, 15, out _, out _);
            Assert.IsFalse(w.TrySpend(new ResourceCost(0, 0, 100), 10, SpendArea.Research, out var err));
            StringAssert.Contains("açılmadı", err);
            Assert.AreEqual(500, w.Get(ResourceType.Gold));
        }

        [Test]
        public void FullStorage_StopsProduction_AndNeverDeletesExcess()
        {
            var cfg = new ResourceConfig(new[]
            {
                new ResourceDef { Type = ResourceType.Wood, UnlockLevel = 1, StorageCapacity = 100, SpendAreas = SpendArea.All },
                new ResourceDef { Type = ResourceType.Stone, UnlockLevel = 5, SpendAreas = SpendArea.All },
                new ResourceDef { Type = ResourceType.Gold, UnlockLevel = 15, SpendAreas = SpendArea.All },
                new ResourceDef { Type = ResourceType.Iron, UnlockLevel = 25, SpendAreas = SpendArea.All },
            });
            var w = new Wallet(cfg);
            w.TryAdd(ResourceType.Wood, 80, 1, out _, out _);
            Assert.IsTrue(w.TryAdd(ResourceType.Wood, 50, 1, out int added, out _));
            Assert.AreEqual(20, added);                       // yalnızca sığan kadar
            Assert.AreEqual(100, w.Get(ResourceType.Wood));
            Assert.IsTrue(w.TryAdd(ResourceType.Wood, 10, 1, out added, out _));
            Assert.AreEqual(0, added);                        // depo dolu: üretim durur
        }

        [Test]
        public void ResourceCost_ToString_ListsOnlyUsedResources()
        {
            Assert.AreEqual("5000 odun, 1000 taş", new ResourceCost(5000, 1000).ToString());
            Assert.AreEqual("ücretsiz", new ResourceCost(0).ToString());
        }

        [Test]
        public void SpendArea_Restriction_IsEnforced()
        {
            var cfg = new ResourceConfig(new[]
            {
                new ResourceDef { Type = ResourceType.Wood, UnlockLevel = 1, SpendAreas = SpendArea.Building },
                new ResourceDef { Type = ResourceType.Stone, UnlockLevel = 5, SpendAreas = SpendArea.All },
                new ResourceDef { Type = ResourceType.Gold, UnlockLevel = 15, SpendAreas = SpendArea.All },
                new ResourceDef { Type = ResourceType.Iron, UnlockLevel = 25, SpendAreas = SpendArea.All },
            });
            var w = new Wallet(cfg);
            w.TryAdd(ResourceType.Wood, 10, 1, out _, out _);
            Assert.IsFalse(w.TrySpend(new ResourceCost(5), 1, SpendArea.Research, out var err));
            StringAssert.Contains("harcanamaz", err);
            Assert.IsTrue(w.TrySpend(new ResourceCost(5), 1, SpendArea.Building, out _));
        }

        [Test]
        public void Plunder_TakesAtMostWhatExists()
        {
            var w = new Wallet(ResourceConfig.CreateDefault());
            w.TryAdd(ResourceType.Wood, 100, 1, out _, out _);
            var taken = w.Plunder(new ResourceCost(70, 50));
            Assert.AreEqual(70, taken.Wood);
            Assert.AreEqual(0, taken.Stone);
            Assert.AreEqual(30, w.Get(ResourceType.Wood));
            taken = w.Plunder(new ResourceCost(500));
            Assert.AreEqual(30, taken.Wood);
            Assert.AreEqual(0, w.Get(ResourceType.Wood));
        }
    }
}
