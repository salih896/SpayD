using System;
using System.Collections.Generic;

namespace GameCore.Economy
{
    /// <summary>Tek bir kaynağın ayarları. Üretim/depo/yağma sayıları dengeleme aşamasına kadar GEÇİCİDİR (0 = belirlenmedi).</summary>
    public sealed class ResourceDef
    {
        public ResourceType Type;
        public int UnlockLevel;            // açılma seviyesi (köy/oyuncu seviyesi)
        public int ProductionAmount;       // bir üretim turunda üretilen miktar
        public int ProductionSeconds;      // üretim turu süresi (sn)
        public int StorageCapacity;        // depo kapasitesi
        public SpendArea SpendAreas;       // harcama alanları
        public LootRule Loot;              // yağmalanma kuralı
        public int LootPercent;            // yağmada alınabilecek en çok yüzde
    }

    /// <summary>
    /// Kaynak ayarlarının TEK merkezi. Açılma seviyeleri yalnızca burada tanımlıdır
    /// (docs/approved-mechanics.md "Kaynaklar"). Sonra JSON'dan yüklenecek; sunucu ve istemci aynı veriyi kullanır.
    /// </summary>
    public sealed class ResourceConfig
    {
        private readonly Dictionary<ResourceType, ResourceDef> defs = new Dictionary<ResourceType, ResourceDef>();

        public ResourceConfig(IEnumerable<ResourceDef> definitions)
        {
            foreach (var d in definitions)
            {
                if (defs.ContainsKey(d.Type)) throw new ArgumentException("Kaynak iki kez tanımlı: " + d.Type);
                defs[d.Type] = d;
            }
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
                if (!defs.ContainsKey(t)) throw new ArgumentException("Kaynak tanımı eksik: " + t);
        }

        public ResourceDef Get(ResourceType t) => defs[t];

        /// <summary>Kesinleşmiş açılma seviyeleri: Odun 1, Taş 5, Altın 15, Demir 25.</summary>
        public static ResourceConfig CreateDefault() => new ResourceConfig(new[]
        {
            new ResourceDef { Type = ResourceType.Wood,  UnlockLevel = 1,  SpendAreas = SpendArea.All, Loot = LootRule.Lootable },
            new ResourceDef { Type = ResourceType.Stone, UnlockLevel = 5,  SpendAreas = SpendArea.All, Loot = LootRule.Lootable },
            new ResourceDef { Type = ResourceType.Gold,  UnlockLevel = 15, SpendAreas = SpendArea.All, Loot = LootRule.Lootable },
            new ResourceDef { Type = ResourceType.Iron,  UnlockLevel = 25, SpendAreas = SpendArea.All, Loot = LootRule.Lootable },
        });
    }
}
