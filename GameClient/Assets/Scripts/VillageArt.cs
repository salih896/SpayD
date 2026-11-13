using System.Collections.Generic;
using UnityEngine;
using GameCore.Combat;
using GameCore.Village;

namespace GameClient
{
    /// <summary>
    /// Kodla üretilen düşük poligonlu sanat (dış dosya/asset yok): bina, kule, asker, yol, tuzak, çevre.
    /// Tüm modeller hücre merkezinde, zemin y=0 üstünde kurulur; kamera -X/-Z yönünden baktığı için kapı/pencere o yüzlerdedir.
    /// Oyun kuralı içermez; yalnızca görünüm. Sonradan gerçek modellerle değiştirilebilir (aynı çağrı noktaları).
    /// </summary>
    public static class VillageArt
    {
        public enum Shape { Box, Cyl, Ball, Capsule, Cone, Roof, Pyramid }

        // ---------- Palet ----------
        public static readonly Color Grass = new Color(0.50f, 0.72f, 0.34f), GrassDark = new Color(0.40f, 0.62f, 0.28f);
        public static readonly Color Meadow = new Color(0.47f, 0.64f, 0.33f);
        public static readonly Color Dirt = new Color(0.62f, 0.48f, 0.32f), DirtDark = new Color(0.50f, 0.38f, 0.25f);
        public static readonly Color Wood = new Color(0.55f, 0.36f, 0.20f), WoodDark = new Color(0.38f, 0.24f, 0.13f), WoodLight = new Color(0.72f, 0.52f, 0.31f);
        public static readonly Color Stone = new Color(0.66f, 0.66f, 0.64f), StoneDark = new Color(0.46f, 0.46f, 0.47f), StoneLight = new Color(0.80f, 0.79f, 0.75f);
        public static readonly Color Plaster = new Color(0.93f, 0.87f, 0.74f), RoofRed = new Color(0.72f, 0.27f, 0.18f), RoofBlue = new Color(0.25f, 0.40f, 0.70f);
        public static readonly Color Leaf = new Color(0.22f, 0.50f, 0.20f), LeafLight = new Color(0.36f, 0.62f, 0.26f), Pine = new Color(0.16f, 0.40f, 0.22f);
        public static readonly Color GoldC = new Color(1.00f, 0.80f, 0.20f), Iron = new Color(0.45f, 0.50f, 0.58f), Skin = new Color(0.96f, 0.78f, 0.62f);
        public static readonly Color Glass = new Color(0.30f, 0.45f, 0.60f), Dark = new Color(0.16f, 0.12f, 0.10f);

        private static Material baseMat;
        private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        private static Mesh cube, cyl, ball, capsule, cone, roof, pyramid;
        private static Texture2D grassTex, dirtTex;

        public static void Init(Material urpLit)
        {
            baseMat = urpLit;
            if (cube != null) return;
            cube = PrimMesh(PrimitiveType.Cube); cyl = PrimMesh(PrimitiveType.Cylinder);
            ball = PrimMesh(PrimitiveType.Sphere); capsule = PrimMesh(PrimitiveType.Capsule);
            cone = MakeCone(12); roof = MakeRoof(); pyramid = MakePyramid();
            grassTex = NoiseTex(64, 0.82f, 1.08f, 11);
            dirtTex = NoiseTex(64, 0.75f, 1.12f, 23);
        }

        private static Mesh PrimMesh(PrimitiveType t)
        {
            var go = GameObject.CreatePrimitive(t);
            var m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            return m;
        }

        // ---------- Materyal ----------
        public static Material Mat(Color c, float smooth = 0.12f, float emit = 0f, Texture2D tex = null, float tile = 1f)
        {
            string key = ColorUtility.ToHtmlStringRGBA(c) + "|" + smooth.ToString("0.00") + "|" + emit.ToString("0.0") + "|" + (tex != null ? tex.GetInstanceID() : 0) + "|" + tile;
            if (mats.TryGetValue(key, out var m)) return m;
            m = new Material(baseMat);
            m.SetColor("_BaseColor", c);
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTextureScale = new Vector2(tile, tile); m.SetTextureScale("_BaseMap", new Vector2(tile, tile)); }
            if (emit > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor("_EmissionColor", c * emit);
            }
            mats[key] = m;
            return m;
        }

        public static Material GrassMat(Color c, float tile = 1f) => Mat(c, 0.05f, 0f, grassTex, tile);
        public static Material DirtMat(Color c, float tile = 1f) => Mat(c, 0.05f, 0f, dirtTex, tile);

        private static Texture2D NoiseTex(int n, float lo, float hi, int seed)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = Mathf.PerlinNoise(x * 0.18f + seed, y * 0.18f + seed) * 0.6f + Rand(x * 131 + y, seed) * 0.4f;
                    float k = Mathf.Lerp(lo, hi, v);
                    px[y * n + x] = new Color(k, k, k, 1f);
                }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        /// <summary>Deterministik [0,1) sözde rastgele (görsel çeşitlilik için; oyun kuralında kullanılmaz).</summary>
        public static float Rand(int a, int b)
        {
            float s = Mathf.Sin(a * 12.9898f + b * 78.233f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        // ---------- Parça ----------
        public static Transform Group(Transform parent, string name, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            return go.transform;
        }

        /// <summary>size = gerçek boyut (genişlik, yükseklik, derinlik); pos = merkez.</summary>
        public static GameObject Part(Transform parent, Shape s, Vector3 pos, Vector3 size, Material m, Vector3 euler = default)
        {
            var go = new GameObject(s.ToString());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            Mesh mesh;
            Vector3 sc = size;
            switch (s)
            {
                case Shape.Cyl: mesh = cyl; sc.y = size.y * 0.5f; break;
                case Shape.Ball: mesh = ball; break;
                case Shape.Capsule: mesh = capsule; sc.y = size.y * 0.5f; break;
                case Shape.Cone: mesh = cone; break;
                case Shape.Roof: mesh = roof; break;
                case Shape.Pyramid: mesh = pyramid; break;
                default: mesh = cube; break;
            }
            go.transform.localScale = sc;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            return go;
        }

        public static GameObject Part(Transform parent, Shape s, Vector3 pos, Vector3 size, Color c, Vector3 euler = default)
            => Part(parent, s, pos, size, Mat(c), euler);

        public static void Animate(GameObject go, ArtMotion.Kind k, float speed = 1f, float amount = 0.05f, float phase = 0f)
        {
            var m = go.AddComponent<ArtMotion>();
            m.Mode = k; m.Speed = speed; m.Amount = amount; m.Phase = phase;
        }

        // ---------- Özel ağlar ----------
        private static Mesh FromTris(List<Vector3> v, Vector3 center)
        {
            var verts = new List<Vector3>(v.Count);
            for (int i = 0; i < v.Count; i += 3)
            {
                Vector3 a = v[i], b = v[i + 1], c = v[i + 2];
                var n = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(n, (a + b + c) / 3f - center) < 0f) { var t = b; b = c; c = t; }
                verts.Add(a); verts.Add(b); verts.Add(c);
            }
            var idx = new int[verts.Count];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;
            var m = new Mesh();
            m.SetVertices(verts);
            m.SetTriangles(idx, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh MakeCone(int n)
        {
            var v = new List<Vector3>();
            var apex = new Vector3(0, 0.5f, 0); var bc = new Vector3(0, -0.5f, 0);
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, -0.5f, Mathf.Sin(a0) * 0.5f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, -0.5f, Mathf.Sin(a1) * 0.5f);
                v.Add(apex); v.Add(p0); v.Add(p1);
                v.Add(bc); v.Add(p1); v.Add(p0);
            }
            // taban üçgenleri merkeze göre içe dönük kalmasın diye merkez biraz yukarıda
            return FromTris(v, new Vector3(0, -0.1f, 0));
        }

        private static Mesh MakeRoof()
        {
            // x: -0.5..0.5, z: -0.5..0.5, y: -0.5 taban, sırt x=0 y=0.5 boyunca z
            var a = new Vector3(-0.5f, -0.5f, -0.5f); var b = new Vector3(0.5f, -0.5f, -0.5f);
            var c = new Vector3(0.5f, -0.5f, 0.5f); var d = new Vector3(-0.5f, -0.5f, 0.5f);
            var r0 = new Vector3(0, 0.5f, -0.5f); var r1 = new Vector3(0, 0.5f, 0.5f);
            var v = new List<Vector3> { a, r0, r1, a, r1, d, b, c, r1, b, r1, r0, a, b, r0, d, r1, c, a, c, b, a, d, c };
            return FromTris(v, new Vector3(0, -0.2f, 0));
        }

        private static Mesh MakePyramid()
        {
            var t = new Vector3(0, 0.5f, 0);
            var a = new Vector3(-0.5f, -0.5f, -0.5f); var b = new Vector3(0.5f, -0.5f, -0.5f);
            var c = new Vector3(0.5f, -0.5f, 0.5f); var d = new Vector3(-0.5f, -0.5f, 0.5f);
            var v = new List<Vector3> { t, a, b, t, b, c, t, c, d, t, d, a, a, c, b, a, d, c };
            return FromTris(v, new Vector3(0, -0.2f, 0));
        }

        // ---------- Ortak küçük öğeler ----------
        private static void Tree(Transform p, Vector3 pos, float s, int seed)
        {
            bool pine = Rand(seed, 3) < 0.55f;
            Part(p, Shape.Cyl, pos + new Vector3(0, 0.12f * s, 0), new Vector3(0.08f, 0.24f, 0.08f) * s, WoodDark);
            if (pine)
            {
                var c = Color.Lerp(Pine, Leaf, Rand(seed, 4) * 0.4f);
                Part(p, Shape.Cone, pos + new Vector3(0, 0.38f * s, 0), new Vector3(0.42f, 0.38f, 0.42f) * s, c);
                Part(p, Shape.Cone, pos + new Vector3(0, 0.60f * s, 0), new Vector3(0.32f, 0.32f, 0.32f) * s, c);
                Part(p, Shape.Cone, pos + new Vector3(0, 0.80f * s, 0), new Vector3(0.20f, 0.24f, 0.20f) * s, c);
            }
            else
            {
                var c = Color.Lerp(Leaf, LeafLight, Rand(seed, 5));
                Part(p, Shape.Ball, pos + new Vector3(0, 0.45f * s, 0), new Vector3(0.46f, 0.42f, 0.46f) * s, c);
                Part(p, Shape.Ball, pos + new Vector3(0.10f * s, 0.62f * s, 0.05f * s), new Vector3(0.30f, 0.28f, 0.30f) * s, Color.Lerp(c, LeafLight, 0.3f));
            }
        }

        private static void Flag(Transform p, Vector3 basePos, float poleH, Color c)
        {
            Part(p, Shape.Cyl, basePos + new Vector3(0, poleH * 0.5f, 0), new Vector3(0.025f, poleH, 0.025f), WoodDark);
            var holder = Group(p, "flag", basePos + new Vector3(0, poleH - 0.07f, 0));
            Part(holder, Shape.Box, new Vector3(0.09f, 0, 0), new Vector3(0.18f, 0.11f, 0.01f), Mat(c, 0.2f));
            Animate(holder.gameObject, ArtMotion.Kind.Flutter, 3f, 0f, basePos.x * 3f + basePos.z);
        }

        private static void Window(Transform p, Vector3 pos, bool facesX)
        {
            var size = facesX ? new Vector3(0.012f, 0.08f, 0.07f) : new Vector3(0.07f, 0.08f, 0.012f);
            Part(p, Shape.Box, pos, size, Mat(new Color(1f, 0.85f, 0.45f), 0.6f, 0.6f));
        }

        // ---------- Binalar ----------
        public static Transform BuildBuilding(Transform parent, BuildingType t, int level, int seed, Vector3 pos)
        {
            float lv = 1.25f * (1f + Mathf.Min(0.25f, (level - 1) * 0.012f)); // hücreyi dolduracak boyut
            var g = Group(parent, t.ToString(), pos, 0f, lv);
            switch (t)
            {
                case BuildingType.VillagerHouse: House(g, seed, RoofRed); break;
                case BuildingType.Forest:
                    for (int i = 0; i < 4; i++)
                        Tree(g, new Vector3((Rand(seed, i) - 0.5f) * 0.55f, 0, (Rand(seed, i + 9) - 0.5f) * 0.55f), 0.75f + Rand(seed, i + 20) * 0.35f, seed * 7 + i);
                    Part(g, Shape.Cyl, new Vector3(0.22f, 0.04f, -0.25f), new Vector3(0.12f, 0.08f, 0.12f), WoodLight); // kütük
                    break;
                case BuildingType.LumberDepot:
                    foreach (var x in new[] { -0.3f, 0.3f }) foreach (var z in new[] { -0.25f, 0.25f })
                            Part(g, Shape.Box, new Vector3(x, 0.22f, z), new Vector3(0.05f, 0.44f, 0.05f), WoodDark);
                    Part(g, Shape.Box, new Vector3(0, 0.47f, 0), new Vector3(0.74f, 0.05f, 0.62f), WoodLight, new Vector3(0, 0, 8f));
                    for (int r = 0; r < 2; r++)
                        for (int i = 0; i < 3 - r; i++)
                            Part(g, Shape.Cyl, new Vector3(-0.1f + i * 0.14f + r * 0.07f, 0.07f + r * 0.12f, 0.02f), new Vector3(0.12f, 0.5f, 0.12f), Wood, new Vector3(90, 0, 0));
                    break;
                case BuildingType.StoneQuarry:
                    for (int i = 0; i < 5; i++)
                        Part(g, Shape.Ball, new Vector3((Rand(seed, i) - 0.5f) * 0.5f, 0.08f, (Rand(seed, i + 5) - 0.5f) * 0.5f),
                            new Vector3(0.26f, 0.18f + Rand(seed, i + 7) * 0.14f, 0.22f), Color.Lerp(StoneDark, Stone, Rand(seed, i + 3)), new Vector3(0, Rand(seed, i + 4) * 90f, 0));
                    Cart(g, new Vector3(-0.18f, 0, -0.22f), StoneLight);
                    break;
                case BuildingType.StoneDepot:
                    Part(g, Shape.Box, new Vector3(0, 0.2f, 0.06f), new Vector3(0.62f, 0.4f, 0.46f), Stone);
                    Part(g, Shape.Box, new Vector3(0, 0.42f, 0.06f), new Vector3(0.68f, 0.05f, 0.52f), StoneDark);
                    Part(g, Shape.Box, new Vector3(0, 0.12f, -0.17f), new Vector3(0.16f, 0.24f, 0.01f), WoodDark);
                    for (int i = 0; i < 3; i++) Part(g, Shape.Box, new Vector3(-0.26f + i * 0.13f, 0.05f, -0.3f), new Vector3(0.11f, 0.1f, 0.1f), StoneLight);
                    break;
                case BuildingType.GoldMine: Mine(g, GoldC, true); break;
                case BuildingType.IronMine: Mine(g, Iron, false); break;
                case BuildingType.Bank:
                    Part(g, Shape.Box, new Vector3(0, 0.03f, 0), new Vector3(0.74f, 0.06f, 0.62f), StoneLight);
                    Part(g, Shape.Box, new Vector3(0, 0.25f, 0.06f), new Vector3(0.62f, 0.38f, 0.44f), Plaster);
                    for (int i = 0; i < 4; i++) Part(g, Shape.Cyl, new Vector3(-0.24f + i * 0.16f, 0.24f, -0.21f), new Vector3(0.06f, 0.38f, 0.06f), StoneLight);
                    Part(g, Shape.Roof, new Vector3(0, 0.52f, 0), new Vector3(0.72f, 0.16f, 0.6f), StoneLight, new Vector3(0, 90, 0));
                    Part(g, Shape.Ball, new Vector3(0, 0.63f, 0.06f), new Vector3(0.18f, 0.18f, 0.18f), Mat(GoldC, 0.8f, 0.25f));
                    break;
                case BuildingType.IronDepot:
                    Part(g, Shape.Box, new Vector3(0, 0.2f, 0.06f), new Vector3(0.62f, 0.4f, 0.46f), new Color(0.36f, 0.38f, 0.42f));
                    Part(g, Shape.Roof, new Vector3(0, 0.5f, 0.06f), new Vector3(0.7f, 0.2f, 0.52f), Iron);
                    for (int i = 0; i < 2; i++) Part(g, Shape.Box, new Vector3(-0.2f + i * 0.2f, 0.07f, -0.28f), new Vector3(0.14f, 0.14f, 0.14f), Wood);
                    break;
                case BuildingType.Barracks:
                    Part(g, Shape.Box, new Vector3(0, 0.22f, 0.04f), new Vector3(0.7f, 0.44f, 0.56f), WoodDark);
                    for (int i = 0; i < 5; i++) Part(g, Shape.Box, new Vector3(-0.28f + i * 0.14f, 0.48f, -0.24f), new Vector3(0.08f, 0.08f, 0.08f), WoodDark);
                    Part(g, Shape.Box, new Vector3(0, 0.14f, -0.245f), new Vector3(0.18f, 0.28f, 0.01f), Dark);
                    Part(g, Shape.Box, new Vector3(0, 0.47f, 0.08f), new Vector3(0.64f, 0.04f, 0.42f), Wood);
                    Flag(g, new Vector3(0.26f, 0.46f, 0.2f), 0.45f, new Color(0.8f, 0.15f, 0.15f));
                    Part(g, Shape.Cyl, new Vector3(-0.3f, 0.12f, -0.34f), new Vector3(0.05f, 0.24f, 0.05f), WoodLight); // talim kuklası
                    Part(g, Shape.Box, new Vector3(-0.3f, 0.2f, -0.34f), new Vector3(0.16f, 0.03f, 0.03f), WoodLight);
                    break;
                case BuildingType.University:
                    Part(g, Shape.Box, new Vector3(-0.08f, 0.2f, 0.06f), new Vector3(0.5f, 0.4f, 0.46f), Plaster);
                    Part(g, Shape.Roof, new Vector3(-0.08f, 0.5f, 0.06f), new Vector3(0.56f, 0.2f, 0.52f), RoofBlue);
                    Part(g, Shape.Cyl, new Vector3(0.24f, 0.32f, -0.12f), new Vector3(0.2f, 0.64f, 0.2f), StoneLight);
                    Part(g, Shape.Ball, new Vector3(0.24f, 0.66f, -0.12f), new Vector3(0.22f, 0.22f, 0.22f), Mat(RoofBlue, 0.5f));
                    Window(g, new Vector3(-0.2f, 0.24f, -0.172f), false); Window(g, new Vector3(0.0f, 0.24f, -0.172f), false);
                    break;
            }
            return g;
        }

        private static void House(Transform g, int seed, Color roofC)
        {
            float yaw = Rand(seed, 1) < 0.5f ? 0f : 90f;
            var h = Group(g, "house", Vector3.zero, yaw);
            Part(h, Shape.Box, new Vector3(0, 0.03f, 0), new Vector3(0.66f, 0.06f, 0.54f), StoneDark);
            Part(h, Shape.Box, new Vector3(0, 0.22f, 0), new Vector3(0.6f, 0.34f, 0.48f), Plaster);
            Part(h, Shape.Box, new Vector3(0, 0.22f, 0), new Vector3(0.62f, 0.04f, 0.5f), Wood); // kiriş
            Part(h, Shape.Roof, new Vector3(0, 0.5f, 0), new Vector3(0.7f, 0.26f, 0.58f), Color.Lerp(roofC, WoodDark, Rand(seed, 2) * 0.3f), new Vector3(0, 90, 0));
            Part(h, Shape.Box, new Vector3(0.18f, 0.58f, 0.1f), new Vector3(0.08f, 0.18f, 0.08f), StoneDark); // baca
            Part(h, Shape.Box, new Vector3(-0.1f, 0.13f, -0.242f), new Vector3(0.12f, 0.2f, 0.01f), WoodDark);  // kapı
            Window(h, new Vector3(0.14f, 0.26f, -0.242f), false);
            Window(h, new Vector3(-0.302f, 0.26f, 0.05f), true);
            var smoke = Part(h, Shape.Ball, new Vector3(0.18f, 0.72f, 0.1f), new Vector3(0.07f, 0.07f, 0.07f), Mat(new Color(0.85f, 0.85f, 0.85f)));
            Animate(smoke, ArtMotion.Kind.Bob, 1.2f, 0.04f, seed);
        }

        private static void Mine(Transform g, Color ore, bool glow)
        {
            Part(g, Shape.Ball, new Vector3(0.05f, 0.05f, 0.12f), new Vector3(0.78f, 0.55f, 0.62f), Color.Lerp(DirtDark, StoneDark, 0.5f));
            Part(g, Shape.Box, new Vector3(-0.05f, 0.15f, -0.17f), new Vector3(0.26f, 0.3f, 0.04f), WoodDark);
            Part(g, Shape.Box, new Vector3(-0.05f, 0.13f, -0.19f), new Vector3(0.18f, 0.24f, 0.02f), Dark);
            for (int i = 0; i < 4; i++)
                Part(g, Shape.Ball, new Vector3(0.12f + i * 0.06f, 0.05f, -0.28f + (i % 2) * 0.05f), new Vector3(0.09f, 0.07f, 0.09f), Mat(ore, 0.7f, glow ? 0.35f : 0f));
            Part(g, Shape.Box, new Vector3(-0.05f, 0.01f, -0.33f), new Vector3(0.18f, 0.02f, 0.2f), Iron);
        }

        private static void Cart(Transform g, Vector3 pos, Color load)
        {
            Part(g, Shape.Box, pos + new Vector3(0, 0.1f, 0), new Vector3(0.2f, 0.08f, 0.14f), Wood);
            Part(g, Shape.Ball, pos + new Vector3(0, 0.16f, 0), new Vector3(0.16f, 0.08f, 0.1f), load);
            foreach (var x in new[] { -0.08f, 0.08f })
                Part(g, Shape.Cyl, pos + new Vector3(x, 0.05f, -0.075f), new Vector3(0.09f, 0.02f, 0.09f), WoodDark, new Vector3(90, 0, 0));
        }

        // ---------- Kuleler ----------
        public static Transform BuildTower(Transform parent, TowerType t, int level, bool damaged, Vector3 pos)
        {
            float lv = 1f + Mathf.Min(0.3f, (level - 1) * 0.015f);
            var g = Group(parent, t.ToString(), pos, 0f, 1f);
            var stone = damaged ? Color.Lerp(StoneDark, Dark, 0.3f) : (t == TowerType.Elf ? StoneLight : Stone);
            float h = 0.95f * lv;
            Part(g, Shape.Cyl, new Vector3(0, 0.06f, 0), new Vector3(0.6f, 0.12f, 0.6f), StoneDark);
            Part(g, Shape.Cyl, new Vector3(0, h * 0.5f, 0), new Vector3(t == TowerType.Elf ? 0.32f : 0.44f, h, t == TowerType.Elf ? 0.32f : 0.44f), stone);
            Part(g, Shape.Box, new Vector3(-0.0f, 0.18f, -0.215f), new Vector3(0.1f, 0.18f, 0.02f), WoodDark);
            Window(g, new Vector3(0, h * 0.62f, -0.222f), false);
            Part(g, Shape.Cyl, new Vector3(0, h + 0.03f, 0), new Vector3(0.56f, 0.07f, 0.56f), StoneDark);
            if (t != TowerType.Elf)
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f;
                    Part(g, Shape.Box, new Vector3(Mathf.Cos(a) * 0.24f, h + 0.11f, Mathf.Sin(a) * 0.24f), new Vector3(0.08f, 0.1f, 0.08f), stone, new Vector3(0, -a * Mathf.Rad2Deg, 0));
                }
            switch (t)
            {
                case TowerType.Archer:
                    foreach (var x in new[] { -0.17f, 0.17f }) foreach (var z in new[] { -0.17f, 0.17f })
                            Part(g, Shape.Box, new Vector3(x, h + 0.22f, z), new Vector3(0.03f, 0.2f, 0.03f), WoodDark);
                    Part(g, Shape.Cone, new Vector3(0, h + 0.44f, 0), new Vector3(0.62f, 0.3f, 0.62f), new Color(0.2f, 0.55f, 0.25f));
                    Flag(g, new Vector3(0, h + 0.56f, 0), 0.32f, new Color(0.2f, 0.7f, 0.25f));
                    break;
                case TowerType.Mage:
                    Part(g, Shape.Cone, new Vector3(0, h + 0.42f, 0), new Vector3(0.5f, 0.52f, 0.5f), new Color(0.42f, 0.2f, 0.65f));
                    var orb = Part(g, Shape.Ball, new Vector3(0, h + 0.86f, 0), new Vector3(0.16f, 0.16f, 0.16f), Mat(new Color(0.75f, 0.4f, 1f), 0.9f, 1.6f));
                    Animate(orb, ArtMotion.Kind.Bob, 2f, 0.05f, pos.x + pos.z);
                    break;
                case TowerType.Spear:
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3f;
                        Part(g, Shape.Cone, new Vector3(Mathf.Cos(a) * 0.3f, h * 0.5f, Mathf.Sin(a) * 0.3f), new Vector3(0.05f, 0.24f, 0.05f), Iron, new Vector3(Mathf.Sin(a) * 90f, 0, -Mathf.Cos(a) * 90f));
                    }
                    Part(g, Shape.Pyramid, new Vector3(0, h + 0.3f, 0), new Vector3(0.42f, 0.3f, 0.42f), new Color(0.75f, 0.45f, 0.2f));
                    Flag(g, new Vector3(0, h + 0.45f, 0), 0.3f, new Color(0.95f, 0.55f, 0.15f));
                    break;
                case TowerType.Elf:
                    Part(g, Shape.Ball, new Vector3(0, h + 0.22f, 0), new Vector3(0.6f, 0.42f, 0.6f), Leaf);
                    Part(g, Shape.Ball, new Vector3(0.12f, h + 0.4f, 0.06f), new Vector3(0.36f, 0.3f, 0.36f), LeafLight);
                    var gem = Part(g, Shape.Ball, new Vector3(0, h + 0.62f, 0), new Vector3(0.1f, 0.14f, 0.1f), Mat(new Color(0.4f, 1f, 0.8f), 0.9f, 1.4f));
                    Animate(gem, ArtMotion.Kind.Bob, 1.5f, 0.04f, pos.x);
                    break;
            }
            if (damaged)
                for (int i = 0; i < 3; i++)
                    Part(g, Shape.Box, new Vector3(-0.2f + i * 0.18f, 0.05f, -0.32f), new Vector3(0.1f, 0.08f, 0.09f), StoneDark, new Vector3(0, i * 30f, 15f));
            return g;
        }

        // ---------- Yol, giriş, tuzak ----------
        /// <summary>open: kuzey(+z), doğu(+x), güney(-z), batı(-x) bağlantıları.</summary>
        public static void BuildRoad(Transform parent, Vector3 c, RoadTier tier, bool[] open, int seed)
        {
            Color baseC = tier == RoadTier.Iron ? new Color(0.48f, 0.49f, 0.52f) : (tier == RoadTier.Reinforced ? WoodLight : Dirt);
            var m = tier == RoadTier.Reinforced ? Mat(baseC, 0.15f) : DirtMat(baseC);
            const float y = 0.02f, th = 0.05f;
            Part(parent, Shape.Box, c + new Vector3(0, y, 0), new Vector3(0.56f, th, 0.56f), m);
            int[] dx = { 0, 1, 0, -1 }, dz = { 1, 0, -1, 0 };
            for (int i = 0; i < 4; i++)
            {
                if (!open[i]) continue;
                bool alongX = dx[i] != 0;
                var dir = new Vector3(dx[i], 0, dz[i]);
                Part(parent, Shape.Box, c + dir * 0.36f + new Vector3(0, y, 0), alongX ? new Vector3(0.28f, th, 0.56f) : new Vector3(0.56f, th, 0.28f), m);
                if (tier == RoadTier.Iron)
                {
                    var perp = alongX ? Vector3.forward : Vector3.right;
                    foreach (var sgn in new[] { -1f, 1f })
                        Part(parent, Shape.Box, c + dir * 0.36f + perp * 0.25f * sgn + new Vector3(0, 0.06f, 0), alongX ? new Vector3(0.28f, 0.04f, 0.03f) : new Vector3(0.03f, 0.04f, 0.28f), Iron);
                }
            }
            if (tier == RoadTier.Reinforced)
            {
                for (int i = -2; i <= 2; i++) Part(parent, Shape.Box, c + new Vector3(i * 0.11f, 0.05f, 0), new Vector3(0.015f, 0.01f, 0.5f), WoodDark);
                Part(parent, Shape.Box, c + new Vector3(0, 0.055f, 0), new Vector3(0.12f, 0.02f, 0.12f), Mat(GoldC, 0.7f));
            }
            else
            {
                // parke taşları
                for (int i = 0; i < 6; i++)
                {
                    var off = new Vector3((Rand(seed, i) - 0.5f) * 0.46f, 0.048f, (Rand(seed, i + 11) - 0.5f) * 0.46f);
                    var col = tier == RoadTier.Iron ? Color.Lerp(StoneDark, Stone, Rand(seed, i + 3)) : Color.Lerp(Stone, StoneLight, Rand(seed, i + 3));
                    Part(parent, Shape.Box, c + off, new Vector3(0.09f, 0.02f, 0.08f), col, new Vector3(0, Rand(seed, i + 5) * 60f, 0));
                }
            }
        }

        public static void BuildGate(Transform parent, Vector3 c)
        {
            var g = Group(parent, "gate", c + new Vector3(0, 0, -0.42f));
            foreach (var x in new[] { -0.4f, 0.4f })
            {
                Part(g, Shape.Box, new Vector3(x, 0.32f, 0), new Vector3(0.1f, 0.64f, 0.1f), WoodDark);
                var flame = Part(g, Shape.Cone, new Vector3(x, 0.72f, 0), new Vector3(0.07f, 0.12f, 0.07f), Mat(new Color(1f, 0.6f, 0.15f), 0.2f, 2.2f));
                Animate(flame, ArtMotion.Kind.Bob, 9f, 0.012f, x * 10f);
            }
            Part(g, Shape.Box, new Vector3(0, 0.6f, 0), new Vector3(0.96f, 0.08f, 0.1f), Wood);
            Part(g, Shape.Box, new Vector3(0, 0.48f, -0.055f), new Vector3(0.36f, 0.12f, 0.01f), Mat(new Color(0.75f, 0.15f, 0.12f), 0.2f));
        }

        public static void BuildTrap(Transform parent, Vector3 c, TrapType t)
        {
            switch (t)
            {
                case TrapType.Spikes:
                    for (int i = 0; i < 5; i++)
                        Part(parent, Shape.Cone, c + new Vector3(-0.16f + i * 0.08f, 0.1f, (i % 2) * 0.1f - 0.05f), new Vector3(0.05f, 0.1f, 0.05f), Iron);
                    break;
                case TrapType.Slippery:
                    Part(parent, Shape.Cyl, c + new Vector3(0, 0.06f, 0), new Vector3(0.36f, 0.01f, 0.3f), Mat(new Color(0.45f, 0.75f, 0.95f), 0.95f, 0.2f));
                    break;
                default:
                    Part(parent, Shape.Cyl, c + new Vector3(0, 0.055f, 0), new Vector3(0.34f, 0.01f, 0.34f), Dark);
                    for (int i = 0; i < 3; i++)
                    {
                        var f = Part(parent, Shape.Cone, c + new Vector3(-0.08f + i * 0.08f, 0.12f, (i - 1) * 0.04f), new Vector3(0.07f, 0.13f, 0.07f), Mat(new Color(1f, 0.45f, 0.1f), 0.2f, 2.5f));
                        Animate(f, ArtMotion.Kind.Bob, 11f, 0.015f, i * 2f);
                    }
                    break;
            }
        }

        public static void BuildRubble(Transform parent, Vector3 c, int seed)
        {
            Part(parent, Shape.Cyl, c + new Vector3(0, 0.015f, 0), new Vector3(0.7f, 0.01f, 0.7f), Mat(new Color(0.18f, 0.15f, 0.13f)));
            for (int i = 0; i < 6; i++)
                Part(parent, Shape.Box, c + new Vector3((Rand(seed, i) - 0.5f) * 0.5f, 0.05f, (Rand(seed, i + 6) - 0.5f) * 0.5f),
                    new Vector3(0.12f, 0.08f, 0.1f), Color.Lerp(StoneDark, WoodDark, Rand(seed, i + 2)), new Vector3(Rand(seed, i + 3) * 40f, Rand(seed, i + 4) * 90f, 0));
        }

        public static void BuildSelection(Transform parent, Vector3 c)
        {
            var r = Part(parent, Shape.Cyl, c + new Vector3(0, 0.03f, 0), new Vector3(0.98f, 0.01f, 0.98f), Mat(new Color(1f, 0.9f, 0.3f), 0.3f, 0.8f));
            Animate(r, ArtMotion.Kind.Bob, 4f, 0.01f);
        }

        // ---------- Çevre (köy havası) ----------
        public static void BuildEnvironment(Transform parent, int size, int attackRows)
        {
            float cx = (size - 1) * 0.5f;
            // geniş çayır
            Part(parent, Shape.Box, new Vector3(cx, -0.16f, cx), new Vector3(90f, 0.2f, 90f), GrassMat(Meadow, 30f));
            // köy arsası çerçevesi (hafif toprak kenar)
            Part(parent, Shape.Box, new Vector3(cx, -0.08f, cx), new Vector3(size + 0.5f, 0.06f, size + 0.5f), DirtMat(DirtDark, 4f));
            // çit (sol, sağ, arka); ön taraf saldırı alanına açık
            var fence = Group(parent, "fence", Vector3.zero);
            for (int i = 0; i <= size; i++)
            {
                float t = i - 0.5f;
                FencePost(fence, new Vector3(-0.85f, 0, t)); FencePost(fence, new Vector3(size - 0.15f, 0, t)); FencePost(fence, new Vector3(t, 0, size - 0.15f));
            }
            Part(fence, Shape.Box, new Vector3(-0.85f, 0.2f, cx), new Vector3(0.03f, 0.04f, size + 1f), WoodLight);
            Part(fence, Shape.Box, new Vector3(size - 0.15f, 0.2f, cx), new Vector3(0.03f, 0.04f, size + 1f), WoodLight);
            Part(fence, Shape.Box, new Vector3(cx, 0.2f, size - 0.15f), new Vector3(size + 1f, 0.04f, 0.03f), WoodLight);

            // orman halkası + çalı, kaya, çiçek
            var deco = Group(parent, "nature", Vector3.zero);
            for (int x = -9; x < size + 9; x++)
                for (int z = -attackRows - 8; z < size + 9; z++)
                {
                    bool inPlot = x >= -1 && x <= size && z >= -1 && z <= size;
                    bool inZone = x >= -1 && x <= size && z < 0 && z >= -attackRows - 1;
                    if (inPlot || inZone) continue;
                    int seed = x * 977 + z * 131;
                    float d = Mathf.Max(Mathf.Max(-1 - x, x - size), Mathf.Max(-attackRows - 1 - z, z - size));
                    float jx = (Rand(seed, 1) - 0.5f) * 0.6f, jz = (Rand(seed, 2) - 0.5f) * 0.6f;
                    var p = new Vector3(x + jx, 0f, z + jz);
                    float r = Rand(seed, 3);
                    if (d >= 2f && r < 0.3f + Mathf.Min(0.45f, (d - 2f) * 0.1f)) Tree(deco, p, 0.9f + Rand(seed, 4) * 0.7f, seed); // köy çevresinde açıklık
                    else if (r < 0.78f)
                    {
                        float k = Rand(seed, 5);
                        if (k < 0.35f) Part(deco, Shape.Ball, p + new Vector3(0, 0.08f, 0), new Vector3(0.3f, 0.2f, 0.3f), Color.Lerp(Leaf, LeafLight, Rand(seed, 6)));
                        else if (k < 0.55f) Part(deco, Shape.Ball, p + new Vector3(0, 0.05f, 0), new Vector3(0.24f, 0.14f, 0.2f), Color.Lerp(StoneDark, Stone, Rand(seed, 7)), new Vector3(0, Rand(seed, 8) * 90f, 0));
                        else
                        {
                            var fc = new[] { new Color(0.95f, 0.85f, 0.3f), new Color(0.95f, 0.45f, 0.6f), new Color(0.95f, 0.95f, 0.95f), new Color(0.6f, 0.5f, 0.95f) }[(int)(Rand(seed, 9) * 3.99f)];
                            for (int i = 0; i < 3; i++)
                                Part(deco, Shape.Ball, p + new Vector3((Rand(seed, 10 + i) - 0.5f) * 0.4f, 0.04f, (Rand(seed, 20 + i) - 0.5f) * 0.4f), new Vector3(0.06f, 0.05f, 0.06f), Mat(fc, 0.3f));
                        }
                    }
                }

            // saldırı alanı: toprak meydan + iki çadır + sancaklar
            var zone = Group(parent, "attackZone", Vector3.zero);
            Part(zone, Shape.Box, new Vector3(cx, -0.07f, -(attackRows + 1) * 0.5f), new Vector3(size, 0.06f, attackRows), DirtMat(new Color(0.74f, 0.58f, 0.40f), 3f));
            foreach (var x in new[] { -1.4f, size + 0.4f })
            {
                Tent(zone, new Vector3(x, 0, -attackRows + 0.6f));
                Flag(zone, new Vector3(x, 0, -1.2f), 0.9f, new Color(0.8f, 0.15f, 0.15f));
            }
        }

        private static void FencePost(Transform p, Vector3 pos) => Part(p, Shape.Box, pos + new Vector3(0, 0.15f, 0), new Vector3(0.05f, 0.3f, 0.05f), WoodDark);

        private static void Tent(Transform p, Vector3 pos)
        {
            var g = Group(p, "tent", pos, 0f);
            Part(g, Shape.Roof, new Vector3(0, 0.25f, 0), new Vector3(0.7f, 0.5f, 0.8f), Mat(new Color(0.92f, 0.86f, 0.72f), 0.1f));
            Part(g, Shape.Box, new Vector3(0, 0.12f, -0.401f), new Vector3(0.16f, 0.22f, 0.01f), Dark);
        }

        // ---------- Askerler ----------
        public static Color TunicOf(SoldierType t)
        {
            switch (t)
            {
                case SoldierType.Swordsman: return new Color(0.25f, 0.42f, 0.80f);
                case SoldierType.Archer: return new Color(0.25f, 0.60f, 0.28f);
                case SoldierType.Mage: return new Color(0.50f, 0.25f, 0.75f);
                case SoldierType.Goblin: return new Color(0.42f, 0.30f, 0.18f);
                case SoldierType.Cavalry: return new Color(0.75f, 0.22f, 0.18f);
                case SoldierType.Spearman: return new Color(0.88f, 0.55f, 0.18f);
                case SoldierType.Fairy: return new Color(1f, 0.6f, 0.85f);
                case SoldierType.Giant: return new Color(0.50f, 0.42f, 0.36f);
                case SoldierType.Catapult: return WoodLight;
                default: return new Color(0.75f, 0.18f, 0.15f);
            }
        }

        private static void Human(Transform g, Color tunic, Color skin, float s, bool helmet, bool hood)
        {
            Part(g, Shape.Capsule, new Vector3(0, 0.15f * s, 0), new Vector3(0.14f, 0.22f, 0.12f) * s, tunic);
            Part(g, Shape.Ball, new Vector3(0, 0.31f * s, 0), new Vector3(0.11f, 0.11f, 0.11f) * s, skin);
            if (helmet) Part(g, Shape.Ball, new Vector3(0, 0.34f * s, 0), new Vector3(0.12f, 0.07f, 0.12f) * s, Mat(StoneLight, 0.7f));
            if (hood) Part(g, Shape.Cone, new Vector3(0, 0.38f * s, 0), new Vector3(0.13f, 0.12f, 0.13f) * s, Color.Lerp(tunic, Dark, 0.3f));
        }

        /// <summary>Asker modelleri okunabilirlik için hücreye göre büyük çizilir (izometrik uzak kamera).</summary>
        public const float SoldierScale = 1.8f;

        public static Transform BuildSoldier(Transform parent, SoldierType t)
        {
            var root = Group(parent, t.ToString(), Vector3.zero, 0f, SoldierScale);
            var g = Group(root, "model", Vector3.zero);
            var tunic = TunicOf(t);
            switch (t)
            {
                case SoldierType.Swordsman:
                    Human(g, tunic, Skin, 1f, true, false);
                    Part(g, Shape.Box, new Vector3(0.09f, 0.18f, 0.04f), new Vector3(0.02f, 0.18f, 0.03f), Mat(StoneLight, 0.8f), new Vector3(30, 0, 0));
                    Part(g, Shape.Cyl, new Vector3(-0.08f, 0.16f, 0.02f), new Vector3(0.1f, 0.01f, 0.1f), new Color(0.2f, 0.3f, 0.7f), new Vector3(0, 0, 90));
                    break;
                case SoldierType.Archer:
                    Human(g, tunic, Skin, 1f, false, true);
                    Part(g, Shape.Box, new Vector3(0.08f, 0.2f, 0.04f), new Vector3(0.015f, 0.22f, 0.015f), WoodDark, new Vector3(0, 0, 10));
                    Part(g, Shape.Cyl, new Vector3(-0.03f, 0.2f, -0.06f), new Vector3(0.04f, 0.14f, 0.04f), Wood, new Vector3(20, 0, 0)); // sadak
                    break;
                case SoldierType.Mage:
                    Part(g, Shape.Cone, new Vector3(0, 0.13f, 0), new Vector3(0.18f, 0.26f, 0.18f), tunic);
                    Part(g, Shape.Ball, new Vector3(0, 0.3f, 0), new Vector3(0.1f, 0.1f, 0.1f), Skin);
                    Part(g, Shape.Cone, new Vector3(0, 0.41f, 0), new Vector3(0.14f, 0.16f, 0.14f), Color.Lerp(tunic, Dark, 0.2f));
                    Part(g, Shape.Cyl, new Vector3(0.09f, 0.2f, 0.02f), new Vector3(0.015f, 0.36f, 0.015f), WoodDark);
                    Part(g, Shape.Ball, new Vector3(0.09f, 0.39f, 0.02f), new Vector3(0.05f, 0.05f, 0.05f), Mat(new Color(0.8f, 0.5f, 1f), 0.9f, 2f));
                    break;
                case SoldierType.Goblin:
                    Human(g, tunic, new Color(0.45f, 0.70f, 0.30f), 0.8f, false, false);
                    Part(g, Shape.Cone, new Vector3(0.06f, 0.27f, 0), new Vector3(0.03f, 0.06f, 0.03f), new Color(0.45f, 0.7f, 0.3f), new Vector3(0, 0, -70));
                    Part(g, Shape.Cone, new Vector3(-0.06f, 0.27f, 0), new Vector3(0.03f, 0.06f, 0.03f), new Color(0.45f, 0.7f, 0.3f), new Vector3(0, 0, 70));
                    Part(g, Shape.Ball, new Vector3(0, 0.15f, -0.07f), new Vector3(0.1f, 0.1f, 0.08f), Wood); // çuval
                    break;
                case SoldierType.Cavalry:
                    Horse(g, new Color(0.45f, 0.30f, 0.18f));
                    var rider = Group(g, "rider", new Vector3(0, 0.18f, -0.02f));
                    Human(rider, tunic, Skin, 0.9f, true, false);
                    Part(rider, Shape.Box, new Vector3(0.08f, 0.16f, 0.06f), new Vector3(0.02f, 0.18f, 0.03f), Mat(StoneLight, 0.8f), new Vector3(40, 0, 0));
                    break;
                case SoldierType.Spearman:
                    Human(g, tunic, Skin, 1f, true, false);
                    Part(g, Shape.Cyl, new Vector3(0.08f, 0.24f, 0.02f), new Vector3(0.015f, 0.46f, 0.015f), WoodDark);
                    Part(g, Shape.Cone, new Vector3(0.08f, 0.5f, 0.02f), new Vector3(0.03f, 0.07f, 0.03f), Mat(StoneLight, 0.8f));
                    Part(g, Shape.Box, new Vector3(-0.08f, 0.16f, 0.02f), new Vector3(0.02f, 0.16f, 0.12f), new Color(0.6f, 0.35f, 0.12f));
                    break;
                case SoldierType.Fairy:
                    Human(g, tunic, Skin, 0.7f, false, false);
                    foreach (var sgn in new[] { -1f, 1f })
                    {
                        var wing = Group(g, "wing", new Vector3(0.03f * sgn, 0.16f, -0.04f));
                        Part(wing, Shape.Ball, new Vector3(0.09f * sgn, 0.02f, 0), new Vector3(0.16f, 0.12f, 0.02f), Mat(new Color(0.8f, 0.95f, 1f), 0.9f, 0.8f));
                        Animate(wing.gameObject, ArtMotion.Kind.Flap, 18f, 35f * sgn);
                    }
                    break;
                case SoldierType.Giant:
                    Human(g, tunic, new Color(0.80f, 0.65f, 0.55f), 2.0f, false, false);
                    Part(g, Shape.Cyl, new Vector3(0.2f, 0.32f, 0.06f), new Vector3(0.07f, 0.4f, 0.07f), WoodDark, new Vector3(25, 0, 0)); // sopa
                    Part(g, Shape.Ball, new Vector3(0.2f, 0.52f, 0.15f), new Vector3(0.12f, 0.12f, 0.12f), WoodDark);
                    break;
                case SoldierType.Catapult:
                    Part(g, Shape.Box, new Vector3(0, 0.08f, 0), new Vector3(0.26f, 0.06f, 0.36f), Wood);
                    foreach (var x in new[] { -0.14f, 0.14f }) foreach (var z in new[] { -0.12f, 0.12f })
                            Part(g, Shape.Cyl, new Vector3(x, 0.06f, z), new Vector3(0.1f, 0.02f, 0.1f), WoodDark, new Vector3(0, 0, 90));
                    Part(g, Shape.Box, new Vector3(0, 0.2f, 0.02f), new Vector3(0.18f, 0.18f, 0.04f), WoodDark);
                    Part(g, Shape.Box, new Vector3(0, 0.22f, -0.04f), new Vector3(0.03f, 0.03f, 0.34f), WoodLight, new Vector3(-35, 0, 0));
                    Part(g, Shape.Ball, new Vector3(0, 0.34f, -0.16f), new Vector3(0.07f, 0.07f, 0.07f), StoneDark);
                    break;
                default: // Ejderha Binicisi
                    var dc = new Color(0.70f, 0.18f, 0.14f);
                    Part(g, Shape.Capsule, new Vector3(0, 0.16f, 0), new Vector3(0.18f, 0.42f, 0.18f), dc, new Vector3(90, 0, 0));
                    Part(g, Shape.Capsule, new Vector3(0, 0.26f, 0.2f), new Vector3(0.08f, 0.2f, 0.08f), dc, new Vector3(40, 0, 0));
                    Part(g, Shape.Box, new Vector3(0, 0.34f, 0.28f), new Vector3(0.1f, 0.08f, 0.14f), dc);
                    Part(g, Shape.Cone, new Vector3(0, 0.14f, -0.28f), new Vector3(0.08f, 0.22f, 0.08f), dc, new Vector3(-100, 0, 0));
                    foreach (var sgn in new[] { -1f, 1f })
                    {
                        var wing = Group(g, "wing", new Vector3(0.08f * sgn, 0.22f, 0));
                        Part(wing, Shape.Box, new Vector3(0.18f * sgn, 0, 0), new Vector3(0.36f, 0.015f, 0.22f), Mat(new Color(0.55f, 0.12f, 0.10f), 0.3f));
                        Animate(wing.gameObject, ArtMotion.Kind.Flap, 6f, 28f * sgn);
                    }
                    var dr = Group(g, "rider", new Vector3(0, 0.22f, -0.02f));
                    Human(dr, new Color(0.25f, 0.25f, 0.3f), Skin, 0.7f, true, false);
                    break;
            }
            return root;
        }

        private static void Horse(Transform g, Color c)
        {
            Part(g, Shape.Box, new Vector3(0, 0.14f, 0), new Vector3(0.12f, 0.1f, 0.3f), c);
            Part(g, Shape.Box, new Vector3(0, 0.22f, 0.15f), new Vector3(0.07f, 0.14f, 0.07f), c, new Vector3(-30, 0, 0));
            Part(g, Shape.Box, new Vector3(0, 0.27f, 0.2f), new Vector3(0.06f, 0.06f, 0.12f), c);
            foreach (var x in new[] { -0.04f, 0.04f }) foreach (var z in new[] { -0.11f, 0.11f })
                    Part(g, Shape.Box, new Vector3(x, 0.05f, z), new Vector3(0.025f, 0.1f, 0.025f), Color.Lerp(c, Dark, 0.4f));
            Part(g, Shape.Box, new Vector3(0, 0.14f, -0.17f), new Vector3(0.02f, 0.08f, 0.04f), Dark, new Vector3(-30, 0, 0));
        }

        // ---------- Savaş efektleri ----------
        public static Transform Projectile(Transform parent, TowerType t)
        {
            var g = Group(parent, "shot", Vector3.zero);
            switch (t)
            {
                case TowerType.Archer: Part(g, Shape.Box, Vector3.zero, new Vector3(0.02f, 0.02f, 0.22f), WoodDark); break;
                case TowerType.Spear: Part(g, Shape.Box, Vector3.zero, new Vector3(0.03f, 0.03f, 0.34f), Iron); break;
                case TowerType.Mage: Part(g, Shape.Ball, Vector3.zero, new Vector3(0.14f, 0.14f, 0.14f), Mat(new Color(0.8f, 0.45f, 1f), 0.9f, 2.5f)); break;
                default: Part(g, Shape.Ball, Vector3.zero, new Vector3(0.08f, 0.08f, 0.08f), Mat(new Color(0.4f, 1f, 0.75f), 0.9f, 2.5f)); break;
            }
            return g;
        }

        public static void Smoke(Transform parent, Vector3 pos, int seed)
        {
            for (int i = 0; i < 5; i++)
            {
                var p = pos + new Vector3((Rand(seed, i) - 0.5f) * 0.5f, 0.2f + i * 0.08f, (Rand(seed, i + 7) - 0.5f) * 0.5f);
                var go = Part(parent, Shape.Ball, p, Vector3.one * (0.18f + Rand(seed, i + 3) * 0.12f), Mat(Color.Lerp(new Color(0.35f, 0.33f, 0.32f), new Color(0.75f, 0.73f, 0.7f), Rand(seed, i + 9))));
                var m = go.AddComponent<ArtMotion>();
                m.Mode = ArtMotion.Kind.Smoke; m.Life = 1.0f + Rand(seed, i + 2) * 0.6f;
            }
        }
    }
}
