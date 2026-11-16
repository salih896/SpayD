using System.Collections.Generic;
using UnityEngine;
using GameCore.Combat;
using GameCore.Game;

namespace GameClient
{
    public sealed partial class VillageBootstrap
    {
        private BattleSim sim;
        private TargetInfo curTarget;
        private BattleResult lastResult;
        private AttackOutcome lastOutcome;
        private bool battleOver;
        private SoldierType selSoldier = SoldierType.Swordsman;
        private float tickAcc, deployTimer;
        private int speed = 1, lastAlive = -1, battleSeed = 1;
        private Transform dynRoot;
        private readonly List<Transform> soldierViews = new List<Transform>();
        private readonly List<Transform> barViews = new List<Transform>();
        private readonly List<Transform> soldierBars = new List<Transform>(), soldierBarFills = new List<Transform>();
        private readonly List<int> soldierBarColor = new List<int>();
        private readonly List<Vector3> soldierOff = new List<Vector3>(); // şerit kayması (yumuşatılmış)
        private readonly List<Vector2> simPrevPos = new List<Vector2>();   // bir önceki savaş adımındaki konum (ara konum için)
        private readonly List<ISoldierAnim> soldierRigs = new List<ISoldierAnim>();
        private readonly List<float> soldierVel = new List<float>();
        private readonly List<float> soldierBarH = new List<float>(); // can çubuğu yüksekliği (model boyu + pay)
        private readonly List<int> soldierPrevAttacks = new List<int>();
        private readonly List<Vector3> soldierDir = new List<Vector3>();   // (eski) yürüme yönü
        private readonly List<Vector3> soldierSep = new List<Vector3>();   // yakın askerlerden ayrışma (yumuşak, birikimli)
        private readonly List<Vector3> soldierDisp = new List<Vector3>();  // bu karedeki çizim konumu (ayrışma hesabı için)

        private void StartBattle(TargetInfo t)
        {
            string err;
            var s = g.StartAttack(t.Defense, t.Seed * 31 + battleSeed++, out err);
            if (s == null) { Toast(err); return; }
            sim = s; curTarget = t;
            battleOver = false; lastResult = null; lastOutcome = null; lastAlive = -1; tickAcc = 0f;
            // İlk bulunan stoklu türü seç
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
                if (sim.StockOf((SoldierType)i) > 0) { selSoldier = (SoldierType)i; break; }
            DestroyDyn();
            tab = Tab.Village;
            dirty = true;
            ResetCamera();
        }

        /// <summary>
        /// Test: kendi kurduğun köye saldır (Ayarlar). Gerçek ordu harcanmaz; açık asker türlerinden ordu kapasitesi kadar
        /// (kışla yoksa 40 yer) test ordusu, mevcut asker seviyeleri ve araştırmalarla. Yağma/XP/görev ilerlemesi yok.
        /// </summary>
        /// <summary>Test ordusu (Ayarlar → Kendi köyüne saldır): oyuncunun seçtiği türler/adetler.</summary>
        private readonly Dictionary<SoldierType, int> testArmy = new Dictionary<SoldierType, int>();
        public const int TestArmyMaxSoldiers = 300; // performans hedefi (≤300 asker)

        private int TestArmyCount() { int n = 0; foreach (var kv in testArmy) n += kv.Value; return n; }

        /// <summary>Kapasite kadar (kışla yoksa 40 yer) açık türlerden sırayla doldurur.</summary>
        private void FillTestArmy()
        {
            testArmy.Clear();
            var types = new List<SoldierType>();
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
                if (ContentCatalog.Soldier((SoldierType)i).UnlockLevel <= g.Level) types.Add((SoldierType)i);
            if (types.Count == 0) types.Add(SoldierType.Swordsman);
            int housing = g.ArmyCapacity > 0 ? g.ArmyCapacity : 40;
            bool any = true;
            while (any)
            {
                any = false;
                foreach (var t in types)
                {
                    if (GameRules.Housing(t) > housing) continue;
                    testArmy[t] = (testArmy.TryGetValue(t, out var n) ? n : 0) + 1;
                    housing -= GameRules.Housing(t);
                    any = true;
                }
            }
        }

        private void StartTestBattle()
        {
            var stock = new Dictionary<SoldierType, int>();
            foreach (var kv in testArmy) if (kv.Value > 0) stock[kv.Key] = kv.Value;
            if (stock.Count == 0) { Toast("Önce test ordusuna asker ekle"); return; }
            var levels = new Dictionary<SoldierType, int>();
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++) levels[(SoldierType)i] = g.SoldierLevel((SoldierType)i);
            int seed = 7000 + battleSeed++;
            var target = new TargetInfo { Name = "Kendi köyün (test)", Level = g.Level, Defense = g.MakeDefense(), Pool = default, Seed = seed };
            sim = new BattleSim(target.Defense, stock, levels, seed) { AttackerMods = g.Research.Mods() };
            curTarget = target;
            testBattle = true;
            battleOver = false; lastResult = null; lastOutcome = null; lastAlive = -1; tickAcc = 0f;
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
                if (sim.StockOf((SoldierType)i) > 0) { selSoldier = (SoldierType)i; break; }
            DestroyDyn();
            tab = Tab.Village;
            dirty = true;
            ResetCamera();
            Toast("TEST: kendi köyüne saldırıyorsun — ordu harcanmaz, ödül yok");
        }

        private bool testBattle;

        /// <summary>
        /// Bir simülasyon adımının gerçek süresi (kullanıcı kararı 15.11.2026: savaş %40 yavaş). Simülasyon 10 adım = "1 savaş saniyesi"
        /// olarak aynen kalır (hasar dengesi ve sunucu sonucu değişmez); ekranda 10 adım 1,67 sn sürer. Savaş süresi 180 savaş sn = 5 dk.
        /// </summary>
        public const float SimTickSeconds = 1f / 6f;
        public static float RealSeconds(int ticks) => ticks * SimTickSeconds;

        private void StepBattle()
        {
            if (!sim.Finished)
            {
                tickAcc += Time.deltaTime * speed;
                int n = 0;
                while (tickAcc >= SimTickSeconds && n < 60 && !sim.Finished) { SnapshotSimPositions(); sim.Step(); tickAcc -= SimTickSeconds; n++; }
                if (n >= 60) tickAcc = 0f;
            }
            int alive = 0;
            foreach (var st in sim.Structures) if (st.Alive) alive++;
            if (alive != lastAlive) { lastAlive = alive; dirty = true; }
            if (sim.Finished && !battleOver) FinishBattle();
            UpdateBattleView();
        }

        /// <summary>Adım öncesi konumları sakla: ekranda iki adım arası doğrusal ara konum çizilir (askerler "nokta nokta" değil akıcı ilerler).</summary>
        private void SnapshotSimPositions()
        {
            for (int i = 0; i < sim.Soldiers.Count; i++)
            {
                var p = new Vector2(sim.Soldiers[i].X, sim.Soldiers[i].Y);
                if (i < simPrevPos.Count) simPrevPos[i] = p; else simPrevPos.Add(p);
            }
        }

        private void FinishBattle()
        {
            battleOver = true;
            lastResult = sim.Result(curTarget.Pool, g.Config, curTarget.Level);
            if (testBattle) { lastOutcome = null; return; } // test: ordu/yağma/XP işlenmez
            lastOutcome = g.FinishAttack(sim, lastResult, curTarget.Level);
            AddLevelUpPopups(lastOutcome.LevelUps);
            LastSave();
        }

        private void CloseBattle()
        {
            sim = null; battleOver = false; lastResult = null; lastOutcome = null; lastAlive = -1; testBattle = false;
            DestroyDyn();
            dirty = true;
            if (g != null) { view = g.MakeDefense(); ResetCamera(); }
        }

        private void DestroyDyn()
        {
            if (dynRoot != null) Destroy(dynRoot.gameObject);
            dynRoot = null;
            soldierViews.Clear(); soldierBars.Clear(); soldierBarFills.Clear(); soldierBarColor.Clear(); soldierOff.Clear(); simPrevPos.Clear(); soldierRigs.Clear(); soldierBarH.Clear(); soldierVel.Clear(); fxs.Clear(); soldierPrevAttacks.Clear(); soldierDir.Clear(); soldierSep.Clear(); soldierDisp.Clear(); barViews.Clear(); barFills.Clear(); soldierPrev.Clear(); shots.Clear();
            towerPrevCd = null; structPrevAlive = null;
        }

        private sealed class Shot { public Transform T; public Vector3 A, B; public float T0, Dur, Arc = 0.35f, HitSize; public Color HitColor; public bool Spin; }
        private sealed class Fx { public Transform T; public float T0, Dur, Size; }
        private readonly List<Fx> fxs = new List<Fx>();

        /// <summary>
        /// Asker vuruşunun görseli (yalnızca görsel; hasar simülasyonda anında işlenmiştir).
        /// Menzilliler mermi atar (okçu ok, büyücü mor büyü, peri pembe büyü, mancınık taş, ejderha ateş topu);
        /// yakın dövüşçüler vurduğu yerde kıvılcım çıkarır. Darbe anı animasyonla eşlenir (impact sn sonra).
        /// </summary>
        private void OnSoldierAttack(SimSoldier s, Vector3 from, float impact)
        {
            if (s.TargetId < 0 || s.TargetId >= sim.Structures.Count) return;
            var tg = sim.Structures[s.TargetId];
            bool tower = tg.Kind == StructKind.Tower;
            var center = new Vector3(tg.Pos.X, tower ? 0.55f : 0.35f, tg.Pos.Y);
            float spd = Mathf.Max(1f, speed);
            float delay = impact; // animasyonun darbe anı (gerçek sn; hız çarpanı animasyona zaten yansıdı)
            if (IsRangedView(s.Type))
            {
                Transform t; float cellsPerSec, arc; Color hit; float hitSize; bool spin = false;
                float h = s.Flying ? 0.95f : 0.42f;
                switch (s.Type)
                {
                    case SoldierType.Archer: t = VillageArt.Arrow(DynRoot()); cellsPerSec = 4.5f; arc = 0.35f; hit = new Color(1f, 0.85f, 0.5f); hitSize = 0.12f; h = 0.45f; break;
                    case SoldierType.Mage: t = VillageArt.MagicOrb(DynRoot(), new Color(0.75f, 0.35f, 1f), 0.2f); cellsPerSec = 3f; arc = 0.12f; hit = new Color(0.8f, 0.45f, 1f); hitSize = 0.4f; h = 0.55f; break;
                    case SoldierType.Fairy: t = VillageArt.MagicOrb(DynRoot(), new Color(1f, 0.5f, 0.85f), 0.14f); cellsPerSec = 3.2f; arc = 0.05f; hit = new Color(1f, 0.6f, 0.9f); hitSize = 0.25f; break;
                    case SoldierType.DragonRider: t = VillageArt.MagicOrb(DynRoot(), new Color(1f, 0.5f, 0.12f), 0.24f); cellsPerSec = 3f; arc = 0.05f; hit = new Color(1f, 0.55f, 0.15f); hitSize = 0.5f; break;
                    default: t = VillageArt.Projectile(DynRoot(), TowerType.Cannon); cellsPerSec = 3.5f; arc = 0.9f; hit = new Color(0.65f, 0.55f, 0.45f); hitSize = 0.45f; spin = true; h = 0.4f; break; // mancınık taşı
                }
                var a = from + new Vector3(0f, h, 0f);
                float dist = Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(center.x, 0f, center.z));
                shots.Add(new Shot { T = t, A = a, B = center, T0 = Time.time + delay, Dur = Mathf.Max(0.32f, dist / cellsPerSec) / spd, Arc = arc, HitColor = hit, HitSize = hitSize, Spin = spin });
                t.position = a;
                t.gameObject.SetActive(false);
            }
            else
            {
                // vurulan nokta: yapının askere bakan yüzü
                var dir = new Vector3(from.x - center.x, 0f, from.z - center.z);
                var p = center + (dir.sqrMagnitude > 0.0001f ? dir.normalized * (tower ? 0.28f : 0.36f) : Vector3.zero);
                p.y = s.Type == SoldierType.Giant ? 0.5f : 0.32f;
                var col = s.Type == SoldierType.Giant ? new Color(1f, 0.75f, 0.35f) : new Color(1f, 0.95f, 0.65f);
                SpawnSpark(p, col, s.Type == SoldierType.Giant ? 0.38f : 0.2f, delay);
            }
        }

        private static bool IsRangedView(SoldierType t) =>
            t == SoldierType.Archer || t == SoldierType.Mage || t == SoldierType.Fairy || t == SoldierType.Catapult || t == SoldierType.DragonRider;

        private void SpawnSpark(Vector3 pos, Color c, float size, float delay = 0f)
        {
            var g = VillageArt.Group(DynRoot(), "spark", pos);
            VillageArt.Part(g, VillageArt.Shape.Ball, Vector3.zero, Vector3.one, VillageArt.Mat(c, 0.9f, 2.2f));
            g.localScale = Vector3.one * size * 0.4f;
            g.gameObject.SetActive(delay <= 0f);
            fxs.Add(new Fx { T = g, T0 = Time.time + delay, Dur = 0.28f, Size = size });
        }
        private readonly List<Shot> shots = new List<Shot>();
        private readonly List<Vector3> soldierPrev = new List<Vector3>();
        private readonly List<Transform> barFills = new List<Transform>();
        private int[] towerPrevCd;
        private bool[] structPrevAlive;

        private Transform DynRoot()
        {
            if (dynRoot == null) dynRoot = new GameObject("Battle").transform;
            return dynRoot;
        }

        private void UpdateBattleView()
        {
            var root = DynRoot();
            // askerler: modele dönüştür, yürüme sallantısı ve yöne dönüş
            while (soldierViews.Count < sim.Soldiers.Count)
            {
                var st = sim.Soldiers[soldierViews.Count];
                var tr = VillageArt.BuildSoldier(root, st.Type, st.Level);
                soldierViews.Add(tr);
                // asker can çubuğu (kameraya bakar, küçük)
                var sb = VillageArt.Group(root, "sbar", Vector3.zero, 45f);
                VillageArt.Part(sb, VillageArt.Shape.Box, Vector3.zero, new Vector3(0.36f, 0.06f, 0.03f), VillageArt.Mat(new Color(0.08f, 0.06f, 0.05f)));
                var sf = VillageArt.Group(sb, "fill", new Vector3(-0.17f, 0f, -0.02f));
                VillageArt.Part(sf, VillageArt.Shape.Box, new Vector3(0.17f, 0f, 0f), new Vector3(0.34f, 0.045f, 0.015f), VillageArt.Mat(new Color(0.3f, 0.9f, 0.3f), 0.2f, 0.6f));
                soldierBars.Add(sb); soldierBarFills.Add(sf); soldierBarColor.Add(0);
                soldierOff.Add(new Vector3(st.OffX / 1000f, 0f, st.OffY / 1000f));
                soldierRigs.Add(tr.GetComponent<ISoldierAnim>());
                soldierVel.Add(0f);
                { var sh = tr.GetComponent<SoldierHeight>(); float mh = sh != null ? sh.Value - 0.12f : ModelLibrary.WorldHeight(tr); soldierBarH.Add(mh > 0.05f ? Mathf.Min(mh, 1.6f) + 0.12f : 0.5f); }
                soldierPrevAttacks.Add(st.AttackCount);
                soldierDir.Add(Vector3.forward);
                soldierSep.Add(Vector3.zero);
                soldierDisp.Add(Vector3.zero);
                soldierPrev.Add(new Vector3(st.X / 1000f, 0f, st.Y / 1000f));
            }
            for (int i = 0; i < sim.Soldiers.Count; i++)
            {
                var s = sim.Soldiers[i];
                var t = soldierViews[i];
                if (t.gameObject.activeSelf != s.Alive) t.gameObject.SetActive(s.Alive);
                if (soldierBars[i].gameObject.activeSelf != s.Alive) soldierBars[i].gameObject.SetActive(s.Alive);
                if (!s.Alive) continue;
                // ara konum: önceki adım → şimdiki adım, kalan süre oranında
                float alpha = sim.Finished ? 1f : Mathf.Clamp01(tickAcc / SimTickSeconds);
                var prevSim = i < simPrevPos.Count ? simPrevPos[i] : new Vector2(s.X, s.Y);
                var p = new Vector3(Mathf.Lerp(prevSim.x, s.X, alpha) / 1000f, 0f, Mathf.Lerp(prevSim.y, s.Y, alpha) / 1000f);
                var d = p - soldierPrev[i];
                float dt = Mathf.Max(Time.deltaTime, 0.0001f);
                // ekrandaki gerçek hız (kare/sn), yumuşatılmış: yürüme animasyonu buna uyar
                soldierVel[i] = Mathf.Lerp(soldierVel[i], d.magnitude / dt, 1f - Mathf.Exp(-10f * dt));
                bool moving = soldierVel[i] > 0.06f;
                // yön: yürürken gidiş yönüne, dururken (saldırırken) hedef yapıya döner; kare hızından bağımsız yumuşak dönüş
                Vector3 face = Vector3.zero;
                if (!moving && s.TargetId >= 0 && s.TargetId < sim.Structures.Count)
                {
                    var tg = sim.Structures[s.TargetId];
                    face = new Vector3(tg.Pos.X, 0f, tg.Pos.Y) - t.position; face.y = 0f;
                }
                else if (d.sqrMagnitude > 0.0000004f) face = d;
                if (face.sqrMagnitude > 0.0001f)
                    t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(face.normalized, Vector3.up), 1f - Mathf.Exp(-(moving ? 10f : 14f) * dt));
                var rig = soldierRigs[i];
                float bob = moving && rig is CharacterRig ? Mathf.Abs(Mathf.Sin(Time.time * 11f + i)) * 0.025f : 0f;
                if (rig != null)
                {
                    rig.SetMoving(moving);
                    rig.SetSpeed(soldierVel[i]);
                }
                if (s.AttackCount != soldierPrevAttacks[i])
                {
                    soldierPrevAttacks[i] = s.AttackCount;
                    float interval = RealSeconds(s.IntervalTicks) / Mathf.Max(1f, speed);
                    float impact = rig != null ? rig.TriggerAttack(interval) : 0.15f;
                    OnSoldierAttack(s, t.position, impact);
                }
                // Kişisel duruş: her yer askeri yolun içinde kendine ait sabit bir sapmayla yürür (dünya ekseninde; köşede dönmez).
                // Uçanlar simülasyonun verdiği dağılımı kullanır. Üst üste binme, aşağıdaki yumuşak ayrışmayla önlenir.
                Vector3 offTarget = s.Flying
                    ? new Vector3(s.OffX / 1000f, 0f, s.OffY / 1000f)
                    : new Vector3(((s.Id * 73) % 100 / 100f - 0.5f) * 0.3f, 0f, ((s.Id * 41 + 17) % 100 / 100f - 0.5f) * 0.3f);
                soldierOff[i] = Vector3.Lerp(soldierOff[i], offTarget + soldierSep[i], Mathf.Min(1f, Time.deltaTime * 5f));
                t.position = p + soldierOff[i] + new Vector3(0f, (s.Flying ? 0.9f + Mathf.Sin(Time.time * 3f + i) * 0.05f : 0.05f) + bob, 0f);
                soldierDisp[i] = t.position;
                soldierPrev[i] = p;
                // can çubuğu: başın üstünde; renk yeşil > sarı > kırmızı
                float hf = s.MaxHp > 0 ? Mathf.Clamp01((float)s.Hp / s.MaxHp) : 0f;
                soldierBars[i].position = t.position + new Vector3(0f, soldierBarH[i], 0f);
                soldierBarFills[i].localScale = new Vector3(Mathf.Max(0.03f, hf), 1f, 1f);
                int ci = hf > 0.6f ? 0 : (hf > 0.3f ? 1 : 2);
                if (ci != soldierBarColor[i])
                {
                    soldierBarColor[i] = ci;
                    soldierBarFills[i].GetComponentInChildren<MeshRenderer>().sharedMaterial = VillageArt.Mat(ci == 0 ? new Color(0.3f, 0.9f, 0.3f) : (ci == 1 ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.3f, 0.2f)), 0.2f, 0.6f);
                }
            }

            SeparateSoldiers();

            // yapılar: can çubuğu, yıkım dumanı, kule atışı
            int n = sim.Structures.Count;
            if (towerPrevCd == null || towerPrevCd.Length != n)
            {
                towerPrevCd = new int[n]; structPrevAlive = new bool[n];
                for (int i = 0; i < n; i++) { towerPrevCd[i] = sim.Structures[i].Cooldown; structPrevAlive[i] = sim.Structures[i].Alive; }
            }
            while (barViews.Count < n)
            {
                var st = sim.Structures[barViews.Count];
                var holder = VillageArt.Group(root, "bar", new Vector3(st.Pos.X, st.Kind == StructKind.Tower ? 1.75f : 1.0f, st.Pos.Y), 45f);
                VillageArt.Part(holder, VillageArt.Shape.Box, Vector3.zero, new Vector3(0.74f, 0.09f, 0.04f), VillageArt.Mat(new Color(0.08f, 0.06f, 0.05f)));
                var fill = VillageArt.Group(holder, "fill", new Vector3(-0.35f, 0f, -0.025f));
                VillageArt.Part(fill, VillageArt.Shape.Box, new Vector3(0.35f, 0f, 0f), new Vector3(0.7f, 0.065f, 0.02f), VillageArt.Mat(Color.white, 0.2f, 0.6f));
                barViews.Add(holder);
                barFills.Add(fill);
            }
            for (int i = 0; i < n; i++)
            {
                var st = sim.Structures[i];
                bool show = st.Alive && st.Hp < st.MaxHp;
                if (barViews[i].gameObject.activeSelf != show) barViews[i].gameObject.SetActive(show);
                if (show)
                {
                    float f = st.MaxHp > 0 ? Mathf.Clamp01((float)st.Hp / st.MaxHp) : 0f;
                    barFills[i].localScale = new Vector3(Mathf.Max(0.02f, f), 1f, 1f);
                    var r = barFills[i].GetComponentInChildren<MeshRenderer>();
                    r.sharedMaterial = VillageArt.Mat(f > 0.6f ? new Color(0.3f, 0.9f, 0.3f) : (f > 0.3f ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.3f, 0.2f)), 0.2f, 0.6f);
                }
                if (structPrevAlive[i] && !st.Alive) VillageArt.Smoke(root, new Vector3(st.Pos.X, 0f, st.Pos.Y), i * 13 + sim.Tick);
                structPrevAlive[i] = st.Alive;

                if (st.Kind == StructKind.Tower && st.Alive && st.Cooldown > towerPrevCd[i]) SpawnShot(st);
                towerPrevCd[i] = st.Cooldown;
            }

            // mermiler
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var sh = shots[i];
                if (sh.T == null) { shots.RemoveAt(i); continue; }
                float a = (Time.time - sh.T0) / sh.Dur;
                if (a < 0f) { if (sh.T.gameObject.activeSelf) sh.T.gameObject.SetActive(false); continue; } // bırakma anı gelmedi
                if (!sh.T.gameObject.activeSelf) sh.T.gameObject.SetActive(true);
                if (a >= 1f)
                {
                    if (sh.HitColor.a > 0f) SpawnSpark(sh.B, sh.HitColor, sh.HitSize);
                    Destroy(sh.T.gameObject); shots.RemoveAt(i); continue;
                }
                float arc = sh.Arc;
                var pos = Vector3.Lerp(sh.A, sh.B, a) + new Vector3(0f, Mathf.Sin(a * Mathf.PI) * arc, 0f);
                // yönü yayın teğetine göre (ok ucu önce yükselir sonra iner)
                var vel = (sh.B - sh.A) + new Vector3(0f, Mathf.Cos(a * Mathf.PI) * Mathf.PI * arc, 0f);
                sh.T.position = pos;
                if (vel.sqrMagnitude > 0.0001f) sh.T.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up);
                if (sh.Spin) sh.T.Rotate(0f, 0f, Time.time * 720f % 360f, Space.Self);
            }

            // vuruş kıvılcımları (gecikmeli başlar, büyüyüp söner)
            for (int i = fxs.Count - 1; i >= 0; i--)
            {
                var f = fxs[i];
                if (f.T == null) { fxs.RemoveAt(i); continue; }
                float a = (Time.time - f.T0) / f.Dur;
                if (a < 0f) { if (f.T.gameObject.activeSelf) f.T.gameObject.SetActive(false); continue; }
                if (a >= 1f) { Destroy(f.T.gameObject); fxs.RemoveAt(i); continue; }
                if (!f.T.gameObject.activeSelf) f.T.gameObject.SetActive(true);
                float k = Mathf.Sin(Mathf.Min(1f, a * 1.6f) * Mathf.PI * 0.5f) * (1f - a * a);
                f.T.localScale = Vector3.one * (f.Size * (0.4f + k));
            }
        }

        /// <summary>
        /// Görsel ayrışma: birbirine 0,26 kareden fazla yaklaşan yer askerleri hafifçe itilir (yalnızca çizim; simülasyon değişmez).
        /// İtme birikir ve yavaşça söner; kalabalıkta askerler doğal biçimde dağılır.
        /// </summary>
        private void SeparateSoldiers()
        {
            const float minD = 0.26f;
            int n = Mathf.Min(sim.Soldiers.Count, soldierDisp.Count);
            for (int i = 0; i < n; i++) soldierSep[i] *= 0.95f; // sönme (birikip itişmesin)
            for (int i = 0; i < n; i++)
            {
                var a = sim.Soldiers[i];
                if (!a.Alive || a.Flying) continue;
                for (int j = i + 1; j < n; j++)
                {
                    var b = sim.Soldiers[j];
                    if (!b.Alive || b.Flying) continue;
                    var dlt = soldierDisp[i] - soldierDisp[j]; dlt.y = 0f;
                    float d2 = dlt.sqrMagnitude;
                    if (d2 >= minD * minD) continue;
                    float dd = Mathf.Sqrt(d2);
                    var dir = dd > 0.0001f ? dlt / dd : new Vector3(((i * 31 + j * 7) % 7) - 3f, 0f, ((i * 13 + j) % 5) - 2f).normalized;
                    var push = dir * ((minD - dd) * 0.15f);
                    soldierSep[i] += push; soldierSep[j] -= push;
                }
            }
            for (int i = 0; i < n; i++) if (soldierSep[i].sqrMagnitude > 0.04f) soldierSep[i] = soldierSep[i].normalized * 0.2f;
        }

        private void SpawnShot(SimStructure tower)
        {
            var best = sim.TowerTargetOf(tower); // simülasyonla aynı hedef kuralı
            if (best == null) return;
            var t = VillageArt.Projectile(DynRoot(), tower.TowerType);
            var a = new Vector3(tower.Pos.X, 1.25f, tower.Pos.Y);
            var b = new Vector3(best.X / 1000f, best.Flying ? 1.0f : 0.25f, best.Y / 1000f);
            shots.Add(new Shot { T = t, A = a, B = b, T0 = Time.time, Dur = 0.25f / Mathf.Max(1, speed) });
            t.position = a;
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
            return x >= 0 && x < layout.Size && row >= 1 && row <= VillageLayoutRows;
        }

        private const int VillageLayoutRows = GameCore.Village.VillageLayout.AttackZoneRows;

        private int LiveDamagePct()
        {
            var pcts = new List<int>();
            foreach (var st in sim.Structures) pcts.Add(st.Alive ? 100 - st.Hp * 100 / st.MaxHp : 100);
            return CombatMath.OverallDamagePct(pcts);
        }
    }
}
