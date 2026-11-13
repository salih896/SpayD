using System;
using System.Collections.Generic;
using GameCore.Combat;
using GameCore.Economy;

namespace GameCore.Military
{
    public sealed class ProductionItem
    {
        public SoldierType Type;
        public int RemainingSeconds;
        public bool Started;
        public ResourceCost Cost;
    }

    /// <summary>
    /// Tek ortak üretim kuyruğu (GDD Bölüm 12). Süre saniye cinsinden ilerler; çevrimdışı ilerleme için
    /// sunucu geçen süreyi Advance'e verir. Maliyet yalnızca oyuncu seviyesinde açık kaynakla ödenir.
    /// Kuyruktaki (başlamamış) askeri iptalde maliyet tam iade; üretimdekinde iade yok (GEÇİCİ kural).
    /// </summary>
    public sealed class ProductionQueue
    {
        private readonly List<ProductionItem> items = new List<ProductionItem>();
        public IReadOnlyList<ProductionItem> Items => items;

        public int TotalSecondsRemaining
        {
            get { int s = 0; foreach (var i in items) s += i.RemainingSeconds; return s; }
        }

        public bool TryEnqueue(SoldierType type, Wallet wallet, int playerLevel, out string error)
        {
            var def = ContentCatalog.Soldier(type);
            if (playerLevel < def.UnlockLevel) { error = def.Name + " Sv " + def.UnlockLevel + "'da açılır"; return false; }
            if (!wallet.TrySpend(def.Cost, playerLevel, SpendArea.Training, out error)) return false;
            items.Add(new ProductionItem { Type = type, RemainingSeconds = def.TrainSeconds, Cost = def.Cost });
            return true;
        }

        /// <summary>Süreyi ilerletir; biten askerleri envantere ekler. Döndürdüğü: tamamlanan asker sayısı.</summary>
        public int Advance(int seconds, SoldierInventory inventory)
        {
            int done = 0;
            while (seconds > 0 && items.Count > 0)
            {
                var it = items[0];
                it.Started = true;
                int use = Math.Min(seconds, it.RemainingSeconds);
                it.RemainingSeconds -= use;
                seconds -= use;
                if (it.RemainingSeconds <= 0)
                {
                    inventory.Add(it.Type, 1);
                    items.RemoveAt(0);
                    done++;
                }
            }
            return done;
        }

        public bool Cancel(int index, Wallet wallet, int playerLevel, out string error)
        {
            error = null;
            if (index < 0 || index >= items.Count) { error = "Geçersiz sıra"; return false; }
            var it = items[index];
            bool refund = !(index == 0 && it.Started);
            items.RemoveAt(index);
            if (refund)
            {
                foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
                {
                    int n = it.Cost.Get(t);
                    if (n > 0) wallet.TryAdd(t, n, playerLevel, out _, out _);
                }
            }
            return true;
        }

        /// <summary>Sürükle-bırak yeniden sıralama. Üretimi başlamış ilk sıradaki asker yerinden oynatılamaz.</summary>
        public bool Move(int from, int to)
        {
            if (from < 0 || from >= items.Count || to < 0 || to >= items.Count || from == to) return false;
            if (items.Count > 0 && items[0].Started && (from == 0 || to == 0)) return false;
            var it = items[from];
            items.RemoveAt(from);
            items.Insert(to, it);
            return true;
        }
    }
}
