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
                t.position = new Vector3(st.Pos.X, st.Kind == StructKind.Tower ? 2.0f : 1.2f, st.Pos.Y);
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
