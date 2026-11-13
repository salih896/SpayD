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
        private enum Tool { Pan, Select, Road, Building, Tower, Trap, Entrance, Erase }
        private static readonly string[] ToolNames = { "Kaydır", "Seç", "Yol", "Bina", "Kule", "Tuzak", "Giriş", "Sil" };
        private enum Tab { Village, Barracks, Research, Quests, Attack, Menu }

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
        private readonly List<Rect> uiRects = new List<Rect>();
        private GridPos lastPainted = new GridPos(-1, -1);
        private bool dragBlocked;
        private float prevPinch = -1f;
        private float secAcc, saveAcc;
        private readonly List<string> log = new List<string>();
        private readonly List<string> popups = new List<string>();
        private TowerType selTower = TowerType.Archer;
        private BuildingType selBuilding = BuildingType.Forest;
        private TrapType selTrap = TrapType.Spikes;
        private GridPos selCell = new GridPos(-1, -1);

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
                popups.Add("Köyüne hoş geldin!\nÖnce Köy sekmesinde bina kur (Orman, Kereste Deposu, Köylü Evi). Her yapı XP verir; Sv2'de kule, Sv3'te kışla ve asker açılır.");
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
            zoom = layout.Size * 0.75f + 4f;
        }

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
            if (popups.Count > 0) return;
            if (sim != null) { HandleBattleInput(pos, down, held, pinching); return; }
            if (!held && !down) { lastPainted = new GridPos(-1, -1); return; }
            if (dragBlocked || pinching) return;
            if (tab != Tab.Village) return;

            if (tool == Tool.Pan)
            {
                if (down) return;
                Vector2 delta = ptr.delta.ReadValue();
                if (RayToPlane(pos, out var cur) && RayToPlane(pos - delta, out var prev)) target += prev - cur;
                return;
            }
            if (!TryGetCell(pos, out var cell) || cell.Equals(lastPainted)) return;
            // Pahalı yerleştirmeler yalnızca dokunma anında (sürüklemede toplu yerleştirme olmasın)
            if ((tool == Tool.Building || tool == Tool.Tower || tool == Tool.Select) && !down) return;
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
                case Tool.Select: selCell = cell; dirty = true; return;
                case Tool.Road: ok = g.TryPlaceRoad(cell, out err); break;
                case Tool.Building: ok = g.TryPlaceBuilding(cell, selBuilding, out err); break;
                case Tool.Tower: ok = g.TryPlaceTower(cell, selTower, out err); break;
                case Tool.Trap: ok = g.TryToggleTrap(cell, selTrap, out err); break;
                case Tool.Entrance: ok = g.TryToggleEntrance(cell, out err); break;
                case Tool.Erase: ok = g.TryRemove(cell, out err); if (ok && selCell.Equals(cell)) selCell = new GridPos(-1, -1); break;
            }
            if (ok) { dirty = true; FlushLevelUps(); }
            else if (!string.IsNullOrEmpty(err)) { toast = err; toastUntil = Time.time + 2.5f; }
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
