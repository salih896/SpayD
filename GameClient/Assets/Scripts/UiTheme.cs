using UnityEngine;

namespace GameClient
{
    /// <summary>
    /// Çalışma anında üretilen arayüz teması (dış görsel dosyası yok): koyu ahşap paneller, altın kenar, yuvarlak düğmeler.
    /// OnGUI başında GUI.skin = UiTheme.Skin atanır; özel stiller (Header, Chip, Tab...) doğrudan kullanılır.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Cream = new Color(0.99f, 0.95f, 0.86f);
        public static readonly Color GoldText = new Color(1f, 0.84f, 0.38f);
        public static readonly Color Muted = new Color(0.80f, 0.74f, 0.64f);
        public static readonly Color Good = new Color(0.55f, 0.92f, 0.45f);
        public static readonly Color Warn = new Color(1f, 0.70f, 0.35f);

        public static GUISkin Skin { get; private set; }
        public static GUIStyle Header, Title, Small, Chip, Tab, TabOn, BtnOn, BtnGreen, BtnRed, Pill, PillBad, Parchment, Big, Center;
        public static Texture2D White, Circle, BarBg, BarFill;
        private static Font font;

        public static void Ensure()
        {
            if (Skin != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Roboto", "Helvetica" }, 16);
            font.hideFlags = HideFlags.HideAndDontSave; // Editör yeniden yüklemede "invalid font reference" uyarısı vermesin
            White = Solid(Color.white);
            Circle = MakeCircle(32);
            BarBg = Rounded(new Color(0.08f, 0.06f, 0.05f, 0.9f), new Color(0.35f, 0.26f, 0.16f), 6, 1);
            BarFill = Rounded(new Color(0.95f, 0.75f, 0.25f), new Color(1f, 0.92f, 0.6f), 6, 1);

            Skin = ScriptableObject.CreateInstance<GUISkin>();
            Skin.hideFlags = HideFlags.HideAndDontSave;
            Skin.font = font;
            var panel = Rounded(new Color(0.16f, 0.11f, 0.07f, 0.94f), new Color(0.78f, 0.60f, 0.28f), 10, 2);
            var btn = Gradient(new Color(0.52f, 0.34f, 0.18f), new Color(0.36f, 0.22f, 0.11f), new Color(0.86f, 0.66f, 0.32f), 9, 2);
            var btnHover = Gradient(new Color(0.64f, 0.43f, 0.23f), new Color(0.44f, 0.28f, 0.14f), new Color(1f, 0.82f, 0.45f), 9, 2);
            var btnDown = Gradient(new Color(0.30f, 0.19f, 0.10f), new Color(0.42f, 0.27f, 0.14f), new Color(0.86f, 0.66f, 0.32f), 9, 2);
            var btnOff = Gradient(new Color(0.30f, 0.27f, 0.24f), new Color(0.22f, 0.20f, 0.18f), new Color(0.45f, 0.40f, 0.34f), 9, 2);
            var gold = Gradient(new Color(1f, 0.82f, 0.36f), new Color(0.85f, 0.58f, 0.16f), new Color(1f, 0.95f, 0.7f), 9, 2);
            var green = Gradient(new Color(0.38f, 0.68f, 0.28f), new Color(0.22f, 0.48f, 0.16f), new Color(0.75f, 0.95f, 0.55f), 9, 2);
            var red = Gradient(new Color(0.78f, 0.28f, 0.20f), new Color(0.55f, 0.16f, 0.12f), new Color(1f, 0.6f, 0.45f), 9, 2);

            Skin.box = new GUIStyle { normal = { background = panel, textColor = Cream }, border = new RectOffset(12, 12, 12, 12), padding = new RectOffset(10, 10, 8, 8), alignment = TextAnchor.MiddleCenter, fontSize = 15, font = font, wordWrap = true };
            Skin.button = new GUIStyle
            {
                normal = { background = btn, textColor = Cream }, hover = { background = btnHover, textColor = Color.white },
                active = { background = btnDown, textColor = GoldText }, focused = { background = btn, textColor = Cream },
                onNormal = { background = gold, textColor = new Color(0.25f, 0.14f, 0.05f) }, onHover = { background = gold, textColor = new Color(0.2f, 0.1f, 0.04f) },
                border = new RectOffset(11, 11, 11, 11), padding = new RectOffset(8, 8, 4, 4), margin = new RectOffset(3, 3, 3, 3),
                alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold, font = font, wordWrap = true
            };
            Skin.label = new GUIStyle { normal = { textColor = Cream }, fontSize = 15, font = font, wordWrap = true, richText = true, padding = new RectOffset(2, 2, 2, 2) };
            Skin.toggle = new GUIStyle(Skin.button);
            Skin.textField = new GUIStyle(Skin.box) { alignment = TextAnchor.MiddleLeft };
            Skin.scrollView = new GUIStyle();
            Skin.verticalScrollbar = new GUIStyle { normal = { background = Rounded(new Color(0f, 0f, 0f, 0.35f), new Color(0, 0, 0, 0), 6, 0) }, fixedWidth = 12, border = new RectOffset(6, 6, 6, 6) };
            Skin.verticalScrollbarThumb = new GUIStyle { normal = { background = Rounded(new Color(0.78f, 0.60f, 0.28f), new Color(1f, 0.85f, 0.5f), 6, 1) }, fixedWidth = 12, border = new RectOffset(6, 6, 6, 6) };
            Skin.verticalScrollbarUpButton = new GUIStyle(); Skin.verticalScrollbarDownButton = new GUIStyle();
            Skin.horizontalScrollbar = new GUIStyle { fixedHeight = 0 }; Skin.horizontalScrollbarThumb = new GUIStyle();
            Skin.horizontalScrollbarLeftButton = new GUIStyle(); Skin.horizontalScrollbarRightButton = new GUIStyle();
            Skin.settings.cursorColor = GoldText;
            Skin.settings.selectionColor = new Color(1f, 0.8f, 0.3f, 0.4f);

            Header = new GUIStyle(Skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = GoldText }, wordWrap = false };
            Title = new GUIStyle(Skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = GoldText }, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            Big = new GUIStyle(Skin.label) { fontSize = 17, alignment = TextAnchor.UpperCenter };
            Center = new GUIStyle(Skin.label) { alignment = TextAnchor.MiddleCenter };
            Small = new GUIStyle(Skin.label) { fontSize = 13, normal = { textColor = Muted } };
            Chip = new GUIStyle(Skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = false, normal = { background = Rounded(new Color(0.10f, 0.07f, 0.05f, 0.85f), new Color(0.55f, 0.42f, 0.22f), 12, 1), textColor = Cream }, border = new RectOffset(12, 12, 12, 12), padding = new RectOffset(30, 10, 2, 2) };
            Tab = new GUIStyle(Skin.button) { fontSize = 16 };
            TabOn = new GUIStyle(Skin.button) { fontSize = 16, normal = { background = gold, textColor = new Color(0.25f, 0.14f, 0.05f) }, hover = { background = gold, textColor = new Color(0.2f, 0.1f, 0.04f) } };
            BtnOn = new GUIStyle(TabOn) { fontSize = 15 };
            BtnGreen = new GUIStyle(Skin.button) { normal = { background = green, textColor = Color.white }, hover = { background = green, textColor = Color.white } };
            BtnRed = new GUIStyle(Skin.button) { normal = { background = red, textColor = Color.white }, hover = { background = red, textColor = Color.white } };
            Pill = new GUIStyle(Skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false, normal = { background = Rounded(new Color(0.12f, 0.30f, 0.12f, 0.88f), new Color(0.5f, 0.85f, 0.4f), 12, 1), textColor = new Color(0.85f, 1f, 0.8f) }, border = new RectOffset(12, 12, 12, 12) };
            PillBad = new GUIStyle(Pill) { normal = { background = Rounded(new Color(0.38f, 0.18f, 0.08f, 0.9f), new Color(1f, 0.6f, 0.3f), 12, 1), textColor = new Color(1f, 0.88f, 0.75f) } };
            Parchment = new GUIStyle(Skin.box) { normal = { background = Rounded(new Color(0.95f, 0.88f, 0.72f, 0.98f), new Color(0.55f, 0.36f, 0.16f), 12, 3), textColor = new Color(0.25f, 0.15f, 0.07f) }, alignment = TextAnchor.UpperCenter, fontSize = 16, padding = new RectOffset(18, 18, 16, 16) };
            Skin.button.onActive.background = gold;
            DisabledBtn = btnOff;
        }

        public static Texture2D DisabledBtn { get; private set; }

        // ---------- Çizim yardımcıları ----------
        public static void Dot(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, Circle);
            GUI.color = old;
        }

        public static void Bar(Rect r, float f, Color c)
        {
            GUI.DrawTexture(r, BarBg, ScaleMode.StretchToFill, true, 0f, Color.white, 0f, 6f);
            if (f <= 0f) return;
            var fr = new Rect(r.x + 2, r.y + 2, Mathf.Max(4f, (r.width - 4) * Mathf.Clamp01(f)), r.height - 4);
            GUI.DrawTexture(fr, White, ScaleMode.StretchToFill, true, 0f, c, 0f, 5f);
        }

        // ---------- Doku üretimi ----------
        private static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.DontSave };
            t.SetPixel(0, 0, c); t.Apply();
            return t;
        }

        private static Texture2D MakeCircle(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                    float a = Mathf.Clamp01(r - d);
                    float rim = d > r - 3f ? 0.75f : 1f;
                    t.SetPixel(x, y, new Color(rim, rim, rim, a));
                }
            t.Apply();
            return t;
        }

        /// <summary>Yuvarlak köşeli, kenarlıklı 9-dilim doku.</summary>
        private static Texture2D Rounded(Color fill, Color edge, int radius, int edgeW) => Gradient(fill, fill, edge, radius, edgeW);

        private static Texture2D Gradient(Color top, Color bottom, Color edge, int radius, int edgeW)
        {
            int n = radius * 2 + 8;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(radius - x - 0.5f, x + 0.5f - (n - radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(radius - y - 0.5f, y + 0.5f - (n - radius)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float inside = Mathf.Clamp01(radius - d + 0.5f);
                    float edgeDist = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
                    bool isEdge = d > radius - edgeW - 0.5f || (dx <= 0 && dy <= 0 && edgeDist < edgeW);
                    // dokuda y=0 alttadır: üst rengi yukarıya koy
                    var c = Color.Lerp(bottom, top, y / (float)(n - 1));
                    if (edgeW > 0 && isEdge) c = edge;
                    else if (edgeW > 0 && y >= n - edgeW - 3) c = Color.Lerp(c, Color.white, 0.12f); // üst parlama
                    c.a *= inside;
                    t.SetPixel(x, y, c);
                }
            t.Apply();
            return t;
        }
    }
}
