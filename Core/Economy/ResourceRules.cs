using System.Collections.Generic;

namespace GameCore.Economy
{
    /// <summary>Kaynak erişim kuralları. Henüz açılmamış kaynak: üretilemez, harcanamaz, gösterilmez.</summary>
    public static class ResourceRules
    {
        public static bool IsUnlocked(ResourceConfig cfg, ResourceType t, int level) => level >= cfg.Get(t).UnlockLevel;
        public static bool CanProduce(ResourceConfig cfg, ResourceType t, int level) => IsUnlocked(cfg, t, level);
        public static bool CanSpend(ResourceConfig cfg, ResourceType t, int level, SpendArea area)
            => IsUnlocked(cfg, t, level) && (cfg.Get(t).SpendAreas & area) != SpendArea.None;

        /// <summary>Oyuncuya gösterilecek (açık) kaynaklar, enum sırasıyla.</summary>
        public static List<ResourceType> Unlocked(ResourceConfig cfg, int level)
        {
            var list = new List<ResourceType>();
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
                if (IsUnlocked(cfg, t, level)) list.Add(t);
            return list;
        }
    }
}
