namespace GameCore.Village
{
    /// <summary>Kaynak maliyeti (tahta, altın, demir). Değerler dengeleme aşamasında veri dosyasına taşınacak.</summary>
    public readonly struct ResourceCost
    {
        public readonly int Wood;
        public readonly int Gold;
        public readonly int Iron;
        public ResourceCost(int wood, int gold, int iron) { Wood = wood; Gold = gold; Iron = iron; }
        public override string ToString() => Wood + " tahta, " + Gold + " altın, " + Iron + " demir";
    }

    /// <summary>
    /// Yol kuralları (docs/approved-mechanics.md #16-#23).
    /// DİKKAT: Araştırma süresi/maliyeti kullanıcının verdiği ÖRNEK değerlerdir (geçici), kesin denge değil.
    /// </summary>
    public static class RoadRules
    {
        public const int BaseRoadCount = 10;          // Seviye 1'deki yol hakkı
        public const int MinEntrances = 1;
        public const int MaxEntrances = 10;           // GDD Bölüm 5
        public const int UpgradeUnlockLevel = 10;     // yol geliştirme araştırması bu seviyede açılır
        public const int ResearchDurationSeconds = 24 * 60 * 60; // ÖRNEK: 1 gün
        public static readonly ResourceCost ResearchCost = new ResourceCost(5000, 1000, 100); // ÖRNEK

        /// <summary>Seviye başına +1 yol: Sv1=10, Sv2=11, Sv3=12 ... Savunmada TAM bu sayıda yol olmalı.</summary>
        public static int RoadLimitForLevel(int level) => BaseRoadCount + (level < 1 ? 0 : level - 1);

        /// <summary>Yol geliştirme (tek tek yola dokunarak) şu an yapılabilir mi?</summary>
        public static bool CanUpgrade(int playerLevel, bool researchDone, out string reason)
        {
            reason = null;
            if (playerLevel < UpgradeUnlockLevel)
            { reason = "Yol geliştirme Sv " + UpgradeUnlockLevel + "'da açılır"; return false; }
            if (!researchDone)
            {
                reason = "Önce yol geliştirme araştırmasını yap (" + ResearchDurationSeconds / 86400 + " gün; " + ResearchCost + ")";
                return false;
            }
            return true;
        }
    }
}
