using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GameCore.Grid;
using GameCore.Village;

namespace GameClient
{
    /// <summary>
    /// Aşama 2 prototipi: izometrik köy gridi, kamera, dokunmatik yerleştirme.
    /// Sahneye elle eklemek gerekmez; oyun başlayınca kendini oluşturur.
    /// Mantık Core'dadır (VillageLayout); burası yalnızca görünüm ve girdi.
    /// </summary>
    public sealed class VillageBootstrap : MonoBehaviour
    {
        private enum Tool { Pan, Road, Building, Tower, Entrance, Upgrade, Erase }
        private static readonly string[] ToolNames = { "Kaydır", "Yol", "Bina", "Kule", "Giriş", "Geliştir", "Sil" };
        private const int MinSize = 5, MaxSize = 21;

        private VillageLayout layout;
        private Tool tool = Tool.Road;
        private int playerLevel = 1;              // test durumu (gerçek oyunda sunucudan gelir)
        private bool roadResearchDone;            // test durumu: yol geliştirme araştırması
        private Camera cam;
        private Vector3 target;
        private float zoom = 8f;
        private Transform viewRoot;
        private Material baseMat;
        private readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
        private bool dirty = true;
        private bool valid;
        private string validationError = "";
        private string toast = "";
        private float toastUntil;
        private float uiScale = 1f;
        private readonly List<Rect> uiRects = new List<Rect>();
        private GridPos lastPainted = new GridPos(-1, -1);
        private bool dragBlocked;
        private float prevPinch = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            new GameObject("VillageBootstrap").AddComponent<VillageBootstrap>();
        }

        private void Start()
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = tmp.GetComponent<MeshRenderer>().sharedMaterial;
            Destroy(tmp);

            layout = new VillageLayout(11, RoadRules.RoadLimitForLevel(playerLevel), RoadRules.MaxEntrances);
            cam = Camera.main;
            if (cam == null)
            {
                var g = new GameObject("Main Camera");
                g.tag = "MainCamera";
                cam = g.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.15f, 0.20f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            ResetCamera();
        }

        private void ResetCamera()
        {
            float c = (layout.Size - 1) * 0.5f;
            target = new Vector3(c, 0f, c - VillageLayout.AttackZoneRows * 0.5f); // saldırı alanı da görünsün
            zoom = layout.Size * 0.75f + 4f;
        }

        private void Update()
        {
            HandleInput();
            if (dirty) Rebuild();
            UpdateCamera();
        }

        // ---------- Kamera ----------
        private void UpdateCamera()
        {
            float c = (layout.Size - 1) * 0.5f;
            float lim = layout.Size * 0.5f + 2f;
            target.x = Mathf.Clamp(target.x, c - lim, c + lim);
            target.z = Mathf.Clamp(target.z, c - lim - VillageLayout.AttackZoneRows, c + lim);
            cam.orthographicSize = zoom;
            cam.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
            cam.transform.position = target - cam.transform.forward * 60f;
        }

        private bool RayToPlane(Vector2 screen, out Vector3 hit)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            var ray = cam.ScreenPointToRay(screen);
            if (plane.Raycast(ray, out float e)) { hit = ray.GetPoint(e); return true; }
            hit = Vector3.zero;
            return false;
        }

        private bool TryGetCell(Vector2 screen, out GridPos cell)
        {
            cell = new GridPos(-1, -1);
            if (!RayToPlane(screen, out var hit)) return false;
            cell = new GridPos(Mathf.RoundToInt(hit.x), Mathf.RoundToInt(hit.z));
            return layout.InBounds(cell);
        }

        // ---------- Girdi ----------
        private void HandleInput()
        {
            HandleZoom(out bool pinching);

            var ptr = Pointer.current;
            if (ptr == null) return;
            Vector2 pos = ptr.position.ReadValue();

            bool down = ptr.press.wasPressedThisFrame;
            bool held = ptr.press.isPressed;
            if (down) dragBlocked = IsOverUi(pos);
            // Çok kısa dokunuşta basma+bırakma aynı karede olabilir: wasPressedThisFrame de sayılır.
            if (!held && !down) { lastPainted = new GridPos(-1, -1); return; }
            if (dragBlocked || pinching) return;

            if (tool == Tool.Pan)
            {
                if (down) return;
                Vector2 delta = ptr.delta.ReadValue();
                if (RayToPlane(pos, out var cur) && RayToPlane(pos - delta, out var prev))
                    target += prev - cur;
                return;
            }

            if (!TryGetCell(pos, out var cell) || cell.Equals(lastPainted)) return;
            lastPainted = cell;
            Apply(cell);
        }

        private void HandleZoom(out bool pinching)
        {
            float z = 0f;
            var m = Mouse.current;
            if (m != null)
            {
                float s = m.scroll.ReadValue().y;
                if (Mathf.Abs(s) > 0.01f) z -= Mathf.Sign(s) * 0.8f;
            }
            pinching = false;
            var t = Touchscreen.current;
            if (t != null && t.touches.Count >= 2 && t.touches[0].isInProgress && t.touches[1].isInProgress)
            {
                float d = Vector2.Distance(t.touches[0].position.ReadValue(), t.touches[1].position.ReadValue());
                if (prevPinch > 0f) z += (prevPinch - d) * 0.01f;
                prevPinch = d;
                pinching = true;
            }
            else prevPinch = -1f;
            zoom = Mathf.Clamp(zoom + z, 3f, 30f);
        }

        private bool IsOverUi(Vector2 screen)
        {
            var gp = new Vector2(screen.x / uiScale, (Screen.height - screen.y) / uiScale);
            foreach (var r in uiRects) if (r.Contains(gp)) return true;
            return false;
        }

        private void Apply(GridPos cell)
        {
            string err = null;
            bool ok = false;
            switch (tool)
            {
                case Tool.Road:
                    ok = layout.TryPlace(cell, CellKind.Road, out err);
                    break;
                case Tool.Upgrade:
                    if (!RoadRules.CanUpgrade(playerLevel, roadResearchDone, out err)) break;
                    ok = layout.TryUpgradeRoad(cell, out err);
                    break;
                case Tool.Building: ok = layout.TryPlace(cell, CellKind.Building, out err); break;
                case Tool.Tower: ok = layout.TryPlace(cell, CellKind.Tower, out err); break;
                case Tool.Entrance: ok = layout.ToggleEntrance(cell, out err); break;
                case Tool.Erase: ok = layout.Remove(cell); break;
            }
            if (ok) dirty = true;
            else if (!string.IsNullOrEmpty(err)) { toast = err; toastUntil = Time.time + 2f; }
        }

        // ---------- Görünüm ----------
        private Material Mat(Color c)
        {
            if (!mats.TryGetValue(c, out var m))
            {
                m = new Material(baseMat);
                m.SetColor("_BaseColor", c);
                m.color = c;
                mats[c] = m;
            }
            return m;
        }

        private void Cube(GridPos p, Vector3 scale, float centerY, Color c)
            => Cube(new Vector3(p.X, centerY, p.Y), scale, c);

        private void Cube(Vector3 pos, Vector3 scale, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(viewRoot, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(c);
        }


        private bool IsRoad(int x, int y) => layout.Get(new GridPos(x, y)) == CellKind.Road;

        /// <summary>Komşu yollara otomatik bağlanan yol parçası: merkez + bağlı yönlere kollar.
        /// Düz, köşe, T, kavşak ve uç şekilleri buradan çıkar. Giriş hücresi dışarıya (güney) da uzanır.</summary>
        private void DrawRoad(GridPos p)
        {
            var tier = layout.GetRoadTier(p);
            Color baseC, rail, stripe;
            switch (tier)
            {
                case RoadTier.Reinforced:
                    baseC = new Color(0.66f, 0.52f, 0.30f); rail = new Color(0.92f, 0.74f, 0.20f); stripe = new Color(0.50f, 0.38f, 0.20f); break;
                case RoadTier.Iron:
                    baseC = new Color(0.40f, 0.42f, 0.47f); rail = new Color(0.72f, 0.74f, 0.80f); stripe = new Color(0.30f, 0.32f, 0.36f); break;
                default:
                    baseC = new Color(0.62f, 0.50f, 0.34f); rail = baseC; stripe = new Color(0.44f, 0.34f, 0.21f); break;
            }
            const float slabH = 0.14f, slabY = 0.02f;
            var c = new Vector3(p.X, 0f, p.Y);

            // merkez
            Cube(c + new Vector3(0, slabY, 0), new Vector3(0.5f, slabH, 0.5f), baseC);
            if (tier != RoadTier.Wood)
                Cube(c + new Vector3(0, 0.1f, 0), new Vector3(0.16f, 0.1f, 0.16f), rail);

            // yönler: (dx, dy)
            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };
            bool[] open =
            {
                IsRoad(p.X, p.Y + 1),
                IsRoad(p.X + 1, p.Y),
                IsRoad(p.X, p.Y - 1) || layout.IsEntrance(p),
                IsRoad(p.X - 1, p.Y)
            };
            for (int i = 0; i < 4; i++)
            {
                if (!open[i]) continue;
                bool alongX = dx[i] != 0;
                var dir = new Vector3(dx[i], 0, dy[i]);
                var armPos = c + dir * 0.375f + new Vector3(0, slabY, 0);
                var armSize = alongX ? new Vector3(0.25f, slabH, 0.5f) : new Vector3(0.5f, slabH, 0.25f);
                Cube(armPos, armSize, baseC);

                if (tier == RoadTier.Wood)
                {
                    // tahta çıtası: kolu enine kesen koyu çizgi
                    var sSize = alongX ? new Vector3(0.04f, slabH + 0.01f, 0.5f) : new Vector3(0.5f, slabH + 0.01f, 0.04f);
                    Cube(c + dir * 0.375f + new Vector3(0, slabY, 0), sSize, stripe);
                }
                else
                {
                    // raylar: kolun iki yan kenarında ince çıta
                    var perp = alongX ? new Vector3(0, 0, 1) : new Vector3(1, 0, 0);
                    var rSize = alongX ? new Vector3(0.25f, 0.2f, 0.05f) : new Vector3(0.05f, 0.2f, 0.25f);
                    Cube(armPos + perp * 0.22f + new Vector3(0, 0.03f, 0), rSize, rail);
                    Cube(armPos - perp * 0.22f + new Vector3(0, 0.03f, 0), rSize, rail);
                }
            }
        }

        private void Rebuild()
        {
            if (viewRoot != null) Destroy(viewRoot.gameObject);
            viewRoot = new GameObject("View").transform;
            int n = layout.Size;
            var grass1 = new Color(0.30f, 0.55f, 0.30f);
            var grass2 = new Color(0.27f, 0.50f, 0.27f);
            var edge = new Color(0.9f, 0.8f, 0.3f);
            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                {
                    var p = new GridPos(x, y);
                    var g = ((x + y) & 1) == 0 ? grass1 : grass2;
                    if (y == 0) g = Color.Lerp(g, edge, 0.35f); // giriş kenarı
                    Cube(p, new Vector3(0.96f, 0.1f, 0.96f), -0.05f, g);
                    switch (layout.Get(p))
                    {
                        case CellKind.Road:
                            DrawRoad(p);
                            break;
                        case CellKind.Building:
                            Cube(p, new Vector3(0.7f, 0.7f, 0.7f), 0.35f, new Color(0.85f, 0.65f, 0.15f));
                            break;
                        case CellKind.Tower:
                            Cube(p, new Vector3(0.45f, 1.3f, 0.45f), 0.65f, new Color(0.55f, 0.58f, 0.65f));
                            Cube(p, new Vector3(0.65f, 0.2f, 0.65f), 1.4f, new Color(0.25f, 0.45f, 0.85f));
                            break;
                    }
                    if (layout.IsEntrance(p))
                        Cube(p, new Vector3(0.5f, 0.2f, 0.5f), 0.15f, new Color(0.95f, 0.45f, 0.1f));
                }
            // Saldırganın asker gönderme alanı (4 x Size): giriş kenarının dışında, kırmızı tonlu.
            var z1 = new Color(0.55f, 0.25f, 0.22f);
            var z2 = new Color(0.48f, 0.21f, 0.19f);
            for (int x = 0; x < n; x++)
                for (int r = 1; r <= VillageLayout.AttackZoneRows; r++)
                    Cube(new GridPos(x, -r), new Vector3(0.96f, 0.1f, 0.96f), -0.05f, ((x + r) & 1) == 0 ? z1 : z2);
            dirty = false;
            valid = layout.Validate(out validationError);
        }

        private void SetLevel(int level)
        {
            playerLevel = level;
            layout = layout.WithLimits(RoadRules.RoadLimitForLevel(level), layout.EntranceLimit);
            dirty = true;
        }

        // ---------- Arayüz (OnGUI: dokunmatik düğmeler de çalışır) ----------
        private void OnGUI()
        {
            if (layout == null) return;
            uiScale = Mathf.Max(1f, Screen.height / 600f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
            GUI.skin.button.fontSize = 14;
            GUI.skin.label.fontSize = 14;
            GUI.skin.box.fontSize = 14;
            uiRects.Clear();

            float w = Screen.width / uiScale;
            float h = Screen.height / uiScale;
            float bw = Mathf.Min(80f, (w - 8f) / ToolNames.Length - 2f);

            for (int i = 0; i < ToolNames.Length; i++)
            {
                var r = new Rect(4 + i * (bw + 2), 4, bw, 36);
                GUI.color = (int)tool == i ? Color.yellow : Color.white;
                if (GUI.Button(r, ToolNames[i])) tool = (Tool)i;
                uiRects.Add(r);
            }
            GUI.color = Color.white;

            var rm = new Rect(4, 44, 60, 32);
            var rl = new Rect(68, 44, 70, 32);
            var rp = new Rect(142, 44, 60, 32);
            var rc = new Rect(206, 44, 70, 32);
            var rt = new Rect(280, 44, 70, 32);
            GUI.Box(rl, layout.Size + "x" + layout.Size);
            if (GUI.Button(rm, "Grid -") && layout.Size > MinSize) { layout = layout.Resized(layout.Size - 1); ResetCamera(); dirty = true; }
            if (GUI.Button(rp, "Grid +") && layout.Size < MaxSize) { layout = layout.Resized(layout.Size + 1); ResetCamera(); dirty = true; }
            if (GUI.Button(rc, "Kamera")) ResetCamera();
            if (GUI.Button(rt, "Temizle")) { layout = new VillageLayout(layout.Size, layout.RoadLimit, layout.EntranceLimit); dirty = true; }
            uiRects.Add(rm); uiRects.Add(rl); uiRects.Add(rp); uiRects.Add(rc); uiRects.Add(rt);

            // Test paneli: seviye ve araştırma durumu (gerçek oyunda sunucu belirler)
            var lm = new Rect(4, 80, 50, 32);
            var ll = new Rect(58, 80, 70, 32);
            var lp = new Rect(132, 80, 50, 32);
            var rr = new Rect(186, 80, 190, 32);
            GUI.Box(ll, "Sv " + playerLevel);
            if (GUI.Button(lm, "Sv -") && playerLevel > 1) SetLevel(playerLevel - 1);
            if (GUI.Button(lp, "Sv +") && playerLevel < 30) SetLevel(playerLevel + 1);
            if (GUI.Button(rr, "Yol araştırması: " + (roadResearchDone ? "VAR" : "yok") + " (test)")) roadResearchDone = !roadResearchDone;
            uiRects.Add(lm); uiRects.Add(ll); uiRects.Add(lp); uiRects.Add(rr);

            var rs = new Rect(4, h - 44, w - 8, 40);
            string msg = valid ? "GEÇERLİ - savunma kaydedilebilir" : "GEÇERSİZ - " + validationError;
            if (Time.time < toastUntil) msg = toast;
            msg += "    [Sv " + playerLevel + " | Yol " + layout.RoadCount + "/" + layout.RoadLimit + " | Giriş " + layout.EntranceCount + "]";
            GUI.color = valid ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.8f, 0.5f);
            GUI.Box(rs, msg);
            GUI.color = Color.white;
            uiRects.Add(rs);
        }
    }
}
