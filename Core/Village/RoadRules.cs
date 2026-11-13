using GameCore.Economy;

namespace GameCore.Village
{
    /// <summary>
    /// Yol kuralları (docs/approved-mechanics.md #16-#23).
    /// DİKKAT: Araştırma süresi/maliyeti kullanıcının verdiği ÖRNEK değerlerdir (geçici), kesin denge değil.
    /// </summary>
    public static class RoadRules
    {
        public const int BaseRoadCount = 10;          // Seviye 1'deki yol hakkı
        public const int MinEntrances = 1;
        public const int MaxEntrances = 10;           // GDD Bölüm 5
        public const int ResearchDurationSeconds = 24 * 60 * 60; // ÖRNEK: 1 gün
        public static readonly ResourceCost ResearchCost = new ResourceCost(wood: 5000, stone: 1000); // ÖRNEK (yalnızca açık kaynaklar; altın/demir kullanılmaz)

        /// <summary>Seviye başına +1 yol: Sv1=10, Sv2=11, Sv3=12 ... Savunmada TAM bu sayıda yol olmalı.</summary>
        public static int RoadLimitForLevel(int level) => BaseRoadCount + (level < 1 ? 0 : level - 1);

        /// <summary>Yol geliştirme her zaman açıktır ama önce araştırması yapılmış olmalı.</summary>
        public static bool CanUpgrade(bool researchDone, out string reason)
        {
            reason = null;
            if (!researchDone)
            {
                reason = "Önce yol geliştirme araştırmasını yap (" + ResearchDurationSeconds / 86400 + " gün; " + ResearchCost + ")";
                return false;
            }
            return true;
        }
    }
}
