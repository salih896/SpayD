using System;

namespace GameCore.Economy
{
    /// <summary>
    /// Oyuncunun kaynak bakiyesi ve doğrulaması. Saf C#: sunucu aynı kodu çağırır; istemci bakiyeyi kendisi değiştirmez.
    /// Kurallar: açılmamış kaynak eklenemez/harcanamaz; harcama ya tamamen olur ya hiç (atomik);
    /// depo doluysa üretim durur; kapasiteyi aşan mevcut kaynak silinmez.
    /// </summary>
    public sealed class Wallet
    {
        private readonly ResourceConfig cfg;
        private readonly int[] amounts = new int[Enum.GetValues(typeof(ResourceType)).Length];

        public Wallet(ResourceConfig config) { cfg = config; }

        public int Get(ResourceType t) => amounts[(int)t];

        /// <summary>Depo kapasitesi; 0 veya negatif = henüz belirlenmedi (sınırsız).</summary>
        public int CapacityOf(ResourceType t)
        {
            int c = cfg.Get(t).StorageCapacity;
            return c <= 0 ? int.MaxValue : c;
        }

        /// <summary>Üretim ekler. Depo yetmezse yalnızca sığan kadar eklenir (added &lt; amount).</summary>
        public bool TryAdd(ResourceType t, int amount, int level, out int added, out string error)
        {
            added = 0; error = null;
            if (amount < 0) { error = "Negatif miktar eklenemez"; return false; }
            if (!ResourceRules.CanProduce(cfg, t, level))
            { error = Name(t) + " henüz açılmadı (Sv " + cfg.Get(t).UnlockLevel + ")"; return false; }
            long room = (long)CapacityOf(t) - amounts[(int)t];
            if (room < 0) room = 0;
            added = (int)Math.Min(amount, room);
            amounts[(int)t] += added;
            return true;
        }

        public bool CanAfford(ResourceCost cost, int level, SpendArea area, out string error)
        {
            error = null;
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
            {
                int need = cost.Get(t);
                if (need < 0) { error = "Geçersiz maliyet"; return false; }
                if (need == 0) continue;
                if (!ResourceRules.IsUnlocked(cfg, t, level))
                { error = Name(t) + " henüz açılmadı (Sv " + cfg.Get(t).UnlockLevel + ")"; return false; }
                if (!ResourceRules.CanSpend(cfg, t, level, area))
                { error = Name(t) + " burada harcanamaz"; return false; }
                if (amounts[(int)t] < need)
                { error = "Yetersiz " + Name(t) + ": gerekli " + need + ", var " + amounts[(int)t]; return false; }
            }
            return true;
        }

        /// <summary>Atomik harcama: bir kalem bile eksikse hiçbir şey düşmez.</summary>
        public bool TrySpend(ResourceCost cost, int level, SpendArea area, out string error)
        {
            if (!CanAfford(cost, level, area, out error)) return false;
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
                amounts[(int)t] -= cost.Get(t);
            return true;
        }

        private static string Name(ResourceType t)
        {
            switch (t)
            {
                case ResourceType.Wood: return "Odun";
                case ResourceType.Stone: return "Taş";
                case ResourceType.Gold: return "Altın";
                default: return "Demir";
            }
        }
    }
}
