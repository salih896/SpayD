using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GameClient
{
    // patch-gg/pixelfx (MIT) ile üretilmiş sprite sayfalarını (Resources/PixelFx/*.png + .txt) oynatan kamerayı-bakan quad.
    // Yalnızca görsel; oyun mantığına dokunmaz. Sayfa yoksa Create null döner, çağıran eski ilkel efektlere düşer.
    public sealed class PixelFxSheet
    {
        public Texture2D Tex; public int Fps, Frames, Cols, FW, FH, PX, PY; public bool Loop;
        private static readonly Dictionary<string, PixelFxSheet> cache = new Dictionary<string, PixelFxSheet>();
        
        public static PixelFxSheet Get(string name)
        {
            if (cache.TryGetValue(name, out var s)) return s;
            s = null;
            var tex = Resources.Load<Texture2D>("PixelFx/" + name);
            var meta = Resources.Load<TextAsset>("PixelFx/" + name);
            if (tex != null && meta != null)
            {
                var p = meta.text.Split(new[] { ' ', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (p.Length >= 8)
                {
                    tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
                    int I(int i) => int.Parse(p[i], CultureInfo.InvariantCulture);
                    s = new PixelFxSheet { Tex = tex, Fps = I(0), Loop = I(1) == 1, Frames = I(2), Cols = I(3), FW = I(4), FH = I(5), PX = I(6), PY = I(7) };
                }
            }
            if (s == null) Debug.LogWarning("PixelFx sheet missing: " + name + " tex=" + (tex != null) + " meta=" + (meta != null));
            cache[name] = s;
            return s;
        }

        private Material own;
        public Material Mat()
        {
            if (own == null)
            {
                var sh = Shader.Find("Sprites/Default");
                if (sh == null) sh = Shader.Find("Unlit/Transparent");
                own = new Material(sh);
                own.mainTexture = Tex;
            }
            return own;
        }
    }

    public sealed class PixelFxPlayer : MonoBehaviour
    {
        private PixelFxSheet sheet; private Mesh mesh; private float t; private bool once; private float speed = 1f;
        private float angleDeg; private bool useAngle; private Vector3[] uv3; private int lastFrame = -1;

        // ppu: dünya birimi başına piksel. Pivot noktası transform konumuna oturur.
        public static PixelFxPlayer Create(string name, Transform parent, Vector3 localPos, float ppu, bool destroyAtEnd = false, Color? tint = null)
        {
            var sh = PixelFxSheet.Get(name);
            if (sh == null) return null;
            var go = new GameObject("fx_" + name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = sh.Mat();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var pl = go.AddComponent<PixelFxPlayer>();
            pl.sheet = sh; pl.once = destroyAtEnd || !sh.Loop;
            float w = sh.FW / ppu, h = sh.FH / ppu, px = sh.PX / ppu, py = (sh.FH - sh.PY) / ppu;
            pl.mesh = new Mesh();
            pl.mesh.vertices = new[] { new Vector3(-px, -py, 0), new Vector3(w - px, -py, 0), new Vector3(w - px, h - py, 0), new Vector3(-px, h - py, 0) };
            pl.mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            var cols = new Color[4]; var c = tint ?? Color.white; for (int i = 0; i < 4; i++) cols[i] = c;
            pl.mesh.colors = cols;
            mf.sharedMesh = pl.mesh;
            pl.Apply(0);
            return pl;
        }

        public void SetSpeed(float s) { speed = s; }
        public void SetScreenAngle(float deg) { angleDeg = deg; useAngle = true; }

        private void Apply(int f)
        {
            if (f == lastFrame) return; lastFrame = f;
            int col = f % sheet.Cols, row = f / sheet.Cols;
            float tw = sheet.Tex.width, th = sheet.Tex.height;
            float u0 = col * sheet.FW / tw, u1 = (col + 1) * sheet.FW / tw;
            float v1 = 1f - row * sheet.FH / th, v0 = 1f - (row + 1) * sheet.FH / th;
            mesh.uv = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1) };
        }

        private void LateUpdate()
        {
            t += Time.deltaTime * speed;
            int f = (int)(t * sheet.Fps);
            if (once && f >= sheet.Frames) { Destroy(gameObject); return; }
            Apply(sheet.Loop ? f % sheet.Frames : Mathf.Min(f, sheet.Frames - 1));
            var cam = Camera.main;
            if (cam != null)
            {
                var rot = cam.transform.rotation;
                transform.rotation = useAngle ? rot * Quaternion.Euler(0f, 0f, angleDeg) : rot;
            }
        }

        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
