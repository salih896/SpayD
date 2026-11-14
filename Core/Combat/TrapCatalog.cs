using GameCore.Economy;

namespace GameCore.Combat
{
    /// <summary>Tuzak tanımı. Tick (10/sn) ve mili-hücre birimleri. TÜM SAYILAR GEÇİCİ (dengeleme).</summary>
    public sealed class TrapDef
    {
        public TrapType Type; public string Name, Desc;
        public int UnlockLevel;
        public ResourceCost Cost;
        public int Damage;                         // anlık hasar (zırhla azalır)
        public int SlowTicks;                      // hız yarıya
        public int RootTicks;                      // yerinde durur (hareket edemez)
        public int DotTicks, DotPerTick;           // ateş/zehir: tick başına hasar
        public bool FireDot;                       // ateş direnci DotPerTick'i azaltır
        public int SplashMilli;                    // patlama alanı (yalnız yer askerleri)
        public bool OneShot;                       // savaş başına bir kez tetiklenir
        public int ChainCount;                     // şimşek: yakındaki ek askerler
    }

    /// <summary>
    /// 8 tuzak (kullanıcı isteği 14.11.2026). Oyuncu seviyesiyle açılır; köyde başta 4, Akademi "Tuzak Ustalığı" ile 6 tuzak hakkı.
    /// Uçan askerleri etkilemez. Demir/Kristal yolda tuzak hasarı artar (RoadRules.TrapBonusPct).
    /// </summary>
    public static class TrapCatalog
    {
        private static readonly TrapDef[] Defs =
        {
            null, // TrapType.None
            new TrapDef { Type = TrapType.Spikes, Name = "Çivili", Desc = "Basana hasar", UnlockLevel = 3, Cost = new ResourceCost(80), Damage = 40 },
            new TrapDef { Type = TrapType.Slippery, Name = "Kaygan", Desc = "3 sn yavaşlatır", UnlockLevel = 5, Cost = new ResourceCost(100, 40), SlowTicks = 30 },
            new TrapDef { Type = TrapType.Fire, Name = "Ateşli", Desc = "3 sn yakar", UnlockLevel = 8, Cost = new ResourceCost(150, 60), DotTicks = 30, DotPerTick = 4, FireDot = true },
            new TrapDef { Type = TrapType.Poison, Name = "Zehirli", Desc = "6 sn zehirler", UnlockLevel = 10, Cost = new ResourceCost(180, 90), DotTicks = 60, DotPerTick = 2 },
            new TrapDef { Type = TrapType.Frost, Name = "Buz", Desc = "1,5 sn dondurur, sonra yavaşlatır", UnlockLevel = 12, Cost = new ResourceCost(200, 120), RootTicks = 15, SlowTicks = 30 },
            new TrapDef { Type = TrapType.Bomb, Name = "Patlayıcı", Desc = "Bir kez patlar, alan hasarı", UnlockLevel = 15, Cost = new ResourceCost(250, 150, 50), Damage = 260, SplashMilli = 1600, OneShot = true },
            new TrapDef { Type = TrapType.Snare, Name = "Kapan", Desc = "3 sn yakalar + hasar", UnlockLevel = 18, Cost = new ResourceCost(260, 160, 60), Damage = 60, RootTicks = 30 },
            new TrapDef { Type = TrapType.Shock, Name = "Şimşek", Desc = "Hasar + 2 askere sıçrar", UnlockLevel = 22, Cost = new ResourceCost(320, 200, 90), Damage = 90, ChainCount = 2 },
        };

        public static int Count => Defs.Length - 1;
        public static TrapDef Get(TrapType t) => (int)t > 0 && (int)t < Defs.Length ? Defs[(int)t] : null;
    }
}
