using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Game;
using GameCore.Grid;
using GameCore.Military;
using GameCore.Progression;
using GameCore.Village;

namespace GameClient
{
    public sealed partial class VillageBootstrap
    {
        private static readonly string[] TabNames = { "Köy", "Kışla", "Araştırma", "Görevler", "Saldır", "Menü" };
        private static readonly string[] TreeNames = { "Ekonomi", "Savunma", "Askerî", "Mimarlık", "Köy Gel.", "Bilim", "Element" };
        private Vector2 scrollPos;
        private int researchTree;
        private string selResearch;
        private TargetInfo[] targets;
        private int targetSeed = 1, targetsLevel = -1;
        private bool confirmNew;

        private bool Btn(Rect r, string text)
        {
            uiRects.Add(r);
            return GUI.Button(r, text);
        }

        /// <summary>Seçili durumu altın renkle gösteren düğme.</summary>
        private bool SelBtn(Rect r, string text, bool on)
        {
            uiRects.Add(r);
            return GUI.Button(r, text, on ? UiTheme.BtnOn : GUI.skin.button);
        }

        private void Panel(Rect r, string title = null)
        {
            uiRects.Add(r);
            GUI.Box(r, "");
            if (title != null) GUI.Label(new Rect(r.x + 14, r.y + 6, r.width - 28, 26), title, UiTheme.Header);
        }

        private void OnGUI()
        {
            if (g == null || view == null) return;
            UiTheme.Ensure();
            GUI.skin = UiTheme.Skin;
            uiScale = Mathf.Clamp(Screen.height / 640f, 0.75f, 3f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
            uiRects.Clear();
            float w = Screen.width / uiScale, h = Screen.height / uiScale;

            if (popups.Count > 0) { DrawTopBar(w); DrawPopup(w, h); return; }
            DrawTopBar(w);
            if (sim != null) { DrawBattleGui(w, h); return; }
            DrawTabs(w, h);
            switch (tab)
            {
                case Tab.Village: DrawVillage(w, h); break;
                case Tab.Barracks: DrawBarracks(w, h); break;
                case Tab.Research: DrawResearch(w, h); break;
                case Tab.Quests: DrawQuests(w, h); break;
                case Tab.Attack: DrawAttack(w, h); break;
                default: DrawMenu(w, h); break;
            }
        }

        // ---------- Ortak ----------
        private static Color ResColor(ResourceType t)
        {
            switch (t)
            {
                case ResourceType.Wood: return new Color(0.70f, 0.45f, 0.22f);
                case ResourceType.Stone: return new Color(0.70f, 0.72f, 0.75f);
                case ResourceType.Gold: return new Color(1f, 0.82f, 0.25f);
                default: return new Color(0.50f, 0.58f, 0.70f);
            }
        }

        private void DrawTopBar(float w)
        {
            var r = new Rect(0, 0, w, 44);
            uiRects.Add(r);
            GUI.DrawTexture(r, UiTheme.White, ScaleMode.StretchToFill, true, 0f, new Color(0.10f, 0.07f, 0.04f, 0.78f), 0f, 0f);
            GUI.DrawTexture(new Rect(0, 43, w, 2), UiTheme.White, ScaleMode.StretchToFill, true, 0f, new Color(0.78f, 0.60f, 0.28f, 0.9f), 0f, 0f);
            // seviye rozeti + XP çubuğu
            UiTheme.Dot(new Rect(6, 3, 38, 38), new Color(0.85f, 0.62f, 0.2f));
            GUI.Label(new Rect(6, 3, 38, 38), g.Level.ToString(), UiTheme.Title);
            bool max = g.Level >= ProgressionRules.MaxLevel;
            float need = max ? 1 : ProgressionRules.XpToNextLevel(g.Level);
            UiTheme.Bar(new Rect(48, 22, 120, 14), max ? 1f : g.XpIntoLevel / need, new Color(0.45f, 0.8f, 1f));
            GUI.Label(new Rect(48, 2, 140, 20), max ? "Seviye MAKS" : "Seviye " + g.Level + "  " + g.XpIntoLevel + "/" + (int)need, UiTheme.Small);
            float x = 180f;
            foreach (var t in ResourceRules.Unlocked(g.Config, g.Level))
            {
                var cr = new Rect(x, 7, 168, 30);
                int have = g.Wallet.Get(t), cap = g.CapacityOf(t);
                GUI.Label(cr, have + "/" + cap + "  <size=12><color=#d8c9a8>+" + g.PerHour(t) + "</color></size>", UiTheme.Chip);
                UiTheme.Dot(new Rect(cr.x + 5, cr.y + 5, 20, 20), ResColor(t));
                if (have >= cap) GUI.Label(new Rect(cr.x + cr.width - 40, cr.y + 6, 38, 22), "<color=#ff9a5a>DOLU</color>", UiTheme.Small);
                x += 174f;
            }
            var ar = new Rect(x, 7, 120, 30);
            GUI.Label(ar, "Ordu " + g.ArmySize + "/" + g.ArmyCapacity, UiTheme.Chip);
            UiTheme.Dot(new Rect(ar.x + 5, ar.y + 5, 20, 20), new Color(0.85f, 0.3f, 0.25f));
            var wr = new Rect(x + 126, 7, 104, 30);
            GUI.Label(wr, "İşçi " + g.FreeBuilders + "/" + g.Builders, UiTheme.Chip);
            UiTheme.Dot(new Rect(wr.x + 5, wr.y + 5, 20, 20), new Color(0.45f, 0.75f, 0.4f));
        }

        private void DrawTabs(float w, float h)
        {
            var bar = new Rect(0, h - 52, w, 52);
            uiRects.Add(bar);
            GUI.DrawTexture(bar, UiTheme.White, ScaleMode.StretchToFill, true, 0f, new Color(0.10f, 0.07f, 0.04f, 0.80f), 0f, 0f);
            GUI.DrawTexture(new Rect(0, h - 52, w, 2), UiTheme.White, ScaleMode.StretchToFill, true, 0f, new Color(0.78f, 0.60f, 0.28f, 0.9f), 0f, 0f);
            float bw = Mathf.Min(150f, (w - 12f) / TabNames.Length - 4f);
            float x0 = (w - TabNames.Length * (bw + 4f)) * 0.5f;
            for (int i = 0; i < TabNames.Length; i++)
            {
                bool badge = (i == (int)Tab.Quests && HasClaimable()) || (i == (int)Tab.Village && g.DamagedTowerCount() > 0);
                var r = new Rect(x0 + i * (bw + 4f), h - 46, bw, 40);
                if (GUI.Button(r, TabNames[i], (int)tab == i ? UiTheme.TabOn : UiTheme.Tab)) { tab = (Tab)i; scrollPos = Vector2.zero; dirty = true; }
                if (badge) { UiTheme.Dot(new Rect(r.xMax - 16, r.y - 4, 18, 18), new Color(0.95f, 0.25f, 0.2f)); GUI.Label(new Rect(r.xMax - 16, r.y - 5, 18, 18), "!", UiTheme.Center); }
            }
        }

        private bool HasClaimable()
        {
            foreach (var q in g.Quests.Visible(g.Level)) if (g.Quests.IsComplete(q)) return true;
            return false;
        }

        private void DrawPopup(float w, float h)
        {
            var lines = popups[0].Split(new[] { '\n' }, 2);
            var dark = new GUIStyle(UiTheme.Big) { normal = { textColor = new Color(0.25f, 0.15f, 0.07f) } };
            float bodyH = lines.Length > 1 ? dark.CalcHeight(new GUIContent(lines[1]), 456f) : 0f;
            float bh = Mathf.Clamp(130f + bodyH, 200f, h - 20f);
            var box = new Rect(w * 0.5f - 250f, (h - bh) * 0.5f, 500f, bh);
            uiRects.Add(new Rect(0, 0, w, h));
            GUI.DrawTexture(new Rect(0, 0, w, h), UiTheme.White, ScaleMode.StretchToFill, true, 0f, new Color(0f, 0f, 0f, 0.55f), 0f, 0f);
            GUI.Box(box, "", UiTheme.Parchment);
            var head = new GUIStyle(UiTheme.Title) { normal = { textColor = new Color(0.55f, 0.30f, 0.08f) } };
            GUI.Label(new Rect(box.x + 16, box.y + 16, box.width - 32, 32), lines[0], head);
            if (lines.Length > 1) GUI.Label(new Rect(box.x + 22, box.y + 58, box.width - 44, box.height - 120), lines[1], dark);
            if (GUI.Button(new Rect(box.x + box.width * 0.5f - 80, box.y + box.height - 56, 160, 42), popups.Count > 1 ? "Tamam (" + (popups.Count - 1) + " daha)" : "Tamam", UiTheme.BtnGreen)) popups.RemoveAt(0);
        }

        private void DrawStatusLine(float w, float h)
        {
            bool showToast = Time.time < toastUntil;
            string msg = showToast ? toast : (valid ? "Savunma hazır" : validationError);
            msg += "     Yol " + layout.RoadCount + "/" + layout.RoadLimit + "   Giriş " + layout.EntranceCount + "   Kule " + g.Towers.Count + "/" + GameRules.MaxTowers(g.Level);
            float tw = Mathf.Min(w - 20f, 760f);
            var rs = new Rect((w - tw) * 0.5f, h - 92, tw, 34);
            GUI.Label(rs, msg, showToast || valid ? UiTheme.Pill : UiTheme.PillBad);
            uiRects.Add(rs);
        }

        // ---------- Köy ----------
        private void DrawVillage(float w, float h)
        {
            float bw = Mathf.Min(86f, (w - 12f) / ToolNames.Length - 3f);
            for (int i = 0; i < ToolNames.Length; i++)
                if (SelBtn(new Rect(6 + i * (bw + 3), 50, bw, 34), ToolNames[i], (int)tool == i)) { tool = (Tool)i; if (tool != Tool.Select) selCell = new GridPos(-1, -1); dirty = true; }

            float y = 90;
            if (tool == Tool.Building)
            {
                var bt = selBuilding; var bd = ContentCatalog.Building(bt);
                string txt = "Bina: " + bd.Name + (g.Level < bd.UnlockLevel ? " (Sv " + bd.UnlockLevel + ")" : "") + "  " + GameRules.Scale(GameRules.BuildCostOf(bt), g.Config, g.Level, 100);
                if (Btn(new Rect(6, y, 440, 34), "< " + txt + " >")) selBuilding = (BuildingType)(((int)selBuilding + 1) % 11);
            }
            else if (tool == Tool.Tower)
            {
                var td = ContentCatalog.Tower(selTower);
                string txt = "Kule: " + td.Name + (g.Level < td.UnlockLevel ? " (Sv " + td.UnlockLevel + ")" : "") + "  " + GameRules.Scale(td.BuildCost, g.Config, g.Level, 100);
                if (Btn(new Rect(6, y, 440, 34), "< " + txt + " >")) selTower = (TowerType)(((int)selTower + 1) % 4);
            }
            else if (tool == Tool.Trap)
            {
                if (Btn(new Rect(6, y, 240, 34), "< Tuzak: " + TrapName(selTrap) + " >")) selTrap = (TrapType)(((int)selTrap % 3) + 1);
            }
            if (g.DamagedTowerCount() > 0)
            {
                string e;
                if (Btn(new Rect(w - 256, 90, 250, 34), "Tüm kuleleri onar (" + g.DamagedTowerCount() + ")")) { if (g.TryRepairAll(out e)) { Toast("Kuleler onarıldı"); dirty = true; } else Toast(e); }
            }

            if (tool == Tool.Select && layout.InBounds(selCell)) DrawSelectPanel(w, h);

            // Son baskın raporu
            if (g.LastRaid != null)
            {
                var rr = g.LastRaid;
                string t = "Son baskın: %" + rr.Result.OverallDamagePct + " hasar, " + (rr.Result.Victory ? "saldırgan kazandı" : "savundun") + ", kayıp: " + rr.LootLost + ", hasarlı kule: " + rr.TowersDamaged;
                var r = new Rect(6, h - 134, w - 12, 36);
                GUI.Label(r, t, rr.Result.Victory ? UiTheme.PillBad : UiTheme.Pill); uiRects.Add(r);
            }
            DrawStatusLine(w, h);
        }

        private void DrawSelectPanel(float w, float h)
        {
            var r = new Rect(w - 336, 132, 330, 280);
            Panel(r);
            float x = r.x + 12, y = r.y + 10, iw = r.width - 24;
            var p = selCell;
            if (g.Buildings.TryGetValue(p, out var b))
            {
                var d = ContentCatalog.Building(b.Type);
                GUI.Label(new Rect(x, y, iw, 26), d.Name + "  Sv " + b.Level, UiTheme.Header); y += 28;
                int hp = CombatMods.Apply(ContentCatalog.Scaled(d.Hp, b.Level), g.Research.Mods().BuildingHp);
                GUI.Label(new Rect(x, y, iw, 24), "Can: " + hp); y += 22;
                var prod = GameRules.ProducedBy(b.Type);
                if (prod.HasValue) { GUI.Label(new Rect(x, y, iw, 24), "Üretim: " + (int)((long)GameRules.BasePerHour(prod.Value) * ContentCatalog.ScalePct(b.Level) / 100) + " " + ResName(prod.Value) + "/sa"); y += 22; }
                if (d.IsDepot) { GUI.Label(new Rect(x, y, iw, 24), "Depo: +" + GameRules.DepotCapacity(d.Stores, b.Level) + " " + ResName(d.Stores)); y += 22; }
                if (b.Type == BuildingType.Barracks) { GUI.Label(new Rect(x, y, iw, 24), "Ordu kapasitesi: +" + (GameRules.ArmyCapacity(g.BarracksLevelSum()) - GameRules.ArmyCapacity(g.BarracksLevelSum() - b.Level))); y += 22; }
                if (b.Type == BuildingType.University) { GUI.Label(new Rect(x, y, iw, 24), "Araştırma Sv " + b.Level + "'e kadar"); y += 22; }
                if (b.Type == BuildingType.VillagerHouse) { GUI.Label(new Rect(x, y, iw, 24), "+1 işçi (en çok 5)"); y += 22; }
                y = DrawUpgradeRow(x, y, iw, JobKind.Building, p, SoldierType.Swordsman);
            }
            else if (g.Towers.TryGetValue(p, out var t))
            {
                var d = ContentCatalog.Tower(t.Type);
                GUI.Label(new Rect(x, y, iw, 26), d.Name + "  Sv " + t.Level, UiTheme.Header); y += 28;
                var m = g.Research.Mods();
                int hp = CombatMods.Apply(ContentCatalog.Scaled(d.Hp, t.Level), m.TowerHp);
                int atk = CombatMods.Apply(ContentCatalog.Scaled(d.Attack, t.Level), m.TowerDamage);
                GUI.Label(new Rect(x, y, iw, 24), "Can: " + hp + "  Hasar: " + atk + "  Menzil: " + (d.RangeMilli / 1000f).ToString("0.0")); y += 22;
                if (g.Wears.TryGetValue(p, out var wr) && wr.PermanentDamagePct > 0)
                {
                    var cost = wr.RepairCost(t.Level, g.Config, g.Level);
                    GUI.Label(new Rect(x, y, iw, 24), "Kalıcı hasar: %" + wr.PermanentDamagePct + (wr.IsDisabled ? " (DEVRE DIŞI)" : "")); y += 22;
                    string e;
                    if (GUI.Button(new Rect(x, y, iw, 28), "Onar (" + cost + ")")) { if (g.TryRepair(p, out e)) { Toast("Onarıldı"); dirty = true; } else Toast(e); }
                    uiRects.Add(new Rect(x, y, iw, 28)); y += 32;
                }
                y = DrawUpgradeRow(x, y, iw, JobKind.Tower, p, SoldierType.Swordsman);
            }
            else if (layout.Get(p) == CellKind.Road) GUI.Label(new Rect(x, y, iw, 48), "Yol (" + layout.GetRoadTier(p) + ")" + (layout.IsEntrance(p) ? " - GİRİŞ" : "") + (g.Traps.TryGetValue(p, out var tr) ? " - " + TrapName(tr) + " tuzak" : ""));
            else GUI.Label(new Rect(x, y, iw, 24), "Boş hücre " + p);
        }

        private float DrawUpgradeRow(float x, float y, float iw, JobKind kind, GridPos p, SoldierType s)
        {
            var info = g.PreviewUpgrade(kind, p, s);
            if (info.Block != null && info.TargetLevel == 0) { GUI.Label(new Rect(x, y, iw, 24), info.Block); return y + 24; }
            GUI.Label(new Rect(x, y, iw, 44), "Sv " + info.TargetLevel + ": " + info.Cost + " | " + Dur(info.Seconds)); y += 44;
            if (info.Block != null) { GUI.Label(new Rect(x, y, iw, 44), info.Block); return y + 44; }
            string err;
            if (GUI.Button(new Rect(x, y, iw, 34), "Yükselt", UiTheme.BtnGreen)) { if (g.TryStartUpgrade(kind, p, s, out err)) { Toast("Yükseltme başladı"); } else Toast(err); }
            uiRects.Add(new Rect(x, y, iw, 30));
            return y + 34;
        }

        // ---------- Kışla ----------
        private void DrawBarracks(float w, float h)
        {
            var r = new Rect(8, 50, w - 16, h - 108);
            Panel(r, "KIŞLA  -  Ordu " + g.ArmySize + "/" + g.ArmyCapacity + (g.BarracksLevelSum() == 0 ? "   (Önce Köy sekmesinden Kışla kur!)" : ""));
            GUILayout.BeginArea(new Rect(r.x + 10, r.y + 36, r.width - 20, r.height - 44));
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            string err;
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
            {
                var t = (SoldierType)i; var d = ContentCatalog.Soldier(t);
                int lvl = g.SoldierLevel(t);
                bool locked = g.Level < d.UnlockLevel;
                GUILayout.BeginHorizontal(GUILayout.Height(44));
                GUILayout.Label((locked ? "[Sv " + d.UnlockLevel + "'de açılır] " : "") + d.Name + "  Sv " + lvl + "  adet " + g.Inventory.Get(t) + "\nCan " + ContentCatalog.Scaled(d.Hp, lvl) + "  Hasar " + ContentCatalog.Scaled(d.Attack, lvl) + "  Hız " + d.SpeedMilli, GUILayout.Width(320));
                GUI.enabled = !locked;
                int ts = Mathf.Max(5, (int)((long)d.TrainSeconds * (1000 - g.Research.Total(ResearchStat.TrainTime)) / 1000));
                if (GUILayout.Button("Eğit\n" + d.Cost + " | " + Dur(ts), GUILayout.Width(210))) { if (g.TryTrain(t, out err)) AddLog(d.Name + " eğitimi sıraya alındı"); else Toast(err); }
                var up = g.PreviewUpgrade(JobKind.Soldier, new GridPos(0, 0), t);
                string utxt = up.Block != null ? "Yükselt\n" + up.Block : "Sv " + up.TargetLevel + " yükselt\n" + up.Cost + " | " + Dur(up.Seconds);
                if (GUILayout.Button(utxt, GUILayout.Width(260))) { if (g.TryStartUpgrade(JobKind.Soldier, new GridPos(0, 0), t, out err)) Toast("Yükseltme başladı"); else Toast(err); }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(8);
            GUILayout.Label("EĞİTİM SIRASI (" + g.Queue.Items.Count + ")", UiTheme.Header);
            int cancelIdx = -1;
            for (int i = 0; i < g.Queue.Items.Count; i++)
            {
                var it = g.Queue.Items[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label((i + 1) + ". " + ContentCatalog.Soldier(it.Type).Name + "  kalan " + Dur(it.RemainingSeconds), GUILayout.Width(380));
                if (GUILayout.Button("İptal", GUILayout.Width(80))) cancelIdx = i;
                GUILayout.EndHorizontal();
            }
            if (cancelIdx >= 0) { if (g.TryCancelTrain(cancelIdx, out err)) Toast("İptal edildi"); else Toast(err); }
            GUILayout.Space(8);
            GUILayout.Label("YÜKSELTMELER (" + g.Jobs.Count + "/" + g.Builders + " işçi)", UiTheme.Header);
            foreach (var j in g.Jobs)
            {
                string nm = j.Kind == JobKind.Soldier ? ContentCatalog.Soldier(j.Soldier).Name : (j.Kind == JobKind.Building && g.Buildings.TryGetValue(j.Pos, out var bs) ? ContentCatalog.Building(bs.Type).Name : (g.Towers.TryGetValue(j.Pos, out var ts2) ? ContentCatalog.Tower(ts2.Type).Name : "Yapı"));
                GUILayout.Label(nm + " → Sv " + j.TargetLevel + "  kalan " + Dur(j.RemainingSeconds));
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ---------- Araştırma ----------
        private void DrawResearch(float w, float h)
        {
            var r = new Rect(8, 50, w - 16, h - 108);
            Panel(r);
            float bw = Mathf.Min(110f, (r.width - 16f) / TreeNames.Length - 2f);
            for (int i = 0; i < TreeNames.Length; i++)
            {
                if (SelBtn(new Rect(r.x + 8 + i * (bw + 2), r.y + 8, bw, 32), TreeNames[i], researchTree == i)) { researchTree = i; scrollPos = Vector2.zero; }
            }

            string head = "Üniversite Sv " + g.UniversityLevel + (g.Level < ResearchState.UniversityUnlockLevel ? " (Sv5'te açılır, sonra Üniversite kur)" : "") + "   Yuva " + g.Research.Active.Count + "/" + g.Research.Slots;
            foreach (var a in g.Research.Active) head += "   | " + ResearchCatalog.Get(a.Id).Name + " " + Dur(a.RemainingSeconds);
            GUI.Label(new Rect(r.x + 12, r.y + 44, r.width - 24, 24), head, UiTheme.Small);

            GUILayout.BeginArea(new Rect(r.x + 8, r.y + 70, r.width - 16, r.height - 172));
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            for (int lv = 1; lv <= ResearchCatalog.MaxLevel; lv++)
            {
                bool any = false;
                for (int c = 1; c <= 3; c++) if (ResearchCatalog.Find((ResearchTree)researchTree, lv, c) != null) any = true;
                if (!any) continue;
                GUILayout.BeginHorizontal(GUILayout.Height(40));
                GUILayout.Label("Sv " + lv, GUILayout.Width(44));
                for (int c = 1; c <= 3; c++)
                {
                    var d = ResearchCatalog.Find((ResearchTree)researchTree, lv, c);
                    if (d == null) { GUILayout.Label("", GUILayout.Width(250)); continue; }
                    string st = g.Research.IsDone(d.Id) ? "[OK]" : (g.Research.IsActive(d.Id) ? "[...]" : (g.Research.LockReason(d, g.Level, g.UniversityLevel) == null ? "[>]" : "[X]"));
                    var style = selResearch == d.Id ? UiTheme.BtnOn : (g.Research.IsDone(d.Id) ? UiTheme.BtnGreen : GUI.skin.button);
                    if (GUILayout.Button(st + " " + d.Name, style, GUILayout.Width(250), GUILayout.Height(38))) selResearch = d.Id;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Seçili araştırma ayrıntısı
            var sel = selResearch != null ? ResearchCatalog.Get(selResearch) : null;
            var dr = new Rect(r.x + 6, r.y + r.height - 94, r.width - 12, 88);
            GUI.Box(dr, "");
            if (sel == null) { GUI.Label(new Rect(dr.x + 8, dr.y + 6, dr.width - 16, 70), "Bir araştırma seç. Ön koşul: aynı sütundaki önceki araştırma; 3. sütun için aynı seviyedeki 1. ve 2. sütun."); return; }
            string reason = g.Research.LockReason(sel, g.Level, g.UniversityLevel);
            string bonus = StatText(sel);
            GUI.Label(new Rect(dr.x + 8, dr.y + 4, dr.width - 180, 80), sel.Name + "  (Sv " + sel.Level + ")  " + bonus + "\nMaliyet: " + g.Research.CostFor(sel, g.Config, g.Level) + "   Süre: " + Dur(g.Research.SecondsFor(sel)) + "\n" + (reason ?? "Başlatılabilir"));
            if (reason == null)
            {
                string err;
                uiRects.Add(new Rect(dr.x + dr.width - 160, dr.y + 24, 150, 40));
                if (GUI.Button(new Rect(dr.x + dr.width - 160, dr.y + 24, 150, 40), "BAŞLAT", UiTheme.BtnGreen)) { if (g.TryResearch(sel.Id, out err)) { AddLog("Araştırma başladı: " + sel.Name); } else Toast(err); }
            }
        }

        private static string StatText(ResearchDef d)
        {
            string v = (d.Permille / 10f).ToString("0.#") + "%";
            switch (d.Stat)
            {
                case ResearchStat.ProductionPct: return "Üretim +" + v;
                case ResearchStat.StoragePct: return "Depo +" + v;
                case ResearchStat.TowerDamage: return "Kule hasarı +" + v;
                case ResearchStat.TowerHp: return "Kule canı +" + v;
                case ResearchStat.SoldierDamage: return "Asker hasarı +" + v;
                case ResearchStat.SoldierHp: return "Asker canı +" + v;
                case ResearchStat.BuildingHp: return "Bina canı +" + v;
                case ResearchStat.UpgradeCost: return "Yükseltme maliyeti -" + v;
                case ResearchStat.BuildTime: return "Yükseltme süresi -" + v;
                case ResearchStat.TrainTime: return "Eğitim süresi -" + v;
                case ResearchStat.ResearchTime: return "Araştırma süresi -" + v;
                case ResearchStat.ResearchCost: return "Araştırma maliyeti -" + v;
                default: return "Element ustalığı: hasar +" + v;
            }
        }

        // ---------- Görevler ----------
        private void DrawQuests(float w, float h)
        {
            var r = new Rect(8, 50, w - 16, h - 108);
            Panel(r, "GÖREVLER");
            GUILayout.BeginArea(new Rect(r.x + 10, r.y + 36, r.width - 20, r.height - 44));
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            var list = g.Quests.Visible(g.Level);
            string claimId = null;
            for (int pass = 0; pass < 2; pass++)
            {
                GUILayout.Label(pass == 0 ? "GÜNLÜK GÖREVLER" : "KİLOMETRE TAŞLARI", UiTheme.Header);
                foreach (var q in list)
                {
                    if ((q.Kind == QuestKind.Daily) != (pass == 0)) continue;
                    bool done = g.Quests.IsComplete(q);
                    GUILayout.BeginHorizontal(GUILayout.Height(34));
                    GUILayout.Label(q.Name + "   " + g.Quests.ProgressOf(q.Id) + "/" + q.Target + "\nÖdül: " + QuestCatalog.RewardFor(q, g.Config, g.Level) + " + " + q.Xp + " XP", GUILayout.Width(w - 220));
                    GUI.enabled = done;
                    if (GUILayout.Button("AL", done ? UiTheme.BtnGreen : GUI.skin.button, GUILayout.Width(90), GUILayout.Height(36))) claimId = q.Id;
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            if (claimId != null)
            {
                string err;
                if (g.TryClaimQuest(claimId, out var ups, out err)) { AddLevelUpPopups(ups); AddLog("Görev ödülü alındı"); dirty = true; }
                else Toast(err);
            }
        }

        // ---------- Saldırı hedefleri ----------
        private void EnsureTargets()
        {
            if (targets != null && targetsLevel == g.Level) return;
            targetsLevel = g.Level;
            targets = new TargetInfo[3];
            for (int d = 0; d < 3; d++) targets[d] = TargetGenerator.Generate(g.Level, d, targetSeed * 3 + d);
        }

        private void DrawAttack(float w, float h)
        {
            EnsureTargets();
            var r = new Rect(8, 50, w - 16, h - 108);
            Panel(r, "SALDIRI HEDEFLERİ   (Ordun: " + (g.ArmySize - g.Queue.Items.Count) + " asker hazır)");
            string[] dn = { "KOLAY", "NORMAL", "ZOR" };
            float cw = (r.width - 40f) / 3f;
            for (int d = 0; d < 3; d++)
            {
                var t = targets[d];
                var cr = new Rect(r.x + 10 + d * (cw + 10), r.y + 40, cw, 240);
                GUI.Box(cr, "", UiTheme.Parchment);
                var dark = new GUIStyle(UiTheme.Skin.label) { normal = { textColor = new Color(0.25f, 0.15f, 0.07f) } };
                var head = new GUIStyle(UiTheme.Title) { fontSize = 20, normal = { textColor = d == 0 ? new Color(0.2f, 0.5f, 0.15f) : (d == 1 ? new Color(0.6f, 0.38f, 0.05f) : new Color(0.7f, 0.15f, 0.1f)) } };
                GUI.Label(new Rect(cr.x + 8, cr.y + 10, cr.width - 16, 28), dn[d], head);
                GUI.Label(new Rect(cr.x + 14, cr.y + 42, cr.width - 28, 140),
                    "<b>" + t.Name + "</b>  (Sv " + t.Level + ")\nKule: " + t.Defense.Towers.Count + "   Bina: " + t.Defense.Buildings.Count + "\nGiriş: " + t.Defense.Layout.EntranceCount + "   Tuzak: " + t.Defense.Traps.Count +
                    "\nYağma: " + t.Pool + "\nXP çarpanı: x" + ((100 + GameState.BattleXpPctPerTargetLevel * (t.Level - 1)) / 100f).ToString("0.00"), dark);
                uiRects.Add(new Rect(cr.x + 12, cr.y + 188, cr.width - 24, 42));
                if (GUI.Button(new Rect(cr.x + 12, cr.y + 188, cr.width - 24, 42), "SALDIR", UiTheme.BtnRed)) StartBattle(t);
            }
            if (Btn(new Rect(r.x + 10, r.y + 290, 180, 36), "Hedefleri yenile")) { targetSeed++; targets = null; }
            GUI.Label(new Rect(r.x + 12, r.y + 334, r.width - 24, 120), "<color=#d8c9a8>İpucu: askerleri kırmızı alana dokunarak (basılı tutarak) gönder. Mızraklı kuleler atlıya, Elf kuleleri periye güçlüdür; Büyücü kalabalığa, Goblin kaynak binalarına etkilidir. Zafer için yapıların ortalama hasarı %51'i geçmeli.</color>");
        }

        // ---------- Menü ----------
        private void DrawMenu(float w, float h)
        {
            var r = new Rect(8, 50, w - 16, h - 108);
            Panel(r, "MENÜ");
            float y = r.y + 34;
            string stats = "Savaş: " + g.BattlesFought + "  Zafer: " + g.BattlesWon + "  Yıldız: " + g.TotalStars + "   Araştırma: " + g.Research.Done.Count + "/" + ResearchCatalog.All.Count;
            GUI.Label(new Rect(r.x + 10, y, r.width - 20, 26), stats); y += 28;
            if (Btn(new Rect(r.x + 10, y, 160, 34), "Kaydet")) { LastSave(); Toast("Kaydedildi"); }
            y += 42;
            GUI.Label(new Rect(r.x + 10, y, r.width - 20, 24), "— TEST ARAÇLARI (zaman/kaynak hızlandırma) —"); y += 28;
            float x = r.x + 10;
            if (Btn(new Rect(x, y, 130, 34), "+1 dakika")) Step(60);
            if (Btn(new Rect(x + 136, y, 130, 34), "+1 saat")) Step(3600);
            if (Btn(new Rect(x + 272, y, 150, 34), "+8 saat (çevrimdışı)")) Step(8 * 3600);
            if (Btn(new Rect(x + 428, y, 130, 34), "+1 gün")) { g.Advance(86400, DayIndex + 1); FlushLevelUps(); dirty = true; Toast("1 gün geçti (günlük görev/baskın)"); }
            y += 42;
            if (Btn(new Rect(x, y, 150, 34), "+500 XP")) { AddLevelUpPopups(g.AddXp(500)); dirty = true; }
            if (Btn(new Rect(x + 156, y, 150, 34), "Kaynak +5000")) { foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType))) g.Wallet.TryAdd(t, 5000, g.Level, out _, out _); }
            if (Btn(new Rect(x + 312, y, 170, 34), "Sv 30 yap (test)")) { AddLevelUpPopups(g.AddXp(200000)); dirty = true; }
            y += 50;
            uiRects.Add(new Rect(x, y, 300, 38));
            if (GUI.Button(new Rect(x, y, 300, 38), confirmNew ? "EMİN MİSİN? Tekrar bas: tüm ilerleme silinir" : "Yeni oyun (sil)", UiTheme.BtnRed)) { if (confirmNew) { confirmNew = false; NewGameConfirmed(); } else confirmNew = true; }
            y += 48;
            var sb = new StringBuilder("Son olaylar:\n");
            foreach (var l in log) sb.Append("• ").Append(l).Append('\n');
            GUI.Label(new Rect(r.x + 10, y, r.width - 20, 160), sb.ToString());
        }

        // ---------- Savaş arayüzü ----------
        private void DrawBattleGui(float w, float h)
        {
            int n = ContentCatalog.SoldierTypeCount;
            float bw = Mathf.Min(120f, (w - 8f) / n - 3f);
            for (int i = 0; i < n; i++)
            {
                var t = (SoldierType)i;
                if (sim.StockOf(t) <= 0 && t != selSoldier) GUI.color = new Color(1f, 1f, 1f, 0.45f);
                var br = new Rect(4 + i * (bw + 3), 50, bw, 52);
                if (SelBtn(br, ContentCatalog.Soldier(t).Name + "\n" + sim.StockOf(t), t == selSoldier)) selSoldier = t;
                GUI.color = Color.white;
                UiTheme.Dot(new Rect(br.x + 4, br.y + 4, 12, 12), VillageArt.TunicOf(t));
            }
            if (!battleOver)
            {
                uiRects.Add(new Rect(4, 108, 120, 36));
                if (GUI.Button(new Rect(4, 108, 120, 36), "Geri Çek", UiTheme.BtnRed) && !sim.Finished) sim.Retreat();
                if (Btn(new Rect(128, 108, 100, 36), "Hız " + speed + "x")) speed = speed == 1 ? 3 : (speed == 3 ? 6 : 1);
            }
            int aliveSoldiers = 0;
            foreach (var s in sim.Soldiers) if (s.Alive) aliveSoldiers++;
            var rs = new Rect(6, h - 46, w - 12, 40);
            string msg = Time.time < toastUntil ? toast : curTarget.Name + " (Sv " + curTarget.Level + ")  |  " + (sim.Tick / 10) + " sn / 180  |  Hasar %" + LiveDamagePct() + " (zafer %51)  |  Sahada " + aliveSoldiers + "   [kırmızı alana dokun/basılı tut]";
            GUI.Label(rs, msg, UiTheme.Pill); uiRects.Add(rs);
            if (battleOver && lastResult != null) DrawResult(w, h);
        }

        private void DrawResult(float w, float h)
        {
            var r = lastResult;
            var box = new Rect(w * 0.5f - 220f, h * 0.5f - 170f, 440f, 340f);
            uiRects.Add(box);
            GUI.Box(box, "", UiTheme.Parchment);
            var head = new GUIStyle(UiTheme.Title) { fontSize = 30, normal = { textColor = r.Victory ? new Color(0.25f, 0.55f, 0.15f) : new Color(0.7f, 0.15f, 0.1f) } };
            GUI.Label(new Rect(box.x, box.y + 12, box.width, 40), r.Victory ? "ZAFER!" : "YENİLGİ", head);
            for (int i = 0; i < 3; i++)
                UiTheme.Dot(new Rect(box.x + box.width * 0.5f - 66 + i * 46, box.y + 56, 40, 40), i < r.Stars ? new Color(1f, 0.82f, 0.2f) : new Color(0.55f, 0.48f, 0.40f));
            float y = box.y + 104f;
            Line(box, ref y, "Genel hasar: %" + r.OverallDamagePct + "   XP: +" + (lastOutcome != null ? lastOutcome.XpGained : r.Xp));
            Line(box, ref y, "Yağma: " + (r.Loot.Wood + r.Loot.Stone + r.Loot.Gold + r.Loot.Iron > 0 ? r.Loot.ToString() : "yok"));
            Line(box, ref y, "Ölü " + BattleResult.Total(r.LostDead) + "  Yaralı " + BattleResult.Total(r.LostInjured) + "  Feda " + BattleResult.Total(r.LostSacrificed));
            Line(box, ref y, "Dönen asker: " + BattleResult.Total(r.Returned));
            if (lastOutcome != null && lastOutcome.LevelUps.Count > 0) Line(box, ref y, "SEVİYE ATLADIN → Sv " + g.Level);
            if (GUI.Button(new Rect(box.x + box.width * 0.5f - 80f, box.y + box.height - 56f, 160f, 42f), "Tamam", UiTheme.BtnGreen)) CloseBattle();
        }

        private static void Line(Rect box, ref float y, string text)
        {
            GUI.Label(new Rect(box.x + 24f, y, box.width - 48f, 28f), text, new GUIStyle(UiTheme.Skin.label) { normal = { textColor = new Color(0.25f, 0.15f, 0.07f) } });
            y += 28f;
        }
    }
}
