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

        private void Panel(Rect r, string title = null)
        {
            uiRects.Add(r);
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.Box(r, "");
            GUI.color = Color.white;
            if (title != null) GUI.Label(new Rect(r.x + 8, r.y + 4, r.width - 16, 24), title);
        }

        private void OnGUI()
        {
            if (g == null || view == null) return;
            uiScale = Mathf.Max(1f, Screen.height / 600f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
            GUI.skin.button.fontSize = 14;
            GUI.skin.label.fontSize = 14;
            GUI.skin.box.fontSize = 14;
            GUI.skin.label.wordWrap = true;
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
        private void DrawTopBar(float w)
        {
            var r = new Rect(0, 0, w, 30);
            uiRects.Add(r);
            var sb = new StringBuilder();
            sb.Append("Sv ").Append(g.Level);
            if (g.Level < ProgressionRules.MaxLevel) sb.Append(" (").Append(g.XpIntoLevel).Append('/').Append(ProgressionRules.XpToNextLevel(g.Level)).Append(" XP)");
            else sb.Append(" (MAKS)");
            foreach (var t in ResourceRules.Unlocked(g.Config, g.Level))
                sb.Append("   ").Append(ResName(t)).Append(' ').Append(g.Wallet.Get(t)).Append('/').Append(g.CapacityOf(t)).Append(" +").Append(g.PerHour(t)).Append("/sa");
            sb.Append("   Ordu ").Append(g.ArmySize).Append('/').Append(g.ArmyCapacity);
            sb.Append("   İşçi ").Append(g.FreeBuilders).Append('/').Append(g.Builders);
            GUI.Box(r, sb.ToString());
        }

        private void DrawTabs(float w, float h)
        {
            float bw = Mathf.Min(120f, (w - 8f) / TabNames.Length - 2f);
            for (int i = 0; i < TabNames.Length; i++)
            {
                bool badge = (i == (int)Tab.Quests && HasClaimable()) || (i == (int)Tab.Village && g.DamagedTowerCount() > 0);
                GUI.color = (int)tab == i ? Color.yellow : (badge ? new Color(0.6f, 1f, 0.6f) : Color.white);
                if (Btn(new Rect(4 + i * (bw + 2), h - 36, bw, 32), TabNames[i] + (badge ? " !" : ""))) { tab = (Tab)i; scrollPos = Vector2.zero; }
            }
            GUI.color = Color.white;
        }

        private bool HasClaimable()
        {
            foreach (var q in g.Quests.Visible(g.Level)) if (g.Quests.IsComplete(q)) return true;
            return false;
        }

        private void DrawPopup(float w, float h)
        {
            var box = new Rect(w * 0.5f - 230f, h * 0.5f - 120f, 460f, 240f);
            uiRects.Add(new Rect(0, 0, w, h));
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.Box(new Rect(0, 0, w, h), "");
            GUI.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            GUI.Box(box, "");
            GUI.color = Color.white;
            GUI.Label(new Rect(box.x + 14, box.y + 10, box.width - 28, box.height - 60), popups[0]);
            if (GUI.Button(new Rect(box.x + box.width * 0.5f - 60, box.y + box.height - 46, 120, 34), popups.Count > 1 ? "Tamam (" + (popups.Count - 1) + " daha)" : "Tamam")) popups.RemoveAt(0);
        }

        private void DrawStatusLine(float w, float h)
        {
            var rs = new Rect(4, h - 72, w - 8, 32);
            string msg = Time.time < toastUntil ? toast : (valid ? "Savunma GEÇERLİ" : "GEÇERSİZ: " + validationError);
            msg += "   [Yol " + layout.RoadCount + "/" + layout.RoadLimit + " | Giriş " + layout.EntranceCount + " | Kule " + g.Towers.Count + "/" + GameRules.MaxTowers(g.Level) + "]";
            GUI.color = Time.time < toastUntil ? Color.white : (valid ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.8f, 0.5f));
            GUI.Box(rs, msg);
            GUI.color = Color.white;
            uiRects.Add(rs);
        }

        // ---------- Köy ----------
        private void DrawVillage(float w, float h)
        {
            float bw = Mathf.Min(78f, (w - 8f) / ToolNames.Length - 2f);
            for (int i = 0; i < ToolNames.Length; i++)
            {
                GUI.color = (int)tool == i ? Color.yellow : Color.white;
                if (Btn(new Rect(4 + i * (bw + 2), 34, bw, 32), ToolNames[i])) { tool = (Tool)i; if (tool != Tool.Select) selCell = new GridPos(-1, -1); dirty = true; }
            }
            GUI.color = Color.white;

            float y = 70;
            if (tool == Tool.Building)
            {
                var bt = selBuilding; var bd = ContentCatalog.Building(bt);
                string txt = "Bina: " + bd.Name + (g.Level < bd.UnlockLevel ? " (Sv " + bd.UnlockLevel + ")" : "") + "  " + GameRules.Scale(GameRules.BuildCostOf(bt), g.Config, g.Level, 100);
                if (Btn(new Rect(4, y, 420, 30), txt)) selBuilding = (BuildingType)(((int)selBuilding + 1) % 11);
            }
            else if (tool == Tool.Tower)
            {
                var td = ContentCatalog.Tower(selTower);
                string txt = "Kule: " + td.Name + (g.Level < td.UnlockLevel ? " (Sv " + td.UnlockLevel + ")" : "") + "  " + GameRules.Scale(td.BuildCost, g.Config, g.Level, 100);
                if (Btn(new Rect(4, y, 420, 30), txt)) selTower = (TowerType)(((int)selTower + 1) % 4);
            }
            else if (tool == Tool.Trap)
            {
                if (Btn(new Rect(4, y, 220, 30), "Tuzak: " + TrapName(selTrap))) selTrap = (TrapType)(((int)selTrap % 3) + 1);
            }
            if (g.DamagedTowerCount() > 0)
            {
                string e;
                if (Btn(new Rect(w - 250, 34, 246, 32), "Tüm kuleleri onar (" + g.DamagedTowerCount() + ")")) { if (g.TryRepairAll(out e)) { Toast("Kuleler onarıldı"); dirty = true; } else Toast(e); }
            }

            if (tool == Tool.Select && layout.InBounds(selCell)) DrawSelectPanel(w, h);

            // Son baskın raporu
            if (g.LastRaid != null)
            {
                var rr = g.LastRaid;
                string t = "Son baskın: %" + rr.Result.OverallDamagePct + " hasar, " + (rr.Result.Victory ? "saldırgan kazandı" : "savundun") + ", kayıp: " + rr.LootLost + ", hasarlı kule: " + rr.TowersDamaged;
                var r = new Rect(4, h - 106, w - 8, 30);
                GUI.Box(r, t); uiRects.Add(r);
            }
            DrawStatusLine(w, h);
        }

        private void DrawSelectPanel(float w, float h)
        {
            var r = new Rect(w - 320, 70, 316, 260);
            Panel(r);
            float x = r.x + 8, y = r.y + 6, iw = r.width - 16;
            var p = selCell;
            if (g.Buildings.TryGetValue(p, out var b))
            {
                var d = ContentCatalog.Building(b.Type);
                GUI.Label(new Rect(x, y, iw, 24), d.Name + "  Sv " + b.Level); y += 24;
                int hp = CombatMods.Apply(ContentCatalog.Scaled(d.Hp, b.Level), g.Research.Mods().BuildingHp);
                GUI.Label(new Rect(x, y, iw, 24), "Can: " + hp); y += 22;
                var prod = GameRules.ProducedBy(b.Type);
                if (prod.HasValue) { GUI.Label(new Rect(x, y, iw, 24), "Üretim: " + (int)((long)GameRules.BasePerHour(prod.Value) * ContentCatalog.ScalePct(b.Level) / 100) + " " + ResName(prod.Value) + "/sa"); y += 22; }
                if (d.IsDepot) { GUI.Label(new Rect(x, y, iw, 24), "Depo: +" + GameRules.DepotCapacity(d.Stores, b.Level) + " " + ResName(d.Stores)); y += 22; }
                if (b.Type == BuildingType.Barracks) { GUI.Label(new Rect(x, y, iw, 24), "Ordu kapasitesi: +" + 6 * b.Level); y += 22; }
                if (b.Type == BuildingType.University) { GUI.Label(new Rect(x, y, iw, 24), "Araştırma Sv " + b.Level + "'e kadar"); y += 22; }
                if (b.Type == BuildingType.VillagerHouse) { GUI.Label(new Rect(x, y, iw, 24), "+1 işçi (en çok 5)"); y += 22; }
                y = DrawUpgradeRow(x, y, iw, JobKind.Building, p, SoldierType.Swordsman);
            }
            else if (g.Towers.TryGetValue(p, out var t))
            {
                var d = ContentCatalog.Tower(t.Type);
                GUI.Label(new Rect(x, y, iw, 24), d.Name + "  Sv " + t.Level); y += 24;
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
            if (GUI.Button(new Rect(x, y, iw, 30), "Yükselt")) { if (g.TryStartUpgrade(kind, p, s, out err)) { Toast("Yükseltme başladı"); } else Toast(err); }
            uiRects.Add(new Rect(x, y, iw, 30));
            return y + 34;
        }

        // ---------- Kışla ----------
        private void DrawBarracks(float w, float h)
        {
            var r = new Rect(6, 34, w - 12, h - 76);
            Panel(r, "KIŞLA  -  Ordu " + g.ArmySize + "/" + g.ArmyCapacity + (g.BarracksLevelSum() == 0 ? "   (Önce Köy sekmesinden Kışla kur!)" : ""));
            GUILayout.BeginArea(new Rect(r.x + 6, r.y + 30, r.width - 12, r.height - 36));
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
            GUILayout.Label("EĞİTİM SIRASI (" + g.Queue.Items.Count + ")");
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
            GUILayout.Label("YÜKSELTMELER (" + g.Jobs.Count + "/" + g.Builders + " işçi)");
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
            var r = new Rect(6, 34, w - 12, h - 76);
            Panel(r);
            float bw = Mathf.Min(110f, (r.width - 16f) / TreeNames.Length - 2f);
            for (int i = 0; i < TreeNames.Length; i++)
            {
                GUI.color = researchTree == i ? Color.yellow : Color.white;
                if (Btn(new Rect(r.x + 6 + i * (bw + 2), r.y + 4, bw, 28), TreeNames[i])) { researchTree = i; scrollPos = Vector2.zero; }
            }
            GUI.color = Color.white;

            string head = "Üniversite Sv " + g.UniversityLevel + (g.Level < ResearchState.UniversityUnlockLevel ? " (Sv5'te açılır, sonra Üniversite kur)" : "") + "   Yuva " + g.Research.Active.Count + "/" + g.Research.Slots;
            foreach (var a in g.Research.Active) head += "   | " + ResearchCatalog.Get(a.Id).Name + " " + Dur(a.RemainingSeconds);
            GUI.Label(new Rect(r.x + 8, r.y + 36, r.width - 16, 24), head);

            GUILayout.BeginArea(new Rect(r.x + 6, r.y + 62, r.width - 12, r.height - 160));
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
                    GUI.color = selResearch == d.Id ? Color.yellow : (g.Research.IsDone(d.Id) ? new Color(0.6f, 1f, 0.6f) : Color.white);
                    if (GUILayout.Button(st + " " + d.Name, GUILayout.Width(250), GUILayout.Height(38))) selResearch = d.Id;
                    GUI.color = Color.white;
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
                if (Btn(new Rect(dr.x + dr.width - 160, dr.y + 24, 150, 36), "BAŞLAT")) { if (g.TryResearch(sel.Id, out err)) { AddLog("Araştırma başladı: " + sel.Name); } else Toast(err); }
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
            var r = new Rect(6, 34, w - 12, h - 76);
            Panel(r, "GÖREVLER");
            GUILayout.BeginArea(new Rect(r.x + 6, r.y + 30, r.width - 12, r.height - 36));
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            var list = g.Quests.Visible(g.Level);
            string claimId = null;
            for (int pass = 0; pass < 2; pass++)
            {
                GUILayout.Label(pass == 0 ? "— GÜNLÜK —" : "— KİLOMETRE TAŞLARI —");
                foreach (var q in list)
                {
                    if ((q.Kind == QuestKind.Daily) != (pass == 0)) continue;
                    bool done = g.Quests.IsComplete(q);
                    GUILayout.BeginHorizontal(GUILayout.Height(34));
                    GUILayout.Label(q.Name + "   " + g.Quests.ProgressOf(q.Id) + "/" + q.Target + "\nÖdül: " + QuestCatalog.RewardFor(q, g.Config, g.Level) + " + " + q.Xp + " XP", GUILayout.Width(w - 220));
                    GUI.enabled = done;
                    if (GUILayout.Button("AL", GUILayout.Width(90), GUILayout.Height(34))) claimId = q.Id;
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
            var r = new Rect(6, 34, w - 12, h - 76);
            Panel(r, "SALDIRI HEDEFLERİ   (Ordun: " + (g.ArmySize - g.Queue.Items.Count) + " asker hazır)");
            string[] dn = { "KOLAY", "NORMAL", "ZOR" };
            float cw = (r.width - 40f) / 3f;
            for (int d = 0; d < 3; d++)
            {
                var t = targets[d];
                var cr = new Rect(r.x + 10 + d * (cw + 10), r.y + 36, cw, 230);
                GUI.Box(cr, "");
                GUI.Label(new Rect(cr.x + 8, cr.y + 6, cr.width - 16, 200),
                    dn[d] + "\n" + t.Name + "  (Sv " + t.Level + ")\nKule: " + t.Defense.Towers.Count + "   Bina: " + t.Defense.Buildings.Count + "\nGiriş: " + t.Defense.Layout.EntranceCount + "   Tuzak: " + t.Defense.Traps.Count +
                    "\nYağma havuzu: " + t.Pool + "\nXP çarpanı: x" + ((100 + 12 * (t.Level - 1)) / 100f).ToString("0.00"));
                if (Btn(new Rect(cr.x + 8, cr.y + 180, cr.width - 16, 40), "SALDIR")) StartBattle(t);
            }
            if (Btn(new Rect(r.x + 10, r.y + 276, 160, 32), "Hedefleri yenile")) { targetSeed++; targets = null; }
            GUI.Label(new Rect(r.x + 10, r.y + 316, r.width - 20, 120), "İpucu: askerleri kırmızı alana dokunarak (basılı tutarak) gönder. Mızraklı kuleler atlıya, Elf kuleleri periye güçlüdür; Büyücü kalabalığa, Goblin kaynak binalarına etkilidir. Zafer için yapıların ortalama hasarı %51'i geçmeli.");
        }

        // ---------- Menü ----------
        private void DrawMenu(float w, float h)
        {
            var r = new Rect(6, 34, w - 12, h - 76);
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
            GUI.color = confirmNew ? new Color(1f, 0.5f, 0.5f) : Color.white;
            if (Btn(new Rect(x, y, 260, 34), confirmNew ? "EMİN MİSİN? Tekrar bas: tüm ilerleme silinir" : "Yeni oyun (sil)")) { if (confirmNew) { confirmNew = false; NewGameConfirmed(); } else confirmNew = true; }
            GUI.color = Color.white;
            y += 48;
            var sb = new StringBuilder("Son olaylar:\n");
            foreach (var l in log) sb.Append("• ").Append(l).Append('\n');
            GUI.Label(new Rect(r.x + 10, y, r.width - 20, 160), sb.ToString());
        }

        // ---------- Savaş arayüzü ----------
        private void DrawBattleGui(float w, float h)
        {
            int n = ContentCatalog.SoldierTypeCount;
            float bw = Mathf.Min(130f, (w - 8f) / n - 2f);
            for (int i = 0; i < n; i++)
            {
                var t = (SoldierType)i;
                GUI.color = t == selSoldier ? Color.yellow : (sim.StockOf(t) > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f));
                if (Btn(new Rect(4 + i * (bw + 2), 34, bw, 36), ContentCatalog.Soldier(t).Name + " " + sim.StockOf(t))) selSoldier = t;
            }
            GUI.color = Color.white;
            if (!battleOver)
            {
                if (Btn(new Rect(4, 74, 110, 32), "Geri Çek") && !sim.Finished) sim.Retreat();
                if (Btn(new Rect(118, 74, 90, 32), "Hız " + speed + "x")) speed = speed == 1 ? 3 : (speed == 3 ? 6 : 1);
            }
            int aliveSoldiers = 0;
            foreach (var s in sim.Soldiers) if (s.Alive) aliveSoldiers++;
            var rs = new Rect(4, h - 40, w - 8, 36);
            string msg = Time.time < toastUntil ? toast : curTarget.Name + " (Sv " + curTarget.Level + ")  |  " + (sim.Tick / 10) + " sn / 180  |  Hasar %" + LiveDamagePct() + " (zafer %51)  |  Sahada " + aliveSoldiers + "   [kırmızı alana dokun/basılı tut]";
            GUI.Box(rs, msg); uiRects.Add(rs);
            if (battleOver && lastResult != null) DrawResult(w, h);
        }

        private void DrawResult(float w, float h)
        {
            var r = lastResult;
            var box = new Rect(w * 0.5f - 200f, h * 0.5f - 150f, 400f, 300f);
            uiRects.Add(box);
            GUI.color = new Color(0f, 0f, 0f, 0.9f);
            GUI.Box(box, "");
            GUI.color = Color.white;
            float y = box.y + 8f;
            Line(box, ref y, r.Victory ? "ZAFER  -  yıldız " + r.Stars + "/3" : "YENİLGİ  -  yıldız 0/3");
            Line(box, ref y, "Genel hasar: %" + r.OverallDamagePct + "   XP: +" + (lastOutcome != null ? lastOutcome.XpGained : r.Xp));
            Line(box, ref y, "Yağma: " + r.Loot);
            Line(box, ref y, "Ölü " + BattleResult.Total(r.LostDead) + "  Yaralı " + BattleResult.Total(r.LostInjured) + "  Feda " + BattleResult.Total(r.LostSacrificed));
            Line(box, ref y, "Dönen asker: " + BattleResult.Total(r.Returned));
            if (lastOutcome != null && lastOutcome.LevelUps.Count > 0) Line(box, ref y, "SEVİYE ATLADIN → Sv " + g.Level);
            if (GUI.Button(new Rect(box.x + 130f, box.y + box.height - 50f, 140f, 38f), "Tamam")) CloseBattle();
        }

        private static void Line(Rect box, ref float y, string text)
        {
            GUI.Label(new Rect(box.x + 14f, y, box.width - 28f, 28f), text);
            y += 30f;
        }
    }
}
