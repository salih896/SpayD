using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GameCore.Combat;
using GameCore.Economy;
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
        private enum Tool { Pan, Road, Building, Tower, Trap, Entrance, Upgrade, Erase }
        private static readonly string[] ToolNames = { "Kaydır", "Yol", "Bina", "Kule", "Tuzak", "Giriş", "Geliştir", "Sil" };
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

        // ---- Savunma içeriği (hangi hücrede hangi kule/bina/tuzak) ----
        private readonly ResourceConfig resCfg = ResourceConfig.CreateDefault();
        private Wallet wallet;
        private readonly Dictionary<GridPos, TowerSpec> towerSpecs = new Dictionary<GridPos, TowerSpec>();
        private readonly Dictionary<GridPos, BuildingSpec> buildingSpecs = new Dictionary<GridPos, BuildingSpec>();
        private readonly Dictionary<GridPos, TrapType> traps = new Dictionary<GridPos, TrapType>();
        private readonly Dictionary<GridPos, TowerWear> wears = new Dictionary<GridPos, TowerWear>();
        private TowerType selTower = TowerType.Archer;
        private BuildingType selBuilding = BuildingType.LumberDepot;
        private TrapType selTrap = TrapType.Spikes;

        // ---- Savaş (test saldırısı) ----
        private BattleSim sim;
        private bool battleOver;
        private BattleResult lastResult;
        private SoldierType selSoldier = SoldierType.Swordsman;
        private float tickAcc, deployTimer;
        private int speed = 1, lastAlive = -1, battleCount;
        private Transform dynRoot;
        private readonly List<Transform> soldierViews = new List<Transform>();
        private readonly List<Transform> barViews = new List<Transform>();
        private const int TestStockPerType = 12;

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
            wallet = new Wallet(resCfg);
            TopUp();
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
            if (sim != null) StepBattle();
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
            if (sim != null) { HandleBattleInput(pos, down, held, pinching); return; }
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
                    if (!RoadRules.CanUpgrade(roadResearchDone, out err)) break;
                    ok = layout.TryUpgradeRoad(cell, out err);
                    break;
                case Tool.Building:
                {
                    var bd = ContentCatalog.Building(selBuilding);
                    if (playerLevel < bd.UnlockLevel) { err = bd.Name + " Sv " + bd.UnlockLevel + "'da açılır"; break; }
                    ok = layout.TryPlace(cell, CellKind.Building, out err);
                    if (ok) buildingSpecs[cell] = new BuildingSpec(selBuilding, 1);
                    break;
                }
                case Tool.Tower:
                {
                    var td = ContentCatalog.Tower(selTower);
                    if (playerLevel < td.UnlockLevel) { err = td.Name + " Sv " + td.UnlockLevel + "'da açılır"; break; }
                    ok = layout.TryPlace(cell, CellKind.Tower, out err);
                    if (ok) { towerSpecs[cell] = new TowerSpec(selTower, 1, 0); wears[cell] = new TowerWear(); }
                    break;
                }
                case Tool.Trap:
                    if (layout.Get(cell) != CellKind.Road) { err = "Tuzak yola konur"; break; }
                    if (traps.TryGetValue(cell, out var cur) && cur == selTrap) traps.Remove(cell); else traps[cell] = selTrap;
                    ok = true;
                    break;
                case Tool.Entrance: ok = layout.ToggleEntrance(cell, out err); break;
                case Tool.Erase:
                    ok = layout.Remove(cell);
                    if (ok) { towerSpecs.Remove(cell); buildingSpecs.Remove(cell); traps.Remove(cell); wears.Remove(cell); }
                    break;
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
                            if (traps.TryGetValue(p, out var tr)) Cube(p, new Vector3(0.3f, 0.05f, 0.3f), 0.13f, TrapColor(tr));
                            break;
                        case CellKind.Building:
                            if (IsDestroyed(p)) { Cube(p, new Vector3(0.6f, 0.15f, 0.6f), 0.08f, new Color(0.2f, 0.18f, 0.16f)); break; }
                            Cube(p, new Vector3(0.7f, 0.7f, 0.7f), 0.35f, BuildingColor(buildingSpecs.TryGetValue(p, out var bs) ? bs.Type : BuildingType.VillagerHouse));
                            break;
                        case CellKind.Tower:
                            if (IsDestroyed(p)) { Cube(p, new Vector3(0.6f, 0.15f, 0.6f), 0.08f, new Color(0.2f, 0.18f, 0.16f)); break; }
                            Cube(p, new Vector3(0.45f, 1.3f, 0.45f), 0.65f, new Color(0.55f, 0.58f, 0.65f));
                            Cube(p, new Vector3(0.65f, 0.2f, 0.65f), 1.4f, TowerColor(towerSpecs.TryGetValue(p, out var ts) ? ts.Type : TowerType.Archer));
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
        // ---------- Renkler ----------
        private static Color BuildingColor(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.Forest: return new Color(0.15f, 0.45f, 0.18f);
                case BuildingType.LumberDepot: return new Color(0.55f, 0.35f, 0.15f);
                case BuildingType.StoneQuarry: return new Color(0.5f, 0.5f, 0.52f);
                case BuildingType.StoneDepot: return new Color(0.7f, 0.7f, 0.72f);
                case BuildingType.GoldMine: return new Color(0.85f, 0.7f, 0.1f);
                case BuildingType.Bank: return new Color(1f, 0.85f, 0.2f);
                case BuildingType.IronMine: return new Color(0.35f, 0.38f, 0.45f);
                case BuildingType.IronDepot: return new Color(0.55f, 0.6f, 0.7f);
                case BuildingType.Barracks: return new Color(0.7f, 0.2f, 0.2f);
                case BuildingType.University: return new Color(0.25f, 0.4f, 0.8f);
                default: return new Color(0.8f, 0.7f, 0.5f);
            }
        }

        private static Color TowerColor(TowerType t)
        {
            switch (t)
            {
                case TowerType.Archer: return new Color(0.2f, 0.7f, 0.25f);
                case TowerType.Mage: return new Color(0.65f, 0.25f, 0.85f);
                case TowerType.Spear: return new Color(0.8f, 0.5f, 0.2f);
                default: return new Color(0.2f, 0.75f, 0.75f);
            }
        }

        private static Color TrapColor(TrapType t)
        {
            switch (t)
            {
                case TrapType.Spikes: return new Color(0.75f, 0.75f, 0.8f);
                case TrapType.Slippery: return new Color(0.5f, 0.8f, 1f);
                default: return new Color(1f, 0.45f, 0.1f);
            }
        }

        private static Color SoldierColor(SoldierType t)
        {
            switch (t)
            {
                case SoldierType.Swordsman: return new Color(0.75f, 0.8f, 0.95f);
                case SoldierType.Archer: return new Color(0.4f, 0.9f, 0.4f);
                case SoldierType.Mage: return new Color(0.8f, 0.4f, 1f);
                case SoldierType.Goblin: return new Color(0.3f, 0.55f, 0.15f);
                case SoldierType.Cavalry: return new Color(0.6f, 0.35f, 0.15f);
                case SoldierType.Spearman: return new Color(1f, 0.65f, 0.2f);
                default: return new Color(1f, 0.6f, 0.85f);
            }
        }

        private static string TrapName(TrapType t)
        {
            switch (t) { case TrapType.Spikes: return "Çivili"; case TrapType.Slippery: return "Kaygan"; default: return "Ateşli"; }
        }

        private static string ResName(ResourceType t)
        {
            switch (t) { case ResourceType.Wood: return "Odun"; case ResourceType.Stone: return "Taş"; case ResourceType.Gold: return "Altın"; default: return "Demir"; }
        }

        private void Toast(string m) { toast = m; toastUntil = Time.time + 2.5f; }

        private bool IsDestroyed(GridPos p)
        {
            if (sim == null) return false;
            foreach (var st in sim.Structures) if (st.Pos.Equals(p)) return !st.Alive;
            return false;
        }

        // ---------- Savunma yardımcıları ----------
        private void TopUp()
        {
            foreach (var t in ResourceRules.Unlocked(resCfg, playerLevel)) wallet.TryAdd(t, 1000, playerLevel, out _, out _);
        }

        private string ResourceText()
        {
            var parts = new List<string>();
            foreach (var t in ResourceRules.Unlocked(resCfg, playerLevel)) parts.Add(ResName(t) + " " + wallet.Get(t));
            return string.Join("  |  ", parts);
        }

        private void RepairAll()
        {
            int wood = 0, stone = 0, gold = 0, iron = 0, seconds = 0, n = 0;
            foreach (var kv in wears)
            {
                if (kv.Value.PermanentDamagePct <= 0) continue;
                int lvl = towerSpecs.TryGetValue(kv.Key, out var sp) ? sp.Level : 1;
                var c = kv.Value.RepairCost(lvl, resCfg, playerLevel);
                wood += c.Wood; stone += c.Stone; gold += c.Gold; iron += c.Iron;
                seconds += kv.Value.RepairSeconds(lvl);
                n++;
            }
            if (n == 0) { Toast("Tamir edilecek kule yok"); return; }
            if (!wallet.TrySpend(new ResourceCost(wood, stone, gold, iron), playerLevel, SpendArea.Repair, out string err)) { Toast(err); return; }
            foreach (var kv in wears)
            {
                kv.Value.RepairAll();
                if (towerSpecs.TryGetValue(kv.Key, out var sp2)) sp2.PermanentDamagePct = 0;
            }
            Toast("Tamir tamam (test: anında). Gerçek süre: " + (seconds / 60) + " dk");
        }

        /// <summary>Seviyeye uygun, geçerli örnek savunma kurar (yol sayısı TAM yol hakkı kadar).</summary>
        private void BuildSample()
        {
            int limit = RoadRules.RoadLimitForLevel(playerLevel);
            layout = new VillageLayout(11, limit, RoadRules.MaxEntrances);
            towerSpecs.Clear(); buildingSpecs.Clear(); traps.Clear(); wears.Clear();
            string e;
            layout.ToggleEntrance(new GridPos(5, 0), out e);
            int roads = 1;
            for (int y = 1; y <= 10 && roads < limit; y++) { layout.TryPlace(new GridPos(5, y), CellKind.Road, out e); roads++; }
            for (int x = 4; x >= 0 && roads < limit; x--) { layout.TryPlace(new GridPos(x, 10), CellKind.Road, out e); roads++; }
            for (int x = 6; x <= 10 && roads < limit; x++) { layout.TryPlace(new GridPos(x, 10), CellKind.Road, out e); roads++; }
            for (int y = 9; y >= 1 && roads < limit; y--) { layout.TryPlace(new GridPos(0, y), CellKind.Road, out e); roads++; }
            for (int y = 9; y >= 1 && roads < limit; y--) { layout.TryPlace(new GridPos(10, y), CellKind.Road, out e); roads++; }

            AddSampleBuilding(6, 3, BuildingType.LumberDepot, 1);
            AddSampleBuilding(4, 5, BuildingType.Forest, 1);
            AddSampleBuilding(4, 3, playerLevel >= 3 ? BuildingType.Barracks : BuildingType.VillagerHouse, 1);
            AddSampleBuilding(6, 7, BuildingType.StoneDepot, 5);
            AddSampleBuilding(4, 1, BuildingType.University, 5);
            AddSampleBuilding(6, 9, BuildingType.Bank, 15);
            AddSampleBuilding(4, 7, BuildingType.IronDepot, 25);

            AddSampleTower(4, 2, TowerType.Archer);
            AddSampleTower(6, 5, playerLevel >= 4 ? TowerType.Spear : TowerType.Archer);
            AddSampleTower(6, 1, playerLevel >= 6 ? TowerType.Mage : TowerType.Archer);
            AddSampleTower(4, 6, playerLevel >= 8 ? TowerType.Elf : TowerType.Archer);
            AddSampleTower(6, 8, TowerType.Archer);

            if (limit >= 8)
            {
                traps[new GridPos(5, 2)] = TrapType.Spikes;
                traps[new GridPos(5, 4)] = TrapType.Slippery;
                traps[new GridPos(5, 6)] = TrapType.Fire;
            }
            dirty = true;
            Toast("Örnek köy kuruldu (Sv " + playerLevel + ", " + limit + " yol)");
        }

        private void AddSampleBuilding(int x, int y, BuildingType t, int minLevel)
        {
            if (playerLevel < minLevel || ContentCatalog.Building(t).UnlockLevel > playerLevel) return;
            var p = new GridPos(x, y);
            if (layout.TryPlace(p, CellKind.Building, out _)) buildingSpecs[p] = new BuildingSpec(t, 1);
        }

        private void AddSampleTower(int x, int y, TowerType t)
        {
            if (ContentCatalog.Tower(t).UnlockLevel > playerLevel) return;
            var p = new GridPos(x, y);
            if (layout.TryPlace(p, CellKind.Tower, out _)) { towerSpecs[p] = new TowerSpec(t, 1, 0); wears[p] = new TowerWear(); }
        }

        // ---------- Savaş ----------
        private void StartBattle()
        {
            if (!valid) { Toast("Önce geçerli bir savunma kur: " + validationError); return; }
            var setup = new DefenseSetup(layout);
            foreach (var kv in towerSpecs)
            {
                int perm = wears.TryGetValue(kv.Key, out var wr) ? wr.PermanentDamagePct : 0;
                setup.Towers[kv.Key] = new TowerSpec(kv.Value.Type, kv.Value.Level, perm);
            }
            foreach (var kv in buildingSpecs) setup.Buildings[kv.Key] = kv.Value;
            foreach (var kv in traps) setup.Traps[kv.Key] = kv.Value;
            var stock = new Dictionary<SoldierType, int>();
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++) stock[(SoldierType)i] = TestStockPerType;
            battleCount++;
            sim = new BattleSim(setup, stock, null, 1000 + battleCount);
            battleOver = false; lastResult = null; lastAlive = -1; tickAcc = 0f;
            DestroyDyn();
            ResetCamera();
            dirty = true;
        }

        private void StepBattle()
        {
            if (!sim.Finished)
            {
                tickAcc += Time.deltaTime * speed;
                int n = 0;
                while (tickAcc >= 0.1f && n < 60 && !sim.Finished) { sim.Step(); tickAcc -= 0.1f; n++; }
                if (n >= 60) tickAcc = 0f;
            }
            int alive = 0;
            foreach (var st in sim.Structures) if (st.Alive) alive++;
            if (alive != lastAlive) { lastAlive = alive; dirty = true; }
            if (sim.Finished && !battleOver) FinishBattle();
            UpdateBattleView();
        }

        private void FinishBattle()
        {
            battleOver = true;
            var have = new ResourceCost(wallet.Get(ResourceType.Wood), wallet.Get(ResourceType.Stone), wallet.Get(ResourceType.Gold), wallet.Get(ResourceType.Iron));
            lastResult = sim.Result(have, resCfg, playerLevel);
            wallet.Plunder(lastResult.Loot); // savunmacı yağmalanan kaynağı kaybeder
            foreach (var rec in lastResult.TowerDamages)
            {
                if (!wears.TryGetValue(rec.Pos, out var wr)) { wr = new TowerWear(); wears[rec.Pos] = wr; }
                wr.ApplyBattle(rec.DamagePct);
                if (towerSpecs.TryGetValue(rec.Pos, out var sp)) sp.PermanentDamagePct = wr.PermanentDamagePct;
            }
        }

        private void CloseBattle()
        {
            sim = null; battleOver = false; lastResult = null; lastAlive = -1;
            DestroyDyn();
            dirty = true;
        }

        private void DestroyDyn()
        {
            if (dynRoot != null) Destroy(dynRoot.gameObject);
            dynRoot = null;
            soldierViews.Clear(); barViews.Clear();
        }

        private Transform MakeDynCube(Color c, Vector3 scale)
        {
            if (dynRoot == null) dynRoot = new GameObject("Battle").transform;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(dynRoot, false);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(c);
            return go.transform;
        }

        private void UpdateBattleView()
        {
            while (soldierViews.Count < sim.Soldiers.Count)
                soldierViews.Add(MakeDynCube(SoldierColor(sim.Soldiers[soldierViews.Count].Type), new Vector3(0.28f, 0.28f, 0.28f)));
            for (int i = 0; i < sim.Soldiers.Count; i++)
            {
                var s = sim.Soldiers[i];
                var t = soldierViews[i];
                if (t.gameObject.activeSelf != s.Alive) t.gameObject.SetActive(s.Alive);
                t.position = new Vector3(s.X / 1000f, s.Flying ? 0.9f : 0.25f, s.Y / 1000f);
            }
            while (barViews.Count < sim.Structures.Count)
                barViews.Add(MakeDynCube(new Color(0.2f, 0.9f, 0.2f), new Vector3(0.8f, 0.08f, 0.08f)));
            for (int i = 0; i < sim.Structures.Count; i++)
            {
                var st = sim.Structures[i];
                var t = barViews[i];
                if (t.gameObject.activeSelf != st.Alive) t.gameObject.SetActive(st.Alive);
                float f = st.MaxHp > 0 ? Mathf.Clamp01((float)st.Hp / st.MaxHp) : 0f;
                t.localScale = new Vector3(Mathf.Max(0.02f, 0.8f * f), 0.08f, 0.08f);
                t.position = new Vector3(st.Pos.X, st.Kind == StructKind.Tower ? 1.8f : 1.0f, st.Pos.Y);
            }
        }

        private void HandleBattleInput(Vector2 pos, bool down, bool held, bool pinching)
        {
            if (battleOver || sim.Finished || dragBlocked || pinching || (!held && !down)) { deployTimer = 0f; return; }
            deployTimer -= Time.deltaTime;
            if (!down && deployTimer > 0f) return;
            if (!TryGetZoneCell(pos, out int x, out int row)) return;
            if (!sim.Deploy(selSoldier, x, row, out string err)) Toast(err);
            deployTimer = 0.12f;
        }

        private bool TryGetZoneCell(Vector2 screen, out int x, out int row)
        {
            x = -1; row = -1;
            if (!RayToPlane(screen, out var hit)) return false;
            x = Mathf.RoundToInt(hit.x);
            row = -Mathf.RoundToInt(hit.z);
            return x >= 0 && x < layout.Size && row >= 1 && row <= VillageLayout.AttackZoneRows;
        }

        private int LiveDamagePct()
        {
            var pcts = new List<int>();
            foreach (var st in sim.Structures) pcts.Add(st.Alive ? 100 - st.Hp * 100 / st.MaxHp : 100);
            return CombatMath.OverallDamagePct(pcts);
        }

        // ---------- Arayüz (OnGUI: dokunmatik düğmeler de çalışır) ----------
        private bool Btn(Rect r, string text)
        {
            uiRects.Add(r);
            return GUI.Button(r, text);
        }

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
            if (sim != null) DrawBattleGui(w, h); else DrawDefenseGui(w, h);
        }

        private void DrawDefenseGui(float w, float h)
        {
            float bw = Mathf.Min(80f, (w - 8f) / ToolNames.Length - 2f);
            for (int i = 0; i < ToolNames.Length; i++)
            {
                GUI.color = (int)tool == i ? Color.yellow : Color.white;
                if (Btn(new Rect(4 + i * (bw + 2), 4, bw, 36), ToolNames[i])) tool = (Tool)i;
            }
            GUI.color = Color.white;

            GUI.Box(new Rect(68, 44, 70, 32), layout.Size + "x" + layout.Size); uiRects.Add(new Rect(68, 44, 70, 32));
            if (Btn(new Rect(4, 44, 60, 32), "Grid -") && layout.Size > MinSize) { layout = layout.Resized(layout.Size - 1); ResetCamera(); dirty = true; }
            if (Btn(new Rect(142, 44, 60, 32), "Grid +") && layout.Size < MaxSize) { layout = layout.Resized(layout.Size + 1); ResetCamera(); dirty = true; }
            if (Btn(new Rect(206, 44, 70, 32), "Kamera")) ResetCamera();
            if (Btn(new Rect(280, 44, 70, 32), "Temizle"))
            {
                layout = new VillageLayout(layout.Size, layout.RoadLimit, layout.EntranceLimit);
                towerSpecs.Clear(); buildingSpecs.Clear(); traps.Clear(); wears.Clear();
                dirty = true;
            }

            GUI.Box(new Rect(58, 80, 70, 32), "Sv " + playerLevel); uiRects.Add(new Rect(58, 80, 70, 32));
            if (Btn(new Rect(4, 80, 50, 32), "Sv -") && playerLevel > 1) SetLevel(playerLevel - 1);
            if (Btn(new Rect(132, 80, 50, 32), "Sv +") && playerLevel < 30) SetLevel(playerLevel + 1);
            if (Btn(new Rect(186, 80, 190, 32), "Yol araştırması: " + (roadResearchDone ? "VAR" : "yok") + " (test)")) roadResearchDone = !roadResearchDone;

            if (Btn(new Rect(4, 116, 150, 32), "Kule: " + ContentCatalog.Tower(selTower).Name)) { selTower = (TowerType)(((int)selTower + 1) % 4); tool = Tool.Tower; }
            if (Btn(new Rect(158, 116, 190, 32), "Bina: " + ContentCatalog.Building(selBuilding).Name)) { selBuilding = (BuildingType)(((int)selBuilding + 1) % 11); tool = Tool.Building; }
            if (Btn(new Rect(352, 116, 130, 32), "Tuzak: " + TrapName(selTrap))) { selTrap = (TrapType)(((int)selTrap % 3) + 1); tool = Tool.Trap; }

            if (Btn(new Rect(4, 152, 100, 32), "Örnek köy")) BuildSample();
            if (Btn(new Rect(108, 152, 90, 32), "Kaynak +")) TopUp();
            if (Btn(new Rect(202, 152, 80, 32), "Tamir")) RepairAll();
            GUI.color = new Color(1f, 0.6f, 0.5f);
            if (Btn(new Rect(286, 152, 110, 32), "SALDIR")) StartBattle();
            GUI.color = Color.white;

            var rh = new Rect(4, 188, 520, 28);
            GUI.Box(rh, ResourceText()); uiRects.Add(rh);

            int damaged = 0;
            foreach (var kv in wears) if (kv.Value.PermanentDamagePct > 0) damaged++;
            var rs = new Rect(4, h - 44, w - 8, 40);
            string msg = valid ? "GEÇERLİ - savunma kaydedilebilir" : "GEÇERSİZ - " + validationError;
            if (Time.time < toastUntil) msg = toast;
            msg += "    [Sv " + playerLevel + " | Yol " + layout.RoadCount + "/" + layout.RoadLimit + " | Giriş " + layout.EntranceCount + (damaged > 0 ? " | Hasarlı kule " + damaged : "") + "]";
            GUI.color = valid ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.8f, 0.5f);
            GUI.Box(rs, msg);
            GUI.color = Color.white;
            uiRects.Add(rs);
        }

        private void DrawBattleGui(float w, float h)
        {
            int n = ContentCatalog.SoldierTypeCount;
            float bw = Mathf.Min(120f, (w - 8f) / n - 2f);
            for (int i = 0; i < n; i++)
            {
                var t = (SoldierType)i;
                GUI.color = t == selSoldier ? Color.yellow : (sim.StockOf(t) > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f));
                if (Btn(new Rect(4 + i * (bw + 2), 4, bw, 36), ContentCatalog.Soldier(t).Name + " " + sim.StockOf(t))) selSoldier = t;
            }
            GUI.color = Color.white;
            if (Btn(new Rect(4, 44, 110, 32), "Geri Çek") && !sim.Finished) sim.Retreat();
            if (Btn(new Rect(118, 44, 90, 32), "Hız " + speed + "x")) speed = speed == 1 ? 3 : (speed == 3 ? 6 : 1);

            int aliveSoldiers = 0;
            foreach (var s in sim.Soldiers) if (s.Alive) aliveSoldiers++;
            var rs = new Rect(4, h - 44, w - 8, 40);
            string msg = Time.time < toastUntil ? toast :
                "SAVAŞ  " + (sim.Tick / 10) + " sn  |  Hasar %" + LiveDamagePct() + " (zafer %51)  |  Sahada " + aliveSoldiers + "  [TEST: her türden " + TestStockPerType + " asker; kırmızı alana dokun]";
            GUI.Box(rs, msg); uiRects.Add(rs);

            if (battleOver && lastResult != null) DrawResult(w, h);
        }

        private void DrawResult(float w, float h)
        {
            var r = lastResult;
            var box = new Rect(w * 0.5f - 190f, h * 0.5f - 130f, 380f, 270f);
            uiRects.Add(box);
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.Box(box, "");
            GUI.color = Color.white;
            float y = box.y + 8f;
            GUI.Label(new Rect(box.x + 12f, y, 360f, 26f), r.Victory ? "ZAFER  -  yıldız " + r.Stars + "/3" : "YENİLGİ  -  yıldız 0/3"); y += 28f;
            GUI.Label(new Rect(box.x + 12f, y, 360f, 26f), "Genel hasar: %" + r.OverallDamagePct + "   XP: +" + r.Xp); y += 26f;
            GUI.Label(new Rect(box.x + 12f, y, 360f, 26f), "Yağma: " + r.Loot); y += 26f;
            GUI.Label(new Rect(box.x + 12f, y, 360f, 26f), "Ölü " + BattleResult.Total(r.LostDead) + "  Yaralı " + BattleResult.Total(r.LostInjured) + "  Feda " + BattleResult.Total(r.LostSacrificed)); y += 26f;
            GUI.Label(new Rect(box.x + 12f, y, 360f, 26f), "Dönen asker: " + BattleResult.Total(r.Returned)); y += 26f;
            int worn = 0; foreach (var kv in wears) if (kv.Value.PermanentDamagePct > 0) worn++;
            GUI.Label(new Rect(box.x + 12f, y, 360f, 26f), "Kalıcı hasarlı kule: " + worn + "  (Tamir ile onar)"); y += 34f;
            if (GUI.Button(new Rect(box.x + 120f, y, 140f, 36f), "Tamam")) CloseBattle();
        }
    }
}
