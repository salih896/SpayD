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

        private readonly int[] capOverride = new int[Enum.GetValues(typeof(ResourceType)).Length];

        public Wallet(ResourceConfig config) { cfg = config; }

        /// <summary>Depo binalarından hesaplanan kapasite (0 = ayar dosyasındaki değer kullanılır).</summary>
        public void SetCapacity(ResourceType t, int capacity) { capOverride[(int)t] = capacity; }

        /// <summary>Kayıttan yükleme: doğrudan bakiye atar (sunucu doğrulaması dışında kullanılmaz).</summary>
        public void SetAmount(ResourceType t, int amount) { amounts[(int)t] = Math.Max(0, amount); }

        public int Get(ResourceType t) => amounts[(int)t];

        /// <summary>Depo kapasitesi; 0 veya negatif = henüz belirlenmedi (sınırsız).</summary>
        public int CapacityOf(ResourceType t)
        {
            int c = capOverride[(int)t] > 0 ? capOverride[(int)t] : cfg.Get(t).StorageCapacity;
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

        /// <summary>Yağma kaybı: alan/kilit denetimi yok, eldekinden fazlası düşmez. Gerçekte düşen miktarı döndürür.</summary>
        public ResourceCost Plunder(ResourceCost loot)
        {
            int[] taken = new int[4];
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
            {
                int n = Math.Max(0, Math.Min(loot.Get(t), amounts[(int)t]));
                amounts[(int)t] -= n; taken[(int)t] = n;
            }
            return new ResourceCost(taken[0], taken[1], taken[2], taken[3]);
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
