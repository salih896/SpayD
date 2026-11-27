using System;
using System.Collections.Generic;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Grid;
using GameCore.Village;

namespace GameCore.Game
{
    public enum DraftKind { Road, Building, Tower, Trap }

    /// <summary>Köy Düzeni taslağındaki bir öğe (yol parçası, bina, kule veya tuzak).</summary>
    public sealed class DraftItem
    {
        public int Id;
        public DraftKind Kind;
        public BuildingType Building;
        public TowerType Tower;
        public TrapType Trap;
        public RoadTier Tier;
        /// <summary>Var olan öğe: GameState'teki konumu (kimliği). Yeni satın almada yoktur.</summary>
        public bool HasOrigin;
        public GridPos Origin;
        /// <summary>Geliştirilmekte olan yol: taşınamaz.</summary>
        public bool Locked;
        /// <summary>Yeni satın alma (yol hakkı ücretsizdir, satın alma sayılmaz).</summary>
        public bool IsNew => !HasOrigin && Kind != DraftKind.Road;
    }

    /// <summary>
    /// Köy Düzeni (kullanıcı kararı 15.11.2026): tüm yapı/yol/tuzak değişiklikleri önce bu taslakta yapılır,
    /// "Onayla" ile GameState.TryApplyLayout tek seferde köye aktarır. Taslakta geçici olarak kural dışı
    /// yerleşim serbesttir; kurallar (yollar bağlı, yapılar yola bağlı, sınırlar, maliyet) onayda denetlenir.
    /// Yeni satın almaların bedeli onayda ödenir; satılan yapıların %50 iadesi onayda verilir.
    /// </summary>
    public sealed class LayoutDraft
    {
        private readonly GameState g;
        public readonly Dictionary<GridPos, DraftItem> Cells = new Dictionary<GridPos, DraftItem>();
        public readonly Dictionary<GridPos, DraftItem> Traps = new Dictionary<GridPos, DraftItem>();
        public readonly List<DraftItem> Inventory = new List<DraftItem>();
        public readonly HashSet<GridPos> Entrances = new HashSet<GridPos>();
        private int nextId = 1;

        public int Size => g.Layout.Size;
        public int RoadLimit => g.Layout.RoadLimit;

        public LayoutDraft(GameState state)
        {
            g = state;
            var L = g.Layout;
            foreach (var p in L.CellsOf(CellKind.Road))
                Cells[p] = new DraftItem { Id = nextId++, Kind = DraftKind.Road, Tier = L.GetRoadTier(p), HasOrigin = true, Origin = p, Locked = g.IsUpgrading(JobKind.Road, p, SoldierType.Swordsman) };
            foreach (var p in L.CellsOf(CellKind.Building))
                if (g.Buildings.TryGetValue(p, out var b)) Cells[p] = new DraftItem { Id = nextId++, Kind = DraftKind.Building, Building = b.Type, HasOrigin = true, Origin = p };
            foreach (var p in L.CellsOf(CellKind.Tower))
                if (g.Towers.TryGetValue(p, out var t)) Cells[p] = new DraftItem { Id = nextId++, Kind = DraftKind.Tower, Tower = t.Type, HasOrigin = true, Origin = p };
            var trapKeys = new List<GridPos>(g.Traps.Keys);
            trapKeys.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            foreach (var p in trapKeys) Traps[p] = new DraftItem { Id = nextId++, Kind = DraftKind.Trap, Trap = g.Traps[p], HasOrigin = true, Origin = p };
            foreach (var e in L.EntrancePositions()) Entrances.Add(e);
            SyncRoadLimit();
        }

        // ---------- Sayımlar ----------
        public int RoadTotal { get { int n = 0; foreach (var kv in Cells) if (kv.Value.Kind == DraftKind.Road) n++; foreach (var i in Inventory) if (i.Kind == DraftKind.Road) n++; return n; } }
        public int PlacedRoads { get { int n = 0; foreach (var kv in Cells) if (kv.Value.Kind == DraftKind.Road) n++; return n; } }

        private IEnumerable<DraftItem> All()
        {
            foreach (var kv in Cells) yield return kv.Value;
            foreach (var kv in Traps) yield return kv.Value;
            foreach (var i in Inventory) yield return i;
        }

        public int CountBuildings(BuildingType t) { int n = 0; foreach (var i in All()) if (i.Kind == DraftKind.Building && i.Building == t) n++; return n; }
        public int TowerCount { get { int n = 0; foreach (var i in All()) if (i.Kind == DraftKind.Tower) n++; return n; } }
        public int TrapCount { get { int n = 0; foreach (var i in All()) if (i.Kind == DraftKind.Trap) n++; return n; } }

        /// <summary>Seviye atlayınca artan yol hakkını envantere ekler.</summary>
        public void SyncRoadLimit()
        {
            for (int n = RoadTotal; n < RoadLimit; n++) Inventory.Add(new DraftItem { Id = nextId++, Kind = DraftKind.Road, Tier = RoadTier.Wood });
        }

        public DraftItem At(GridPos p) => Traps.TryGetValue(p, out var t) ? t : (Cells.TryGetValue(p, out var c) ? c : null);

        public ResourceCost CostOf(DraftItem it)
        {
            int lvl = g.Level;
            switch (it.Kind)
            {
                case DraftKind.Building: return GameRules.Scale(GameRules.BuildCostOf(it.Building), g.Config, lvl, 100);
                case DraftKind.Tower: return GameRules.Scale(ContentCatalog.Tower(it.Tower).BuildCost, g.Config, lvl, 100);
                case DraftKind.Trap: return GameRules.Scale(TrapCatalog.Get(it.Trap).Cost, g.Config, lvl, 100);
                default: return default;
            }
        }

        private static ResourceCost Add(ResourceCost a, ResourceCost b) => new ResourceCost(a.Wood + b.Wood, a.Stone + b.Stone, a.Gold + b.Gold, a.Iron + b.Iron);

        public ResourceCost NewCost()
        {
            ResourceCost sum = default;
            foreach (var i in All()) if (i.IsNew) sum = Add(sum, CostOf(i));
            return sum;
        }

        public static string NameOf(DraftItem it)
        {
            switch (it.Kind)
            {
                case DraftKind.Building: return ContentCatalog.Building(it.Building).Name;
                case DraftKind.Tower: return ContentCatalog.Tower(it.Tower).Name;
                case DraftKind.Trap: return TrapCatalog.Get(it.Trap).Name + " Tuzağı";
                default: return RoadRules.Name(it.Tier) + " Yol";
            }
        }

        // ---------- Düzenleme ----------
        /// <summary>Hücredeki öğeyi envantere alır (önce tuzak, sonra yol/yapı). Yolun üstündeki tuzak önce alınmalıdır.</summary>
        public DraftItem PickUp(GridPos p, out string error)
        {
            error = null;
            if (Traps.TryGetValue(p, out var trap)) { Traps.Remove(p); Inventory.Add(trap); return trap; }
            if (!Cells.TryGetValue(p, out var it)) { error = "Burada bir şey yok"; return null; }
            if (it.Locked) { error = "Bu yol geliştiriliyor, taşınamaz"; return null; }
            Cells.Remove(p);
            if (it.Kind == DraftKind.Road) Entrances.Remove(p);
            Inventory.Add(it);
            return it;
        }

        /// <summary>Köydeki her şeyi envantere alır (tuzaklar, yollar, binalar, kuleler; girişler kalkar). Geliştirilmekte olan yollar yerinde kalır. Dönen: alınan öğe sayısı.</summary>
        public int PickUpAll()
        {
            int n = 0;
            foreach (var p in new List<GridPos>(Traps.Keys)) { Inventory.Add(Traps[p]); Traps.Remove(p); n++; }
            var keys = new List<GridPos>(Cells.Keys);
            keys.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            foreach (var p in keys)
            {
                var it = Cells[p];
                if (it.Locked) continue;
                Cells.Remove(p);
                Entrances.Remove(p);
                Inventory.Add(it);
                n++;
            }
            return n;
        }

        /// <summary>Envanterdeki öğeyi hücreye koyar. Tuzak yalnızca yola (giriş hariç), diğerleri boş hücreye.</summary>
        public bool Place(DraftItem it, GridPos p, out string error)
        {
            error = null;
            if (it == null || !Inventory.Contains(it)) { error = "Öğe envanterde değil"; return false; }
            if (p.X < 0 || p.Y < 0 || p.X >= Size || p.Y >= Size) { error = "Köy alanının dışı"; return false; }
            if (it.Kind == DraftKind.Trap)
            {
                if (!Cells.TryGetValue(p, out var road) || road.Kind != DraftKind.Road) { error = "Tuzak yalnızca yola konur"; return false; }
                if (Entrances.Contains(p)) { error = "Girişe tuzak konmaz"; return false; }
                if (Traps.ContainsKey(p)) { error = "Bu yolda zaten tuzak var"; return false; }
                Inventory.Remove(it);
                Traps[p] = it;
                return true;
            }
            if (Cells.ContainsKey(p)) { error = "Hücre dolu"; return false; }
            Inventory.Remove(it);
            Cells[p] = it;
            return true;
        }

        /// <summary>Mağazadan yeni öğe (envantere). Açılma seviyesi, sınırlar ve toplam bedel denetlenir; bedel onayda ödenir.</summary>
        public DraftItem AddNew(DraftKind kind, int type, out string error)
        {
            error = null;
            int lvl = g.Level;
            var it = new DraftItem { Kind = kind };
            switch (kind)
            {
                case DraftKind.Building:
                    {
                        var t = (BuildingType)type; var d = ContentCatalog.Building(t);
                        if (lvl < d.UnlockLevel) { error = d.Name + " Sv " + d.UnlockLevel + "'de açılır"; return null; }
                        int max = g.MaxCountOf(t);
                        if (CountBuildings(t) >= max) { error = d.Name + " sınırı " + max + LimitHint(t); return null; }
                        it.Building = t; break;
                    }
                case DraftKind.Tower:
                    {
                        var t = (TowerType)type; var d = ContentCatalog.Tower(t);
                        if (lvl < d.UnlockLevel) { error = d.Name + " Sv " + d.UnlockLevel + "'de açılır"; return null; }
                        if (TowerCount >= g.MaxTowers) { error = "Kule sınırı " + g.MaxTowers + (g.MaxTowers < GameRules.MaxTowerSlots ? " (Akademi: Kule Hakkı araştırmalarıyla artar)" : ""); return null; }
                        it.Tower = t; break;
                    }
                case DraftKind.Trap:
                    {
                        var t = (TrapType)type; var d = TrapCatalog.Get(t);
                        if (d == null) { error = "Geçersiz tuzak"; return null; }
                        if (lvl < d.UnlockLevel) { error = d.Name + " tuzağı Sv " + d.UnlockLevel + "'te açılır"; return null; }
                        if (TrapCount >= g.MaxTraps) { error = "Tuzak hakkı " + g.MaxTraps + (g.MaxTraps < GameRules.ResearchTrapSlots ? " (Akademi: Savunma'da Tuzak Hakkı ile " + GameRules.ResearchTrapSlots + ")" : ""); return null; }
                        it.Trap = t; break;
                    }
                default: error = "Yol hakkı seviyeyle artar"; return null;
            }
            var total = Add(NewCost(), CostOf(it));
            if (!g.Wallet.CanAfford(total, lvl, SpendArea.Building, out error)) return null;
            it.Id = nextId++;
            Inventory.Add(it);
            return it;
        }

        private string LimitHint(BuildingType t)
        {
            if (g.LimitHint(t) != "") return g.LimitHint(t);
            if (t == BuildingType.Barracks && g.Level < 26) return g.Level < 13 ? " (2. kışla Sv13'te)" : " (3. kışla Sv26'da)";
            return "";
        }

        /// <summary>Henüz ödenmemiş yeni satın almayı iptal et. Var olan yapı/kule/tuzak/yol satılamaz (approved #99).</summary>
        public bool CancelPurchase(DraftItem it, out string error)
        {
            error = null;
            if (it == null) { error = "Öğe yok"; return false; }
            if (!it.IsNew) { error = "Yapılar satılamaz"; return false; }
            if (!RemoveEverywhere(it)) { error = "Öğe bulunamadı"; return false; }
            return true;
        }

        private bool RemoveEverywhere(DraftItem it)
        {
            if (Inventory.Remove(it)) return true;
            foreach (var kv in Cells) if (kv.Value == it) { Cells.Remove(kv.Key); return true; }
            foreach (var kv in Traps) if (kv.Value == it) { Traps.Remove(kv.Key); return true; }
            return false;
        }

        /// <summary>Alt kenardaki yolu giriş yap / girişi kaldır.</summary>
        public bool ToggleEntrance(GridPos p, out string error)
        {
            error = null;
            if (p.Y != 0 || p.X < 0 || p.X >= Size) { error = "Giriş yalnızca alt kenarda olabilir"; return false; }
            if (!Cells.TryGetValue(p, out var it) || it.Kind != DraftKind.Road) { error = "Önce buraya yol koy"; return false; }
            if (Entrances.Remove(p)) return true;
            if (Entrances.Count >= g.Layout.EntranceLimit) { error = "Giriş sınırı " + g.Layout.EntranceLimit; return false; }
            if (Traps.ContainsKey(p)) { error = "Girişte tuzak olamaz: önce tuzağı kaldır"; return false; }
            Entrances.Add(p);
            return true;
        }

        // ---------- Denetim / görünüm ----------
        public VillageLayout BuildLayout()
        {
            var L = new VillageLayout(Size, RoadLimit, g.Layout.EntranceLimit);
            string e;
            foreach (var kv in Cells) if (kv.Value.Kind == DraftKind.Road) L.TryPlace(kv.Key, CellKind.Road, out e, kv.Value.Tier);
            foreach (var p in Entrances) L.ToggleEntrance(p, out e);
            foreach (var kv in Cells)
            {
                if (kv.Value.Kind == DraftKind.Tower) L.TryPlace(kv.Key, CellKind.Tower, out e, RoadTier.Wood, false);
                else if (kv.Value.Kind == DraftKind.Building) L.TryPlace(kv.Key, CellKind.Building, out e);
            }
            return L;
        }

        /// <summary>Onaylanabilir mi? Envanter boş olmalı; yollar girişe bağlı, binalar ve kuleler yola bağlı olmalı.</summary>
        public bool Validate(out string error)
        {
            SyncRoadLimit();
            int roads = 0, other = 0; string firstName = null;
            foreach (var i in Inventory) { if (i.Kind == DraftKind.Road) roads++; else { other++; if (firstName == null) firstName = NameOf(i); } }
            if (other > 0) { error = "Envanterde yerleştirilmemiş " + (other == 1 ? firstName : other + " yapı (" + firstName + "...)") + " var"; return false; }
            if (roads > 0) { error = "Envanterde " + roads + " yol var: tüm yol haklarını kullanmalısın"; return false; }
            return BuildLayout().Validate(out error);
        }

        /// <summary>Kurala uymayan hücreler (arayüzde kırmızı): girişe ulaşmayan yollar, yola bağlı olmayan bina/kuleler.</summary>
        public HashSet<GridPos> ProblemCells()
        {
            var L = BuildLayout();
            var bad = new HashSet<GridPos>();
            var roads = new HashSet<GridPos>(L.CellsOf(CellKind.Road));
            var seen = new HashSet<GridPos>();
            var q = new Queue<GridPos>();
            foreach (var e in Entrances) if (roads.Contains(e) && seen.Add(e)) q.Enqueue(e);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var n in new[] { new GridPos(c.X + 1, c.Y), new GridPos(c.X - 1, c.Y), new GridPos(c.X, c.Y + 1), new GridPos(c.X, c.Y - 1) })
                    if (roads.Contains(n) && seen.Add(n)) q.Enqueue(n);
            }
            foreach (var r in roads) if (!seen.Contains(r)) bad.Add(r);
            foreach (var p in L.CellsOf(CellKind.Building)) if (!L.HasAdjacentRoad(p)) bad.Add(p);
            foreach (var p in L.CellsOf(CellKind.Tower)) if (!L.HasAdjacentRoad(p)) bad.Add(p);
            return bad;
        }

        /// <summary>Çizim için savunma görünümü (seviyeler köyden okunur; yeni yapılar Sv1).</summary>
        public DefenseSetup ToDefense()
        {
            var d = new DefenseSetup(BuildLayout());
            foreach (var kv in Cells)
            {
                var it = kv.Value;
                if (it.Kind == DraftKind.Building) d.Buildings[kv.Key] = new BuildingSpec(it.Building, LevelOf(it));
                else if (it.Kind == DraftKind.Tower)
                {
                    int perm = it.HasOrigin && g.Wears.TryGetValue(it.Origin, out var w) ? w.PermanentDamagePct : 0;
                    d.Towers[kv.Key] = new TowerSpec(it.Tower, LevelOf(it), perm);
                }
            }
            foreach (var kv in Traps) d.Traps[kv.Key] = kv.Value.Trap;
            return d;
        }

        /// <summary>Köydeki seviyesi (taslak öğesi için; yeni = 1).</summary>
        public int LevelOf(DraftItem it)
        {
            if (!it.HasOrigin) return 1;
            if (it.Kind == DraftKind.Building && g.Buildings.TryGetValue(it.Origin, out var b)) return b.Level;
            if (it.Kind == DraftKind.Tower && g.Towers.TryGetValue(it.Origin, out var t)) return t.Level;
            return 1;
        }
    }
}
