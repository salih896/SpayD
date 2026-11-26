using System.Collections.Generic;
using GameCore.Combat;
using UnityEngine;

namespace GameClient
{
    /// <summary>
    /// Element efekt kiti (yalnızca görsel; oyun mantığına dokunmaz). Kodla üretilen dokular (parıltı, halka, duman, yıldız)
    /// + Unity parçacık sistemleri. Kule bekleme efektleri (alev, kar, kabarcık, kıvılcım), atış parlaması, mermi izi ve
    /// element isabetleri (patlama, buz dalgası, zehir sıçraması, büyü patlaması, yıldırım).
    /// Mobil için sınırlı parçacık sayısı; tek seferlik efektler bitince kendini siler.
    /// </summary>
    public static class FxKit
    {
        // ---------- Element renkleri: çekirdek, orta, dış, duman ----------
        public struct Palette { public Color Core, Mid, Outer, Smoke; }

        public static Palette Colors(TowerElement e)
        {
            switch (e)
            {
                case TowerElement.Fire: return new Palette { Core = new Color(1f, 0.95f, 0.6f), Mid = new Color(1f, 0.55f, 0.1f), Outer = new Color(0.85f, 0.18f, 0.03f), Smoke = new Color(0.22f, 0.18f, 0.16f) };
                case TowerElement.Ice: return new Palette { Core = new Color(0.95f, 1f, 1f), Mid = new Color(0.55f, 0.88f, 1f), Outer = new Color(0.3f, 0.55f, 1f), Smoke = new Color(0.8f, 0.92f, 1f) };
                case TowerElement.Poison: return new Palette { Core = new Color(0.85f, 1f, 0.5f), Mid = new Color(0.45f, 0.95f, 0.2f), Outer = new Color(0.2f, 0.5f, 0.1f), Smoke = new Color(0.35f, 0.62f, 0.15f) };
                case TowerElement.Magic: return new Palette { Core = new Color(1f, 0.85f, 1f), Mid = new Color(0.78f, 0.4f, 1f), Outer = new Color(0.45f, 0.15f, 0.9f), Smoke = new Color(0.4f, 0.2f, 0.6f) };
                case TowerElement.Lightning: return new Palette { Core = Color.white, Mid = new Color(1f, 0.97f, 0.6f), Outer = new Color(0.55f, 0.75f, 1f), Smoke = new Color(0.6f, 0.65f, 0.75f) };
                case TowerElement.Arrow: return new Palette { Core = new Color(1f, 1f, 0.85f), Mid = new Color(1f, 0.82f, 0.3f), Outer = new Color(0.9f, 0.6f, 0.15f), Smoke = new Color(0.6f, 0.55f, 0.45f) };
                default: return new Palette { Core = new Color(1f, 0.95f, 0.8f), Mid = new Color(0.85f, 0.75f, 0.55f), Outer = new Color(0.55f, 0.45f, 0.3f), Smoke = new Color(0.55f, 0.5f, 0.42f) };
            }
        }

        // ---------- Dokular ----------
        private static Texture2D glow, ring, smoke, star, beam;
        /// <summary>Çizgi (yıldırım) için: enine yumuşak, boyuna dolu.</summary>
        public static Texture2D Beam => beam ? beam : (beam = MakeTex(32, (x, y) => { float a = Mathf.Clamp01(1f - Mathf.Abs(y)); return a * a; }));
        public static Texture2D Glow => glow ? glow : (glow = MakeTex(64, (x, y) => { float r = Mathf.Sqrt(x * x + y * y); float a = Mathf.Clamp01(1f - r); return a * a; }));
        public static Texture2D Ring => ring ? ring : (ring = MakeTex(64, (x, y) => { float r = Mathf.Sqrt(x * x + y * y); float d = (r - 0.78f) / 0.1f; return Mathf.Exp(-d * d) * (r < 1f ? 1f : 0f); }));
        public static Texture2D Smoke => smoke ? smoke : (smoke = MakeTex(64, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float n = Mathf.PerlinNoise(x * 3f + 7.3f, y * 3f + 2.1f) * 0.5f + 0.5f;
            float a = Mathf.Clamp01(1f - r * (0.85f + 0.3f * n));
            return a * a * (0.6f + 0.4f * n);
        }));
        public static Texture2D Star => star ? star : (star = MakeTex(64, (x, y) =>
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            float rays = Mathf.Exp(-ax * 14f) * Mathf.Exp(-ay * ay * 4f) + Mathf.Exp(-ay * 14f) * Mathf.Exp(-ax * ax * 4f);
            float core = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 2.5f);
            return Mathf.Clamp01(rays + core * core);
        }));

        private static Texture2D MakeTex(int n, System.Func<float, float, float> alpha)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = (i + 0.5f) / n * 2f - 1f, y = (j + 0.5f) / n * 2f - 1f;
                    px[j * n + i] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(x, y)));
                }
            t.SetPixels(px); t.Apply(false, true);
            return t;
        }

        // ---------- Materyaller ----------
        private static Shader shader;
        private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        /// <summary>additive: toplamalı parlama; değilse alfa karışımı (duman).</summary>
        public static Material Mat(Texture2D tex, bool additive)
        {
            string key = tex.GetInstanceID() + (additive ? "+" : "a");
            if (mats.TryGetValue(key, out var m) && m != null) return m;
            if (shader == null) shader = Resources.Load<Shader>("Fx/FxParticle");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // yedek: yalnızca alfa karışımı
            m = new Material(shader) { mainTexture = tex, renderQueue = 3000 };
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", additive ? 1f : 10f);
            mats[key] = m;
            return m;
        }

        // ---------- Parçacık kurucu ----------
        private static Gradient Grad(Color a, Color b, Color c, float aIn = 0.1f, float aOut = 1f)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.45f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, aIn), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, aOut) });
            return g;
        }

        private static AnimationCurve Curve(params float[] kv)
        {
            var c = new AnimationCurve();
            for (int i = 0; i + 1 < kv.Length; i += 2) c.AddKey(kv[i], kv[i + 1]);
            return c;
        }

        /// <summary>Boş parçacık sistemi: dünya uzayı; Go() ile başlar; loop değilse bitince silinir.</summary>
        public static ParticleSystem PS(Transform parent, Vector3 worldPos, string name, Material mat, bool loop, float duration = 1f, int max = 40)
        {
            var go = new GameObject("fx_" + name);
            go.SetActive(false); // yapılandırma bitene kadar çalışmasın
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var ps = go.AddComponent<ParticleSystem>();
            var m = ps.main;
            m.playOnAwake = true; m.loop = loop; m.duration = duration; m.maxParticles = max;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.scalingMode = ParticleSystemScalingMode.Hierarchy;
            if (!loop) m.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortMode = ParticleSystemSortMode.Distance;
            return ps;
        }

        private static ParticleSystem Go(ParticleSystem ps) { ps.gameObject.SetActive(true); return ps; }

        private static void Life(ParticleSystem ps, float lo, float hi, float sizeLo, float sizeHi, float speedLo = 0f, float speedHi = 0f)
        {
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(lo, hi);
            m.startSize = new ParticleSystem.MinMaxCurve(sizeLo, sizeHi);
            m.startSpeed = new ParticleSystem.MinMaxCurve(speedLo, speedHi);
        }

        private static void Burst(ParticleSystem ps, int count) { var em = ps.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) }); }
        private static void Rate(ParticleSystem ps, float r) { var em = ps.emission; em.rateOverTime = r; }
        private static void ColorLife(ParticleSystem ps, Gradient g) { var c = ps.colorOverLifetime; c.enabled = true; c.color = g; }
        private static void SizeLife(ParticleSystem ps, AnimationCurve c) { var s = ps.sizeOverLifetime; s.enabled = true; s.size = new ParticleSystem.MinMaxCurve(1f, c); }
        private static void Sphere(ParticleSystem ps, float radius, bool hemi = false) { var sh = ps.shape; sh.enabled = true; sh.shapeType = hemi ? ParticleSystemShapeType.Hemisphere : ParticleSystemShapeType.Sphere; sh.radius = radius; sh.rotation = hemi ? new Vector3(-90f, 0f, 0f) : Vector3.zero; }
        private static void Circle(ParticleSystem ps, float radius) { var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius; sh.rotation = new Vector3(90f, 0f, 0f); }
        private static void Gravity(ParticleSystem ps, float g) { var m = ps.main; m.gravityModifier = g; }
        private static void Up(ParticleSystem ps, float lo, float hi) { var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new ParticleSystem.MinMaxCurve(0f, 0f); v.z = new ParticleSystem.MinMaxCurve(0f, 0f); v.y = new ParticleSystem.MinMaxCurve(lo, hi); }
        private static void Noise(ParticleSystem ps, float strength, float freq = 1.5f) { var n = ps.noise; n.enabled = true; n.strength = strength; n.frequency = freq; n.quality = ParticleSystemNoiseQuality.Low; }
        private static void Spin(ParticleSystem ps, float deg) { var m = ps.main; m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f); var r = ps.rotationOverLifetime; r.enabled = true; r.z = new ParticleSystem.MinMaxCurve(-deg * Mathf.Deg2Rad, deg * Mathf.Deg2Rad); }
        private static void Stretch(ParticleSystem ps, float vel, float len = 1f) { var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = vel; r.lengthScale = len; }
        private static void Flat(ParticleSystem ps) { ps.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard; }
        private static void Speed(ParticleSystem ps, float s) { var m = ps.main; m.simulationSpeed = s; }

        // ================= KULE BEKLEME EFEKTLERİ (döngülü, kule görünümüne bağlı) =================

        /// <summary>Elementli kulenin tepesindeki sürekli efekt. top: kulenin tepe noktası (dünya).</summary>
        public static void TowerIdle(Transform parent, TowerElement e, Vector3 top, float scale = 1f)
        {
            var c = Colors(e);
            switch (e)
            {
                case TowerElement.Fire:
                {
                    var flame = PS(parent, top, "flame", Mat(Glow, true), true, 1f, 30);
                    Life(flame, 0.45f, 0.75f, 0.18f * scale, 0.3f * scale); Rate(flame, 26f); Sphere(flame, 0.08f * scale, true);
                    Up(flame, 0.5f * scale, 0.9f * scale); ColorLife(flame, Grad(c.Core, c.Mid, c.Outer, 0.15f)); SizeLife(flame, Curve(0f, 0.6f, 0.3f, 1f, 1f, 0.2f)); Go(flame);
                    var ember = PS(parent, top, "ember", Mat(Glow, true), true, 1f, 20);
                    Life(ember, 0.8f, 1.4f, 0.03f * scale, 0.06f * scale); Rate(ember, 7f); Sphere(ember, 0.12f * scale, true);
                    Up(ember, 0.5f * scale, 1.1f * scale); Noise(ember, 0.35f * scale); ColorLife(ember, Grad(c.Core, c.Mid, c.Outer, 0.05f)); Go(ember);
                    Halo(parent, top + new Vector3(0f, 0.08f, 0f), c.Mid, 0.75f * scale);
                    break;
                }
                case TowerElement.Ice:
                {
                    var snow = PS(parent, top + new Vector3(0f, 0.45f * scale, 0f), "snow", Mat(Star, true), true, 1f, 24);
                    Life(snow, 1.4f, 2.2f, 0.04f * scale, 0.08f * scale); Rate(snow, 9f); Sphere(snow, 0.35f * scale);
                    Up(snow, -0.35f * scale, -0.15f * scale); Noise(snow, 0.15f * scale, 0.8f); Spin(snow, 90f); ColorLife(snow, Grad(c.Core, c.Mid, c.Mid, 0.15f)); Go(snow);
                    var mist = PS(parent, top - new Vector3(0f, 0.1f, 0f), "mist", Mat(Smoke, false), true, 1f, 10);
                    Life(mist, 1.6f, 2.4f, 0.35f * scale, 0.55f * scale); Rate(mist, 3f); Circle(mist, 0.18f * scale);
                    Up(mist, -0.12f, -0.04f); Spin(mist, 25f);
                    ColorLife(mist, Grad(c.Smoke, c.Smoke, c.Mid, 0.3f)); Go(mist);
                    Halo(parent, top + new Vector3(0f, 0.2f, 0f), c.Mid, 0.6f * scale);
                    break;
                }
                case TowerElement.Poison:
                {
                    var bub = PS(parent, top, "bubbles", Mat(Glow, true), true, 1f, 24);
                    Life(bub, 0.7f, 1.2f, 0.05f * scale, 0.11f * scale); Rate(bub, 10f); Circle(bub, 0.14f * scale);
                    Up(bub, 0.25f * scale, 0.55f * scale); Noise(bub, 0.08f * scale); ColorLife(bub, Grad(c.Core, c.Mid, c.Outer, 0.1f));
                    SizeLife(bub, Curve(0f, 0.5f, 0.85f, 1f, 1f, 1.5f)); Go(bub); // sonunda patlayarak büyür
                    var vapor = PS(parent, top + new Vector3(0f, 0.1f, 0f), "vapor", Mat(Smoke, false), true, 1f, 12);
                    Life(vapor, 1.2f, 2f, 0.25f * scale, 0.45f * scale); Rate(vapor, 4f); Circle(vapor, 0.1f * scale);
                    Up(vapor, 0.2f, 0.4f); Spin(vapor, 30f); Noise(vapor, 0.1f);
                    ColorLife(vapor, Grad(c.Smoke, c.Smoke, c.Outer, 0.25f)); SizeLife(vapor, Curve(0f, 0.5f, 1f, 1.4f)); Go(vapor);
                    Halo(parent, top + new Vector3(0f, 0.04f, 0f), c.Mid, 0.6f * scale);
                    break;
                }
                case TowerElement.Magic:
                {
                    var orbit = PS(parent, top, "orbit", Mat(Star, true), true, 1f, 24);
                    var om = orbit.main; om.simulationSpace = ParticleSystemSimulationSpace.Local;
                    Life(orbit, 1.2f, 1.8f, 0.05f * scale, 0.1f * scale); Rate(orbit, 10f); Circle(orbit, 0.34f * scale);
                    var v = orbit.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.Local;
                    v.x = new ParticleSystem.MinMaxCurve(0f); v.y = new ParticleSystem.MinMaxCurve(0.1f, 0.3f); v.z = new ParticleSystem.MinMaxCurve(0f);
                    v.orbitalY = new ParticleSystem.MinMaxCurve(2.4f);
                    Spin(orbit, 180f); ColorLife(orbit, Grad(c.Core, c.Mid, c.Outer, 0.15f)); Go(orbit);
                    Halo(parent, top, c.Mid, 0.85f * scale);
                    break;
                }
                case TowerElement.Lightning:
                {
                    var arc = PS(parent, top, "arcs", Mat(Glow, true), true, 1f, 30);
                    Life(arc, 0.08f, 0.16f, 0.025f * scale, 0.045f * scale, 2.5f * scale, 4.5f * scale); Sphere(arc, 0.06f * scale);
                    var em = arc.emission; em.rateOverTime = 0f;
                    em.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 10, 99, 0.35f) }); // aralıklı çatırtı
                    Stretch(arc, 0.12f, 1f); ColorLife(arc, Grad(c.Core, c.Mid, c.Outer, 0.01f)); Go(arc);
                    Halo(parent, top, c.Outer, 0.7f * scale, 6f);
                    break;
                }
                case TowerElement.Arrow:
                {
                    var glint = PS(parent, top, "glint", Mat(Star, true), true, 1f, 8);
                    Life(glint, 0.5f, 0.8f, 0.12f * scale, 0.2f * scale); Rate(glint, 1.6f); Sphere(glint, 0.3f * scale);
                    ColorLife(glint, Grad(c.Core, c.Mid, c.Mid, 0.3f)); SizeLife(glint, Curve(0f, 0f, 0.4f, 1f, 1f, 0f)); Go(glint);
                    break;
                }
            }
        }

        /// <summary>Kule savunma elementi: tabanda dönen parlak halka + yükselen ışık parçacıkları.</summary>
        public static void DefenseAura(Transform parent, TowerElement e, Vector3 groundPos, float radius = 0.42f)
        {
            if (e == TowerElement.None) return;
            var c = Colors(e);
            var rg = PS(parent, groundPos + new Vector3(0f, 0.03f, 0f), "defring", Mat(Ring, true), true, 1f, 4);
            Life(rg, 1.6f, 1.6f, radius * 2.2f, radius * 2.2f); Rate(rg, 1.25f); Flat(rg); Spin(rg, 40f);
            ColorLife(rg, Grad(c.Mid, c.Mid, c.Outer, 0.3f)); SizeLife(rg, Curve(0f, 0.85f, 1f, 1.05f)); Go(rg);
            var up = PS(parent, groundPos, "defmotes", Mat(Glow, true), true, 1f, 12);
            Life(up, 0.9f, 1.4f, 0.04f, 0.07f); Rate(up, 5f); Circle(up, radius);
            Up(up, 0.3f, 0.6f); ColorLife(up, Grad(c.Core, c.Mid, c.Outer, 0.2f)); Go(up);
        }

        /// <summary>Atımlı yumuşak parlama (tek büyük parçacık sürekli yenilenir).</summary>
        private static void Halo(Transform parent, Vector3 pos, Color c, float size, float rate = 2.5f)
        {
            var h = PS(parent, pos, "halo", Mat(Glow, true), true, 1f, 6);
            Life(h, 0.8f, 0.8f, size, size); Rate(h, rate);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.35f, 0.5f), new GradientAlphaKey(0f, 1f) });
            ColorLife(h, g); SizeLife(h, Curve(0f, 0.8f, 0.5f, 1f, 1f, 0.85f)); Go(h);
        }

        // ================= SALDIRI EFEKTLERİ (tek seferlik) =================

        /// <summary>Atış anı: kulenin namlusunda parlama + birkaç kıvılcım.</summary>
        public static void Muzzle(Transform parent, TowerElement e, Vector3 pos, Vector3 dir, float speed = 1f, float scale = 1f)
        {
            var c = Colors(e);
            var f = PS(parent, pos, "muzzle", Mat(e == TowerElement.Magic || e == TowerElement.Ice ? Star : Glow, true), false, 0.3f, 4);
            Life(f, 0.14f, 0.2f, 0.45f * scale, 0.6f * scale); Burst(f, 1);
            ColorLife(f, Grad(c.Core, c.Mid, c.Outer, 0.05f)); SizeLife(f, Curve(0f, 0.4f, 0.3f, 1f, 1f, 0.6f)); Speed(f, speed); Go(f);
            var sp = PS(parent, pos, "muzzlesparks", Mat(Glow, true), false, 0.4f, 10);
            Life(sp, 0.15f, 0.3f, 0.03f * scale, 0.05f * scale, 1.5f * scale, 3f * scale);
            var sh = sp.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 28f; sh.radius = 0.02f;
            sp.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward);
            Burst(sp, 6); Stretch(sp, 0.08f); ColorLife(sp, Grad(c.Core, c.Mid, c.Outer, 0.02f)); Speed(sp, speed); Go(sp);
        }

        /// <summary>Mermiye bağlı iz (dünya uzayında bırakılır). Mermi bitince TrailStop çağrılır.</summary>
        public static ParticleSystem Trail(Transform projectile, TowerElement e, float scale = 1f)
        {
            if (e == TowerElement.None) return null;
            var c = Colors(e);
            bool smoky = e == TowerElement.Fire || e == TowerElement.Poison;
            var t = PS(projectile, projectile.position, "trail", Mat(e == TowerElement.Ice || e == TowerElement.Magic ? Star : Glow, true), true, 1f, 40);
            t.transform.localPosition = Vector3.zero;
            Life(t, 0.25f, 0.45f, 0.09f * scale, 0.16f * scale);
            var em = t.emission; em.rateOverDistance = e == TowerElement.Arrow ? 14f : 22f;
            Sphere(t, 0.03f * scale); ColorLife(t, Grad(c.Core, c.Mid, c.Outer, 0.02f)); SizeLife(t, Curve(0f, 1f, 1f, 0f));
            if (e == TowerElement.Ice || e == TowerElement.Magic) Spin(t, 160f);
            if (e == TowerElement.Poison) Gravity(t, 0.4f);
            Go(t);
            if (smoky)
            {
                var s = PS(t.transform, projectile.position, "trailsmoke", Mat(Smoke, false), true, 1f, 20);
                s.transform.localPosition = Vector3.zero;
                Life(s, 0.4f, 0.7f, 0.12f * scale, 0.2f * scale); var se = s.emission; se.rateOverDistance = 9f;
                Spin(s, 40f); ColorLife(s, Grad(c.Smoke, c.Smoke, c.Smoke, 0.1f)); SizeLife(s, Curve(0f, 0.6f, 1f, 1.6f)); Go(s);
            }
            return t;
        }

        /// <summary>İzi mermiden ayırır, yayımı durdurur; kalan parçacıklar sönünce nesne silinir.</summary>
        public static void TrailStop(ParticleSystem t)
        {
            if (t == null) return;
            t.transform.SetParent(null, true);
            foreach (var ps in t.GetComponentsInChildren<ParticleSystem>()) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            Object.Destroy(t.gameObject, 1f);
        }

        /// <summary>Element isabeti. scale: 1 = kule isabeti; asker vuruşları için küçük.</summary>
        public static void Impact(Transform parent, TowerElement e, Vector3 p, float scale = 1f, float speed = 1f, bool crit = false)
        {
            var c = Colors(e);
            var ground = new Vector3(p.x, 0.04f, p.z);
            // her elementte ortak: kısa parlama
            var flash = PS(parent, p, "flash", Mat(Glow, true), false, 0.3f, 2);
            Life(flash, 0.16f, 0.2f, 0.7f * scale, 0.8f * scale); Burst(flash, 1);
            ColorLife(flash, Grad(c.Core, c.Mid, c.Outer, 0.05f)); SizeLife(flash, Curve(0f, 0.5f, 0.25f, 1f, 1f, 1.2f)); Speed(flash, speed); Go(flash);
            switch (e)
            {
                case TowerElement.Fire:
                {
                    var fire = PS(parent, p, "explode", Mat(Glow, true), false, 0.6f, 24);
                    Life(fire, 0.35f, 0.6f, 0.22f * scale, 0.38f * scale, 0.8f * scale, 1.8f * scale); Sphere(fire, 0.12f * scale);
                    Burst(fire, 14); Gravity(fire, -0.15f); ColorLife(fire, Grad(c.Core, c.Mid, c.Outer, 0.05f)); SizeLife(fire, Curve(0f, 0.6f, 0.4f, 1f, 1f, 0.3f)); Speed(fire, speed); Go(fire);
                    var ember = PS(parent, p, "embers", Mat(Glow, true), false, 1f, 16);
                    Life(ember, 0.5f, 0.9f, 0.03f * scale, 0.06f * scale, 1.5f * scale, 3f * scale); Sphere(ember, 0.08f * scale, true);
                    Burst(ember, 12); Gravity(ember, 0.6f); Stretch(ember, 0.06f); ColorLife(ember, Grad(c.Core, c.Mid, c.Outer, 0.02f)); Speed(ember, speed); Go(ember);
                    GroundRing(parent, ground, c.Mid, c.Outer, 1.5f * scale, speed);
                    SmokePuff(parent, p, c.Smoke, 6, 0.3f * scale, speed);
                    break;
                }
                case TowerElement.Ice:
                {
                    var shards = PS(parent, p, "shards", Mat(Star, true), false, 0.8f, 16);
                    Life(shards, 0.35f, 0.6f, 0.07f * scale, 0.14f * scale, 1.2f * scale, 2.4f * scale); Sphere(shards, 0.06f * scale, true);
                    Burst(shards, 12); Gravity(shards, 0.8f); Spin(shards, 360f); ColorLife(shards, Grad(c.Core, c.Mid, c.Outer, 0.02f)); Speed(shards, speed); Go(shards);
                    GroundRing(parent, ground, c.Core, c.Mid, 1.1f * scale, speed);
                    var mist = PS(parent, ground, "frostmist", Mat(Smoke, false), false, 1.2f, 8);
                    Life(mist, 0.6f, 1f, 0.3f * scale, 0.45f * scale, 0.3f * scale, 0.6f * scale); Circle(mist, 0.1f * scale);
                    Burst(mist, 6); Spin(mist, 40f);
                    ColorLife(mist, Grad(c.Smoke, c.Smoke, c.Mid, 0.15f)); SizeLife(mist, Curve(0f, 0.6f, 1f, 1.3f)); Speed(mist, speed); Go(mist);
                    break;
                }
                case TowerElement.Poison:
                {
                    var drops = PS(parent, p, "drops", Mat(Glow, true), false, 0.8f, 16);
                    Life(drops, 0.35f, 0.6f, 0.05f * scale, 0.09f * scale, 1f * scale, 2.2f * scale); Sphere(drops, 0.05f * scale, true);
                    Burst(drops, 12); Gravity(drops, 1.2f); ColorLife(drops, Grad(c.Core, c.Mid, c.Outer, 0.02f)); Speed(drops, speed); Go(drops);
                    var cloud = PS(parent, p, "toxiccloud", Mat(Smoke, false), false, 1.6f, 10);
                    Life(cloud, 1f, 1.5f, 0.3f * scale, 0.5f * scale, 0.1f * scale, 0.35f * scale); Sphere(cloud, 0.15f * scale);
                    Burst(cloud, 7); Spin(cloud, 30f); Up(cloud, 0.05f, 0.18f);
                    ColorLife(cloud, Grad(c.Smoke, c.Smoke, c.Outer, 0.15f)); SizeLife(cloud, Curve(0f, 0.5f, 1f, 1.4f)); Speed(cloud, speed); Go(cloud);
                    var pud = PS(parent, ground, "puddle", Mat(Glow, true), false, 1.5f, 2);
                    Life(pud, 1.2f, 1.2f, 0.9f * scale, 0.9f * scale); Burst(pud, 1); Flat(pud);
                    ColorLife(pud, Grad(c.Mid, c.Mid, c.Outer, 0.1f)); SizeLife(pud, Curve(0f, 0.3f, 0.2f, 1f, 1f, 0.9f)); Speed(pud, speed); Go(pud);
                    break;
                }
                case TowerElement.Magic:
                {
                    var ringV = PS(parent, p, "arcane", Mat(Ring, true), false, 0.5f, 2);
                    Life(ringV, 0.35f, 0.35f, 0.9f * scale, 0.9f * scale); Burst(ringV, 1);
                    ColorLife(ringV, Grad(c.Core, c.Mid, c.Outer, 0.05f)); SizeLife(ringV, Curve(0f, 0.2f, 1f, 1.2f)); Speed(ringV, speed); Go(ringV);
                    var sparkle = PS(parent, p, "sparkle", Mat(Star, true), false, 0.9f, 16);
                    Life(sparkle, 0.4f, 0.7f, 0.07f * scale, 0.14f * scale, 0.6f * scale, 1.6f * scale); Sphere(sparkle, 0.08f * scale);
                    Burst(sparkle, 12); Spin(sparkle, 270f); Noise(sparkle, 0.3f * scale, 2f);
                    ColorLife(sparkle, Grad(c.Core, c.Mid, c.Outer, 0.02f)); SizeLife(sparkle, Curve(0f, 1f, 1f, 0f)); Speed(sparkle, speed); Go(sparkle);
                    GroundRing(parent, ground, c.Mid, c.Outer, 0.9f * scale, speed);
                    break;
                }
                case TowerElement.Lightning:
                {
                    var sp = PS(parent, p, "zap", Mat(Glow, true), false, 0.5f, 16);
                    Life(sp, 0.12f, 0.25f, 0.025f * scale, 0.045f * scale, 2f * scale, 4f * scale); Sphere(sp, 0.04f * scale);
                    Burst(sp, 12); Stretch(sp, 0.1f); ColorLife(sp, Grad(c.Core, c.Mid, c.Outer, 0.01f)); Speed(sp, speed); Go(sp);
                    GroundRing(parent, ground, c.Core, c.Outer, 0.8f * scale, speed);
                    break;
                }
                default: // Ok / elementsiz: kıvılcım; kritikte altın yıldız
                {
                    var dust = PS(parent, p, "hitspark", Mat(Glow, true), false, 0.4f, 8);
                    Life(dust, 0.12f, 0.22f, 0.025f * scale, 0.04f * scale, 1f * scale, 2f * scale); Sphere(dust, 0.03f * scale, true);
                    Burst(dust, 5); Stretch(dust, 0.06f); ColorLife(dust, Grad(c.Core, c.Mid, c.Outer, 0.02f)); Speed(dust, speed); Go(dust);
                    if (crit)
                    {
                        var st = PS(parent, p, "crit", Mat(Star, true), false, 0.5f, 2);
                        Life(st, 0.35f, 0.35f, 0.9f * scale, 0.9f * scale); Burst(st, 1); Spin(st, 90f);
                        ColorLife(st, Grad(Color.white, new Color(1f, 0.9f, 0.3f), new Color(1f, 0.6f, 0.1f), 0.05f)); SizeLife(st, Curve(0f, 0.3f, 0.3f, 1.2f, 1f, 0.5f)); Speed(st, speed); Go(st);
                        GroundRing(parent, ground, new Color(1f, 0.9f, 0.3f), new Color(1f, 0.6f, 0.1f), 0.7f * scale, speed);
                    }
                    break;
                }
            }
        }

        /// <summary>Yerde genişleyen halka (şok dalgası).</summary>
        public static void GroundRing(Transform parent, Vector3 ground, Color a, Color b, float size, float speed = 1f)
        {
            var r = PS(parent, ground, "shock", Mat(Ring, true), false, 0.6f, 2);
            Life(r, 0.4f, 0.4f, size, size); Burst(r, 1); Flat(r);
            ColorLife(r, Grad(a, a, b, 0.05f)); SizeLife(r, Curve(0f, 0.15f, 1f, 1f)); Speed(r, speed); Go(r);
        }

        private static void SmokePuff(Transform parent, Vector3 p, Color c, int n, float size, float speed)
        {
            var s = PS(parent, p, "smoke", Mat(Smoke, false), false, 1.5f, 10);
            Life(s, 0.7f, 1.2f, size, size * 1.6f, 0.2f, 0.6f); Sphere(s, 0.1f); Burst(s, n); Spin(s, 40f); Up(s, 0.15f, 0.35f);
            ColorLife(s, Grad(c, c, c, 0.1f)); SizeLife(s, Curve(0f, 0.6f, 1f, 1.5f)); Speed(s, speed); Go(s);
        }

        // ================= TUZAKLAR =================

        /// <summary>Tuzak üstünde küçük sürekli efekt (uzaktan fark edilsin, göz yormasın).</summary>
        public static void TrapIdle(Transform parent, TrapType t, Vector3 p)
        {
            switch (t)
            {
                case TrapType.Fire:
                {
                    var c = Colors(TowerElement.Fire);
                    var f = PS(parent, p, "trapflame", Mat(Glow, true), true, 1f, 14);
                    Life(f, 0.35f, 0.55f, 0.08f, 0.14f); Rate(f, 14f); Circle(f, 0.1f); Up(f, 0.3f, 0.55f);
                    ColorLife(f, Grad(c.Core, c.Mid, c.Outer, 0.15f)); SizeLife(f, Curve(0f, 0.6f, 0.3f, 1f, 1f, 0.2f)); Go(f);
                    break;
                }
                case TrapType.Poison:
                {
                    var c = Colors(TowerElement.Poison);
                    var b = PS(parent, p, "trapbubble", Mat(Glow, true), true, 1f, 10);
                    Life(b, 0.6f, 1f, 0.03f, 0.06f); Rate(b, 5f); Circle(b, 0.12f); Up(b, 0.12f, 0.25f);
                    ColorLife(b, Grad(c.Core, c.Mid, c.Outer, 0.1f)); SizeLife(b, Curve(0f, 0.5f, 0.85f, 1f, 1f, 1.5f)); Go(b);
                    break;
                }
                case TrapType.Frost:
                {
                    var c = Colors(TowerElement.Ice);
                    var s = PS(parent, p + new Vector3(0f, 0.05f, 0f), "trapfrost", Mat(Star, true), true, 1f, 8);
                    Life(s, 0.8f, 1.3f, 0.03f, 0.06f); Rate(s, 4f); Circle(s, 0.14f); Up(s, 0.05f, 0.15f); Spin(s, 90f);
                    ColorLife(s, Grad(c.Core, c.Mid, c.Mid, 0.2f)); Go(s);
                    break;
                }
                case TrapType.Shock:
                {
                    var c = Colors(TowerElement.Lightning);
                    var a = PS(parent, p, "trapzap", Mat(Glow, true), true, 1f, 10);
                    Life(a, 0.06f, 0.12f, 0.015f, 0.03f, 1.2f, 2.2f); Sphere(a, 0.05f, true);
                    var em = a.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 3, 5, 99, 0.6f) });
                    Stretch(a, 0.1f); ColorLife(a, Grad(c.Core, c.Mid, c.Outer, 0.01f)); Go(a);
                    break;
                }
                case TrapType.Bomb:
                {
                    var c = Colors(TowerElement.Fire);
                    var fuse = PS(parent, p + new Vector3(0.03f, 0.12f, 0f), "fuse", Mat(Glow, true), true, 1f, 10);
                    Life(fuse, 0.15f, 0.3f, 0.015f, 0.03f, 0.3f, 0.8f); Rate(fuse, 16f); Sphere(fuse, 0.01f, true); Gravity(fuse, 0.5f);
                    ColorLife(fuse, Grad(c.Core, c.Mid, c.Outer, 0.02f)); Go(fuse);
                    break;
                }
            }
        }

        /// <summary>Tuzak tetiklenince (savaş): her türe özgü patlama/sıçrama.</summary>
        public static void TrapTrigger(Transform parent, TrapType t, Vector3 p, float speed = 1f)
        {
            var ground = new Vector3(p.x, 0.05f, p.z);
            switch (t)
            {
                case TrapType.Fire: Impact(parent, TowerElement.Fire, p, 0.7f, speed); break;
                case TrapType.Poison: Impact(parent, TowerElement.Poison, p, 0.7f, speed); break;
                case TrapType.Frost: Impact(parent, TowerElement.Ice, p, 0.85f, speed); break;
                case TrapType.Shock:
                    Lightning(parent, ground, ground + new Vector3(0f, 0.9f, 0f), Colors(TowerElement.Lightning).Mid, 0.15f);
                    Impact(parent, TowerElement.Lightning, p, 0.7f, speed); break;
                case TrapType.Bomb:
                    Impact(parent, TowerElement.Fire, p + new Vector3(0f, 0.15f, 0f), 1.6f, speed);
                    SmokePuff(parent, p + new Vector3(0f, 0.2f, 0f), new Color(0.2f, 0.18f, 0.17f), 9, 0.45f, speed);
                    break;
                case TrapType.Slippery:
                {
                    var w = new Color(0.55f, 0.85f, 1f);
                    var sp = PS(parent, ground, "splash", Mat(Glow, true), false, 0.8f, 14);
                    Life(sp, 0.3f, 0.55f, 0.04f, 0.07f, 0.8f, 1.6f); Sphere(sp, 0.05f, true); Burst(sp, 12); Gravity(sp, 1.4f);
                    ColorLife(sp, Grad(Color.white, w, w, 0.02f)); Speed(sp, speed); Go(sp);
                    GroundRing(parent, ground, Color.white, w, 0.7f, speed);
                    break;
                }
                default: // Diken, Kapan: toz + metal kıvılcım
                {
                    var dust = Colors(TowerElement.None);
                    SmokePuff(parent, ground, new Color(0.55f, 0.45f, 0.33f), 5, 0.22f, speed);
                    var sp = PS(parent, ground + new Vector3(0f, 0.08f, 0f), "metal", Mat(Glow, true), false, 0.4f, 10);
                    Life(sp, 0.12f, 0.25f, 0.02f, 0.035f, 1.2f, 2.2f); Sphere(sp, 0.04f, true); Burst(sp, 8); Stretch(sp, 0.07f);
                    ColorLife(sp, Grad(Color.white, dust.Core, dust.Mid, 0.02f)); Speed(sp, speed); Go(sp);
                    if (t == TrapType.Snare) GroundRing(parent, ground, new Color(0.85f, 0.85f, 0.9f), new Color(0.5f, 0.5f, 0.55f), 0.6f, speed);
                    break;
                }
            }
        }

        // ================= YAPILAR =================

        /// <summary>Yapıya göre sürekli efekt: baca dumanı, altın pırıltısı, demirci kıvılcımı, büyü ışıkları... top: yapının tepesi.</summary>
        public static void BuildingIdle(Transform parent, BuildingType t, Vector3 basePos, float topY, int level)
        {
            var top = new Vector3(basePos.x, topY, basePos.z);
            switch (t)
            {
                case BuildingType.VillagerHouse:
                case BuildingType.Barracks:
                {
                    var sm = PS(parent, top + new Vector3(0.12f, 0.05f, 0.1f), "chimney", Mat(Smoke, false), true, 1f, 14);
                    Life(sm, 1.6f, 2.4f, 0.12f, 0.2f); Rate(sm, 3f); Sphere(sm, 0.03f); Up(sm, 0.25f, 0.4f); Noise(sm, 0.12f, 0.6f); Spin(sm, 30f);
                    var sc = new Color(0.78f, 0.76f, 0.74f);
                    ColorLife(sm, Grad(sc, sc, new Color(0.6f, 0.6f, 0.6f), 0.15f)); SizeLife(sm, Curve(0f, 0.5f, 1f, 1.8f)); Go(sm);
                    if (t == BuildingType.Barracks) Embers(parent, basePos + new Vector3(-0.3f, 0.35f, -0.3f), 4f);
                    break;
                }
                case BuildingType.GoldMine:
                case BuildingType.Bank:
                    Glints(parent, top - new Vector3(0f, topY * 0.4f, 0f), new Color(1f, 0.85f, 0.3f), 0.35f, 2.2f + level * 0.05f);
                    break;
                case BuildingType.IronMine:
                case BuildingType.IronDepot:
                    Embers(parent, basePos + new Vector3(0.2f, 0.3f, -0.2f), 6f);
                    break;
                case BuildingType.StoneQuarry:
                case BuildingType.StoneDepot:
                {
                    var d = PS(parent, basePos + new Vector3(0f, 0.12f, 0f), "dust", Mat(Smoke, false), true, 1f, 8);
                    Life(d, 1.2f, 1.8f, 0.15f, 0.25f); Rate(d, 1.5f); Circle(d, 0.3f); Up(d, 0.05f, 0.15f); Spin(d, 20f);
                    var dc = new Color(0.75f, 0.72f, 0.66f, 0.6f);
                    ColorLife(d, Grad(dc, dc, dc, 0.3f)); SizeLife(d, Curve(0f, 0.6f, 1f, 1.4f)); Go(d);
                    break;
                }
                case BuildingType.Forest:
                case BuildingType.LumberDepot:
                {
                    var lf = PS(parent, top + new Vector3(0f, 0.1f, 0f), "leaves", Mat(Glow, false), true, 1f, 8);
                    Life(lf, 2f, 3f, 0.04f, 0.07f); Rate(lf, 1.5f); Sphere(lf, 0.35f); Up(lf, -0.2f, -0.1f); Noise(lf, 0.25f, 0.7f);
                    var a = new Color(0.55f, 0.8f, 0.3f); var b = new Color(0.95f, 0.75f, 0.25f);
                    ColorLife(lf, Grad(a, a, b, 0.1f)); Go(lf);
                    break;
                }
                case BuildingType.University:
                {
                    var c = Colors(TowerElement.Magic);
                    var o = PS(parent, top, "academy", Mat(Star, true), true, 1f, 14);
                    var om = o.main; om.simulationSpace = ParticleSystemSimulationSpace.Local;
                    Life(o, 1.4f, 2f, 0.05f, 0.09f); Rate(o, 6f); Circle(o, 0.3f);
                    var v = o.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.Local;
                    v.x = new ParticleSystem.MinMaxCurve(0f); v.y = new ParticleSystem.MinMaxCurve(0.05f, 0.15f); v.z = new ParticleSystem.MinMaxCurve(0f);
                    v.orbitalY = new ParticleSystem.MinMaxCurve(1.6f);
                    Spin(o, 120f); ColorLife(o, Grad(c.Core, c.Mid, c.Outer, 0.15f)); Go(o);
                    break;
                }
            }
            if (level >= 10) Glints(parent, top, new Color(1f, 0.9f, 0.5f), 0.3f, 1f); // yüksek seviye: hafif parıltı
        }

        /// <summary>İnşaat/yükseltme sürüyor: toz bulutu + çekiç kıvılcımı.</summary>
        public static void Construction(Transform parent, Vector3 basePos)
        {
            var d = PS(parent, basePos + new Vector3(0f, 0.08f, 0f), "builddust", Mat(Smoke, false), true, 1f, 12);
            Life(d, 0.8f, 1.3f, 0.15f, 0.28f, 0.1f, 0.3f); Rate(d, 5f); Circle(d, 0.35f); Up(d, 0.1f, 0.25f); Spin(d, 30f);
            var dc = new Color(0.72f, 0.62f, 0.48f, 0.7f);
            ColorLife(d, Grad(dc, dc, dc, 0.2f)); SizeLife(d, Curve(0f, 0.5f, 1f, 1.5f)); Go(d);
            var sp = PS(parent, basePos + new Vector3(0.2f, 0.45f, -0.2f), "hammer", Mat(Glow, true), true, 1f, 12);
            Life(sp, 0.15f, 0.3f, 0.02f, 0.035f, 0.8f, 1.6f); Sphere(sp, 0.03f, true); Gravity(sp, 0.8f);
            var em = sp.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 4, 6, 999, 0.55f) });
            var c = Colors(TowerElement.Arrow);
            ColorLife(sp, Grad(c.Core, c.Mid, c.Outer, 0.02f)); Go(sp);
        }

        /// <summary>Yükseltme bitti: altın ışık sütunu + yıldızlar.</summary>
        public static void LevelUpBurst(Transform parent, Vector3 basePos)
        {
            var c = Colors(TowerElement.Arrow);
            var col = PS(parent, basePos + new Vector3(0f, 0.1f, 0f), "lvlup", Mat(Star, true), false, 1.2f, 24);
            Life(col, 0.7f, 1.1f, 0.06f, 0.12f, 0.6f, 1.4f); Circle(col, 0.3f); Burst(col, 20); Spin(col, 180f);
            var v = col.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(0f); v.z = new ParticleSystem.MinMaxCurve(0f); v.y = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            ColorLife(col, Grad(c.Core, c.Mid, c.Outer, 0.05f)); Go(col);
            GroundRing(parent, new Vector3(basePos.x, 0.05f, basePos.z), c.Core, c.Mid, 1.3f);
        }

        private static void Embers(Transform parent, Vector3 p, float rate)
        {
            var c = Colors(TowerElement.Fire);
            var e = PS(parent, p, "embers", Mat(Glow, true), true, 1f, 12);
            Life(e, 0.6f, 1.1f, 0.02f, 0.04f); Rate(e, rate); Sphere(e, 0.06f, true); Up(e, 0.3f, 0.6f); Noise(e, 0.2f);
            ColorLife(e, Grad(c.Core, c.Mid, c.Outer, 0.05f)); Go(e);
        }

        private static void Glints(Transform parent, Vector3 p, Color c, float radius, float rate)
        {
            var g = PS(parent, p, "glint", Mat(Star, true), true, 1f, 8);
            Life(g, 0.5f, 0.8f, 0.08f, 0.14f); Rate(g, rate); Sphere(g, radius);
            ColorLife(g, Grad(Color.white, c, c, 0.3f)); SizeLife(g, Curve(0f, 0f, 0.4f, 1f, 1f, 0f)); Go(g);
        }

        // ================= YILDIRIM =================

        /// <summary>Parlayan zikzak yıldırım (dış renkli + iç beyaz çizgi + küçük dal). dur sn sonra silinir.</summary>
        public static GameObject Lightning(Transform parent, Vector3 a, Vector3 b, Color c, float dur = 0.22f)
        {
            var g = new GameObject("fx_lightning");
            g.transform.SetParent(parent, false);
            var pts = Jagged(a, b, 8, 0.14f);
            Line(g.transform, pts, c, 0.16f, 0.9f);
            Line(g.transform, pts, Color.white, 0.05f, 1f);
            int k = Random.Range(2, 6);
            var end = pts[k] + (b - a).normalized * 0.25f + Random.insideUnitSphere * 0.25f;
            Line(g.transform, Jagged(pts[k], end, 3, 0.06f), c, 0.06f, 0.8f);
            Object.Destroy(g, dur);
            return g;
        }

        private static Vector3[] Jagged(Vector3 a, Vector3 b, int segs, float amp)
        {
            var p = new Vector3[segs + 1];
            for (int i = 0; i <= segs; i++)
            {
                p[i] = Vector3.Lerp(a, b, i / (float)segs);
                if (i > 0 && i < segs) p[i] += Random.insideUnitSphere * amp;
            }
            return p;
        }

        private static void Line(Transform parent, Vector3[] pts, Color c, float width, float alpha)
        {
            var go = new GameObject("bolt");
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = pts.Length;
            lr.SetPositions(pts);
            lr.widthMultiplier = width;
            lr.sharedMaterial = Mat(Beam, true);
            lr.textureMode = LineTextureMode.Stretch;
            lr.alignment = LineAlignment.View;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            c.a = alpha;
            lr.startColor = c; lr.endColor = c;
        }
    }
}
