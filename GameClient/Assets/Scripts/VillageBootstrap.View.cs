using System.Collections.Generic;
using UnityEngine;
using GameCore.Combat;
using GameCore.Game;
using GameCore.Grid;
using GameCore.Village;

namespace GameClient
{
    public sealed partial class VillageBootstrap
    {
        private Material Mat(Color c)
        {
            if (!mats.TryGetValue(c, out var m))
            {
                m = new Material(baseMat);
                m.SetColor("_BaseColor", c);
                m.color = c;
                mats[c] = m;
            }
            return m;
        }

        private bool IsRoad(int x, int y) => layout.Get(new GridPos(x, y)) == CellKind.Road;

        private Transform envRoot;
        private int envSize = -1;

        /// <summary>Gökyüzü, ışık, sis ve ortam rengi: sıcak öğleden sonra köy havası.</summary>
        private void SetupAtmosphere()
        {
            VillageArt.Init(baseMat);
            cam.backgroundColor = new Color(0.56f, 0.76f, 0.92f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.86f, 0.90f, 1.0f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.72f, 0.64f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.32f, 0.27f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.66f, 0.80f, 0.90f);
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 75f;
            Light sun = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null) sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.82f);
            sun.intensity = 1.55f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            GraphicsFx.Setup(cam, sun); // son işleme: parlama, renk, kenar yumuşatma (Ayarlar'dan Yüksek/Düşük)
        }

        private void EnsureEnvironment()
        {
            if (envRoot != null && envSize == layout.Size) return;
            if (envRoot != null) Destroy(envRoot.gameObject);
            envRoot = new GameObject("Environment").transform;
            envSize = layout.Size;
            VillageArt.BuildEnvironment(envRoot, layout.Size, VillageLayout.AttackZoneRows);
            // küçük çevre süsleri (çiçek, taş, çimen) gölge düşürmesin: gölge geçişinde draw call yarıya iner
            foreach (var mr in envRoot.GetComponentsInChildren<MeshRenderer>())
                if (mr.bounds.size.magnitude < 0.45f) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            BatchStatic(envRoot.gameObject);
            if (GraphicsFx.High) AmbientLife.Create(envRoot, layout.Size, VillageLayout.AttackZoneRows); // kuşlar, bulut gölgeleri, kelebekler, su pırıltısı
        }

        /// <summary>Hareketsiz parçaları birleştirir (draw call azaltır); ArtMotion taşıyanlar ayrı kalır.</summary>
        private static void BatchStatic(GameObject root)
        {
            var list = new List<GameObject>();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
                if (r.GetComponentInParent<ArtMotion>() == null && r.GetComponentInParent<TrapAnim>() == null && r.GetComponentInParent<KickAnim>() == null) list.Add(r.gameObject); // hareketli parçalar birleştirilmez
            if (list.Count > 1) StaticBatchingUtility.Combine(list.ToArray(), root);
        }

        /// <summary>Çizilen tuzakların animasyonu (tetiklenince oynatılır).</summary>
        private readonly Dictionary<GridPos, TrapAnim> trapAnims = new Dictionary<GridPos, TrapAnim>();
        private GridPos trapPreview = new GridPos(-1, -1);

        private void Rebuild()
        {
            view = sim != null ? curTarget.Defense : (draft != null ? draft.ToDefense() : g.MakeDefense());
            problemCells = sim == null && draft != null ? draft.ProblemCells() : new HashSet<GridPos>();
            if (sim == null && draft != null) draftOk = draft.Validate(out draftError);
            EnsureEnvironment();
            if (viewRoot != null) Destroy(viewRoot.gameObject);
            viewRoot = new GameObject("View").transform;
            trapAnims.Clear(); towerViews.Clear();
            int n = layout.Size;
            bool building = sim == null && draft != null; // Köy Düzeni: hafif ızgara
            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                {
                    var p = new GridPos(x, y);
                    var c = new Vector3(x, 0f, y);
                    int seed = x * 31 + y * 17;
                    // zemin karosu: yumuşak çim, inşa modunda hafif ızgara
                    var gc = Color.Lerp(VillageArt.Grass, VillageArt.GrassDark, VillageArt.Rand(seed, 1) * 0.45f);
                    if (building && ((x + y) & 1) == 0) gc = Color.Lerp(gc, Color.white, 0.07f);
                    VillageArt.Part(viewRoot, VillageArt.Shape.Box, c + new Vector3(0, -0.05f, 0), new Vector3(building ? 0.96f : 1.0f, 0.1f, building ? 0.96f : 1.0f), VillageArt.GrassMat(gc));
                    if (layout.Get(p) == CellKind.Empty && VillageArt.Rand(seed, 3) < 0.55f)
                        for (int k = 0; k < 3; k++)
                            VillageArt.Part(viewRoot, VillageArt.Shape.Cone, c + new Vector3((VillageArt.Rand(seed, 10 + k) - 0.5f) * 0.7f, 0.04f, (VillageArt.Rand(seed, 20 + k) - 0.5f) * 0.7f),
                                new Vector3(0.035f, 0.1f, 0.035f), Color.Lerp(VillageArt.GrassDark, VillageArt.LeafLight, VillageArt.Rand(seed, 30 + k)));
                    switch (layout.Get(p))
                    {
                        case CellKind.Road:
                            var open = new[] { IsRoad(x, y + 1), IsRoad(x + 1, y), IsRoad(x, y - 1) || layout.IsEntrance(p), IsRoad(x - 1, y) };
                            VillageArt.BuildRoad(viewRoot, c, layout.GetRoadTier(p), open, seed);
                            if (view.Traps.TryGetValue(p, out var tr) && !(sim != null && sim.TrapUsed(p))) trapAnims[p] = VillageArt.BuildTrap(viewRoot, c, tr); // patlamış tuzak çizilmez
                            break;
                        case CellKind.Building:
                            if (IsDestroyed(p)) { VillageArt.BuildRubble(viewRoot, c, seed); break; }
                            {
                                var bt = view.Buildings.TryGetValue(p, out var bs) ? bs.Type : BuildingType.VillagerHouse;
                                int bl = bs != null ? bs.Level : 1;
                                var bT = VillageArt.BuildBuilding(viewRoot, bt, bl, seed, c);
                                if (bT != null) FxKit.BuildingIdle(bT, bt, c, ModelLibrary.WorldHeight(bT), bl); // baca dumanı, pırıltı, kıvılcım...
                                if (bT != null && DayCycle.Night > 0.5f) FxKit.NightGlow(bT, c + new Vector3(0f, ModelLibrary.WorldHeight(bT) * 0.45f, -0.3f)); // gece pencere ışığı
                                if (sim == null && g.IsUpgrading(JobKind.Building, p, SoldierType.Swordsman)) FxKit.Construction(viewRoot, c); // inşaat sürüyor
                            }
                            break;
                        case CellKind.Tower:
                            if (IsDestroyed(p)) { VillageArt.BuildRubble(viewRoot, c, seed); break; }
                            {
                                view.Towers.TryGetValue(p, out var ts);
                                int tl = ts != null ? ts.Level : 1;
                                bool dmg = ts != null && ts.PermanentDamagePct > 0;
                                var el = ts != null && ts.Type == TowerType.Archer ? ts.Element : TowerElement.None;
                                // elementli kule: kendine özgü model + tepede sürekli element efekti (FxKit)
                                Vector3 top = default;
                                var towerT = el != TowerElement.None ? ModelArt.TryBuildElementTower(viewRoot, el, tl, dmg, c, out top) : null;
                                if (towerT != null) FxKit.TowerIdle(towerT, el, top);
                                else
                                {
                                    towerT = VillageArt.BuildTower(viewRoot, ts != null ? ContentCatalog.ModelLook(ts.Type, ts.Element) : TowerType.Archer, tl, dmg, c);
                                    if (el != TowerElement.None) VillageArt.ElementAura(viewRoot, c, el, ModelLibrary.WorldHeight(towerT) + 0.18f); // model yoksa eski aura
                                }
                                towerViews[p] = towerT;
                                if (sim != null && towerT != null) towerT.gameObject.AddComponent<KickAnim>(); // savaşta geri teper (birleştirilmez)
                                if (ts != null && ts.DefElement != TowerElement.None) FxKit.DefenseAura(towerT, ts.DefElement, c, ts.Type == TowerType.Wall ? 0.5f : 0.42f);
                                if (sim == null && (g.IsUpgrading(JobKind.Tower, p, SoldierType.Swordsman) || g.IsUpgrading(JobKind.TowerElement, p, SoldierType.Swordsman))) FxKit.Construction(viewRoot, c);
                            }
                            break;
                    }
                    if (layout.IsEntrance(p)) VillageArt.BuildGate(viewRoot, c);
                    // Köy Düzeni: kurala uymayan hücre (kopuk yol / yola bağlı olmayan yapı) kırmızı
                    if (problemCells.Contains(p)) VillageArt.Part(viewRoot, VillageArt.Shape.Box, c + new Vector3(0, 0.012f, 0), new Vector3(0.98f, 0.02f, 0.98f), VillageArt.Mat(new Color(0.95f, 0.15f, 0.1f), 0.2f, 0.6f));
                    if (p.Equals(selCell) && sim == null) VillageArt.BuildSelection(viewRoot, c);
                }
            if (sim == null && draft != null)
                foreach (var p in ArrowCells())
                {
                    var k = layout.Get(p);
                    float hgt = k == CellKind.Tower ? 2.3f : (k == CellKind.Building ? 1.7f : 0.9f);
                    var grp = VillageArt.Group(viewRoot, "arrow", new Vector3(p.X, hgt, p.Y));
                    var red = VillageArt.Mat(new Color(1f, 0.25f, 0.12f), 0.3f, 1.2f);
                    VillageArt.Part(grp, VillageArt.Shape.Cone, new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.3f, 0.34f), red, new Vector3(180f, 0f, 0f));
                    VillageArt.Part(grp, VillageArt.Shape.Cyl, new Vector3(0f, 0.3f, 0f), new Vector3(0.13f, 0.22f, 0.13f), red);
                    VillageArt.Animate(grp.gameObject, ArtMotion.Kind.Bob, 4f, 0.12f);
                }
            BatchStatic(viewRoot.gameObject);
            dirty = false;
            if (sim == null) valid = g.VillageValid(out validationError);
            villagersDirty = true;
            if (trapPreview.X >= 0 && trapAnims.TryGetValue(trapPreview, out var pa) && pa != null) pa.Trigger(false); // köyde dokunulan tuzağın önizlemesi
            trapPreview = new GridPos(-1, -1);
        }

        private bool IsDestroyed(GridPos p)
        {
            if (sim == null) return false;
            foreach (var st in sim.Structures) if (st.Pos.Equals(p)) return !st.Alive;
            return false;
        }

        private static Color BuildingColor(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.Forest: return new Color(0.15f, 0.45f, 0.18f);
                case BuildingType.LumberDepot: return new Color(0.55f, 0.35f, 0.15f);
                case BuildingType.StoneQuarry: return new Color(0.5f, 0.5f, 0.52f);
                case BuildingType.StoneDepot: return new Color(0.7f, 0.7f, 0.72f);
                case BuildingType.GoldMine: return new Color(0.85f, 0.7f, 0.1f);
                case BuildingType.Bank: return new Color(1f, 0.85f, 0.2f);
                case BuildingType.IronMine: return new Color(0.35f, 0.38f, 0.45f);
                case BuildingType.IronDepot: return new Color(0.55f, 0.6f, 0.7f);
                case BuildingType.Barracks: return new Color(0.7f, 0.2f, 0.2f);
                case BuildingType.University: return new Color(0.25f, 0.4f, 0.8f);
                default: return new Color(0.8f, 0.7f, 0.5f);
            }
        }

        private static Color TowerColor(TowerType t)
        {
            switch (t)
            {
                case TowerType.Archer: return new Color(0.2f, 0.7f, 0.25f);
                case TowerType.Mage: return new Color(0.65f, 0.25f, 0.85f);
                case TowerType.Wall: return new Color(0.6f, 0.6f, 0.6f);
                default: return new Color(0.2f, 0.75f, 0.75f);
            }
        }

        private static Color TrapColor(TrapType t)
        {
            switch (t)
            {
                case TrapType.Spikes: return new Color(0.75f, 0.75f, 0.8f);
                case TrapType.Slippery: return new Color(0.5f, 0.8f, 1f);
                default: return new Color(1f, 0.45f, 0.1f);
            }
        }

        private static Color SoldierColor(SoldierType t) => VillageArt.TunicOf(t);

        private static string TrapName(TrapType t)
        {
            var d = TrapCatalog.Get(t);
            return d != null ? d.Name : "Tuzak";
        }
    }
}
