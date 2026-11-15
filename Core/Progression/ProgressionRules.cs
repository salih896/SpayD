namespace GameCore.Progression
{
    /// <summary>Oyuncu seviye/XP kuralları. DİKKAT: GDD Bölüm 21 formülü "taslak, onaylanmadı"; tüm değerler GEÇİCİDİR.</summary>
    public static class ProgressionRules
    {
        public const int MaxLevel = 30;

        /// <summary>Seviye L'den L+1'e geçmek için gereken XP (taslak: 100 + 85(L-1) + 18(L-1)²; kullanıcı hedefi Sv30 ≈ 100 gün, simülasyonla ayarlandı,
        /// bkz. docs/balance-report.txt). Sv1→2 = 100 (Sv1'de yalnızca kurma XP'si var). Sv1→30 toplam ≈ 175.000 XP (15.11.2026: köy düzeni dengesinden sonra yeniden ayarlandı).</summary>
        public static int XpToNextLevel(int level)
        {
            int l = level < 1 ? 0 : level - 1;
            return 100 + 85 * l + 18 * l * l;
        }

        /// <summary>Toplam XP'den seviye (1..MaxLevel) ve seviye içi kalan XP.</summary>
        public static int LevelFromTotalXp(long totalXp, out int xpIntoLevel)
        {
            int level = 1;
            long rest = totalXp < 0 ? 0 : totalXp;
            while (level < MaxLevel && rest >= XpToNextLevel(level)) { rest -= XpToNextLevel(level); level++; }
            xpIntoLevel = (int)rest;
            return level;
        }

        /// <summary>Savaştan XP: katılım + hasar + yıldız (GEÇİCİ). Kaybeden de az XP alır (öğrenme/ilerleme hissi).</summary>
        public static int BattleXp(int overallDamagePct, int stars)
        {
            return 10 + overallDamagePct / 4 + stars * 15;
        }
    }
}
