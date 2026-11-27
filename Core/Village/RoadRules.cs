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

        /// <summary>Seviye başına +1 yol: Sv1=10, Sv2=11, Sv3=12 ... Savunmada TAM bu sayıda yol olmalı.</summary>
        public static int RoadLimitForLevel(int level) => BaseRoadCount + (level < 1 ? 0 : level - 1);

        /// <summary>
        /// Tek yolun bir basamak geliştirme maliyeti (approved #23: Tahta → Altın+Tahta → Demir). GEÇİCİ (open-questions).
        /// Altın+Tahta altın ister (Sv15'te açılır), Demir demir ister (Sv25) — kilitli kaynak harcanamadığı için seviye kapısı kendiliğinden oluşur.
        /// </summary>
        public static ResourceCost UpgradeCost(RoadTier next)
        {
            switch (Rank(next))
            {
                case 1: return new ResourceCost(200, 150);            // Taş (taş Sv5)
                case 2: return new ResourceCost(300, 0, 100);         // Altın+Tahta (altın Sv15)
                case 3: return new ResourceCost(400, 0, 150, 80);     // Demir (demir Sv25)
                default: return new ResourceCost(700, 400, 300, 200); // Kristal
            }
        }

        // ---- 5 basamak (kullanıcı isteği 14.11.2026): sıra, ad, süre, seviye şartı, savaş etkisi. GEÇİCİ değerler. ----
        private static readonly string[] TierNames = { "Tahta", "Taş", "Altın+Tahta", "Demir", "Kristal" };
        public static int Rank(RoadTier t) => t == RoadTier.Wood ? 0 : t == RoadTier.Stone ? 1 : t == RoadTier.Reinforced ? 2 : t == RoadTier.Iron ? 3 : 4;
        public static RoadTier ByRank(int r) => r <= 0 ? RoadTier.Wood : r == 1 ? RoadTier.Stone : r == 2 ? RoadTier.Reinforced : r == 3 ? RoadTier.Iron : RoadTier.Crystal;
        public static RoadTier Next(RoadTier t) => ByRank(Rank(t) + 1);
        public static bool IsMax(RoadTier t) => Rank(t) >= 4;
        public static string Name(RoadTier t) => TierNames[Rank(t)];
        /// <summary>Geliştirme süresi (işçi kullanır).</summary>
        public static int UpgradeSeconds(RoadTier next) => new[] { 0, 600, 1800, 3600, 7200 }[Rank(next)];
        /// <summary>Oyuncu seviyesi şartı (kaynak kilitleriyle uyumlu; Kristal için ek şart).</summary>
        public static int MinLevel(RoadTier next) => new[] { 1, 5, 15, 25, 28 }[Rank(next)];
        /// <summary>Savaşta bu yolda yürüyen düşman askerinin yavaşlaması (%).</summary>
        public static int SlowPct(RoadTier t) => new[] { 0, 10, 15, 25, 35 }[Rank(t)];
        /// <summary>Bu yoldaki tuzakların hasar çarpanı (%).</summary>
        public static int TrapBonusPct(RoadTier t) => new[] { 100, 100, 110, 150, 200 }[Rank(t)];

        /// <summary>Yol geliştirme her zaman açıktır ama önce araştırması yapılmış olmalı.</summary>
    }
}
