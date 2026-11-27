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
            ball = MakeLowSphere(10, 7); // 768 yerine ~120 üçgen (low-poly stile de uygun; mobil performans)
            capsule = PrimMesh(PrimitiveType.Capsule);
            cone = MakeCone(12); roof = MakeRoof(); pyramid = MakePyramid();
            grassTex = NoiseTex(64, 0.82f, 1.08f, 11);
            dirtTex = NoiseTex(64, 0.75f, 1.12f, 23);
        }

        /// <summary>Az yüzlü UV küre (çap 1, merkez 0): lon dilim, lat halka.</summary>
        private static Mesh MakeLowSphere(int lon, int lat)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int y = 0; y <= lat; y++)
            {
                float a = Mathf.PI * y / lat;
                for (int x = 0; x <= lon; x++)
                {
                    float b = 2f * Mathf.PI * x / lon;
                    var p = new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b));
                    v.Add(p * 0.5f); n.Add(p); uv.Add(new Vector2(x / (float)lon, 1f - y / (float)lat));
                }
            }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int i0 = y * (lon + 1) + x, i1 = i0 + 1, i2 = i0 + lon + 1, i3 = i2 + 1;
                    if (y > 0) { tri.Add(i0); tri.Add(i1); tri.Add(i2); }
                    if (y < lat - 1) { tri.Add(i1); tri.Add(i3); tri.Add(i2); }
                }
            var m = new Mesh { name = "lowSphere" };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
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
            // yakın tonlar aynı materyali paylaşsın (rastgele renkli yüzlerce süs = yüzlerce materyal; draw call için)
            const float q = 24f;
            c = new Color(Mathf.Round(c.r * q) / q, Mathf.Round(c.g * q) / q, Mathf.Round(c.b * q) / q, c.a);
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

        // ---------- Seviye kademeleri (görünüm seviyeyle gelişir) ----------
        /// <summary>Yapı kademesi: Sv1-4 → 0, 5-9 → 1, 10-14 → 2, 15-19 → 3, 20+ → 4.</summary>
        public static int Tier(int level) => level >= 20 ? 4 : level >= 15 ? 3 : level >= 10 ? 2 : level >= 5 ? 1 : 0;

        /// <summary>Asker kademesi (seviye daha yavaş yükselir): 1-2 → 0, 3-5 → 1, 6-9 → 2, 10-14 → 3, 15+ → 4.</summary>
        public static int SoldierTier(int level) => level >= 15 ? 4 : level >= 10 ? 3 : level >= 6 ? 2 : level >= 3 ? 1 : 0;

        private static readonly Color[] RoofByTier =
        {
            new Color(0.80f, 0.66f, 0.36f),   // saman
            new Color(0.72f, 0.27f, 0.18f),   // kırmızı kiremit
            new Color(0.30f, 0.42f, 0.66f),   // mavi arduvaz
            new Color(0.28f, 0.26f, 0.34f),   // koyu arduvaz
            new Color(0.45f, 0.16f, 0.30f),   // asil bordo
        };

        /// <summary>Her binaya kademe süsleri: taş temel (1+), fener (2+), altın kubbe/çatı süsü (3+), sancak + parıltı (4).</summary>
        private static void Embellish(Transform g, int tier, int seed)
        {
            if (tier >= 1) Part(g, Shape.Box, new Vector3(0, 0.015f, 0), new Vector3(0.82f, 0.03f, 0.72f), StoneLight);
            if (tier >= 2)
            {
                Part(g, Shape.Cyl, new Vector3(-0.36f, 0.12f, -0.32f), new Vector3(0.03f, 0.24f, 0.03f), WoodDark);
                var lamp = Part(g, Shape.Ball, new Vector3(-0.36f, 0.26f, -0.32f), new Vector3(0.06f, 0.06f, 0.06f), Mat(new Color(1f, 0.8f, 0.4f), 0.4f, 1.8f));
                Animate(lamp, ArtMotion.Kind.Bob, 3f, 0.004f, seed);
            }
            if (tier >= 3)
                foreach (var x in new[] { -0.38f, 0.38f })
                    Part(g, Shape.Pyramid, new Vector3(x, 0.07f, 0.34f), new Vector3(0.07f, 0.1f, 0.07f), Mat(GoldC, 0.8f, 0.2f));
            if (tier >= 4) Flag(g, new Vector3(0.36f, 0f, 0.32f), 0.75f, new Color(0.55f, 0.15f, 0.65f));
        }

        // ---------- Binalar ----------
        /// <summary>Bina önüne küçük bahçe: iki çalı ve çiçek kümesi (tohumlu, her binada farklı).</summary>
        private static void Garden(Transform g, int seed)
        {
            var flowers = new[] { new Color(0.95f, 0.85f, 0.3f), new Color(0.95f, 0.45f, 0.6f), new Color(0.98f, 0.98f, 0.98f), new Color(0.65f, 0.5f, 0.95f) };
            foreach (var x in new[] { -0.36f, 0.36f })
            {
                if (Rand(seed, x < 0 ? 41 : 42) < 0.35f) continue;
                var p = new Vector3(x, 0, -0.34f);
                Part(g, Shape.Ball, p + new Vector3(0, 0.05f, 0), new Vector3(0.14f, 0.1f, 0.12f), Color.Lerp(Leaf, LeafLight, Rand(seed, 43)));
                var fc = flowers[(int)(Rand(seed, x < 0 ? 44 : 45) * 3.99f)];
                for (int i = 0; i < 3; i++)
                    Part(g, Shape.Ball, p + new Vector3((Rand(seed, 50 + i) - 0.5f) * 0.12f, 0.1f, (Rand(seed, 60 + i) - 0.5f) * 0.08f - 0.03f), new Vector3(0.035f, 0.03f, 0.035f), Mat(fc, 0.3f));
            }
        }

        /// <summary>Hazır modelli görünüm (ModelArt) için dış erişim.</summary>
        public static void EmbellishPublic(Transform g, int tier, int seed) => Embellish(g, tier, seed);
        public static void LevelEffectsPublic(Transform g, int L, float foot, float headY) => LevelEffects(g, L, foot, headY);

        /// <summary>Peri kanatları (hazır modelli Peri için): boy oranında, çırpan iki kanat.</summary>
        public static void FairyWings(Transform root, float height)
        {
            float k = height / 0.42f;
            foreach (var sgn in new[] { -1f, 1f })
            {
                var wing = Group(root, "wing", new Vector3(0.03f * sgn * k, height * 0.6f, -0.05f * k));
                Part(wing, Shape.Ball, new Vector3(0.12f * sgn * k, 0.02f * k, 0), new Vector3(0.22f * k, 0.16f * k, 0.02f), Mat(new Color(0.8f, 0.95f, 1f), 0.9f, 0.8f));
                Animate(wing.gameObject, ArtMotion.Kind.Flap, 18f, 35f * sgn);
            }
        }

        public static Transform BuildBuilding(Transform parent, BuildingType t, int level, int seed, Vector3 pos)
        {
            var model = ModelArt.TryBuildBuilding(parent, t, level, seed, pos);
            if (model != null) return model;
            int tier = Tier(level);
            float lv = 1.25f * (1f + Mathf.Min(0.25f, (level - 1) * 0.012f)); // hücreyi dolduracak boyut
            var g = Group(parent, t.ToString(), pos, 0f, lv);
            if (t != BuildingType.Forest && t != BuildingType.StoneQuarry && t != BuildingType.GoldMine && t != BuildingType.IronMine) Garden(g, seed);
            switch (t)
            {
                case BuildingType.VillagerHouse: House(g, seed, tier); break;
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
            Embellish(g, tier, seed);
            return g;
        }

        private static void House(Transform g, int seed, int tier)
        {
            float yaw = Rand(seed, 1) < 0.5f ? 0f : 90f;
            var h = Group(g, "house", Vector3.zero, yaw);
            float up = tier >= 2 ? 0.2f : 0f; // 2. kat
            var wall = tier >= 3 ? StoneLight : Plaster;
            Part(h, Shape.Box, new Vector3(0, 0.03f, 0), new Vector3(0.66f, 0.06f, 0.54f), StoneDark);
            Part(h, Shape.Box, new Vector3(0, 0.22f, 0), new Vector3(0.6f, 0.34f, 0.48f), tier >= 1 ? Stone : Plaster);
            if (up > 0f)
            {
                Part(h, Shape.Box, new Vector3(0, 0.49f, 0), new Vector3(0.62f, 0.22f, 0.5f), wall);
                Window(h, new Vector3(-0.1f, 0.5f, -0.252f), false); Window(h, new Vector3(0.14f, 0.5f, -0.252f), false);
            }
            Part(h, Shape.Box, new Vector3(0, 0.39f, 0), new Vector3(0.64f, 0.04f, 0.52f), Wood); // kiriş
            var roofC = RoofByTier[tier];
            Part(h, Shape.Roof, new Vector3(0, 0.5f + up, 0), new Vector3(0.72f, 0.26f, 0.6f), Color.Lerp(roofC, WoodDark, Rand(seed, 2) * 0.2f), new Vector3(0, 90, 0));
            if (tier >= 3) Part(h, Shape.Box, new Vector3(0, 0.635f + up, 0), new Vector3(0.03f, 0.03f, 0.6f), Mat(GoldC, 0.8f, 0.2f)); // altın mahya
            Part(h, Shape.Box, new Vector3(0.18f, 0.58f + up, 0.1f), new Vector3(0.08f, 0.18f, 0.08f), StoneDark); // baca
            Part(h, Shape.Box, new Vector3(-0.1f, 0.13f, -0.242f), new Vector3(0.12f, 0.2f, 0.01f), WoodDark);  // kapı
            Window(h, new Vector3(0.14f, 0.26f, -0.242f), false);
            Window(h, new Vector3(-0.302f, 0.26f, 0.05f), true);
            var smoke = Part(h, Shape.Ball, new Vector3(0.18f, 0.72f + up, 0.1f), new Vector3(0.07f, 0.07f, 0.07f), Mat(new Color(0.85f, 0.85f, 0.85f)));
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
            var model = ModelArt.TryBuildTower(parent, t, level, damaged, pos);
            if (model != null) return model;
            int tier = Tier(level);
            float lv = 1f + tier * 0.08f + Mathf.Min(0.1f, (level - 1) * 0.005f);
            var g = Group(parent, t.ToString(), pos, 0f, 1f);
            if (t == TowerType.Wall) { BuildWall(g, tier, damaged); return g; }
            var body = tier == 0 ? WoodLight : (t == TowerType.Elf ? StoneLight : (tier >= 3 ? StoneLight : Stone));
            var stone = damaged ? Color.Lerp(StoneDark, Dark, 0.3f) : body;
            float h = 0.95f * lv;
            Part(g, Shape.Cyl, new Vector3(0, 0.06f, 0), new Vector3(0.6f, 0.12f, 0.6f), StoneDark);
            Part(g, Shape.Cyl, new Vector3(0, h * 0.5f, 0), new Vector3(t == TowerType.Elf ? 0.32f : 0.44f, h, t == TowerType.Elf ? 0.32f : 0.44f), stone);
            Part(g, Shape.Box, new Vector3(-0.0f, 0.18f, -0.215f), new Vector3(0.1f, 0.18f, 0.02f), WoodDark);
            Window(g, new Vector3(0, h * 0.62f, -0.222f), false);
            Part(g, Shape.Cyl, new Vector3(0, h + 0.03f, 0), new Vector3(0.56f, 0.07f, 0.56f), StoneDark);
            if (tier == 0) // ahşap kule: bağlama kirişleri
                for (int i = 0; i < 3; i++) Part(g, Shape.Cyl, new Vector3(0, 0.2f + i * h * 0.3f, 0), new Vector3(0.47f, 0.025f, 0.47f), WoodDark);
            if (tier >= 3) // altın bantlar
                for (int i = 1; i <= tier - 2; i++) Part(g, Shape.Cyl, new Vector3(0, h * (0.3f + 0.25f * i), 0), new Vector3(t == TowerType.Elf ? 0.35f : 0.47f, 0.035f, t == TowerType.Elf ? 0.35f : 0.47f), Mat(GoldC, 0.85f, 0.25f));
            if (tier >= 4)
            {
                var crystal = Part(g, Shape.Pyramid, new Vector3(0.24f, h + 0.25f, -0.24f), new Vector3(0.08f, 0.16f, 0.08f), Mat(new Color(0.55f, 0.9f, 1f), 0.95f, 2f));
                Animate(crystal, ArtMotion.Kind.Spin, 1.5f);
                Flag(g, new Vector3(-0.24f, h + 0.1f, 0.24f), 0.4f, new Color(0.55f, 0.15f, 0.65f));
            }
            if (t != TowerType.Elf && tier >= 2)
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
                case TowerType.Cannon:
                    Part(g, Shape.Cyl, new Vector3(0, h + 0.1f, 0), new Vector3(0.3f, 0.08f, 0.3f), WoodDark);
                    Part(g, Shape.Cyl, new Vector3(0, h + 0.22f, -0.1f), new Vector3(0.13f, 0.4f, 0.13f), Mat(new Color(0.18f, 0.18f, 0.2f), 0.6f), new Vector3(-65, 0, 0));
                    Part(g, Shape.Cyl, new Vector3(0, h + 0.3f, -0.26f), new Vector3(0.16f, 0.04f, 0.16f), Mat(tier >= 3 ? GoldC : Iron, 0.7f), new Vector3(-65, 0, 0));
                    for (int i = 0; i < 3; i++) Part(g, Shape.Ball, new Vector3(0.14f - i * 0.07f, h + 0.1f, 0.14f), new Vector3(0.07f, 0.07f, 0.07f), Mat(new Color(0.15f, 0.15f, 0.16f), 0.5f));
                    break;
                case TowerType.Ice:
                    Part(g, Shape.Cone, new Vector3(0, h + 0.2f, 0), new Vector3(0.46f, 0.18f, 0.46f), new Color(0.92f, 0.96f, 1f));
                    for (int i = 0; i < 3 + Mathf.Min(2, tier); i++)
                    {
                        float a = i * Mathf.PI * 2f / (3 + Mathf.Min(2, tier));
                        Part(g, Shape.Pyramid, new Vector3(Mathf.Cos(a) * 0.12f, h + 0.36f, Mathf.Sin(a) * 0.12f), new Vector3(0.07f, 0.28f, 0.07f), Mat(new Color(0.55f, 0.9f, 1f), 0.95f, 1.2f), new Vector3(Mathf.Sin(a) * 15f, 0, -Mathf.Cos(a) * 15f));
                    }
                    var core = Part(g, Shape.Pyramid, new Vector3(0, h + 0.5f, 0), new Vector3(0.1f, 0.34f, 0.1f), Mat(new Color(0.7f, 0.95f, 1f), 0.95f, 2f));
                    Animate(core, ArtMotion.Kind.Spin, 1f);
                    break;
                case TowerType.Lightning:
                    Part(g, Shape.Cyl, new Vector3(0, h + 0.3f, 0), new Vector3(0.04f, 0.5f, 0.04f), Mat(Iron, 0.8f));
                    for (int i = 0; i < 2 + Mathf.Min(2, tier); i++) Part(g, Shape.Cyl, new Vector3(0, h + 0.15f + i * 0.09f, 0), new Vector3(0.16f - i * 0.02f, 0.015f, 0.16f - i * 0.02f), Mat(new Color(0.75f, 0.5f, 0.25f), 0.8f));
                    var bolt = Part(g, Shape.Ball, new Vector3(0, h + 0.6f, 0), new Vector3(0.15f, 0.15f, 0.15f), Mat(new Color(1f, 0.95f, 0.45f), 0.9f, 2.6f));
                    Animate(bolt, ArtMotion.Kind.Bob, 6f, 0.02f, pos.x);
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

        /// <summary>Duvar: kademe 0 ahşap çit (kazık), 1 taş duvar, 2 mazgallı, 3 altın şerit, 4 sancak + kristal. Seviye boyunu artırır.</summary>
        private static void BuildWall(Transform g, int tier, bool damaged)
        {
            if (tier == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    float x = -0.4f + i * 0.16f;
                    Part(g, Shape.Cyl, new Vector3(x, 0.24f, 0), new Vector3(0.12f, 0.48f, 0.12f), damaged ? WoodDark : Wood);
                    Part(g, Shape.Cone, new Vector3(x, 0.54f, 0), new Vector3(0.12f, 0.14f, 0.12f), WoodLight);
                }
                Part(g, Shape.Box, new Vector3(0, 0.3f, -0.07f), new Vector3(0.9f, 0.05f, 0.03f), WoodDark);
                return;
            }
            float wh = 0.45f + tier * 0.07f;
            var stone = damaged ? StoneDark : (tier >= 3 ? StoneLight : Stone);
            Part(g, Shape.Box, new Vector3(0, 0.04f, 0), new Vector3(0.98f, 0.08f, 0.44f), StoneDark);
            Part(g, Shape.Box, new Vector3(0, wh * 0.5f, 0), new Vector3(0.94f, wh, 0.34f), stone);
            for (int i = 0; i < 4; i++) Part(g, Shape.Box, new Vector3(-0.33f + i * 0.22f, wh * 0.45f, -0.172f), new Vector3(0.18f, 0.004f, 0.004f), StoneDark); // taş derzleri
            if (tier >= 2)
                for (int i = 0; i < 5; i++) Part(g, Shape.Box, new Vector3(-0.38f + i * 0.19f, wh + 0.06f, 0), new Vector3(0.12f, 0.12f, 0.34f), stone);
            if (tier >= 3) Part(g, Shape.Box, new Vector3(0, wh - 0.03f, -0.175f), new Vector3(0.95f, 0.04f, 0.01f), Mat(GoldC, 0.85f, 0.25f));
            if (tier >= 4)
            {
                foreach (var x in new[] { -0.4f, 0.4f }) Flag(g, new Vector3(x, wh + 0.12f, 0), 0.35f, new Color(0.55f, 0.15f, 0.65f));
                var gem = Part(g, Shape.Pyramid, new Vector3(0, wh * 0.6f, -0.18f), new Vector3(0.1f, 0.14f, 0.02f), Mat(new Color(0.55f, 0.9f, 1f), 0.95f, 2f));
                Animate(gem, ArtMotion.Kind.Bob, 2f, 0.01f);
            }
        }

        // ---------- Yol, giriş, tuzak ----------
        /// <summary>open: kuzey(+z), doğu(+x), güney(-z), batı(-x) bağlantıları.</summary>
        public static void BuildRoad(Transform parent, Vector3 c, RoadTier tier, bool[] open, int seed)
        {
            Color baseC = tier == RoadTier.Iron ? new Color(0.48f, 0.49f, 0.52f) : tier == RoadTier.Reinforced ? WoodLight
                : tier == RoadTier.Stone ? new Color(0.66f, 0.65f, 0.62f) : tier == RoadTier.Crystal ? new Color(0.28f, 0.30f, 0.38f) : Dirt;
            var m = tier == RoadTier.Reinforced ? Mat(baseC, 0.15f) : tier == RoadTier.Crystal ? Mat(baseC, 0.7f) : DirtMat(baseC);
            const float y = 0.02f, th = 0.05f;
            Part(parent, Shape.Box, c + new Vector3(0, y, 0), new Vector3(0.56f, th, 0.56f), m);
            int[] dx = { 0, 1, 0, -1 }, dz = { 1, 0, -1, 0 };
            for (int i = 0; i < 4; i++)
            {
                if (!open[i]) continue;
                bool alongX = dx[i] != 0;
                var dir = new Vector3(dx[i], 0, dz[i]);
                Part(parent, Shape.Box, c + dir * 0.39f + new Vector3(0, y, 0), alongX ? new Vector3(0.22f, th, 0.56f) : new Vector3(0.56f, th, 0.22f), m); // merkezle çakışmaz (titreme yok)
                if (tier == RoadTier.Iron)
                {
                    var perp = alongX ? Vector3.forward : Vector3.right;
                    foreach (var sgn in new[] { -1f, 1f })
                        Part(parent, Shape.Box, c + dir * 0.39f + perp * 0.25f * sgn + new Vector3(0, 0.06f, 0), alongX ? new Vector3(0.22f, 0.04f, 0.03f) : new Vector3(0.03f, 0.04f, 0.22f), Iron);
                }
            }
            if (tier == RoadTier.Crystal)
            {
                var glow = Part(parent, Shape.Box, c + new Vector3(0, 0.05f, 0), new Vector3(0.16f, 0.015f, 0.16f), Mat(new Color(0.45f, 0.95f, 1f), 0.95f, 2.2f), new Vector3(0, 45, 0));
                Animate(glow, ArtMotion.Kind.Bob, 2f, 0.004f, seed);
                for (int i = 0; i < 4; i++)
                    if (open[i]) Part(parent, Shape.Box, c + new Vector3(new[] { 0, 1, 0, -1 }[i], 0, new[] { 1, 0, -1, 0 }[i]) * 0.3f + new Vector3(0, 0.05f, 0),
                        i % 2 == 1 ? new Vector3(0.3f, 0.01f, 0.03f) : new Vector3(0.03f, 0.01f, 0.3f), Mat(new Color(0.45f, 0.95f, 1f), 0.95f, 1.6f));
            }
            else if (tier == RoadTier.Stone)
            {
                for (int i = 0; i < 4; i++)
                    Part(parent, Shape.Box, c + new Vector3((i % 2 - 0.5f) * 0.26f, 0.048f, (i / 2 - 0.5f) * 0.26f), new Vector3(0.24f, 0.02f, 0.24f), Color.Lerp(Stone, StoneLight, Rand(seed, i)));
            }
            else if (tier == RoadTier.Reinforced)
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

        /// <summary>
        /// Tuzak modeli: sabit taban + hareketli parçalar (TrapAnim: bekleme ve tetiklenme animasyonu) + küçük sürekli efekt (FxKit).
        /// Grup hücre merkezinde durur; parçalar ona göre konumlanır (ölçek/dönüş merkezden olur).
        /// </summary>
        public static TrapAnim BuildTrap(Transform parent, Vector3 c, TrapType t)
        {
            var g = Group(parent, "trap_" + t, c + new Vector3(0f, 0.015f, 0f)); // yol taşlarıyla aynı yükseklikte titremesin
            var anim = g.gameObject.AddComponent<TrapAnim>();
            anim.Type = t;
            FxKit.TrapIdle(g, t, c + new Vector3(0f, 0.08f, 0f));
            Transform Mov(Vector3 p) { var m = Group(g, "mov", p); anim.Mov = m; return m; }
            switch (t)
            {
                case TrapType.Spikes:
                {
                    // delikli demir plaka; dikenler plakanın altında bekler
                    Part(g, Shape.Box, new Vector3(0f, 0.045f, 0f), new Vector3(0.36f, 0.02f, 0.36f), Mat(new Color(0.3f, 0.3f, 0.33f), 0.6f));
                    var m = Mov(Vector3.zero);
                    for (int i = 0; i < 9; i++)
                    {
                        var sp = new Vector3((i % 3 - 1) * 0.11f, 0.11f, (i / 3 - 1) * 0.11f);
                        Part(g, Shape.Cyl, new Vector3(sp.x, 0.056f, sp.z), new Vector3(0.05f, 0.003f, 0.05f), Dark); // delik
                        Part(m, Shape.Cone, sp, new Vector3(0.045f, 0.12f, 0.045f), Mat(new Color(0.78f, 0.8f, 0.84f), 0.85f));
                    }
                    break;
                }
                case TrapType.Snare:
                {
                    Part(g, Shape.Cyl, new Vector3(0f, 0.045f, 0f), new Vector3(0.3f, 0.012f, 0.3f), Iron);
                    Part(g, Shape.Cyl, new Vector3(0f, 0.055f, 0f), new Vector3(0.06f, 0.01f, 0.06f), Mat(new Color(0.75f, 0.2f, 0.15f), 0.5f)); // tetik
                    var jawMat = Mat(new Color(0.62f, 0.64f, 0.68f), 0.8f);
                    foreach (var side in new[] { -1f, 1f })
                    {
                        // menteşe kenarda; çene açıkken yatık durur, kapanınca yukarı kalkıp ortada buluşur
                        var jaw = Group(g, side < 0 ? "jawL" : "jawR", new Vector3(0.14f * side, 0.055f, 0f));
                        Part(jaw, Shape.Box, new Vector3(-0.07f * side, 0f, 0f), new Vector3(0.14f, 0.012f, 0.24f), jawMat);
                        for (int k = 0; k < 4; k++)
                            Part(jaw, Shape.Cone, new Vector3(-0.13f * side, 0.025f, -0.09f + k * 0.06f), new Vector3(0.025f, 0.05f, 0.025f), StoneLight);
                        if (side < 0) anim.JawL = jaw; else anim.JawR = jaw;
                    }
                    break;
                }
                case TrapType.Fire:
                {
                    Part(g, Shape.Cyl, new Vector3(0f, 0.045f, 0f), new Vector3(0.34f, 0.012f, 0.34f), Dark);
                    for (int i = 0; i < 4; i++) Part(g, Shape.Box, new Vector3(0f, 0.054f, -0.12f + i * 0.08f), new Vector3(0.3f, 0.008f, 0.02f), Iron); // ızgara
                    Part(g, Shape.Cyl, new Vector3(0f, 0.05f, 0f), new Vector3(0.26f, 0.005f, 0.26f), Mat(new Color(1f, 0.35f, 0.05f), 0.3f, 1.6f)); // kor
                    var m = Mov(new Vector3(0f, 0.055f, 0f));
                    for (int i = 0; i < 3; i++)
                    {
                        Part(m, Shape.Cone, new Vector3(-0.08f + i * 0.08f, 0.065f, (i - 1) * 0.04f), new Vector3(0.07f, 0.13f, 0.07f), Mat(new Color(1f, 0.45f, 0.1f), 0.2f, 2.5f));
                        Part(m, Shape.Cone, new Vector3(-0.08f + i * 0.08f, 0.05f, (i - 1) * 0.04f), new Vector3(0.04f, 0.08f, 0.04f), Mat(new Color(1f, 0.85f, 0.35f), 0.2f, 3f));
                    }
                    break;
                }
                case TrapType.Poison:
                {
                    var m = Mov(new Vector3(0f, 0.05f, 0f));
                    Part(m, Shape.Cyl, Vector3.zero, new Vector3(0.34f, 0.01f, 0.3f), Mat(new Color(0.35f, 0.75f, 0.2f), 0.9f, 0.6f));
                    Part(m, Shape.Cyl, new Vector3(0.06f, 0.004f, -0.03f), new Vector3(0.16f, 0.01f, 0.13f), Mat(new Color(0.5f, 0.95f, 0.3f), 0.95f, 1f));
                    for (int i = 0; i < 3; i++) { var b = Part(m, Shape.Ball, new Vector3(-0.08f + i * 0.08f, 0.02f, (i - 1) * 0.05f), Vector3.one * 0.05f, Mat(new Color(0.5f, 0.95f, 0.3f), 0.9f, 1.2f)); Animate(b, ArtMotion.Kind.Bob, 4f + i, 0.02f, i); }
                    break;
                }
                case TrapType.Frost:
                {
                    Part(g, Shape.Cyl, new Vector3(0f, 0.045f, 0f), new Vector3(0.34f, 0.01f, 0.34f), Mat(new Color(0.85f, 0.95f, 1f), 0.9f, 0.4f));
                    var m = Mov(new Vector3(0f, 0.05f, 0f));
                    var ice = Mat(new Color(0.6f, 0.9f, 1f), 0.95f, 1.2f);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * Mathf.PI * 0.4f;
                        Part(m, Shape.Pyramid, new Vector3(Mathf.Cos(a) * 0.1f, 0.05f, Mathf.Sin(a) * 0.1f), new Vector3(0.045f, 0.1f + (i % 2) * 0.04f, 0.045f), ice, new Vector3(Mathf.Sin(a) * 12f, 0f, -Mathf.Cos(a) * 12f));
                    }
                    Part(m, Shape.Pyramid, new Vector3(0f, 0.07f, 0f), new Vector3(0.06f, 0.15f, 0.06f), ice);
                    break;
                }
                case TrapType.Bomb:
                {
                    Part(g, Shape.Cyl, new Vector3(0f, 0.045f, 0f), new Vector3(0.24f, 0.01f, 0.24f), Mat(new Color(0.25f, 0.2f, 0.16f)));
                    var m = Mov(new Vector3(0f, 0.05f, 0f));
                    Part(m, Shape.Cyl, new Vector3(0f, 0.05f, 0f), new Vector3(0.16f, 0.1f, 0.16f), Mat(new Color(0.18f, 0.16f, 0.15f), 0.4f));
                    Part(m, Shape.Cyl, new Vector3(0f, 0.05f, 0f), new Vector3(0.17f, 0.02f, 0.17f), Iron);
                    Part(m, Shape.Cyl, new Vector3(0.03f, 0.12f, 0f), new Vector3(0.012f, 0.05f, 0.012f), Mat(new Color(0.55f, 0.45f, 0.3f))); // fitil
                    { var spark = Part(m, Shape.Ball, new Vector3(0.03f, 0.15f, 0f), Vector3.one * 0.035f, Mat(new Color(1f, 0.7f, 0.2f), 0.5f, 3f)); Animate(spark, ArtMotion.Kind.Bob, 12f, 0.01f); }
                    break;
                }
                case TrapType.Shock:
                {
                    Part(g, Shape.Box, new Vector3(0f, 0.046f, 0f), new Vector3(0.3f, 0.012f, 0.3f), Mat(new Color(0.5f, 0.52f, 0.58f), 0.8f));
                    foreach (var x in new[] { -0.12f, 0.12f }) Part(g, Shape.Cyl, new Vector3(x, 0.08f, x), new Vector3(0.04f, 0.06f, 0.04f), Mat(new Color(0.78f, 0.45f, 0.25f), 0.8f)); // bakır uçlar
                    var m = Mov(new Vector3(0f, 0.065f, 0f));
                    for (int i = 0; i < 3; i++) Part(m, Shape.Box, Vector3.zero, new Vector3(0.02f, 0.02f, 0.22f), Mat(new Color(1f, 0.95f, 0.4f), 0.9f, 3f), new Vector3(0, 30 + i * 60, 0));
                    break;
                }
                case TrapType.Slippery:
                {
                    var m = Mov(new Vector3(0f, 0.05f, 0f));
                    Part(m, Shape.Cyl, Vector3.zero, new Vector3(0.36f, 0.01f, 0.3f), Mat(new Color(0.45f, 0.75f, 0.95f), 0.95f, 0.2f));
                    Part(m, Shape.Cyl, new Vector3(-0.05f, 0.004f, 0.04f), new Vector3(0.12f, 0.01f, 0.08f), Mat(new Color(0.85f, 0.95f, 1f), 0.95f, 0.5f)); // parlama
                    break;
                }
            }
            return anim;
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
        /// <summary>Nehrin x konumu (z boyunca hafif kıvrılır); köyün sağ çitinin dışında.</summary>
        public static float RiverX(float z, int size) => size + 6.2f + Mathf.Sin(z * 0.22f) * 1.3f + Mathf.Sin(z * 0.07f + 1f) * 0.8f;

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
                    bool inRiver = Mathf.Abs(x - RiverX(z, size)) < 1.6f;              // nehir yatağı ve kıyısı
                    bool inFarm = x >= 1 && x <= size - 2 && z >= size + 2 && z <= size + 6; // köyün arkasındaki tarlalar
                    if (inPlot || inZone || inRiver || inFarm) continue;
                    int seed = x * 977 + z * 131;
                    float d = Mathf.Max(Mathf.Max(-1 - x, x - size), Mathf.Max(-attackRows - 1 - z, z - size));
                    float jx = (Rand(seed, 1) - 0.5f) * 0.6f, jz = (Rand(seed, 2) - 0.5f) * 0.6f;
                    var p = new Vector3(x + jx, 0f, z + jz);
                    float r = Rand(seed, 3);
                    if (d >= 2f && r < 0.3f + Mathf.Min(0.45f, (d - 2f) * 0.1f)) Tree(deco, p, 0.9f + Rand(seed, 4) * 0.7f, seed); // köy çevresinde açıklık
                    else if (r > 0.9f)
                    {
                        // çimen tutamı / devrik kütük / mantar
                        float k2 = Rand(seed, 11);
                        if (k2 < 0.6f)
                            for (int i = 0; i < 4; i++)
                                Part(deco, Shape.Cone, p + new Vector3((Rand(seed, 30 + i) - 0.5f) * 0.25f, 0.05f, (Rand(seed, 40 + i) - 0.5f) * 0.25f), new Vector3(0.04f, 0.12f, 0.04f), Color.Lerp(Leaf, LeafLight, Rand(seed, 50 + i)), new Vector3((Rand(seed, 60 + i) - 0.5f) * 30f, 0, (Rand(seed, 70 + i) - 0.5f) * 30f));
                        else if (k2 < 0.8f)
                            Part(deco, Shape.Cyl, p + new Vector3(0, 0.05f, 0), new Vector3(0.1f, 0.42f, 0.1f), Wood, new Vector3(90, Rand(seed, 12) * 180f, 0));
                        else
                        {
                            Part(deco, Shape.Cyl, p + new Vector3(0, 0.03f, 0), new Vector3(0.025f, 0.06f, 0.025f), new Color(0.95f, 0.92f, 0.85f));
                            Part(deco, Shape.Ball, p + new Vector3(0, 0.07f, 0), new Vector3(0.08f, 0.04f, 0.08f), new Color(0.85f, 0.18f, 0.12f));
                        }
                    }
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

            // gölet (köyün sol arkası): su + taş kenar + saz
            var pond = Group(parent, "pond", new Vector3(-5.2f, 0f, size + 2.5f));
            Part(pond, Shape.Cyl, new Vector3(0, -0.04f, 0), new Vector3(3.6f, 0.04f, 2.4f), Mat(new Color(0.25f, 0.52f, 0.75f), 0.95f, 0.05f));
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                Part(pond, Shape.Ball, new Vector3(Mathf.Cos(a) * 1.85f, 0.02f, Mathf.Sin(a) * 1.25f), new Vector3(0.28f, 0.14f, 0.22f), Color.Lerp(StoneDark, Stone, Rand(i, 77)), new Vector3(0, a * 57f, 0));
                if (i % 3 == 0)
                    for (int k = 0; k < 3; k++)
                        Part(pond, Shape.Cyl, new Vector3(Mathf.Cos(a) * 1.6f + k * 0.06f, 0.14f, Mathf.Sin(a) * 1.05f), new Vector3(0.02f, 0.3f, 0.02f), new Color(0.35f, 0.55f, 0.22f));
            }
            // uzak dağlar (arka ve yanlar; sis yumuşatır)
            var mtn = Group(parent, "mountains", Vector3.zero);
            for (int i = 0; i < 11; i++)
            {
                float t2 = i / 10f;
                var mp = Vector3.Lerp(new Vector3(-24f, 0, size + 26f), new Vector3(size + 26f, 0, size + 18f), t2) + new Vector3(Rand(i, 91) * 4f, 0, Rand(i, 92) * 5f);
                float hgt = 7f + Rand(i, 93) * 6f, w = 9f + Rand(i, 94) * 5f;
                Part(mtn, Shape.Cone, mp + new Vector3(0, hgt * 0.5f - 0.2f, 0), new Vector3(w, hgt, w * 0.8f), Color.Lerp(new Color(0.42f, 0.48f, 0.55f), new Color(0.52f, 0.58f, 0.62f), Rand(i, 95)));
                Part(mtn, Shape.Cone, mp + new Vector3(0, hgt * 0.84f - 0.2f, 0), new Vector3(w * 0.34f, hgt * 0.32f, w * 0.28f), new Color(0.95f, 0.96f, 0.98f));
            }

            // ---------- nehir (sağda kıvrılarak akar) + su değirmeni ----------
            var river = Group(parent, "river", Vector3.zero);
            var water = Mat(new Color(0.24f, 0.5f, 0.74f), 0.95f, 0.08f);
            var bank = DirtMat(new Color(0.62f, 0.55f, 0.4f), 2f);
            for (int z = -attackRows - 12; z < size + 14; z++)
            {
                float x0 = RiverX(z, size), x1 = RiverX(z + 1, size);
                var a = new Vector3(x0, 0f, z); var b = new Vector3(x1, 0f, z + 1);
                var mid = (a + b) * 0.5f; float len = Vector3.Distance(a, b) + 0.1f;
                float yaw = Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg;
                Part(river, Shape.Box, mid + new Vector3(0f, -0.075f, 0f), new Vector3(2.6f, 0.03f, len), bank, new Vector3(0f, yaw, 0f));
                Part(river, Shape.Box, mid + new Vector3(0f, -0.055f, 0f), new Vector3(1.8f, 0.03f, len), water, new Vector3(0f, yaw, 0f));
                if ((z & 3) == 0) Part(river, Shape.Ball, mid + new Vector3(Rand(z, 401) > 0.5f ? 1.05f : -1.05f, 0.0f, 0f), new Vector3(0.3f, 0.14f, 0.25f), Color.Lerp(StoneDark, Stone, Rand(z, 402)));
                if ((z % 5) == 2) for (int k = 0; k < 3; k++) Part(river, Shape.Cyl, mid + new Vector3(-1.1f + k * 0.07f, 0.12f, 0.1f * k), new Vector3(0.02f, 0.26f, 0.02f), new Color(0.35f, 0.55f, 0.22f)); // saz
            }
            var mill = Group(parent, "watermill", new Vector3(RiverX(size * 0.5f, size) - 1.9f, 0f, size * 0.5f));
            if (ModelLibrary.Available)
            {
                ModelLibrary.Spawn(mill, "Kenney/Town/watermill", Vector3.zero, 90f, 0.55f);
                var wheel = ModelLibrary.Spawn(mill, "Kenney/Town/wheel", new Vector3(0.62f, 0.32f, 0f), 90f, 0.55f);
                if (wheel != null) Animate(wheel.gameObject, ArtMotion.Kind.Wheel, -0.9f, 0f);
            }

            // ---------- köyün arkasında tarlalar + yel değirmeni ----------
            var farm = Group(parent, "farm", Vector3.zero);
            var wheat = new Color(0.92f, 0.78f, 0.35f); var crop = new Color(0.45f, 0.68f, 0.3f);
            for (int row = 0; row < 8; row++)
            {
                float z = size + 2.3f + row * 0.5f;
                Part(farm, Shape.Box, new Vector3(cx - 0.5f, -0.06f, z), new Vector3(size - 3.5f, 0.04f, 0.42f), DirtMat(new Color(0.5f, 0.38f, 0.25f), 3f));
                var cc = row < 4 ? wheat : crop;
                for (int k = 0; k < (int)(size - 4); k++)
                {
                    var bush = Part(farm, Shape.Box, new Vector3(1.5f + k, 0.05f, z), new Vector3(0.7f, 0.16f + Rand(row * 31 + k, 5) * 0.08f, 0.26f), Color.Lerp(cc, Color.white, Rand(row * 17 + k, 6) * 0.15f));
                    if (row < 4) Animate(bush, ArtMotion.Kind.Sway, 1.2f + Rand(k, row) * 0.4f, 0.04f, k * 0.3f + row); // rüzgârda dalgalanan buğday
                }
            }
            // korkuluk
            var scare = Group(farm, "scarecrow", new Vector3(cx + 2.2f, 0f, size + 4.2f));
            Part(scare, Shape.Cyl, new Vector3(0f, 0.3f, 0f), new Vector3(0.04f, 0.6f, 0.04f), WoodDark);
            Part(scare, Shape.Box, new Vector3(0f, 0.45f, 0f), new Vector3(0.4f, 0.04f, 0.04f), WoodDark);
            Part(scare, Shape.Box, new Vector3(0f, 0.42f, 0f), new Vector3(0.18f, 0.2f, 0.1f), new Color(0.65f, 0.3f, 0.2f));
            Part(scare, Shape.Ball, new Vector3(0f, 0.62f, 0f), Vector3.one * 0.13f, new Color(0.9f, 0.75f, 0.45f));
            Part(scare, Shape.Cone, new Vector3(0f, 0.72f, 0f), new Vector3(0.22f, 0.1f, 0.22f), new Color(0.75f, 0.6f, 0.3f));
            if (ModelLibrary.Available)
            {
                var wm = Group(parent, "windmill", new Vector3(-3.4f, 0f, size + 5.2f));
                ModelLibrary.Spawn(wm, "Kenney/Town/windmill", Vector3.zero, 135f, 0.7f);
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

        private static readonly Color[] HairColors = { new Color(0.25f, 0.16f, 0.09f), new Color(0.12f, 0.09f, 0.07f), new Color(0.62f, 0.42f, 0.18f), new Color(0.48f, 0.22f, 0.10f) };

        /// <summary>
        /// İnsansı karakter (kullanıcı isteği 15.11.2026): kalçadan dönen bacaklar ("legL/legR"), omuzdan dönen kollar
        /// ("armL/armR"), çizme, tunik + etek, boyun, gözler, saç. CharacterRig yürürken uzuvları sallar.
        /// Boyutlar s ile ölçeklenir; ekipman (HumanGear) eski gövde ölçülerine oturur (baş merkezi 0,31s).
        /// </summary>
        private static void Human(Transform g, Color tunic, Color skin, float s, bool helmet, bool hood)
        {
            var pants = Color.Lerp(tunic, Dark, 0.55f);
            var boot = new Color(0.28f, 0.18f, 0.10f);
            foreach (var side in new[] { -1f, 1f })
            {
                var leg = Group(g, side < 0 ? "legL" : "legR", new Vector3(0.036f * side, 0.115f, 0) * s);
                Part(leg, Shape.Cyl, new Vector3(0, -0.05f, 0) * s, new Vector3(0.046f, 0.1f, 0.046f) * s, pants);
                Part(leg, Shape.Box, new Vector3(0, -0.1f, 0.012f) * s, new Vector3(0.052f, 0.03f, 0.075f) * s, boot);
            }
            Part(g, Shape.Cyl, new Vector3(0, 0.125f, 0) * s, new Vector3(0.14f, 0.05f, 0.11f) * s, Color.Lerp(tunic, Dark, 0.15f)); // etek
            Part(g, Shape.Capsule, new Vector3(0, 0.19f, 0) * s, new Vector3(0.13f, 0.16f, 0.1f) * s, tunic);                      // gövde
            Part(g, Shape.Cyl, new Vector3(0, 0.265f, 0) * s, new Vector3(0.04f, 0.025f, 0.04f) * s, skin);                       // boyun
            foreach (var side in new[] { -1f, 1f })
            {
                var arm = Group(g, side < 0 ? "armL" : "armR", new Vector3(0.083f * side, 0.245f, 0) * s);
                Part(arm, Shape.Cyl, new Vector3(0, -0.05f, 0) * s, new Vector3(0.036f, 0.1f, 0.036f) * s, tunic);
                Part(arm, Shape.Ball, new Vector3(0, -0.105f, 0.005f) * s, new Vector3(0.038f, 0.038f, 0.038f) * s, skin);
            }
            Part(g, Shape.Ball, new Vector3(0, 0.31f, 0) * s, new Vector3(0.105f, 0.105f, 0.105f) * s, skin);                     // baş
            foreach (var x in new[] { -0.022f, 0.022f })
                Part(g, Shape.Ball, new Vector3(x, 0.318f, 0.048f) * s, new Vector3(0.014f, 0.016f, 0.01f) * s, Dark);             // gözler
            Part(g, Shape.Ball, new Vector3(0, 0.302f, 0.052f) * s, new Vector3(0.016f, 0.016f, 0.014f) * s, Color.Lerp(skin, new Color(0.8f, 0.4f, 0.3f), 0.3f)); // burun
            if (!helmet && !hood)
            {
                int h = Mathf.Abs((int)(tunic.r * 97 + tunic.g * 31 + tunic.b * 13)) % HairColors.Length;
                Part(g, Shape.Ball, new Vector3(0, 0.334f, -0.008f) * s, new Vector3(0.112f, 0.07f, 0.112f) * s, HairColors[h]); // saç
            }
            if (helmet) Part(g, Shape.Ball, new Vector3(0, 0.34f * s, 0), new Vector3(0.12f, 0.07f, 0.12f) * s, Mat(StoneLight, 0.7f));
            if (hood) Part(g, Shape.Cone, new Vector3(0, 0.38f * s, 0), new Vector3(0.13f, 0.12f, 0.13f) * s, Color.Lerp(tunic, Dark, 0.3f));
        }

        /// <summary>Asker modelleri okunabilirlik için hücreye göre büyük çizilir (izometrik uzak kamera).</summary>
        public const float SoldierScale = 1.8f;

        // ---------- Asker geliştirme görselleri: HER seviye bir görsel ekler (birikimli, Sv2..Sv30) ----------
        private static readonly string[] LevelVisuals =
        {
            "", "", "Kemer", "Deri yelek", "Bileklik", "Deri omuzluk", "Demir göğüslük", "Demir omuzluk", "Miğfer siperi", "Sorguç", "Pelerin",
            "Çelik parıltısı", "Mavi aura", "İşlemeli pelerin", "Silah parıltısı", "Yörünge kıvılcımı", "Altın toka", "Altın omuzluk", "Mor aura",
            "Boynuzlu miğfer", "Hale", "Altın zırh", "İkinci kıvılcım", "Uzun pelerin", "Parlayan gözler", "Taç", "Altın aura", "Üçüncü kıvılcım",
            "Işık kanatları", "Işık sütunu", "Efsane parıltısı"
        };

        /// <summary>Bu seviyeye geliştirince eklenen görselin adı (arayüz için).</summary>
        public static string LevelVisualName(int level) => level >= 2 && level < LevelVisuals.Length ? LevelVisuals[level] : "";

        /// <summary>Rütbe (GDD Bölüm 11 gelişim fikri: Kılıçlı → Eğitimli → Uzman → Zırhlı → Gladyatör).</summary>
        public static string RankName(int level) =>
            level >= 25 ? "Efsane" : level >= 20 ? "Kahraman" : level >= 15 ? "Usta" : level >= 10 ? "Zırhlı" : level >= 6 ? "Uzman" : level >= 3 ? "Eğitimli" : "Acemi";

        private static readonly Color Leather = new Color(0.50f, 0.33f, 0.18f), IronC = new Color(0.58f, 0.60f, 0.64f), Steel = new Color(0.86f, 0.89f, 0.94f);

        private static Material MetalFor(int level, int ironAt, int steelAt, int goldAt)
        {
            if (level >= goldAt) return Mat(GoldC, 0.85f, 0.25f);
            if (level >= steelAt) return Mat(Steel, 0.9f, 0.05f);
            if (level >= ironAt) return Mat(IronC, 0.6f);
            return Mat(Leather, 0.25f);
        }

        /// <summary>İnsansı asker ekipmanı (a: gövde kökü, s: boy ölçeği). Her seviye bir parça ekler veya malzemeyi yükseltir.</summary>
        private static void HumanGear(Transform a, int L, float s)
        {
            if (L >= 2) Part(a, Shape.Box, new Vector3(0, 0.1f, 0) * s, new Vector3(0.15f, 0.025f, 0.13f) * s, Mat(Color.Lerp(Leather, Dark, 0.3f), 0.2f));
            if (L >= 16) Part(a, Shape.Box, new Vector3(0, 0.1f, 0.066f) * s, new Vector3(0.035f, 0.03f, 0.01f) * s, Mat(GoldC, 0.9f, 0.4f));
            if (L >= 3) Part(a, Shape.Box, new Vector3(0, 0.17f, 0) * s, new Vector3(0.152f, 0.09f, 0.132f) * s, MetalFor(L, 6, 11, 21));
            if (L >= 4) foreach (var x in new[] { -0.085f, 0.085f }) Part(a, Shape.Box, new Vector3(x, 0.12f, 0.01f) * s, new Vector3(0.035f, 0.05f, 0.035f) * s, MetalFor(L, 7, 11, 21));
            if (L >= 5) foreach (var x in new[] { -0.08f, 0.08f }) Part(a, Shape.Ball, new Vector3(x, 0.22f, 0) * s, new Vector3(0.075f, 0.05f, 0.075f) * s, MetalFor(L, 7, 11, 17));
            if (L >= 8) Part(a, Shape.Box, new Vector3(0, 0.325f, 0.056f) * s, new Vector3(0.1f, 0.018f, 0.02f) * s, Mat(IronC, 0.7f));
            if (L >= 9) Part(a, Shape.Cone, new Vector3(0, 0.42f, -0.01f) * s, new Vector3(0.04f, 0.09f, 0.06f) * s, new Color(0.85f, 0.15f, 0.15f));
            if (L >= 10)
            {
                float capeH = L >= 23 ? 0.28f : 0.22f;
                var capeC = L >= 21 ? new Color(0.45f, 0.08f, 0.40f) : new Color(0.65f, 0.1f, 0.12f);
                Part(a, Shape.Box, new Vector3(0, 0.26f - capeH * 0.5f, -0.075f) * s, new Vector3(0.14f, capeH, 0.01f) * s, Mat(capeC, 0.2f), new Vector3(-8, 0, 0));
                if (L >= 13) Part(a, Shape.Box, new Vector3(0, 0.26f - capeH + 0.01f, -0.083f) * s, new Vector3(0.145f, 0.02f, 0.012f) * s, Mat(GoldC, 0.9f, 0.3f), new Vector3(-8, 0, 0));
            }
            if (L >= 14) Part(a, Shape.Ball, new Vector3(0.095f, 0.2f, 0.05f) * s, new Vector3(0.05f, 0.05f, 0.05f) * s, Mat(L >= 21 ? GoldC : new Color(0.5f, 0.85f, 1f), 0.9f, 2.2f));
            if (L >= 19) foreach (var x in new[] { -1f, 1f }) Part(a, Shape.Cone, new Vector3(0.06f * x, 0.385f, 0) * s, new Vector3(0.025f, 0.08f, 0.025f) * s, new Color(0.95f, 0.92f, 0.82f), new Vector3(0, 0, -40f * x));
            if (L >= 24) foreach (var x in new[] { -0.022f, 0.022f }) Part(a, Shape.Ball, new Vector3(x, 0.315f, 0.052f) * s, new Vector3(0.016f, 0.016f, 0.016f) * s, Mat(new Color(0.4f, 0.95f, 1f), 0.9f, 3f));
            if (L >= 25) Part(a, Shape.Cyl, new Vector3(0, 0.39f, 0) * s, new Vector3(0.1f, 0.03f, 0.1f) * s, Mat(GoldC, 0.95f, 0.5f));
        }

        /// <summary>İnsansı olmayan birim (Mancınık) için: Sv2-11 metal levha/perçin, Sv9 bayrak; efektler ortak.</summary>
        private static void FrameGear(Transform a, int L)
        {
            var spots = new[]
            {
                new Vector3(-0.13f, 0.11f, 0.12f), new Vector3(0.13f, 0.11f, 0.12f), new Vector3(-0.13f, 0.11f, -0.12f), new Vector3(0.13f, 0.11f, -0.12f), new Vector3(0, 0.11f, 0.18f),
                new Vector3(0, 0.11f, -0.18f), new Vector3(-0.09f, 0.25f, 0.04f), new Vector3(0.09f, 0.25f, 0.04f), new Vector3(-0.13f, 0.11f, 0), new Vector3(0.13f, 0.11f, 0)
            };
            for (int lv = 2; lv <= Mathf.Min(L, 11); lv++)
                Part(a, Shape.Box, spots[lv - 2], new Vector3(0.05f, 0.05f, 0.05f), MetalFor(L, 6, 11, 21));
            if (L >= 9) Flag(a, new Vector3(0.11f, 0.1f, -0.15f), 0.35f, new Color(0.85f, 0.15f, 0.15f));
        }

        /// <summary>Sv12+ efektleri (tüm türler): aura, yörünge kıvılcımları, hale, ışık kanatları/sütunu, efsane parıltısı.</summary>
        private static void LevelEffects(Transform g, int L, float foot, float headY)
        {
            if (L >= 12)
            {
                var ac = L >= 26 ? GoldC : (L >= 18 ? new Color(0.7f, 0.35f, 1f) : new Color(0.35f, 0.7f, 1f));
                var aura = Part(g, Shape.Cyl, new Vector3(0, 0.012f, 0), new Vector3(0.2f * foot, 0.004f, 0.2f * foot), Mat(ac, 0.5f, 0.9f));
                Animate(aura, ArtMotion.Kind.Bob, 3f, 0.004f);
            }
            if (L >= 15)
            {
                var orbit = Group(g, "orbit", new Vector3(0, headY * 0.55f, 0));
                Animate(orbit.gameObject, ArtMotion.Kind.Spin, 4f);
                var sc = Mat(L >= 21 ? GoldC : new Color(0.6f, 0.9f, 1f), 0.9f, 3f);
                Part(orbit, Shape.Ball, new Vector3(0.12f * foot, 0, 0), Vector3.one * 0.028f, sc);
                if (L >= 22) Part(orbit, Shape.Ball, new Vector3(-0.12f * foot, 0.04f, 0), Vector3.one * 0.028f, sc);
                if (L >= 27) Part(orbit, Shape.Ball, new Vector3(0, -0.04f, 0.12f * foot), Vector3.one * 0.028f, sc);
            }
            if (L >= 20)
            {
                var halo = Part(g, Shape.Cyl, new Vector3(0, headY + 0.04f, 0), new Vector3(L >= 30 ? 0.16f : 0.12f, 0.006f, L >= 30 ? 0.16f : 0.12f), Mat(new Color(1f, 0.92f, 0.5f), 0.9f, 2.5f));
                Animate(halo, ArtMotion.Kind.Bob, 2f, 0.01f);
            }
            if (L >= 28)
                foreach (var x in new[] { -1f, 1f })
                {
                    var wg = Group(g, "lightWing", new Vector3(0.04f * x, headY * 0.6f, -0.06f));
                    Part(wg, Shape.Box, new Vector3(0.1f * x, 0.03f, 0), new Vector3(0.2f, 0.01f, 0.08f), Mat(new Color(1f, 0.95f, 0.75f), 0.9f, 2f), new Vector3(0, 0, 25f * x));
                    Animate(wg.gameObject, ArtMotion.Kind.Flap, 5f, 15f * x);
                }
            if (L >= 29) { var col = Part(g, Shape.Cyl, new Vector3(0, headY + 0.13f, 0), new Vector3(0.012f, 0.14f, 0.012f), Mat(new Color(1f, 0.95f, 0.8f), 0.9f, 2.5f)); Animate(col, ArtMotion.Kind.Bob, 2.5f, 0.015f); }
            if (L >= 30)
            {
                var ring = Part(g, Shape.Cyl, new Vector3(0, 0.02f, 0), new Vector3(0.28f * foot, 0.003f, 0.28f * foot), Mat(new Color(1f, 0.9f, 0.5f), 0.9f, 1.4f));
                Animate(ring, ArtMotion.Kind.Bob, 1.5f, 0.01f, 1f);
            }
        }

        public static Transform BuildSoldier(Transform parent, SoldierType t, int level = 1)
        {
            var model = ModelArt.TryBuildSoldier(parent, t, level);
            if (model != null) return model;
            int tier = SoldierTier(level);
            var root = Group(parent, t.ToString(), Vector3.zero, 0f, SoldierScale * (1f + tier * 0.04f));
            var g = Group(root, "model", Vector3.zero);
            var tunic = TunicOf(t);
            int L = Mathf.Clamp(level, 1, 30);
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
                    HumanGear(rider, L, 0.9f);
                    if (L >= 6) Part(g, Shape.Box, new Vector3(0, 0.15f, 0), new Vector3(0.135f, 0.07f, 0.24f), MetalFor(L, 6, 11, 21)); // at zırhı
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
                    HumanGear(dr, L, 0.7f);
                    break;
            }
            // insansı askerlerde ekipman; diğerlerinde gövde süsü; Sv12+ efektler herkes için
            switch (t)
            {
                case SoldierType.Swordsman: case SoldierType.Archer: case SoldierType.Mage: case SoldierType.Spearman: HumanGear(g, L, 1f); break;
                case SoldierType.Goblin: HumanGear(g, L, 0.8f); break;
                case SoldierType.Fairy: HumanGear(g, L, 0.7f); break;
                case SoldierType.Giant: HumanGear(g, L, 2f); break;
                case SoldierType.Catapult: FrameGear(g, L); break;
            }
            float foot = t == SoldierType.Giant || t == SoldierType.DragonRider ? 1.8f : (t == SoldierType.Cavalry || t == SoldierType.Catapult ? 1.4f : 1f);
            float headY = t == SoldierType.Giant ? 0.82f : (t == SoldierType.Cavalry ? 0.58f : (t == SoldierType.DragonRider ? 0.5f : (t == SoldierType.Catapult ? 0.4f : (t == SoldierType.Fairy ? 0.3f : (t == SoldierType.Goblin ? 0.34f : 0.42f)))));
            LevelEffects(g, L, foot, headY);
            var rig = root.gameObject.AddComponent<CharacterRig>();
            if (t == SoldierType.Giant) { rig.StrideSpeed = 6f; rig.StrideDeg = 26f; }
            else if (t == SoldierType.Goblin) rig.StrideSpeed = 15f;
            else if (t == SoldierType.Cavalry) { rig.StrideSpeed = 14f; rig.StrideDeg = 40f; }
            return root;
        }

        private static void Horse(Transform g, Color c)
        {
            Part(g, Shape.Box, new Vector3(0, 0.14f, 0), new Vector3(0.12f, 0.1f, 0.3f), c);
            Part(g, Shape.Box, new Vector3(0, 0.22f, 0.15f), new Vector3(0.07f, 0.14f, 0.07f), c, new Vector3(-30, 0, 0));
            Part(g, Shape.Box, new Vector3(0, 0.27f, 0.2f), new Vector3(0.06f, 0.06f, 0.12f), c);
            foreach (var x in new[] { -0.04f, 0.04f }) foreach (var z in new[] { -0.11f, 0.11f })
                {
                    var leg = Group(g, (x < 0) == (z < 0) ? "legL" : "legR", new Vector3(x, 0.1f, z)); // çapraz bacaklar birlikte
                    Part(leg, Shape.Box, new Vector3(0, -0.05f, 0), new Vector3(0.025f, 0.1f, 0.025f), Color.Lerp(c, Dark, 0.4f));
                    Part(leg, Shape.Box, new Vector3(0, -0.095f, 0.004f), new Vector3(0.03f, 0.015f, 0.032f), Dark); // toynak
                }
            Part(g, Shape.Box, new Vector3(0, 0.14f, -0.17f), new Vector3(0.02f, 0.08f, 0.04f), Dark, new Vector3(-30, 0, 0));
        }

        // ---------- Savaş efektleri ----------
        public static Transform Projectile(Transform parent, TowerType t)
        {
            var g = Group(parent, "shot", Vector3.zero);
            switch (t)
            {
                case TowerType.Archer: Part(g, Shape.Box, Vector3.zero, new Vector3(0.02f, 0.02f, 0.22f), WoodDark); break;
                case TowerType.Cannon: Part(g, Shape.Ball, Vector3.zero, new Vector3(0.13f, 0.13f, 0.13f), Mat(new Color(0.12f, 0.12f, 0.13f), 0.5f)); break;
                case TowerType.Ice: Part(g, Shape.Pyramid, Vector3.zero, new Vector3(0.06f, 0.06f, 0.24f), Mat(new Color(0.6f, 0.95f, 1f), 0.95f, 2.5f), new Vector3(90, 0, 0)); break;
                case TowerType.Lightning: Part(g, Shape.Box, Vector3.zero, new Vector3(0.03f, 0.03f, 0.5f), Mat(new Color(1f, 0.95f, 0.5f), 0.9f, 3.5f)); break;
                case TowerType.Mage: Part(g, Shape.Ball, Vector3.zero, new Vector3(0.14f, 0.14f, 0.14f), Mat(new Color(0.8f, 0.45f, 1f), 0.9f, 2.5f)); break;
                default: Part(g, Shape.Ball, Vector3.zero, new Vector3(0.08f, 0.08f, 0.08f), Mat(new Color(0.4f, 1f, 0.75f), 0.9f, 2.5f)); break;
            }
            return g;
        }

        /// <summary>Kule elementinin rengi (aura, mermi, isabet).</summary>
        public static Color ElementColor(TowerElement e)
        {
            switch (e)
            {
                case TowerElement.Arrow: return new Color(1f, 0.82f, 0.25f);
                case TowerElement.Fire: return new Color(1f, 0.42f, 0.08f);
                case TowerElement.Ice: return new Color(0.55f, 0.9f, 1f);
                case TowerElement.Poison: return new Color(0.45f, 0.95f, 0.25f);
                case TowerElement.Magic: return new Color(0.75f, 0.35f, 1f);
                case TowerElement.Lightning: return new Color(1f, 0.97f, 0.55f);
                default: return Color.white;
            }
        }

        /// <summary>Elementli kulenin tepesinde süzülen parlak küre + etrafında dönen 3 parçacık (yalnızca görsel).</summary>
        public static void ElementAura(Transform parent, Vector3 pos, TowerElement e, float height)
        {
            if (e == TowerElement.None) return;
            var c = ElementColor(e);
            var g = Group(parent, "aura", pos + new Vector3(0f, height, 0f));
            var core = Group(g, "core", Vector3.zero);
            Part(core, Shape.Ball, Vector3.zero, Vector3.one * 0.11f, Mat(Color.Lerp(c, Color.white, 0.35f), 0.9f, 1.4f));
            Part(core, Shape.Ball, Vector3.zero, Vector3.one * 0.19f, Mat(c, 0.9f, 0.6f));
            Animate(core.gameObject, ArtMotion.Kind.Bob, 2.2f, 0.05f);
            var ring = Group(g, "ring", Vector3.zero);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.094f;
                Part(ring, e == TowerElement.Ice ? Shape.Pyramid : Shape.Ball, new Vector3(Mathf.Cos(a) * 0.22f, Mathf.Sin(a * 2f) * 0.04f, Mathf.Sin(a) * 0.22f), Vector3.one * 0.06f, Mat(c, 0.9f, 1f));
            }
            Animate(ring.gameObject, ArtMotion.Kind.Spin, e == TowerElement.Lightning ? 6f : 2.5f, 0f);
        }

        /// <summary>Okçu oku: gövde, demir uç, tüy (z ekseni boyunca, uç önde).</summary>
        public static Transform Arrow(Transform parent)
        {
            var g = Group(parent, "arrow", Vector3.zero);
            Part(g, Shape.Box, Vector3.zero, new Vector3(0.028f, 0.028f, 0.4f), Mat(new Color(0.72f, 0.52f, 0.3f)));
            Part(g, Shape.Pyramid, new Vector3(0f, 0f, 0.23f), new Vector3(0.07f, 0.07f, 0.09f), Mat(new Color(0.85f, 0.87f, 0.9f), 0.8f, 0.3f), new Vector3(90, 0, 0));
            Part(g, Shape.Box, new Vector3(0f, 0f, -0.17f), new Vector3(0.09f, 0.008f, 0.09f), Mat(new Color(1f, 1f, 0.96f), 0.2f, 0.4f));
            Part(g, Shape.Box, new Vector3(0f, 0f, -0.17f), new Vector3(0.008f, 0.09f, 0.09f), Mat(new Color(0.95f, 0.22f, 0.18f), 0.2f, 0.4f));
            return g;
        }

        /// <summary>Parlayan büyü küresi (çekirdek + hale), dönen kıvılcımlı.</summary>
        public static Transform MagicOrb(Transform parent, Color c, float size)
        {
            var g = Group(parent, "orb", Vector3.zero);
            Part(g, Shape.Ball, Vector3.zero, Vector3.one * size * 0.6f, Mat(Color.Lerp(c, Color.white, 0.6f), 0.9f, 3.5f));
            Part(g, Shape.Ball, Vector3.zero, Vector3.one * size, Mat(c, 0.9f, 2.2f));
            for (int i = 0; i < 3; i++)
            {
                float an = i * 2.094f;
                Part(g, Shape.Ball, new Vector3(Mathf.Cos(an) * size * 0.8f, Mathf.Sin(an) * size * 0.8f, -size * 0.6f), Vector3.one * size * 0.3f, Mat(c, 0.9f, 2.5f));
            }
            return g;
        }

        /// <summary>Alev topu: beyaz-sarı çekirdek, turuncu alev, kırmızı dış hale ve arkada alev kuyruğu (z ekseni ileri).</summary>
        public static Transform Fireball(Transform parent, float size)
        {
            var g = Group(parent, "fireball", Vector3.zero);
            Part(g, Shape.Ball, Vector3.zero, Vector3.one * size * 0.55f, Mat(new Color(1f, 0.95f, 0.7f), 0.9f, 4f));
            Part(g, Shape.Ball, Vector3.zero, Vector3.one * size * 0.8f, Mat(new Color(1f, 0.62f, 0.12f), 0.9f, 3f));
            Part(g, Shape.Ball, new Vector3(0f, 0f, -size * 0.1f), Vector3.one * size, Mat(new Color(0.95f, 0.25f, 0.05f), 0.9f, 2f));
            for (int i = 0; i < 3; i++)
            {
                float an = i * 2.094f, r = size * 0.22f;
                Part(g, Shape.Cone, new Vector3(Mathf.Cos(an) * r, Mathf.Sin(an) * r, -size * 0.75f), new Vector3(size * 0.45f, size * 1.1f, size * 0.45f),
                    Mat(i == 0 ? new Color(1f, 0.55f, 0.1f) : new Color(1f, 0.35f, 0.05f), 0.9f, 2.4f), new Vector3(-90f, 0f, 0f));
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
