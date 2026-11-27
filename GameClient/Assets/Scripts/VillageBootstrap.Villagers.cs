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
            // sevimli köylüler (kullanıcı isteği 27.11.2026 "1i kız 1i erkek, köylü gibi giydir"): iri baş, nokta gözler,
            // pembe yanaklar; çiftler kız (önlüklü elbise, örgülü saç, başörtüsü), tekler erkek (keten gömlek, yelek, hasır şapka)
            bool girl = i % 2 == 0;
            var cloth = VillagerShirts[i % VillagerShirts.Length];
            var linen = new Color(0.92f, 0.86f, 0.72f);
            var hair = (i / 2) % 2 == 0 ? new Color(0.42f, 0.25f, 0.12f) : new Color(0.75f, 0.52f, 0.25f);
            var shoe = new Color(0.3f, 0.2f, 0.12f);
            foreach (var sx in new[] { -0.03f, 0.03f }) VillageArt.Part(body, VillageArt.Shape.Box, new Vector3(sx, 0.012f, 0.008f), new Vector3(0.035f, 0.024f, 0.05f), shoe); // ayakkabılar
            if (girl)
            {
                VillageArt.Part(body, VillageArt.Shape.Cone, new Vector3(0f, 0.1f, 0f), new Vector3(0.17f, 0.2f, 0.17f), cloth);                                  // etek
                VillageArt.Part(body, VillageArt.Shape.Capsule, new Vector3(0f, 0.16f, 0f), new Vector3(0.1f, 0.1f, 0.085f), cloth);                             // korsaj
                VillageArt.Part(body, VillageArt.Shape.Box, new Vector3(0f, 0.08f, 0.05f), new Vector3(0.075f, 0.1f, 0.008f), Color.white, new Vector3(-22f, 0f, 0f)); // önlük
                VillageArt.Part(body, VillageArt.Shape.Cyl, new Vector3(0f, 0.135f, 0f), new Vector3(0.1f, 0.006f, 0.09f), Color.white);                          // önlük bağı
            }
            else
            {
                VillageArt.Part(body, VillageArt.Shape.Cyl, new Vector3(0f, 0.06f, 0f), new Vector3(0.085f, 0.07f, 0.075f), new Color(0.45f, 0.32f, 0.2f));        // pantolon
                VillageArt.Part(body, VillageArt.Shape.Capsule, new Vector3(0f, 0.15f, 0f), new Vector3(0.105f, 0.12f, 0.09f), linen);                            // keten gömlek
                VillageArt.Part(body, VillageArt.Shape.Cyl, new Vector3(0f, 0.15f, 0f), new Vector3(0.112f, 0.06f, 0.095f), cloth);                               // yelek
                VillageArt.Part(body, VillageArt.Shape.Cyl, new Vector3(0f, 0.115f, 0f), new Vector3(0.11f, 0.01f, 0.095f), new Color(0.35f, 0.22f, 0.12f));      // kemer
            }
            // baş: iri, sevimli
            VillageArt.Part(body, VillageArt.Shape.Ball, new Vector3(0f, 0.27f, 0f), new Vector3(0.13f, 0.125f, 0.13f), VillageArt.Skin);
            foreach (var sx in new[] { -0.025f, 0.025f })
            {
                VillageArt.Part(body, VillageArt.Shape.Ball, new Vector3(sx, 0.276f, 0.058f), new Vector3(0.018f, 0.022f, 0.012f), VillageArt.Dark);              // göz
                VillageArt.Part(body, VillageArt.Shape.Ball, new Vector3(sx * 1.75f, 0.254f, 0.052f), new Vector3(0.02f, 0.012f, 0.01f), new Color(1f, 0.6f, 0.6f)); // yanak
            }
            if (girl)
            {
                VillageArt.Part(body, VillageArt.Shape.Ball, new Vector3(0f, 0.29f, -0.012f), new Vector3(0.138f, 0.12f, 0.13f), hair);                          // saç
                foreach (var sx in new[] { -0.058f, 0.058f })
                {
                    VillageArt.Part(body, VillageArt.Shape.Capsule, new Vector3(sx, 0.215f, -0.015f), new Vector3(0.03f, 0.08f, 0.03f), hair);                   // örgü
                    VillageArt.Part(body, VillageArt.Shape.Ball, new Vector3(sx, 0.18f, -0.015f), new Vector3(0.025f, 0.018f, 0.025f), cloth);                   // kurdele
                }
                VillageArt.Part(body, VillageArt.Shape.Ball, new Vector3(0f, 0.315f, -0.008f), new Vector3(0.142f, 0.075f, 0.135f), new Color(0.88f, 0.3f, 0.3f)); // başörtüsü
            }
            else
            {
                VillageArt.Part(body, VillageArt.Shape.Ball, new Vector3(0f, 0.3f, -0.014f), new Vector3(0.136f, 0.09f, 0.128f), hair);                           // saç
                var straw = new Color(0.86f, 0.74f, 0.42f);
                VillageArt.Part(body, VillageArt.Shape.Cyl, new Vector3(0f, 0.33f, 0f), new Vector3(0.21f, 0.01f, 0.21f), straw);                                 // hasır şapka kenarı
                VillageArt.Part(body, VillageArt.Shape.Cyl, new Vector3(0f, 0.355f, 0f), new Vector3(0.1f, 0.05f, 0.1f), straw);
                VillageArt.Part(body, VillageArt.Shape.Cyl, new Vector3(0f, 0.34f, 0f), new Vector3(0.104f, 0.012f, 0.104f), new Color(0.75f, 0.2f, 0.18f));     // şapka bandı
            }
            VillageArt.Part(body, VillageArt.Shape.Capsule, new Vector3(-0.062f, 0.15f, 0f), new Vector3(0.028f, 0.07f, 0.028f), linen, new Vector3(0f, 0f, -12f)); // sol kol
            VillageArt.Part(body, VillageArt.Shape.Ball, new Vector3(-0.07f, 0.11f, 0f), Vector3.one * 0.025f, VillageArt.Skin);
            var arm = new GameObject("arm").transform;
            arm.SetParent(body, false);
            arm.localPosition = new Vector3(0.062f, 0.19f, 0f);
            VillageArt.Part(arm, VillageArt.Shape.Capsule, new Vector3(0f, -0.035f, 0f), new Vector3(0.028f, 0.07f, 0.028f), linen);
            VillageArt.Part(arm, VillageArt.Shape.Ball, new Vector3(0f, -0.075f, 0.005f), Vector3.one * 0.025f, VillageArt.Skin);                                // el
            VillageArt.Part(arm, VillageArt.Shape.Cyl, new Vector3(0f, -0.075f, 0.05f), new Vector3(0.012f, 0.045f, 0.012f), VillageArt.Wood, new Vector3(90f, 0f, 0f)); // çekiç sapı
            VillageArt.Part(arm, VillageArt.Shape.Box, new Vector3(0f, -0.075f, 0.095f), new Vector3(0.04f, 0.025f, 0.025f), VillageArt.Iron);                    // çekiç başı
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
