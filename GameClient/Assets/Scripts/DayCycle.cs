using System;
using UnityEngine;

namespace GameClient
{
    /// <summary>
    /// Gerçek saate bağlı gün/gece (yalnızca görsel; kullanıcı isteği 27.11.2026 "görsel ve grafikleri geliştir"):
    /// şafak (pembe), gündüz, gün batımı (turuncu, uzun gölge), gece (okunaklı mavi ay ışığı). Geçişler yumuşak.
    /// Ayarlar'dan "Hep gündüz" seçilebilir. Gece binalarda pencere ışıkları yanar (FxKit.NightGlow).
    /// </summary>
    public sealed class DayCycle : MonoBehaviour
    {
        private const string Key = "daynight_real";
        private struct Look { public float Hour; public Color Sun; public float Intensity, Pitch; public Color Sky, Eq, Ground, Bg, Fog; }

        // saat sırasına göre anahtar kareler (24 saatlik döngü)
        private static readonly Look[] Keys =
        {
            new Look { Hour = 0f,  Sun = new Color(0.7f, 0.8f, 1f), Intensity = 0.95f, Pitch = 55f, Sky = new Color(0.55f, 0.62f, 0.88f), Eq = new Color(0.42f, 0.46f, 0.62f), Ground = new Color(0.25f, 0.25f, 0.32f), Bg = new Color(0.1f, 0.13f, 0.25f), Fog = new Color(0.16f, 0.2f, 0.34f) },
            new Look { Hour = 5.5f, Sun = new Color(0.7f, 0.8f, 1f), Intensity = 0.95f, Pitch = 55f, Sky = new Color(0.55f, 0.62f, 0.88f), Eq = new Color(0.42f, 0.46f, 0.62f), Ground = new Color(0.25f, 0.25f, 0.32f), Bg = new Color(0.1f, 0.13f, 0.25f), Fog = new Color(0.16f, 0.2f, 0.34f) },
            new Look { Hour = 7f,  Sun = new Color(1f, 0.78f, 0.7f),    Intensity = 1.2f,  Pitch = 25f, Sky = new Color(0.9f, 0.78f, 0.85f), Eq = new Color(0.75f, 0.62f, 0.6f), Ground = new Color(0.32f, 0.27f, 0.25f), Bg = new Color(0.95f, 0.72f, 0.7f), Fog = new Color(0.92f, 0.75f, 0.75f) },
            new Look { Hour = 9f,  Sun = new Color(1f, 0.94f, 0.82f),   Intensity = 1.55f, Pitch = 52f, Sky = new Color(0.86f, 0.9f, 1f),    Eq = new Color(0.72f, 0.72f, 0.64f), Ground = new Color(0.36f, 0.32f, 0.27f), Bg = new Color(0.56f, 0.76f, 0.92f), Fog = new Color(0.66f, 0.8f, 0.9f) },
            new Look { Hour = 16.5f, Sun = new Color(1f, 0.94f, 0.82f), Intensity = 1.55f, Pitch = 52f, Sky = new Color(0.86f, 0.9f, 1f),    Eq = new Color(0.72f, 0.72f, 0.64f), Ground = new Color(0.36f, 0.32f, 0.27f), Bg = new Color(0.56f, 0.76f, 0.92f), Fog = new Color(0.66f, 0.8f, 0.9f) },
            new Look { Hour = 18.5f, Sun = new Color(1f, 0.62f, 0.35f), Intensity = 1.25f, Pitch = 20f, Sky = new Color(0.95f, 0.7f, 0.55f), Eq = new Color(0.78f, 0.55f, 0.42f), Ground = new Color(0.32f, 0.24f, 0.2f), Bg = new Color(0.98f, 0.6f, 0.42f), Fog = new Color(0.95f, 0.65f, 0.5f) },
            new Look { Hour = 20.5f, Sun = new Color(0.7f, 0.8f, 1f), Intensity = 0.95f, Pitch = 55f, Sky = new Color(0.55f, 0.62f, 0.88f), Eq = new Color(0.42f, 0.46f, 0.62f), Ground = new Color(0.25f, 0.25f, 0.32f), Bg = new Color(0.1f, 0.13f, 0.25f), Fog = new Color(0.16f, 0.2f, 0.34f) },
            new Look { Hour = 24f, Sun = new Color(0.7f, 0.8f, 1f), Intensity = 0.95f, Pitch = 55f, Sky = new Color(0.55f, 0.62f, 0.88f), Eq = new Color(0.42f, 0.46f, 0.62f), Ground = new Color(0.25f, 0.25f, 0.32f), Bg = new Color(0.1f, 0.13f, 0.25f), Fog = new Color(0.16f, 0.2f, 0.34f) },
        };

        private static DayCycle inst;
        private Light sun;
        private Camera cam;
        private float next;

        public static bool RealTime
        {
            get { try { return PlayerPrefs.GetInt(Key, 1) == 1; } catch { return true; } }
            set { try { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); } catch { } if (inst != null) inst.next = 0f; }
        }

        /// <summary>0 = gündüz, 1 = tam gece (pencere ışıkları bu değere göre yanar).</summary>
        public static float Night { get; private set; }

        /// <summary>Gece/gündüz eşiği geçildi (bir kez okunur).</summary>
        public static bool NightChanged;

        public static void Setup(Camera c, Light s)
        {
            if (inst == null)
            {
                var go = new GameObject("DayCycle");
                DontDestroyOnLoad(go);
                inst = go.AddComponent<DayCycle>();
            }
            inst.cam = c; inst.sun = s; inst.next = 0f;
        }

        private void Update()
        {
            if (Time.unscaledTime < next || sun == null || cam == null) return;
            next = Time.unscaledTime + 2f;
            float hour = RealTime ? (float)DateTime.Now.TimeOfDay.TotalHours : 12f;
            Look a = Keys[0], b = Keys[1];
            for (int i = 0; i < Keys.Length - 1; i++) if (hour >= Keys[i].Hour && hour <= Keys[i + 1].Hour) { a = Keys[i]; b = Keys[i + 1]; break; }
            float k = b.Hour > a.Hour ? Mathf.SmoothStep(0f, 1f, (hour - a.Hour) / (b.Hour - a.Hour)) : 0f;
            sun.color = Color.Lerp(a.Sun, b.Sun, k);
            sun.intensity = Mathf.Lerp(a.Intensity, b.Intensity, k);
            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(a.Pitch, b.Pitch, k), -35f, 0f);
            RenderSettings.ambientSkyColor = Color.Lerp(a.Sky, b.Sky, k);
            RenderSettings.ambientEquatorColor = Color.Lerp(a.Eq, b.Eq, k);
            RenderSettings.ambientGroundColor = Color.Lerp(a.Ground, b.Ground, k);
            cam.backgroundColor = Color.Lerp(a.Bg, b.Bg, k);
            RenderSettings.fogColor = Color.Lerp(a.Fog, b.Fog, k);
            float intensity = sun.intensity;
            bool wasNight = Night > 0.5f;
            Night = Mathf.Clamp01((1.4f - intensity) / 0.45f); // 0,95 yoğunluk = tam gece
            if ((Night > 0.5f) != wasNight) NightChanged = true; // pencere ışıkları için köy yeniden çizilir
        }
    }
}
