using System.Collections.Generic;
using UnityEngine;
using GameCore.Combat;
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
        }

        private void EnsureEnvironment()
        {
            if (envRoot != null && envSize == layout.Size) return;
            if (envRoot != null) Destroy(envRoot.gameObject);
            envRoot = new GameObject("Environment").transform;
            envSize = layout.Size;
            VillageArt.BuildEnvironment(envRoot, layout.Size, VillageLayout.AttackZoneRows);
            BatchStatic(envRoot.gameObject);
        }

        /// <summary>Hareketsiz parçaları birleştirir (draw call azaltır); ArtMotion taşıyanlar ayrı kalır.</summary>
        private static void BatchStatic(GameObject root)
        {
            var list = new List<GameObject>();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
                if (r.GetComponentInParent<ArtMotion>() == null) list.Add(r.gameObject);
            if (list.Count > 1) StaticBatchingUtility.Combine(list.ToArray(), root);
        }

        private void Rebuild()
        {
            view = sim != null ? curTarget.Defense : g.MakeDefense();
            EnsureEnvironment();
            if (viewRoot != null) Destroy(viewRoot.gameObject);
            viewRoot = new GameObject("View").transform;
            int n = layout.Size;
            bool building = sim == null && tab == Tab.Village && tool != Tool.Select && tool != Tool.Pan;
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
                    switch (layout.Get(p))
                    {
                        case CellKind.Road:
                            var open = new[] { IsRoad(x, y + 1), IsRoad(x + 1, y), IsRoad(x, y - 1) || layout.IsEntrance(p), IsRoad(x - 1, y) };
                            VillageArt.BuildRoad(viewRoot, c, layout.GetRoadTier(p), open, seed);
                            if (view.Traps.TryGetValue(p, out var tr)) VillageArt.BuildTrap(viewRoot, c, tr);
                            break;
                        case CellKind.Building:
                            if (IsDestroyed(p)) { VillageArt.BuildRubble(viewRoot, c, seed); break; }
                            {
                                var bt = view.Buildings.TryGetValue(p, out var bs) ? bs.Type : BuildingType.VillagerHouse;
                                VillageArt.BuildBuilding(viewRoot, bt, bs != null ? bs.Level : 1, seed, c);
                            }
                            break;
                        case CellKind.Tower:
                            if (IsDestroyed(p)) { VillageArt.BuildRubble(viewRoot, c, seed); break; }
                            {
                                var tt = view.Towers.TryGetValue(p, out var ts) ? ts.Type : TowerType.Archer;
                                VillageArt.BuildTower(viewRoot, tt, ts != null ? ts.Level : 1, ts != null && ts.PermanentDamagePct > 0, c);
                            }
                            break;
                    }
                    if (layout.IsEntrance(p)) VillageArt.BuildGate(viewRoot, c);
                    if (p.Equals(selCell) && sim == null) VillageArt.BuildSelection(viewRoot, c);
                }
            BatchStatic(viewRoot.gameObject);
            dirty = false;
            if (sim == null) valid = g.VillageValid(out validationError);
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
                case TowerType.Spear: return new Color(0.8f, 0.5f, 0.2f);
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
            switch (t) { case TrapType.Spikes: return "Çivili"; case TrapType.Slippery: return "Kaygan"; default: return "Ateşli"; }
        }
    }
}
