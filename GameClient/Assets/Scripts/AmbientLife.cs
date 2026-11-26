using System.Collections.Generic;
using UnityEngine;

namespace GameClient
{
    /// <summary>
    /// Köy çevresinin canlılığı (yalnızca görsel; kullanıcı isteği 27.11.2026 "harita görünümünü geliştir"):
    /// gökyüzünde kanat çırpan kuş sürüsü, yerde yavaşça kayan bulut gölgeleri, çiçeklerin üstünde kelebekler,
    /// gölette su pırıltısı. Mobil için az sayıda nesne; her şey tek bileşende güncellenir.
    /// </summary>
    public sealed class AmbientLife : MonoBehaviour
    {
        private sealed class Bird { public Transform T, WingL, WingR; public float Radius, Speed, Height, Phase; public Vector3 Center; }
        private sealed class Cloud { public Transform T; public Vector3 Dir; public float Speed; }
        private sealed class Fly { public Transform T, WingL, WingR; public Vector3 Home; public float Phase; }

        private readonly List<Bird> birds = new List<Bird>();
        private readonly List<Cloud> clouds = new List<Cloud>();
        private readonly List<Fly> flies = new List<Fly>();
        private float minX, maxX, minZ, maxZ;

        public static AmbientLife Create(Transform parent, int size, int attackRows)
        {
            var go = new GameObject("AmbientLife");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<AmbientLife>();
            a.Build(size, attackRows);
            return a;
        }

        private void Build(int size, int attackRows)
        {
            float cx = (size - 1) * 0.5f;
            minX = -14f; maxX = size + 14f; minZ = -attackRows - 12f; maxZ = size + 14f;
            var root = transform;

            // kuş sürüsü: köyün üstünde geniş daireler çizer
            var birdMat = VillageArt.Mat(new Color(0.18f, 0.16f, 0.15f));
            for (int i = 0; i < 5; i++)
            {
                var b = new Bird { Center = new Vector3(cx + Random.Range(-3f, 3f), 0f, cx + Random.Range(-3f, 3f)), Radius = 7f + i * 0.6f, Speed = 0.22f + Random.Range(-0.03f, 0.03f), Height = 4.2f + Random.Range(-0.4f, 0.6f), Phase = i * 0.35f };
                b.T = VillageArt.Group(root, "bird", Vector3.zero);
                VillageArt.Part(b.T, VillageArt.Shape.Ball, Vector3.zero, new Vector3(0.06f, 0.05f, 0.14f), birdMat);
                b.WingL = VillageArt.Group(b.T, "wl", new Vector3(-0.03f, 0f, 0f));
                VillageArt.Part(b.WingL, VillageArt.Shape.Box, new Vector3(-0.1f, 0f, 0f), new Vector3(0.2f, 0.01f, 0.07f), birdMat);
                b.WingR = VillageArt.Group(b.T, "wr", new Vector3(0.03f, 0f, 0f));
                VillageArt.Part(b.WingR, VillageArt.Shape.Box, new Vector3(0.1f, 0f, 0f), new Vector3(0.2f, 0.01f, 0.07f), birdMat);
                birds.Add(b);
            }

            // bulut gölgeleri: zemine yatık, yumuşak kenarlı yarı saydam koyu lekeler
            var shadowMat = new Material(FxKit.Mat(FxKit.Smoke, false));
            shadowMat.SetColor("_Color", new Color(0.05f, 0.08f, 0.12f, 0.16f));
            var wind = new Vector3(1f, 0f, 0.35f).normalized;
            for (int i = 0; i < 4; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.name = "cloudShadow";
                q.transform.SetParent(root, false);
                q.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                float s = Random.Range(6f, 10f);
                q.transform.localScale = new Vector3(s, s * Random.Range(0.6f, 0.9f), 1f);
                q.transform.position = new Vector3(Random.Range(minX, maxX), 0.03f, Random.Range(minZ, maxZ));
                var mr = q.GetComponent<MeshRenderer>();
                mr.sharedMaterial = shadowMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                clouds.Add(new Cloud { T = q.transform, Dir = wind, Speed = Random.Range(0.25f, 0.4f) });
            }

            // kelebekler: köy çitinin dışında, çiçeklerin üstünde
            var cols = new[] { new Color(1f, 0.85f, 0.25f), new Color(1f, 0.55f, 0.75f), new Color(0.6f, 0.75f, 1f), Color.white };
            for (int i = 0; i < 6; i++)
            {
                float side = Random.value;
                var home = side < 0.33f ? new Vector3(-2.5f, 0.35f, Random.Range(0f, size)) : side < 0.66f ? new Vector3(size + 1.5f, 0.35f, Random.Range(0f, size)) : new Vector3(Random.Range(0f, size), 0.35f, size + 1.5f);
                var f = new Fly { Home = home, Phase = Random.Range(0f, 6.28f) };
                var m = VillageArt.Mat(cols[i % cols.Length], 0.3f, 0.3f);
                f.T = VillageArt.Group(root, "butterfly", home);
                f.WingL = VillageArt.Group(f.T, "wl", Vector3.zero);
                VillageArt.Part(f.WingL, VillageArt.Shape.Box, new Vector3(-0.035f, 0f, 0f), new Vector3(0.07f, 0.005f, 0.06f), m);
                f.WingR = VillageArt.Group(f.T, "wr", Vector3.zero);
                VillageArt.Part(f.WingR, VillageArt.Shape.Box, new Vector3(0.035f, 0f, 0f), new Vector3(0.07f, 0.005f, 0.06f), m);
                flies.Add(f);
            }

            // nehir: akıntı yönünde kayan pırıltılar
            for (int k = 0; k < 5; k++)
            {
                float rz = -attackRows - 6 + k * (size + attackRows + 14) / 5f;
                var rp = new Vector3(VillageArt.RiverX(rz, size), 0f, rz);
                var rs = FxKit.PS(root, rp, "riversparkle", FxKit.Mat(FxKit.Star, true), true, 1f, 8);
                var rm = rs.main; rm.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f); rm.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f); rm.startSpeed = 0f;
                var re = rs.emission; re.rateOverTime = 4f;
                var rsh = rs.shape; rsh.enabled = true; rsh.shapeType = ParticleSystemShapeType.Box; rsh.scale = new Vector3(1.4f, 0.01f, 3f);
                var rv = rs.velocityOverLifetime; rv.enabled = true; rv.space = ParticleSystemSimulationSpace.World; rv.x = new ParticleSystem.MinMaxCurve(0f); rv.y = new ParticleSystem.MinMaxCurve(0f); rv.z = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
                var rc = rs.colorOverLifetime; rc.enabled = true;
                var rg = new Gradient();
                rg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.75f, 0.92f, 1f), 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.3f), new GradientAlphaKey(0f, 1f) });
                rc.color = rg;
                rs.gameObject.SetActive(true);
            }

            // gölet pırıltısı (BuildEnvironment'taki gölet: köyün sol arkası)
            var pond = new Vector3(-5.2f, 0.02f, size + 2.5f);
            var sp = FxKit.PS(root, pond, "pondsparkle", FxKit.Mat(FxKit.Star, true), true, 1f, 10);
            var mm = sp.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f); mm.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f); mm.startSpeed = 0f;
            var em = sp.emission; em.rateOverTime = 5f;
            var sh = sp.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.4f; sh.rotation = new Vector3(90f, 0f, 0f);
            var col = sp.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.8f, 0.95f, 1f), 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            sp.gameObject.SetActive(true);
        }

        private void Update()
        {
            float t = Time.time, dt = Time.deltaTime;
            foreach (var b in birds)
            {
                float a = t * b.Speed + b.Phase;
                var p = b.Center + new Vector3(Mathf.Cos(a) * b.Radius, b.Height + Mathf.Sin(t * 0.7f + b.Phase * 3f) * 0.25f, Mathf.Sin(a) * b.Radius * 0.7f);
                var vel = new Vector3(-Mathf.Sin(a) * b.Radius, 0f, Mathf.Cos(a) * b.Radius * 0.7f);
                b.T.position = p;
                if (vel.sqrMagnitude > 0.001f) b.T.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, -12f); // dönüşe yatar
                float flap = Mathf.Sin(t * 9f + b.Phase * 5f) * 35f * (0.5f + 0.5f * Mathf.Sin(t * 0.5f + b.Phase)); // ara ara süzülür
                b.WingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                b.WingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
            }
            foreach (var c in clouds)
            {
                c.T.position += c.Dir * (c.Speed * dt);
                var p = c.T.position;
                if (p.x > maxX + 6f) { p.x = minX - 6f; p.z = Random.Range(minZ, maxZ); }
                if (p.z > maxZ + 6f) p.z = minZ - 6f;
                c.T.position = p;
            }
            foreach (var f in flies)
            {
                float a = t * 0.8f + f.Phase;
                f.T.position = f.Home + new Vector3(Mathf.Sin(a) * 0.6f + Mathf.Sin(a * 2.3f) * 0.2f, Mathf.Sin(a * 3.1f) * 0.12f, Mathf.Cos(a * 1.3f) * 0.5f);
                f.T.rotation = Quaternion.Euler(0f, a * 57f, 0f);
                float flap = Mathf.Sin(t * 22f + f.Phase) * 60f;
                f.WingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                f.WingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
            }
        }
    }
}
