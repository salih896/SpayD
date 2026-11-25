using System.Collections.Generic;

namespace GameCore.Combat
{
    /// <summary>Asker özel yeteneği: belirli seviyede açılır, savaşta otomatik çalışır. Etki alanları ‰/% olarak; GEÇİCİ.</summary>
    public sealed class AbilityDef
    {
        public string Name, Desc;
        public int Level;
        public int DmgTakenPermille;   // gelen hasar azaltma (‰)
        public int RangePct, SpeedPct, ArmorAdd, SplashAdd;
        public int VsTowerPct, VsResourcePct;
        public int LowHpAtkPct;        // canı %50 altındayken saldırı +%
        public int EveryN, EveryMultPct; // her N. saldırı ×(Mult/100)
        public int FirstHitMultPct;    // ilk saldırı ×(Mult/100)
        public int HealPermille;       // Peri: 2 sn'de bir yakındaki dostlara azami canın ‰'i kadar iyileştirme
        public int RegenPermille;      // saniyede azami canın ‰'i kadar yenilenme
        public bool ImmuneSlow;        // yavaşlama/yakalanma etkilemez
    }

    /// <summary>Askerin seviyesine göre etkin yeteneklerin toplamı (BattleSim'de kullanılır).</summary>
    public sealed class SoldierPerks
    {
        public int DmgTakenPermille, RangePct, SpeedPct, ArmorAdd, SplashAdd, VsTowerPct, VsResourcePct, LowHpAtkPct, EveryN, EveryMultPct, FirstHitMultPct, HealPermille, RegenPermille;
        public bool ImmuneSlow;
    }

    /// <summary>10 askerin 3'er yeteneği (asker Sv2 / Sv3 / Sv8 — oyuncu ≈ Sv7 / Sv9 / Sv18). Otomatik kullanılır.</summary>
    public static class SoldierAbilities
    {
        public static readonly int[] UnlockLevels = { 2, 3, 8 }; // kullanıcı isteği: kule sıçramalarının olduğu Sv6/Sv9 civarında açılsın

        private static AbilityDef A(string n, string d, int lvl) => new AbilityDef { Name = n, Desc = d, Level = lvl };

        private static readonly Dictionary<SoldierType, AbilityDef[]> Table = new Dictionary<SoldierType, AbilityDef[]>
        {
            { SoldierType.Swordsman, new[] {
                Set(A("Kalkan", "Gelen hasar -%20", 2), x => x.DmgTakenPermille = 200),
                Set(A("Öfke", "Canı yarının altındayken hasar +%30", 3), x => x.LowHpAtkPct = 30),
                Set(A("Girdap", "Saldırıları çevreye de vurur", 8), x => x.SplashAdd = 800) } },
            { SoldierType.Archer, new[] {
                Set(A("Kartal Gözü", "Menzil +%25", 2), x => x.RangePct = 25),
                Set(A("Çift Ok", "Her 3. atış 2 kat", 3), x => { x.EveryN = 3; x.EveryMultPct = 200; }),
                Set(A("Kule Avcısı", "Kulelere hasar +%40", 8), x => x.VsTowerPct = 40) } },
            { SoldierType.Mage, new[] {
                Set(A("Geniş Alan", "Alan hasarı genişler", 2), x => x.SplashAdd = 700),
                Set(A("Taş Eritme", "Kulelere hasar +%25", 3), x => x.VsTowerPct = 25),
                Set(A("Meteor", "Her 5. büyü 3 kat", 8), x => { x.EveryN = 5; x.EveryMultPct = 300; }) } },
            { SoldierType.Goblin, new[] {
                Set(A("Çevik", "Hız +%20", 2), x => x.SpeedPct = 20),
                Set(A("Yağmacı", "Kaynak binalarına hasar +%50", 3), x => x.VsResourcePct = 50),
                Set(A("Kaçak", "Gelen hasar -%30", 8), x => x.DmgTakenPermille = 300) } },
            { SoldierType.Cavalry, new[] {
                Set(A("Hücum", "İlk saldırı 3 kat", 2), x => x.FirstHitMultPct = 300),
                Set(A("Ağır Zırh", "Zırh +10", 3), x => x.ArmorAdd = 10),
                Set(A("Ezici", "Saldırıları çevreye de vurur", 8), x => x.SplashAdd = 800) } },
            { SoldierType.Spearman, new[] {
                Set(A("Falanks", "Zırh +10", 2), x => x.ArmorAdd = 10),
                Set(A("Delici", "Kulelere hasar +%30", 3), x => x.VsTowerPct = 30),
                Set(A("Sarsılmaz", "Yavaşlama ve kapanlar etkilemez", 8), x => x.ImmuneSlow = true) } },
            { SoldierType.Fairy, new[] {
                Set(A("Hızlı Kanat", "Hız +%20", 2), x => x.SpeedPct = 20),
                Set(A("Şifa", "2 sn'de bir yakındaki dostları iyileştirir", 3), x => x.HealPermille = 30),
                Set(A("Büyü Kalkanı", "Gelen hasar -%25", 8), x => x.DmgTakenPermille = 250) } },
            { SoldierType.Giant, new[] {
                Set(A("Taş Deri", "Zırh +10", 2), x => x.ArmorAdd = 10),
                Set(A("Sarsıntı", "Saldırıları çevreye de vurur", 3), x => x.SplashAdd = 1000),
                Set(A("Yenilenme", "Saniyede canının %1'ini yeniler", 8), x => x.RegenPermille = 10) } },
            { SoldierType.Catapult, new[] {
                Set(A("Uzun Kol", "Menzil +%20", 2), x => x.RangePct = 20),
                Set(A("Ateş Güllesi", "Hasar +%30 (kulelere)", 3), x => x.VsTowerPct = 30),
                Set(A("Çifte Atış", "Her 2. atış 2 kat", 8), x => { x.EveryN = 2; x.EveryMultPct = 200; }) } },
            { SoldierType.DragonRider, new[] {
                Set(A("Ateş Nefesi", "Alan hasarı genişler", 2), x => x.SplashAdd = 600),
                Set(A("Pul Zırh", "Gelen hasar -%20", 3), x => x.DmgTakenPermille = 200),
                Set(A("Ejder Öfkesi", "Canı yarının altındayken hasar +%40", 8), x => x.LowHpAtkPct = 40) } },
            { SoldierType.Shieldbearer, new[] {
                Set(A("Kalkan Duvarı", "Gelen hasar -%15", 2), x => x.DmgTakenPermille = 150),
                Set(A("Sarsılmaz", "Yavaşlama ve kapan işlemez", 3), x => x.ImmuneSlow = true),
                Set(A("Kale", "Zırh +10", 8), x => x.ArmorAdd = 10) } },
            { SoldierType.Assassin, new[] {
                Set(A("Gölge Adım", "Hız +%15", 2), x => x.SpeedPct = 15),
                Set(A("Sinsi Darbe", "İlk vuruş 3 kat", 3), x => x.FirstHitMultPct = 300),
                Set(A("Kule Avcısı", "Kulelere +%50", 8), x => x.VsTowerPct = 50) } },
            { SoldierType.Healer, new[] {
                Set(A("Şifalı Eller", "İyileştirme +%3", 2), x => x.HealPermille = 30),
                Set(A("Kutsal Kalkan", "Gelen hasar -%15", 3), x => x.DmgTakenPermille = 150),
                Set(A("Toplu Şifa", "İyileştirme +%4", 8), x => x.HealPermille = 40) } },
            { SoldierType.Crossbowman, new[] {
                Set(A("Delici Ok", "Kulelere +%30", 2), x => x.VsTowerPct = 30),
                Set(A("Uzun Menzil", "Menzil +%15", 3), x => x.RangePct = 15),
                Set(A("Çift Atış", "Her 3. atış 2 kat", 8), x => { x.EveryN = 3; x.EveryMultPct = 200; }) } },
            { SoldierType.Bomber, new[] {
                Set(A("Büyük Fitil", "Patlama alanı büyür", 2), x => x.SplashAdd = 500),
                Set(A("Hızlı Koşu", "Hız +%15", 3), x => x.SpeedPct = 15),
                Set(A("Yıkım", "Kulelere +%50", 8), x => x.VsTowerPct = 50) } },
        };

        private static AbilityDef Set(AbilityDef a, System.Action<AbilityDef> f) { f(a); return a; }

        public static AbilityDef[] Of(SoldierType t) => Table[t];

        /// <summary>Seviyede açık yeteneklerin toplam etkisi.</summary>
        public static SoldierPerks PerksFor(SoldierType t, int level)
        {
            var p = new SoldierPerks();
            // taban özellikler (yetenek değil): Şifacı iyileştirir, Arbaletli kulelere +%40
            if (t == SoldierType.Healer) p.HealPermille = 50;
            if (t == SoldierType.Crossbowman) p.VsTowerPct = 40;
            foreach (var a in Table[t])
            {
                if (level < a.Level) continue;
                p.DmgTakenPermille += a.DmgTakenPermille; p.RangePct += a.RangePct; p.SpeedPct += a.SpeedPct; p.ArmorAdd += a.ArmorAdd;
                p.SplashAdd += a.SplashAdd; p.VsTowerPct += a.VsTowerPct; p.VsResourcePct += a.VsResourcePct; p.LowHpAtkPct += a.LowHpAtkPct;
                if (a.EveryN > 0) { p.EveryN = a.EveryN; p.EveryMultPct = a.EveryMultPct; }
                if (a.FirstHitMultPct > 0) p.FirstHitMultPct = a.FirstHitMultPct;
                p.HealPermille += a.HealPermille; p.RegenPermille += a.RegenPermille;
                p.ImmuneSlow |= a.ImmuneSlow;
            }
            if (p.DmgTakenPermille > 600) p.DmgTakenPermille = 600;
            return p;
        }
    }
}
