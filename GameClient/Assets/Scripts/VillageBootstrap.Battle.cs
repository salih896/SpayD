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
            lastResult = sim.Result(curTarget.Pool, g.Config, curTarget.Level);
            lastOutcome = g.FinishAttack(sim, lastResult, curTarget.Level);
            AddLevelUpPopups(lastOutcome.LevelUps);
            LastSave();
        }

        private void CloseBattle()
        {
            sim = null; battleOver = false; lastResult = null; lastOutcome = null; lastAlive = -1;
            DestroyDyn();
            dirty = true;
            if (g != null) { view = g.MakeDefense(); ResetCamera(); }
        }

        private void DestroyDyn()
        {
            if (dynRoot != null) Destroy(dynRoot.gameObject);
            dynRoot = null;
            soldierViews.Clear(); barViews.Clear(); barFills.Clear(); soldierPrev.Clear(); shots.Clear();
            towerPrevCd = null; structPrevAlive = null;
        }

        private sealed class Shot { public Transform T; public Vector3 A, B; public float T0, Dur; }
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
                var tr = VillageArt.BuildSoldier(root, st.Type);
                soldierViews.Add(tr);
                soldierPrev.Add(new Vector3(st.X / 1000f, 0f, st.Y / 1000f));
            }
            for (int i = 0; i < sim.Soldiers.Count; i++)
            {
                var s = sim.Soldiers[i];
                var t = soldierViews[i];
                if (t.gameObject.activeSelf != s.Alive) t.gameObject.SetActive(s.Alive);
                if (!s.Alive) continue;
                var p = new Vector3(s.X / 1000f, 0f, s.Y / 1000f);
                var d = p - soldierPrev[i];
                if (d.sqrMagnitude > 0.00002f) t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), 0.35f);
                bool moving = d.sqrMagnitude > 0.00002f;
                float bob = moving ? Mathf.Abs(Mathf.Sin(Time.time * 14f + i)) * 0.04f : 0f;
                t.position = p + new Vector3(0f, (s.Flying ? 0.9f + Mathf.Sin(Time.time * 3f + i) * 0.05f : 0.05f) + bob, 0f);
                soldierPrev[i] = p;
            }

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
                float a = (Time.time - sh.T0) / sh.Dur;
                if (a >= 1f || sh.T == null) { if (sh.T != null) Destroy(sh.T.gameObject); shots.RemoveAt(i); continue; }
                var pos = Vector3.Lerp(sh.A, sh.B, a) + new Vector3(0f, Mathf.Sin(a * Mathf.PI) * 0.35f, 0f);
                var dir = sh.B - sh.A;
                sh.T.position = pos;
                if (dir.sqrMagnitude > 0.0001f) sh.T.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        private void SpawnShot(SimStructure tower)
        {
            var def = ContentCatalog.Tower(tower.TowerType);
            long r2 = (long)def.RangeMilli * def.RangeMilli;
            SimSoldier best = null; long bestD = long.MaxValue;
            foreach (var s in sim.Soldiers)
            {
                if (!s.Alive || (s.Flying && !def.CanHitAir)) continue;
                long dx = s.X - tower.CenterX, dy = s.Y - tower.CenterY, d = dx * dx + dy * dy;
                if (d <= r2 && d < bestD) { best = s; bestD = d; }
            }
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
