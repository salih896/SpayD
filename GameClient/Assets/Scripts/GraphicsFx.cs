using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameClient
{
    /// <summary>
    /// Görüntü kalitesi (yalnızca görsel): URP son işleme — parlama (bloom: kor, kristal, element ışıkları gerçekten parlar),
    /// renk ayarı (biraz daha canlı ve kontrastlı), hafif kenar kararması, kenar yumuşatma ve yumuşak gölge.
    /// "Yüksek / Düşük" ayarı cihazda saklanır (PlayerPrefs); düşük ayar zayıf telefonlarda pil ve kare hızı için.
    /// </summary>
    public static class GraphicsFx
    {
        private const string Key = "gfx_high";
        private static Volume vol;
        private static Camera cam;
        private static Light sun;

        public static bool High
        {
            get { try { return PlayerPrefs.GetInt(Key, 1) == 1; } catch { return true; } }
            set { try { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); } catch { } Apply(); }
        }

        public static void Setup(Camera c, Light s)
        {
            cam = c; sun = s;
            DayCycle.Setup(c, s); // gerçek saate göre şafak / gündüz / gün batımı / gece
            if (vol == null)
            {
                var go = new GameObject("PostFx");
                Object.DontDestroyOnLoad(go);
                vol = go.AddComponent<Volume>();
                vol.isGlobal = true;
                vol.priority = 10f;
                var p = ScriptableObject.CreateInstance<VolumeProfile>();
                var bloom = p.Add<Bloom>(true);
                bloom.threshold.Override(0.92f); bloom.intensity.Override(0.6f); bloom.scatter.Override(0.62f);
                bloom.tint.Override(new Color(1f, 0.95f, 0.88f));
                var tone = p.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.Neutral);
                var ca = p.Add<ColorAdjustments>(true);
                ca.postExposure.Override(0.12f); ca.contrast.Override(9f); ca.saturation.Override(14f);
                var wb = p.Add<WhiteBalance>(true);
                wb.temperature.Override(6f); // hafif sıcak, masalsı ton
                var vg = p.Add<Vignette>(true);
                vg.intensity.Override(0.2f); vg.smoothness.Override(0.5f);
                vol.profile = p;
            }
            Apply();
        }

        public static void Apply()
        {
            bool h = High;
            if (vol != null) vol.enabled = h;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null)
                {
                    data.renderPostProcessing = h;
                    data.antialiasing = h ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
                }
            }
            if (sun != null) sun.shadows = h ? LightShadows.Soft : LightShadows.None; // düşük: gölge geçişi yok (draw call yarıya iner)
        }
    }
}
