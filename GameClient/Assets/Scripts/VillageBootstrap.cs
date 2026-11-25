using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Game;
using GameCore.Grid;
using GameCore.Village;

namespace GameClient
{
    /// <summary>
    /// Oynanabilir oyun istemcisi (prototip arayüz). Tüm kurallar Core'dadır (GameState); burası görünüm, girdi ve kayıttır.
    /// Dosyalar: VillageBootstrap.cs (çekirdek+girdi), .View (çizim), .Ui (paneller), .Battle (savaş).
    /// Sahneye elle eklemek gerekmez; oyun başlayınca kendini oluşturur.
    /// </summary>
    public sealed partial class VillageBootstrap : MonoBehaviour
    {
        private enum Tool { Pan, Select }
        private enum Tab { Village, Barracks, Research, Quests, Attack, Menu, TestArmy }

        private GameState g;
        private DefenseSetup view;
        private VillageLayout layout => view.Layout;
        private Tool tool = Tool.Select;
        private Tab tab = Tab.Village;
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
        private Vector2 uiOffset; // güvenli alan (çentik/kamera deliği) sol-üst köşesi, piksel

        /// <summary>Ekran pikseli (alt-sol köken) → arayüz koordinatı (ölçekli, güvenli alan içi).</summary>
        private Vector2 ScreenToGui(Vector2 screen) => new Vector2((screen.x - uiOffset.x) / uiScale, (Screen.height - screen.y - uiOffset.y) / uiScale);
        private readonly List<Rect> uiRects = new List<Rect>();
        private GridPos lastPainted = new GridPos(-1, -1);
        private bool dragBlocked;
        private float prevPinch = -1f;
        private float secAcc, saveAcc;
        private readonly List<string> log = new List<string>();
        private readonly List<string> popups = new List<string>();
        private GridPos selCell = new GridPos(-1, -1);
        private Vector2 pressStart;
        private bool dragging;

        private static string SavePath => Path.Combine(Application.persistentDataPath, "save.txt");
        private static long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private static int DayIndex => (int)(NowUnix / 86400);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            new GameObject("VillageBootstrap").AddComponent<VillageBootstrap>();
        }

        private void Start()
        {
            if (Application.isMobilePlatform)
            {
                // Çözünürlük telefona göre: yerel en-boy oranı korunur, çok yüksek çözünürlükte (1440p+) 1080p'ye indirilir (pil/ısı, 60 FPS hedefi)
                Application.targetFrameRate = 60;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                var full = Screen.currentResolution;
                int fw = Mathf.Max(full.width, full.height), fh = Mathf.Min(full.width, full.height); // yatay oyun
                if (fh > 1080) Screen.SetResolution(Mathf.RoundToInt(fw * 1080f / fh), 1080, FullScreenMode.FullScreenWindow);
            }
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = tmp.GetComponent<MeshRenderer>().sharedMaterial;
            Destroy(tmp);

            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.15f, 0.20f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            SetupAtmosphere();

            LoadGame();
            view = g.MakeDefense();
            ResetCamera();
        }

        // ---------- Kayıt ----------
        private void LoadGame()
        {
            g = null;
            try
            {
                if (File.Exists(SavePath))
                {
                    g = GameSave.Load(File.ReadAllText(SavePath), ResourceConfig.CreateDefault(), out long ts);
                    if (g != null)
                    {
                        long away = NowUnix - ts;
                        if (away > 60)
                        {
                            var ev = g.Advance((int)Math.Min(away, GameState.MaxOfflineSeconds), DayIndex);
                            string msg = "Hoş geldin! " + Dur((int)Math.Min(away, GameState.MaxOfflineSeconds)) + " yoktun.";
                            foreach (var e in ev) msg += "\n• " + e;
                            popups.Add(msg);
                            FlushLevelUps();
                        }
                    }
                }
            }
            catch (Exception) { g = null; }
            if (g == null)
            {
                g = GameState.NewGame();
                g.Advance(0, DayIndex);
                popups.Add("Köyüne hoş geldin!\nKÖY DÜZENİ düğmesine bas: yapıları envanterden sürükleyip yola bağlı yerlere bırak, sonra ONAYLA. İlk iş Kereste Deposu kur. Sv2'de kule, Sv3'te kışla ve Kılıçlı açılır.");
            }
            LastSave();
        }

        private void LastSave()
        {
            try { File.WriteAllText(SavePath, GameSave.Save(g, NowUnix)); } catch (Exception) { }
        }

        private void OnApplicationPause(bool paused) { if (paused && g != null) LastSave(); }
        private void OnApplicationQuit() { if (g != null) LastSave(); }

        private void NewGameConfirmed()
        {
            g = GameState.NewGame();
            g.Advance(0, DayIndex);
            CloseBattle();
            log.Clear();
            popups.Clear();
            popups.Add("Yeni oyun başladı.");
            dirty = true;
            LastSave();
        }

        // ---------- Döngü ----------
        private void Update()
        {
            HandleInput();
            if (sim != null) StepBattle();
            else TickGame();
            if (dirty) Rebuild();
            UpdateCamera();
            UpdateVillagers();
        }

        private void TickGame()
        {
            secAcc += Time.deltaTime;
            saveAcc += Time.deltaTime;
            if (secAcc >= 1f)
            {
                int n = (int)secAcc;
                secAcc -= n;
                Step(n);
            }
            if (saveAcc >= 15f) { saveAcc = 0f; LastSave(); }
        }

        private void Step(int seconds)
        {
            var ev = g.Advance(seconds, DayIndex);
            foreach (var e in ev) AddLog(e);
            FlushLevelUps();
            if (ev.Count > 0) dirty = true;
        }

        private void AddLog(string m)
        {
            log.Add(m);
            if (log.Count > 6) log.RemoveAt(0);
            toast = m; toastUntil = Time.time + 3f;
        }

        private void FlushLevelUps()
        {
            AddLevelUpPopups(g.PendingLevelUps);
            g.PendingLevelUps.Clear();
        }

        private void AddLevelUpPopups(List<LevelUpInfo> ups)
        {
            if (ups == null || ups.Count == 0) return;
            if (ups.Count == 1) { popups.Add(LevelUpText(ups[0])); return; }
            int w = 0, s = 0, gd = 0, ir = 0;
            var unlocks = new List<string>();
            foreach (var u in ups)
            {
                w += u.Reward.Wood; s += u.Reward.Stone; gd += u.Reward.Gold; ir += u.Reward.Iron;
                foreach (var k in u.Unlocks) if (!k.StartsWith("Yol hakkı")) unlocks.Add(k);
            }
            string t = "SEVİYE " + ups[0].NewLevel + " → " + ups[ups.Count - 1].NewLevel + "!\nToplam ödül: " + new ResourceCost(w, s, gd, ir);
            t += "\nYol hakkı: " + RoadRules.RoadLimitForLevel(ups[ups.Count - 1].NewLevel);
            if (unlocks.Count > 0) t += "\nAçılan: " + string.Join(", ", unlocks);
            popups.Add(t);
        }

        private static string LevelUpText(LevelUpInfo up)
        {
            string s = "SEVİYE " + up.NewLevel + "!\nÖdül: " + up.Reward;
            if (up.Unlocks.Count > 0) s += "\nAçılan: " + string.Join(", ", up.Unlocks);
            return s;
        }

        // ---------- Kamera ----------
        private void ResetCamera()
        {
            float c = (layout.Size - 1) * 0.5f;
            target = new Vector3(c, 0f, c - VillageLayout.AttackZoneRows * 0.5f);
            zoom = layout.Size * 0.42f + 2.2f; // köy ekranı doldursun
        }

        private void UpdateCamera()
        {
            float c = (layout.Size - 1) * 0.5f;
            float lim = layout.Size * 0.5f + 2f;
            target.x = Mathf.Clamp(target.x, c - lim, c + lim);
            target.z = Mathf.Clamp(target.z, c - lim - VillageLayout.AttackZoneRows, c + lim);
            cam.orthographicSize = zoom;
            cam.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
            cam.transform.position = target - cam.transform.forward * 30f; // yakın: gölge mesafesine sığar (ortografik, boyut değişmez)
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
            // Esc / Android geri tuşu: bildirimi veya pencereyi kapat
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && sim == null)
            {
                if (popups.Count > 0) popups.RemoveAt(0);
                else if (tab != Tab.Village) { tab = Tab.Village; selCell = new GridPos(-1, -1); dirty = true; }
                else if (selCell.X >= 0) { selCell = new GridPos(-1, -1); dirty = true; }
            }
            var ptr = Pointer.current;
            if (ptr == null) return;
            Vector2 pos = ptr.position.ReadValue();
            bool down = ptr.press.wasPressedThisFrame;
            bool held = ptr.press.isPressed;
            bool released = ptr.press.wasReleasedThisFrame;
            if (down) { dragBlocked = IsOverUi(pos); dragging = false; pressStart = pos; }
            if (popups.Count > 0) return;
            if (sim != null) { HandleBattleInput(pos, down, held, pinching); return; }
            if (LayoutMode && tab == Tab.Village) { HandleLayoutInput(pos, down, held, pinching); return; }
            if (!held)
            {
                // Seç: sürüklemeden bırakılan dokunuş = seçim (basma ve bırakma aynı karede olsa da); kışla ve Akademi kendi penceresini açar
                if (tool == Tool.Select && released && !dragging && !dragBlocked && tab == Tab.Village)
                {
                    selCell = TryGetCell(pos, out var tapCell) ? tapCell : new GridPos(-1, -1);
                    if (g.Buildings.TryGetValue(selCell, out var tb))
                    {
                        if (tb.Type == BuildingType.Barracks) { tab = Tab.Barracks; scrollPos = Vector2.zero; }
                        else if (tb.Type == BuildingType.University) { tab = Tab.Research; scrollPos = Vector2.zero; }
                    }
                    dirty = true;
                }
                lastPainted = new GridPos(-1, -1);
                return;
            }
            if (dragBlocked || pinching) return;
            if (tab != Tab.Village) return;

            if (tool == Tool.Pan || tool == Tool.Select)
            {
                if (down) { pressStart = pos; dragging = false; return; }
                if (!dragging && (pos - pressStart).sqrMagnitude > 144f) dragging = true;
                if (dragging || tool == Tool.Pan)
                {
                    Vector2 delta = ptr.delta.ReadValue();
                    if (RayToPlane(pos, out var cur) && RayToPlane(pos - delta, out var prev)) target += prev - cur;
                }
                return;
            }
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
            zoom = Mathf.Clamp(zoom + z, 3f, 14f);
        }

        private bool IsOverUi(Vector2 screen)
        {
            var gp = ScreenToGui(screen);
            foreach (var r in uiRects) if (r.Contains(gp)) return true;
            return false;
        }

        // ---------- Biçim yardımcıları ----------
        private static string Dur(int s)
        {
            if (s < 60) return s + " sn";
            if (s < 3600) return (s / 60) + " dk " + (s % 60) + " sn";
            return (s / 3600) + " sa " + ((s % 3600) / 60) + " dk";
        }

        private static string ResName(ResourceType t) => GameState.ResName(t);
        private void Toast(string m) { toast = m; toastUntil = Time.time + 3f; }
    }
}
