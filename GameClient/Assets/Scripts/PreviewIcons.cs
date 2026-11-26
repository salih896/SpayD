using System.Collections.Generic;
using GameCore.Combat;
using UnityEngine;

namespace GameClient
{
    /// <summary>
    /// Menülerde yapı / kule / tuzak / asker önizlemesi (kullanıcı isteği 27.11.2026: "yapılacak yapının görseli görünsün,
    /// oluşturulan askerin görseli oluşsun"). Model ekran dışında, ayrı katmanda kurulur; iki kare beklenir (animasyon pozu
    /// otursun, T-poz olmasın), ayrı bir kamerayla RenderTexture'a bir kez çizilir ve önbelleğe alınır. Hazır değilse null döner
    /// (çağıran yerine renkli daire çizer). Yalnızca görsel.
    /// </summary>
    public sealed class PreviewIcons : MonoBehaviour
    {
        private const int Layer = 31;
        private const int Size = 128;
        private static readonly Vector3 Stage = new Vector3(1000f, 0f, 1000f);
        private static PreviewIcons inst;

        private readonly Dictionary<string, RenderTexture> cache = new Dictionary<string, RenderTexture>();
        private readonly Queue<(string key, System.Func<Transform, Transform> build)> queue = new Queue<(string, System.Func<Transform, Transform>)>();
        private readonly HashSet<string> queued = new HashSet<string>();
        private Camera cam;
        private Transform stageRoot;
        private string pendingKey;
        private int waitFrames;

        public static Texture Building(BuildingType t, int level) =>
            Get("b" + (int)t + "_" + level, p => VillageArt.BuildBuilding(p, t, level, 7, Vector3.zero));

        public static Texture Tower(TowerType t, TowerElement e, int level) =>
            Get("t" + (int)t + "_" + (int)e + "_" + level, p =>
            {
                var m = e != TowerElement.None && t == TowerType.Archer ? ModelArt.TryBuildElementTower(p, e, level, false, Vector3.zero, out _) : null;
                return m != null ? m : VillageArt.BuildTower(p, t, level, false, Vector3.zero);
            });

        public static Texture Trap(TrapType t) =>
            Get("p" + (int)t, p => { var g = VillageArt.Group(p, "trap", Vector3.zero); VillageArt.BuildTrap(g, Vector3.zero, t); return g; });

        public static Texture Soldier(SoldierType t, int level) =>
            Get("s" + (int)t + "_" + level, p => VillageArt.BuildSoldier(p, t, level));

        private static Texture Get(string key, System.Func<Transform, Transform> build)
        {
            if (inst == null) inst = new GameObject("PreviewIcons").AddComponent<PreviewIcons>();
            if (inst.cache.TryGetValue(key, out var rt) && rt != null) return rt;
            if (inst.queued.Add(key)) inst.queue.Enqueue((key, build));
            return null;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            var main = Camera.main;
            if (main != null) main.cullingMask &= ~(1 << Layer); // ana kamera önizleme modellerini görmesin
            var go = new GameObject("PreviewCamera");
            go.transform.SetParent(transform, false);
            cam = go.AddComponent<Camera>();
            cam.enabled = false; // yalnızca elle Render()
            cam.orthographic = true;
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 60f;
            // önizlemeye özel dolgu ışığı (kameranın yönünden; sahnedeki ana ışık arkadan kalabiliyor)
            var lg = new GameObject("PreviewLight");
            lg.transform.SetParent(go.transform, false);
            var lt = lg.AddComponent<Light>();
            lt.type = LightType.Directional; lt.intensity = 0.9f; lt.color = new Color(1f, 0.97f, 0.9f);
            lt.cullingMask = 1 << Layer; lt.shadows = LightShadows.None;
            lg.transform.localRotation = Quaternion.Euler(15f, -25f, 0f);
            stageRoot = new GameObject("PreviewStage").transform;
            stageRoot.SetParent(transform, false);
            stageRoot.position = Stage;
        }

        private void LateUpdate()
        {
            if (pendingKey != null)
            {
                if (--waitFrames > 0) return;
                Shoot(pendingKey);
                pendingKey = null;
                return;
            }
            if (queue.Count == 0) return;
            var (key, build) = queue.Dequeue();
            Transform model = null;
            try { model = build(stageRoot); }
            catch (System.Exception ex) { Debug.LogWarning("Önizleme kurulamadı: " + key + " " + ex.Message); }
            if (model == null) { queued.Remove(key); return; }
            foreach (var ps in model.GetComponentsInChildren<ParticleSystem>(true)) Destroy(ps.gameObject); // önizlemede efekt yok
            SetLayer(model, Layer);
            pendingKey = key; waitFrames = 2; // animasyon pozu otursun
        }

        private void Shoot(string key)
        {
            if (stageRoot.childCount == 0) { queued.Remove(key); return; }
            var model = stageRoot.GetChild(0);
            var rends = model.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) { Clear(); queued.Remove(key); return; }
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            var sh = model.GetComponent<SoldierHeight>(); // askerde deri (skinned) sınırları güvenilmez: bilinen boyu kullan
            if (sh != null && sh.Value > 0.05f)
            {
                float hh = sh.Value;
                b = new Bounds(model.position + Vector3.up * hh * 0.5f, new Vector3(hh * 0.8f, hh, hh * 0.8f));
            }
            var main = Camera.main;
            var rot = main != null ? main.transform.rotation : Quaternion.Euler(30f, 45f, 0f);
            cam.transform.rotation = rot;
            cam.transform.position = b.center - rot * Vector3.forward * 20f;
            // ekrana göre en geniş boyut: kutunun 8 köşesini kamera uzayına izdüşür
            float half = 0.01f;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var lp = Quaternion.Inverse(rot) * (corner - b.center);
                half = Mathf.Max(half, Mathf.Abs(lp.x), Mathf.Abs(lp.y));
            }
            cam.orthographicSize = half * 1.08f;
            var rt = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32) { antiAliasing = 2, name = "preview_" + key };
            rt.Create();
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            cache[key] = rt;
            Clear();
        }

        private void Clear()
        {
            for (int i = stageRoot.childCount - 1; i >= 0; i--) Destroy(stageRoot.GetChild(i).gameObject);
        }

        private static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }
    }
}
