using NUnit.Framework;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Military;

namespace GameCore.Tests
{
    public class ProductionTests
    {
        private static Wallet Funded(int level = 5)
        {
            var w = new Wallet(ResourceConfig.CreateDefault());
            w.TryAdd(ResourceType.Wood, 10000, level, out _, out _);
            if (level >= 5) w.TryAdd(ResourceType.Stone, 10000, level, out _, out _);
            return w;
        }

        [Test]
        public void Enqueue_PaysCost_AndRespectsUnlockLevel()
        {
            var w = Funded(5);
            var q = new ProductionQueue();
            Assert.IsTrue(q.TryEnqueue(SoldierType.Swordsman, w, 5, out _));
            Assert.AreEqual(10000 - 40, w.Get(ResourceType.Wood));
            Assert.IsFalse(q.TryEnqueue(SoldierType.Cavalry, w, 5, out var err));   // Sv9
            StringAssert.Contains("Sv 9", err);
            Assert.AreEqual(1, q.Items.Count);
        }

        [Test]
        public void Enqueue_FailsWithoutResources()
        {
            var w = new Wallet(ResourceConfig.CreateDefault());
            var q = new ProductionQueue();
            Assert.IsFalse(q.TryEnqueue(SoldierType.Swordsman, w, 5, out var err));
            StringAssert.Contains("Yetersiz", err);
            Assert.AreEqual(0, q.Items.Count);
        }

        [Test]
        public void Queue_ProducesInOrder_AndSupportsOfflineAdvance()
        {
            var w = Funded(5);
            var q = new ProductionQueue(); var inv = new SoldierInventory();
            q.TryEnqueue(SoldierType.Swordsman, w, 5, out _);   // 120 sn
            q.TryEnqueue(SoldierType.Goblin, w, 5, out _);      // 120 sn
            Assert.AreEqual(0, q.Advance(119, inv));
            Assert.AreEqual(1, q.Advance(1, inv));
            Assert.AreEqual(1, inv.Get(SoldierType.Swordsman));
            Assert.AreEqual(0, inv.Get(SoldierType.Goblin));
            Assert.AreEqual(1, q.Advance(100000, inv));          // çevrimdışı büyük süre
            Assert.AreEqual(1, inv.Get(SoldierType.Goblin));
            Assert.AreEqual(0, q.Items.Count);
        }

        [Test]
        public void Cancel_WaitingItem_RefundsFully_StartedItem_DoesNot()
        {
            var w = Funded(5);
            var q = new ProductionQueue(); var inv = new SoldierInventory();
            q.TryEnqueue(SoldierType.Swordsman, w, 5, out _);
            q.TryEnqueue(SoldierType.Goblin, w, 5, out _);
            int afterBoth = w.Get(ResourceType.Wood);
            Assert.IsTrue(q.Cancel(1, w, 5, out _));              // bekleyen: iade
            Assert.AreEqual(afterBoth + 20, w.Get(ResourceType.Wood));
            q.Advance(10, inv);                                    // ilk sıra başladı
            int before = w.Get(ResourceType.Wood);
            Assert.IsTrue(q.Cancel(0, w, 5, out _));               // üretimdeki: iade yok
            Assert.AreEqual(before, w.Get(ResourceType.Wood));
        }

        [Test]
        public void Reorder_Works_ButNotStartedFirstItem()
        {
            var w = Funded(5);
            var q = new ProductionQueue(); var inv = new SoldierInventory();
            q.TryEnqueue(SoldierType.Swordsman, w, 5, out _);
            q.TryEnqueue(SoldierType.Archer, w, 5, out _);
            q.TryEnqueue(SoldierType.Goblin, w, 5, out _);
            Assert.IsTrue(q.Move(2, 0));
            Assert.AreEqual(SoldierType.Goblin, q.Items[0].Type);
            q.Advance(1, inv);
            Assert.IsFalse(q.Move(0, 1));
            Assert.IsTrue(q.Move(2, 1));
        }

        [Test]
        public void Inventory_StockCopy_AndAddAll()
        {
            var inv = new SoldierInventory();
            inv.Add(SoldierType.Goblin, 5);
            var stock = inv.ToStock();
            Assert.AreEqual(5, stock[SoldierType.Goblin]);
            Assert.IsTrue(inv.TryRemove(SoldierType.Goblin, 5));
            Assert.IsFalse(inv.TryRemove(SoldierType.Goblin, 1));
            inv.AddAll(stock);
            Assert.AreEqual(5, inv.Get(SoldierType.Goblin));
        }
    }
}
