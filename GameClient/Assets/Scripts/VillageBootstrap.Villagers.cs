using System.Collections.Generic;
using UnityEngine;
using GameCore.Game;
using GameCore.Grid;
using GameCore.Village;

namespace GameClient
{
    /// <summary>
    /// Köylüler (kullanıcı isteği 15.11.2026): her işçi köyde bir figür. Boştaki köylü yollarda dolaşır,
    /// çalışan köylü yükseltilen yapının yanında çekiç sallar. Yalnızca görsel; kurallar Core'dadır.
    /// </summary>
    public sealed partial class VillageBootstrap
    {
        private sealed class Villager
        {
            public Transform Root, Body, Arm;
            public Vector3 Pos, Target;
            public float Wait;
            public bool Working;
        }

        private readonly List<Villager> villagers = new List<Villager>();
        private Transform villagerRoot;
        private bool villagersDirty = true;
        private readonly System.Random villagerRng = new System.Random(7);
        private static readonly Color[] VillagerShirts =
        {
            new Color(0.75f, 0.35f, 0.22f), new Color(0.30f, 0.50f, 0.75f), new Color(0.45f, 0.62f, 0.30f),
            new Color(0.80f, 0.65f, 0.25f), new Color(0.55f, 0.35f, 0.60f), new Color(0.35f, 0.60f, 0.60f)
        };

        private void UpdateVillagers()
        {
            bool show = sim == null && draft == null && g != null && view != null;
            if (villagerRoot == null) villagerRoot = new GameObject("Villagers").transform;
            villagerRoot.gameObject.SetActive(show);
            if (!show) return;

            int want = g.Builders;
            while (villagers.Count < want) villagers.Add(MakeVillager(villagers.Count));
            while (villagers.Count > want) { Destroy(villagers[villagers.Count - 1].Root.gameObject); villagers.RemoveAt(villagers.Count - 1); }

            // iş yerleri: yükseltilen yapı/kule/yol (asker geliştirme kışlada yapılır)
            var sites = new List<GridPos>();
            foreach (var j in g.Jobs)
            {
                if (j.Kind != JobKind.Soldier) sites.Add(j.Pos);
                else foreach (var kv in g.Buildings) if (kv.Value.Type == GameCore.Combat.BuildingType.Barracks) { sites.Add(kv.Key); break; }
            }
            var roads = layout.CellsOf(CellKind.Road);
            float dt = Time.deltaTime;
            for (int i = 0; i < villagers.Count; i++)
            {
                var v = villagers[i];
                bool work = i < sites.Count;
                if (work)
                {
                    var s = sites[i];
                    v.Target = new Vector3(s.X + 0.42f * (i % 2 == 0 ? 1 : -1), 0f, s.Y - 0.42f);
                }
                else if (v.Working || villagersDirty && !OnRoad(v.Target) || (v.Pos - v.Target).sqrMagnitude < 0.0025f && (v.Wait -= dt) <= 0f)
                {
                    // dolaşma: rastgele bir yol karesine yürü, varınca biraz bekle
                    if (roads.Count > 0)
                    {
                        var r = roads[villagerRng.Next(roads.Count)];
                        v.Target = new Vector3(r.X + (float)(villagerRng.NextDouble() - 0.5) * 0.4f, 0f, r.Y + (float)(villagerRng.NextDouble() - 0.5) * 0.4f);
                    }
                    v.Wait = 1f + (float)villagerRng.NextDouble() * 2.5f;
                }
                v.Working = work;
                var to = v.Target - v.Pos;
                float dist = to.magnitude;
                float speed = work ? 1.4f : 0.6f;
                bool walking = dist > 0.03f;
                if (walking)
                {
                    v.Pos += to / dist * Mathf.Min(dist, speed * dt);
                    v.Root.rotation = Quaternion.Slerp(v.Root.rotation, Quaternion.LookRotation(new Vector3(to.x, 0f, to.z)), 10f * dt);
                }
                else if (work && sites.Count > i)
                {
                    var s = sites[i];
                    var face = new Vector3(s.X, 0f, s.Y) - v.Pos;
                    if (face.sqrMagnitude > 0.001f) v.Root.rotation = Quaternion.Slerp(v.Root.rotation, Quaternion.LookRotation(face), 10f * dt);
                }
                v.Root.position = v.Pos;
                float t = Time.time * (walking ? 9f : 6f) + i;
                v.Body.localPosition = new Vector3(0f, walking ? Mathf.Abs(Mathf.Sin(t)) * 0.03f : 0f, 0f);
                v.Arm.localRotation = Quaternion.Euler(work && !walking ? -60f + Mathf.Sin(t * 1.5f) * 50f : (walking ? Mathf.Sin(t) * 30f : 0f), 0f, 0f);
            }
            villagersDirty = false;
        }

        private bool OnRoad(Vector3 p) => layout.Get(new GridPos(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z))) == CellKind.Road;

        private Villager MakeVillager(int i)
        {
            var root = new GameObject("Villager" + i).transform;
            root.SetParent(villagerRoot, false);
            root.localScale = Vector3.one * 1.6f; // binaların yanında seçilebilsin
            var body = new GameObject("body").transform;
            body.SetParent(root, false);
            var shirt = VillagerShirts[i % VillagerShirts.Length];
            VillageArt.Part(body, VillageArt.Shape.Cyl, new Vector3(0f, 0.07f, 0f), new Vector3(0.09f, 0.07f, 0.09f), VillageArt.WoodDark);       // pantolon
            VillageArt.Part(body, VillageArt.Shape.Capsule, new Vector3(0f, 0.19f, 0f), new Vector3(0.12f, 0.1f, 0.1f), shirt);                // gövde
            VillageArt.Part(body, VillageArt.Shape.Ball, new Vector3(0f, 0.31f, 0f), new Vector3(0.09f, 0.09f, 0.09f), VillageArt.Skin);       // baş
            VillageArt.Part(body, VillageArt.Shape.Cone, new Vector3(0f, 0.37f, 0f), new Vector3(0.13f, 0.05f, 0.13f), new Color(0.82f, 0.70f, 0.40f)); // hasır şapka
            var arm = new GameObject("arm").transform;
            arm.SetParent(body, false);
            arm.localPosition = new Vector3(0.07f, 0.24f, 0f);
            VillageArt.Part(arm, VillageArt.Shape.Cyl, new Vector3(0f, -0.05f, 0.02f), new Vector3(0.03f, 0.05f, 0.03f), shirt);
            VillageArt.Part(arm, VillageArt.Shape.Cyl, new Vector3(0f, -0.06f, 0.08f), new Vector3(0.015f, 0.06f, 0.015f), VillageArt.Wood, new Vector3(90f, 0f, 0f)); // çekiç sapı
            VillageArt.Part(arm, VillageArt.Shape.Box, new Vector3(0f, -0.06f, 0.14f), new Vector3(0.05f, 0.03f, 0.03f), VillageArt.Iron);     // çekiç başı
            // başlangıç: bir yol karesi
            var roads = layout.CellsOf(CellKind.Road);
            var start = roads.Count > 0 ? roads[villagerRng.Next(roads.Count)] : new GridPos(layout.Size / 2, 1);
            var v = new Villager { Root = root, Body = body, Arm = arm, Pos = new Vector3(start.X, 0f, start.Y) };
            v.Target = v.Pos;
            root.position = v.Pos;
            return v;
        }
    }
}
