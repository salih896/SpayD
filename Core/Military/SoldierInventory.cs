using System.Collections.Generic;
using GameCore.Combat;

namespace GameCore.Military
{
    /// <summary>Oyuncunun asker envanteri (kışlada üretilen, saldırıya gönderilmeyi bekleyen askerler).</summary>
    public sealed class SoldierInventory
    {
        private readonly Dictionary<SoldierType, int> counts = new Dictionary<SoldierType, int>();

        public int Get(SoldierType t) => counts.TryGetValue(t, out var n) ? n : 0;
        public void Add(SoldierType t, int n) { if (n > 0) counts[t] = Get(t) + n; }

        public bool TryRemove(SoldierType t, int n)
        {
            if (n < 0 || Get(t) < n) return false;
            counts[t] = Get(t) - n;
            return true;
        }

        /// <summary>Savaşa verilecek kopya.</summary>
        public Dictionary<SoldierType, int> ToStock() => new Dictionary<SoldierType, int>(counts);

        /// <summary>Savaş sonucundaki "dönenler"i envantere işler (gönderilen askerler önceden düşülmüş olmalı).</summary>
        public void AddAll(Dictionary<SoldierType, int> returned)
        {
            foreach (var kv in returned) Add(kv.Key, kv.Value);
        }
    }
}
