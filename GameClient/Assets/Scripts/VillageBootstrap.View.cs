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

        private void Cube(GridPos p, Vector3 scale, float centerY, Color c) => Cube(new Vector3(p.X, centerY, p.Y), scale, c);

        private void Cube(Vector3 pos, Vector3 scale, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(viewRoot, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(c);
        }

        private bool IsRoad(int x, int y) => layout.Get(new GridPos(x, y)) == CellKind.Road;

        /// <summary>Komşu yollara otomatik bağlanan yol parçası: merkez + bağlı yönlere kollar.</summary>
        private void DrawRoad(GridPos p)
        {
            var tier = layout.GetRoadTier(p);
            Color baseC, rail, stripe;
            switch (tier)
            {
                case RoadTier.Reinforced:
                    baseC = new Color(0.66f, 0.52f, 0.30f); rail = new Color(0.92f, 0.74f, 0.20f); stripe = new Color(0.50f, 0.38f, 0.20f); break;
                case RoadTier.Iron:
                    baseC = new Color(0.40f, 0.42f, 0.47f); rail = new Color(0.72f, 0.74f, 0.80f); stripe = new Color(0.30f, 0.32f, 0.36f); break;
                default:
                    baseC = new Color(0.62f, 0.50f, 0.34f); rail = baseC; stripe = new Color(0.44f, 0.34f, 0.21f); break;
            }
            const float slabH = 0.14f, slabY = 0.02f;
            var c = new Vector3(p.X, 0f, p.Y);
            Cube(c + new Vector3(0, slabY, 0), new Vector3(0.5f, slabH, 0.5f), baseC);
            if (tier != RoadTier.Wood) Cube(c + new Vector3(0, 0.1f, 0), new Vector3(0.16f, 0.1f, 0.16f), rail);
            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };
            bool[] open = { IsRoad(p.X, p.Y + 1), IsRoad(p.X + 1, p.Y), IsRoad(p.X, p.Y - 1) || layout.IsEntrance(p), IsRoad(p.X - 1, p.Y) };
            for (int i = 0; i < 4; i++)
            {
                if (!open[i]) continue;
                bool alongX = dx[i] != 0;
                var dir = new Vector3(dx[i], 0, dy[i]);
                var armPos = c + dir * 0.375f + new Vector3(0, slabY, 0);
                var armSize = alongX ? new Vector3(0.25f, slabH, 0.5f) : new Vector3(0.5f, slabH, 0.25f);
                Cube(armPos, armSize, baseC);
                if (tier == RoadTier.Wood)
                {
                    var sSize = alongX ? new Vector3(0.04f, slabH + 0.01f, 0.5f) : new Vector3(0.5f, slabH + 0.01f, 0.04f);
                    Cube(c + dir * 0.375f + new Vector3(0, slabY, 0), sSize, stripe);
                }
                else
                {
                    var perp = alongX ? new Vector3(0, 0, 1) : new Vector3(1, 0, 0);
                    var rSize = alongX ? new Vector3(0.25f, 0.2f, 0.05f) : new Vector3(0.05f, 0.2f, 0.25f);
                    Cube(armPos + perp * 0.22f + new Vector3(0, 0.03f, 0), rSize, rail);
                    Cube(armPos - perp * 0.22f + new Vector3(0, 0.03f, 0), rSize, rail);
                }
            }
        }

        private void Rebuild()
        {
            view = sim != null ? curTarget.Defense : g.MakeDefense();
            if (viewRoot != null) Destroy(viewRoot.gameObject);
            viewRoot = new GameObject("View").transform;
            int n = layout.Size;
            var grass1 = new Color(0.30f, 0.55f, 0.30f);
            var grass2 = new Color(0.27f, 0.50f, 0.27f);
            var edge = new Color(0.9f, 0.8f, 0.3f);
            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                {
                    var p = new GridPos(x, y);
                    var gc = ((x + y) & 1) == 0 ? grass1 : grass2;
                    if (y == 0) gc = Color.Lerp(gc, edge, 0.35f);
                    Cube(p, new Vector3(0.96f, 0.1f, 0.96f), -0.05f, gc);
                    switch (layout.Get(p))
                    {
                        case CellKind.Road:
                            DrawRoad(p);
                            if (view.Traps.TryGetValue(p, out var tr)) Cube(p, new Vector3(0.3f, 0.05f, 0.3f), 0.13f, TrapColor(tr));
                            break;
                        case CellKind.Building:
                            if (IsDestroyed(p)) { Cube(p, new Vector3(0.6f, 0.15f, 0.6f), 0.08f, new Color(0.2f, 0.18f, 0.16f)); break; }
                            {
                                var bt = view.Buildings.TryGetValue(p, out var bs) ? bs.Type : BuildingType.VillagerHouse;
                                int bl = bs != null ? bs.Level : 1;
                                float h = 0.5f + Mathf.Min(0.6f, bl * 0.03f);
                                Cube(p, new Vector3(0.7f, h, 0.7f), h * 0.5f, BuildingColor(bt));
                                if (p.Equals(selCell) && sim == null) Cube(p, new Vector3(0.9f, 0.05f, 0.9f), 0.02f, Color.yellow);
                            }
                            break;
                        case CellKind.Tower:
                            if (IsDestroyed(p)) { Cube(p, new Vector3(0.6f, 0.15f, 0.6f), 0.08f, new Color(0.2f, 0.18f, 0.16f)); break; }
                            {
                                var tt = view.Towers.TryGetValue(p, out var ts) ? ts.Type : TowerType.Archer;
                                int perm = ts != null ? ts.PermanentDamagePct : 0;
                                var body = perm > 0 ? new Color(0.38f, 0.32f, 0.32f) : new Color(0.55f, 0.58f, 0.65f);
                                float th = 1.0f + Mathf.Min(0.8f, (ts != null ? ts.Level : 1) * 0.03f);
                                Cube(p, new Vector3(0.45f, th, 0.45f), th * 0.5f, body);
                                Cube(p, new Vector3(0.65f, 0.2f, 0.65f), th + 0.1f, TowerColor(tt));
                                if (p.Equals(selCell) && sim == null) Cube(p, new Vector3(0.9f, 0.05f, 0.9f), 0.02f, Color.yellow);
                            }
                            break;
                    }
                    if (layout.IsEntrance(p)) Cube(p, new Vector3(0.5f, 0.2f, 0.5f), 0.15f, new Color(0.95f, 0.45f, 0.1f));
                }
            var z1 = new Color(0.55f, 0.25f, 0.22f);
            var z2 = new Color(0.48f, 0.21f, 0.19f);
            for (int x = 0; x < n; x++)
                for (int r = 1; r <= VillageLayout.AttackZoneRows; r++)
                    Cube(new GridPos(x, -r), new Vector3(0.96f, 0.1f, 0.96f), -0.05f, ((x + r) & 1) == 0 ? z1 : z2);
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

        private static Color SoldierColor(SoldierType t)
        {
            switch (t)
            {
                case SoldierType.Swordsman: return new Color(0.75f, 0.8f, 0.95f);
                case SoldierType.Archer: return new Color(0.4f, 0.9f, 0.4f);
                case SoldierType.Mage: return new Color(0.8f, 0.4f, 1f);
                case SoldierType.Goblin: return new Color(0.3f, 0.55f, 0.15f);
                case SoldierType.Cavalry: return new Color(0.6f, 0.35f, 0.15f);
                case SoldierType.Spearman: return new Color(1f, 0.65f, 0.2f);
                default: return new Color(1f, 0.6f, 0.85f);
            }
        }

        private static string TrapName(TrapType t)
        {
            switch (t) { case TrapType.Spikes: return "Çivili"; case TrapType.Slippery: return "Kaygan"; default: return "Ateşli"; }
        }
    }
}
