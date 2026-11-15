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
        private static readonly string[] TreeNames = { "Ekonomi", "Savunma", "Askerî", "Mimarlık", "Köy Gel.", "Bilim", "Element" };
        private Vector2 scrollPos;
        private int researchTree;
        private string selResearch;
        private TargetInfo[] targets;
        private int targetSeed = 1, targetsLevel = -1;
        private bool confirmNew;
        private int detailSoldier = -1;

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

        /// <summary>Büyük yuvarlak ana ekran düğmesi (renkli daire + yazı); badge = kırmızı ünlem.</summary>
        private bool RoundBtn(Rect r, string label, Color c, bool badge = false)
        {
            uiRects.Add(r);
            UiTheme.Dot(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(0.12f, 0.08f, 0.04f, 0.9f));
            UiTheme.Dot(r, c);
            UiTheme.Dot(new Rect(r.x + r.width * 0.18f, r.y + r.height * 0.08f, r.width * 0.64f, r.height * 0.4f), new Color(1f, 1f, 1f, 0.18f));
            var st = new GUIStyle(UiTheme.Header) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Clamp((int)(r.width * 0.2f), 12, 22), normal = { textColor = Color.white } };
            GUI.Label(new Rect(r.x + 1, r.y + 2, r.width, r.height), "<color=#00000088>" + label + "</color>", st);
            GUI.Label(r, label, st);
            if (badge) { UiTheme.Dot(new Rect(r.xMax - 20, r.y - 2, 22, 22), new Color(0.95f, 0.2f, 0.15f)); GUI.Label(new Rect(r.xMax - 20, r.y - 3, 22, 22), "!", UiTheme.Center); }
            // Tıklamayı doğrudan olaydan oku (görünmez stilli GUI.Button bazı durumlarda tıklama almıyor)
            // Basıldığı anda tetiklenir (mobil hissi; bırakma olayına bağlı kalmaz)
            var e = Event.current;
            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition)) { e.Use(); return true; }
            return false;
        }

        /// <summary>Basıldığı anda tetiklenen düğme (GUI.Button bazı durumlarda bırakma olayını kaçırıyor; RoundBtn ile aynı yöntem).</summary>
        private bool PressBtn(Rect r, string text, GUIStyle style)
        {
            uiRects.Add(r);
            GUI.Label(r, text, style);
            var e = Event.current;
            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition)) { e.Use(); return true; }
            return false;
        }

        private void Panel(Rect r, string title = null)
        {
            uiRects.Add(r);
            GUI.Box(r, "");
            if (title != null) GUI.Label(new Rect(r.x + 14, r.y + 6, r.width - 28, 26), title, UiTheme.Header);
        }

        private static Rect ModalRect(float w, float h) => new Rect(Mathf.Max(8f, w * 0.5f - 480f), 70f, Mathf.Min(w - 16f, 960f), h - 84f);

        /// <summary>Ortada pencere: altın başlık şeridi + kapat (X). Kapatınca köye dönülür.</summary>
        private void Modal(Rect r, string title)
        {
            uiRects.Add(new Rect(0, 0, 10000, 10000));
            GUI.DrawTexture(new Rect(0, 0, 10000, 10000), UiTheme.White, ScaleMode.StretchToFill, true, 0f, new Color(0f, 0f, 0f, 0.35f), 0f, 0f);
            GUI.Box(r, "");
            float tw = Mathf.Clamp(UiTheme.TabOn.CalcSize(new GUIContent(title)).x + 60f, 220f, r.width - 120f);
            GUI.Label(new Rect(r.center.x - tw * 0.5f, r.y - 22f, tw, 42f), title, UiTheme.TabOn);
            if (PressBtn(new Rect(r.xMax - 50f, r.y - 14f, 44f, 44f), "X", UiTheme.BtnRed)) { tab = Tab.Village; selCell = new GridPos(-1, -1); dirty = true; }
        }

        private void OnGUI()
        {
            if (g == null || view == null) return;
            UiTheme.Ensure();
            GUI.skin = UiTheme.Skin;
            uiScale = Mathf.Clamp(Screen.height / 640f, 1f, 3f); // büyük, dokunmaya uygun düğmeler
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
            uiRects.Clear();
            float w = Screen.width / uiScale, h = Screen.height / uiScale;

            if (popups.Count > 0) { DrawHud(w, h, false); DrawPopup(w, h); return; }
            if (sim != null) { DrawHud(w, h, true); DrawBattleGui(w, h); return; }
            if (tab == Tab.Village) DrawJobTimers();
            DrawHud(w, h, false);
            if (LayoutMode && tab == Tab.Village) { DrawLayoutMode(w, h); return; }
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
                case ResourceType.Wood: return new Color(0.78f, 0.50f, 0.25f);
                case ResourceType.Stone: return new Color(0.72f, 0.74f, 0.78f);
                case ResourceType.Gold: return new Color(1f, 0.82f, 0.25f);
                default: return new Color(0.50f, 0.60f, 0.75f);
            }
        }

        /// <summary>Ana ekran üst bilgisi: sol üst seviye/XP, orta işçi + ordu, sağ üst kaynak çubukları (dolu oranı).</summary>
        private void DrawHud(float w, float h, bool battle)
        {
            // seviye
            var lr = new Rect(8, 8, 214, 52);
            uiRects.Add(lr);
            GUI.Label(lr, "", UiTheme.Chip);
            UiTheme.Dot(new Rect(10, 6, 56, 56), new Color(0.15f, 0.35f, 0.75f));
            UiTheme.Dot(new Rect(14, 10, 48, 48), new Color(0.35f, 0.6f, 1f));
            GUI.Label(new Rect(10, 6, 56, 56), g.Level.ToString(), UiTheme.Title);
            bool max = g.Level >= ProgressionRules.MaxLevel;
            float need = max ? 1 : ProgressionRules.XpToNextLevel(g.Level);
            GUI.Label(new Rect(72, 10, 150, 20), max ? "Seviye MAKS" : "XP " + g.XpIntoLevel + " / " + (int)need, UiTheme.Small);
            UiTheme.Bar(new Rect(72, 32, 140, 16), max ? 1f : g.XpIntoLevel / need, new Color(0.45f, 0.8f, 1f));
            if (battle) return;

            // işçi + ordu (seviyenin altında, küçük)
            var br = new Rect(8, 64, 104, 28);
            GUI.Label(br, "<size=13>İşçi " + g.FreeBuilders + "/" + g.Builders + "</size>", UiTheme.Chip);
            UiTheme.Dot(new Rect(br.x + 5, br.y + 5, 18, 18), new Color(0.45f, 0.78f, 0.4f));
            var ar = new Rect(116, 64, 106, 28);
            GUI.Label(ar, "<size=13>Ordu " + g.ArmySize + "/" + g.ArmyCapacity + "</size>", UiTheme.Chip);
            UiTheme.Dot(new Rect(ar.x + 5, ar.y + 5, 18, 18), new Color(0.88f, 0.3f, 0.25f));
            uiRects.Add(br); uiRects.Add(ar);

            // kaynaklar: sağ üstte küçük yatay şerit, her birinin simgesi var
            var res = ResourceRules.Unlocked(g.Config, g.Level);
            float cw = 138f, x = w - 8f - cw * res.Count;
            foreach (var t in res)
            {
                int have = g.Wallet.Get(t), cap = g.CapacityOf(t);
                var rr = new Rect(x, 8f, cw - 6f, 34f);
                uiRects.Add(rr);
                GUI.Label(rr, "", UiTheme.Chip);
                GUI.DrawTexture(new Rect(rr.x + 3f, rr.y + 3f, 28f, 28f), UiTheme.ResIcon((int)t));
                GUI.Label(new Rect(rr.x + 34f, rr.y + 1f, rr.width - 38f, 18f), "<size=13><b>" + Short(have) + "</b><color=#d8c9a8>/" + Short(cap) + "</color></size>", UiTheme.Small);
                UiTheme.Bar(new Rect(rr.x + 34f, rr.y + 21f, rr.width - 42f, 8f), cap > 0 ? (float)have / cap : 0f, ResColor(t));
                x += cw;
            }
        }

        private static string Short(int v) => v >= 100000 ? (v / 1000) + "b" : v >= 10000 ? (v / 1000f).ToString("0.0") + "b" : v.ToString();

        private bool HasClaimable()
        {
            foreach (var q in g.Quests.Visible(g.Level)) if (g.Quests.IsComplete(q)) return true;
            if (g.CountBuildings(BuildingType.University) > 0)
                foreach (var t in AcademyTasks.All) if (!g.AcademyClaimed.Contains(t.Id) && g.AcademyTaskComplete(t)) return true;
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

        // ---------- Köy (ana ekran) ----------
        private void DrawVillage(float w, float h)
        {
            // sol alt: SALDIR
            if (RoundBtn(new Rect(16, h - 132, 116, 116), "SALDIR", new Color(0.85f, 0.25f, 0.15f))) { tab = Tab.Attack; tool = Tool.Select; selCell = new GridPos(-1, -1); }
            // sağ alt: KÖY DÜZENİ (yapı kurmanın tek yolu) + Görev + Ayar
            if (RoundBtn(new Rect(w - 132, h - 132, 116, 116), "KÖY\nDÜZENİ", new Color(0.95f, 0.68f, 0.15f))) EnterLayout();
            if (RoundBtn(new Rect(w - 104, h - 212, 70, 70), "Görev", new Color(0.35f, 0.62f, 0.3f), HasClaimable())) { tab = Tab.Quests; scrollPos = Vector2.zero; }
            if (RoundBtn(new Rect(w - 214, h - 84, 68, 68), "Ayar", new Color(0.45f, 0.45f, 0.5f))) tab = Tab.Menu;

            if (g.DamagedTowerCount() > 0)
            {
                string e;
                if (Btn(new Rect(16, h - 182, 200, 40), "Kuleleri onar (" + g.DamagedTowerCount() + ")")) { if (g.TryRepairAll(out e)) { Toast("Kuleler onarıldı"); dirty = true; } else Toast(e); }
            }
            if (tool == Tool.Select && layout.InBounds(selCell)) DrawSelectPanel(w, h);

            // son baskın (sol, işçi/ordunun altında)
            if (g.LastRaid != null)
            {
                var rr = g.LastRaid;
                var r = new Rect(8, 98, 214, 32);
                GUI.Label(r, "<size=12>Son baskın: %" + rr.Result.OverallDamagePct + " " + (rr.Result.Victory ? "kaybettin" : "savundun") + "</size>", rr.Result.Victory ? UiTheme.PillBad : UiTheme.Pill);
                uiRects.Add(r);
            }
            DrawStatusLine(w, h);
        }

        /// <summary>Yükseltilen yapıların üstünde kalan süre (dünya → ekran).</summary>
        private void DrawJobTimers()
        {
            if (cam == null) return;
            foreach (var j in g.Jobs)
            {
                if (j.Kind == JobKind.Soldier) continue;
                var wp = new Vector3(j.Pos.X, j.Kind == JobKind.Road ? 0.6f : 1.55f, j.Pos.Y);
                var sp = cam.WorldToScreenPoint(wp);
                if (sp.z < 0f) continue;
                var gp = new Vector2(sp.x / uiScale, (Screen.height - sp.y) / uiScale);
                string txt = "Sv " + (j.Kind == JobKind.Road ? RoadRules.Name((RoadTier)j.TargetLevel) : j.TargetLevel.ToString()) + "  " + Dur(j.RemainingSeconds);
                var st = new GUIStyle(UiTheme.Pill) { fontSize = 12 };
                float tw = st.CalcSize(new GUIContent(txt)).x + 24f;
                GUI.Label(new Rect(gp.x - tw * 0.5f, gp.y - 14f, tw, 24f), txt, st);
            }
        }

        /// <summary>Durum: yalnızca bildirim veya savunma geçersizken görünür (ana ekran sade kalsın).</summary>
        private void DrawStatusLine(float w, float h)
        {
            bool showToast = Time.time < toastUntil;
            if (!showToast && valid) return;
            string msg = showToast ? toast : validationError + "  — KÖY DÜZENİ'nden düzelt";
            float tw = Mathf.Min(w - 520f, 620f);
            if (tw < 300f) tw = w - 20f;
            var rs = new Rect((w - tw) * 0.5f, 52f, tw, 34);
            GUI.Label(rs, msg, showToast || valid ? UiTheme.Pill : UiTheme.PillBad);
            uiRects.Add(rs);
        }

        private static string TowerRole(TowerType t)
        {
            switch (t)
            {
                case TowerType.Archer: return "Hızlı, havayı da vurur";
                case TowerType.Mage: return "Alan hasarı, zırh deler";
                case TowerType.Wall: return "Saldırmaz; askerleri üstüne çeker";
                case TowerType.Elf: return "Uçanlara çok güçlü";
                case TowerType.Cannon: return "Büyük alan hasarı, hava yok";
                case TowerType.Ice: return "Vurduğunu yavaşlatır";
                default: return "Zincirleme şimşek (3 hedef)";
            }
        }

        // ---------- Seçili yapı (alt orta kart) ----------
        private void DrawSelectPanel(float w, float h)
        {
            var r = new Rect(w * 0.5f - 230f, h - 196f, 460f, 184f);
            Panel(r);
            var xr = new Rect(r.xMax - 40f, r.y - 12f, 40f, 40f);
            uiRects.Add(xr);
            if (PressBtn(xr, "X", UiTheme.BtnRed)) { selCell = new GridPos(-1, -1); dirty = true; return; }
            float x = r.x + 14, y = r.y + 10, iw = r.width - 28;
            var p = selCell;
            if (g.Buildings.TryGetValue(p, out var b))
            {
                var d = ContentCatalog.Building(b.Type);
                GUI.Label(new Rect(x, y, iw, 26), d.Name + "  <size=14>Sv " + b.Level + "</size>", UiTheme.Header); y += 28;
                int hp = CombatMods.Apply(ContentCatalog.Scaled(d.Hp, b.Level), g.Research.Mods().BuildingHp);
                string info = "Can " + hp;
                var prod = GameRules.ProducedBy(b.Type);
                if (prod.HasValue) info += "   Üretim " + (int)((long)GameRules.BasePerHour(prod.Value) * ContentCatalog.ScalePct(b.Level) / 100) + " " + ResName(prod.Value) + "/sa";
                if (d.IsDepot) info += "   Depo +" + GameRules.DepotCapacity(d.Stores, b.Level) + " " + ResName(d.Stores);
                if (b.Type == BuildingType.Barracks) info += "   Ordu +" + (GameRules.ArmyCapacity(g.BarracksLevelSum()) - GameRules.ArmyCapacity(g.BarracksLevelSum() - b.Level)) + " yer";
                if (b.Type == BuildingType.University) info += "   Araştırma Sv " + b.Level + "'e kadar";
                if (b.Type == BuildingType.VillagerHouse) info += "   +1 işçi";
                GUI.Label(new Rect(x, y, iw, 22), "<size=13>" + info + "</size>"); y += 26;
                DrawUpgradeRow(x, y, iw, JobKind.Building, p, SoldierType.Swordsman);
            }
            else if (g.Towers.TryGetValue(p, out var t))
            {
                var d = ContentCatalog.Tower(t.Type);
                GUI.Label(new Rect(x, y, iw, 26), d.Name + "  <size=14>Sv " + t.Level + "</size>", UiTheme.Header); y += 28;
                var m = g.Research.Mods();
                int hp = CombatMods.Apply(ContentCatalog.Scaled(d.Hp, t.Level), m.TowerHp);
                int atk = CombatMods.Apply(ContentCatalog.Scaled(d.Attack, t.Level), m.TowerDamage);
                string info = d.Attack <= 0 ? "Can " + hp + "   " + TowerRole(t.Type) : "Can " + hp + "   Hasar " + atk + "   Menzil " + (d.RangeMilli / 1000f).ToString("0.0") + "   " + TowerRole(t.Type);
                if (g.Wears.TryGetValue(p, out var wr) && wr.PermanentDamagePct > 0)
                {
                    info += "   <color=#ffb070>Hasar %" + wr.PermanentDamagePct + (wr.IsDisabled ? " (DEVRE DIŞI)" : "") + "</color>";
                    string e;
                    var rb = new Rect(r.xMax - 190, r.y + 8, 136, 30);
                    uiRects.Add(rb);
                    if (GUI.Button(rb, "Onar", UiTheme.BtnRed)) { if (g.TryRepair(p, out e)) { Toast("Onarıldı"); dirty = true; } else Toast(e); }
                }
                GUI.Label(new Rect(x, y, iw, 22), "<size=13>" + info + "</size>"); y += 26;
                DrawUpgradeRow(x, y, iw, JobKind.Tower, p, SoldierType.Swordsman);
            }
            else if (layout.Get(p) == CellKind.Road)
            {
                var rt = layout.GetRoadTier(p);
                string tierName = RoadRules.Name(rt) + "   <size=12>yavaşlatma %" + RoadRules.SlowPct(rt) + ", tuzak hasarı %" + RoadRules.TrapBonusPct(rt) + "</size>";
                GUI.Label(new Rect(x, y, iw, 26), "Yol  <size=14>" + tierName + "</size>", UiTheme.Header); y += 30;
                GUI.Label(new Rect(x, y, iw - 170, 24), (layout.IsEntrance(p) ? "GİRİŞ   " : "") + (g.Traps.TryGetValue(p, out var tr) ? TrapName(tr) + " tuzak" : ""));
                y += 24;
                string info;
                if (!g.RoadResearchDone) info = g.RoadResearchRemaining > 0 ? "Yol araştırması sürüyor: " + Dur(g.RoadResearchRemaining) : "Geliştirmek için Akademi'de yol araştırması gerekli";
                else if (RoadRules.IsMax(rt)) info = "En üst basamak (Kristal)";
                else if (g.IsUpgrading(JobKind.Road, p, SoldierType.Swordsman)) info = "Geliştiriliyor";
                else { var nx = RoadRules.Next(rt); info = RoadRules.Name(nx) + " (Sv " + RoadRules.MinLevel(nx) + "): " + RoadRules.UpgradeCost(nx) + "  " + Dur(RoadRules.UpgradeSeconds(nx)); }
                GUI.Label(new Rect(x, y, iw - 170, 60), "<size=13>" + info + "</size>");
                var ub = new Rect(x + iw - 160, y, 160, 50);
                uiRects.Add(ub);
                string err;
                if (GUI.Button(ub, "Geliştir", UiTheme.BtnGreen)) { if (g.TryUpgradeRoad(p, out err)) { Toast("Yol geliştirme başladı"); dirty = true; } else Toast(err); }
            }
            else GUI.Label(new Rect(x, y, iw, 48), "Boş alan — yapı kurmak için KÖY DÜZENİ'ne gir");
        }

        private float DrawUpgradeRow(float x, float y, float iw, JobKind kind, GridPos p, SoldierType s)
        {
            var info = g.PreviewUpgrade(kind, p, s);
            if (info.Block != null && info.TargetLevel == 0) { GUI.Label(new Rect(x, y, iw, 24), info.Block); return y + 24; }
            GUI.Label(new Rect(x, y, iw - 170, 60), "<size=13>Sv " + info.TargetLevel + ": " + info.Cost + "\n" + Dur(info.Seconds) + (info.Block != null ? "\n<color=#ffb070>" + info.Block + "</color>" : "") + "</size>");
            var rb = new Rect(x + iw - 160, y + 6, 160, 50);
            uiRects.Add(rb);
            string err;
            if (info.Block == null && GUI.Button(rb, "Yükselt", UiTheme.BtnGreen)) { if (g.TryStartUpgrade(kind, p, s, out err)) { Toast("Yükseltme başladı"); } else Toast(err); }
            if (info.Block != null) GUI.Label(rb, "Yükseltilemez", UiTheme.PillBad);
            return y + 60;
        }

        // ---------- Kışla ----------
        private void DrawBarracks(float w, float h)
        {
            var r = ModalRect(w, h);
            Modal(r, "KIŞLA  —  Ordu " + g.ArmySize + "/" + g.ArmyCapacity + " yer");
            var bpos = selCell;
            if (!g.Buildings.TryGetValue(bpos, out var bsel) || bsel.Type != BuildingType.Barracks)
                foreach (var kv in g.Buildings) if (kv.Value.Type == BuildingType.Barracks) { bpos = kv.Key; bsel = kv.Value; break; }
            float top = r.y + 30;
            if (bsel != null && bsel.Type == BuildingType.Barracks)
            {
                GUI.Label(new Rect(r.x + 20, top, 300, 24), "Kışla Sv " + bsel.Level + "   <size=12>(" + g.CountBuildings(BuildingType.Barracks) + "/" + g.MaxCountOf(BuildingType.Barracks) + " kışla; 2. Sv13, 3. Sv26)</size>", UiTheme.Header);
                DrawUpgradeRow(r.x + 20, top + 24, r.width - 40, JobKind.Building, bpos, SoldierType.Swordsman);
                top += 88;
            }
            else GUI.Label(new Rect(r.x + 20, top, r.width - 40, 22), "<color=#ffb070>Önce KÖY DÜZENİ'nden Kışla kur</color>", UiTheme.Small);
            GUILayout.BeginArea(new Rect(r.x + 12, top, r.width - 24, r.yMax - top - 10));
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            string err;
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
            {
                var t = (SoldierType)i; var d = ContentCatalog.Soldier(t);
                int lvl = g.SoldierLevel(t);
                bool locked = g.Level < d.UnlockLevel;
                GUILayout.BeginHorizontal(GUILayout.Height(44));
                if (GUILayout.Button((locked ? "<color=#ffb070>[Sv " + d.UnlockLevel + "'de açılır]</color> " : "") + "<b>" + d.Name + "</b>  <color=#ffd560>" + VillageArt.RankName(lvl) + "</color>  Sv " + lvl + "  adet " + g.Inventory.Get(t) + "  <color=#9fd0ff>" + GameRules.Housing(t) + " yer</color>" + (detailSoldier == i ? "  ▲" : "  ▼") + "\n<size=13>Can " + ContentCatalog.Scaled(d.Hp, lvl) + "  Hasar " + ContentCatalog.Scaled(d.Attack, lvl) + "  Hız " + d.SpeedMilli + "</size>", UiTheme.Skin.label, GUILayout.Width(320)))
                    detailSoldier = detailSoldier == i ? -1 : i;
                GUI.enabled = !locked;
                int ts = Mathf.Max(5, (int)((long)d.TrainSeconds * (1000 - g.Research.Total(ResearchStat.TrainTime)) / 1000));
                if (GUILayout.Button("Eğit\n" + d.Cost + " | " + Dur(ts), GUILayout.Width(210))) { if (g.TryTrain(t, out err)) AddLog(d.Name + " eğitimi sıraya alındı"); else Toast(err); }
                var up = g.PreviewUpgrade(JobKind.Soldier, new GridPos(0, 0), t);
                string vis = VillageArt.LevelVisualName(up.TargetLevel);
                string utxt = (up.Block != null ? "Yükselt: " + up.Block : "Sv " + up.TargetLevel + " yükselt  " + up.Cost + " | " + Dur(up.Seconds)) + (vis.Length > 0 ? "\n<size=12><color=#ffd560>+ " + vis + "</color></size>" : "");
                if (GUILayout.Button(utxt, GUILayout.Width(260))) { if (g.TryStartUpgrade(JobKind.Soldier, new GridPos(0, 0), t, out err)) Toast("Yükseltme başladı"); else Toast(err); }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                if (detailSoldier == i)
                {
                    var sb2 = new StringBuilder();
                    int nx = lvl + 1;
                    sb2.Append("<size=13>Şimdi Sv ").Append(lvl).Append(": Can ").Append(ContentCatalog.Scaled(d.Hp, lvl)).Append("  Hasar ").Append(ContentCatalog.Scaled(d.Attack, lvl))
                       .Append("   →   Sv ").Append(nx).Append(": Can <color=#8fe07a>").Append(ContentCatalog.Scaled(d.Hp, nx)).Append("</color>  Hasar <color=#8fe07a>").Append(ContentCatalog.Scaled(d.Attack, nx)).Append("</color>");
                    string nv = VillageArt.LevelVisualName(nx);
                    if (nv.Length > 0) sb2.Append("   Görsel: <color=#ffd560>").Append(nv).Append("</color>");
                    sb2.Append("\nYetenekler (savaşta otomatik):");
                    foreach (var ab in SoldierAbilities.Of(t))
                        sb2.Append("\n  ").Append(lvl >= ab.Level ? "<color=#8fe07a>✔ " : "<color=#b0a090>Sv " + ab.Level + " ").Append(ab.Name).Append("</color> — ").Append(ab.Desc);
                    sb2.Append("</size>");
                    GUILayout.Label(sb2.ToString(), UiTheme.Pill);
                }
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
            var r = ModalRect(w, h);
            Modal(r, "AKADEMİ");
            var upos = selCell;
            if (!g.Buildings.TryGetValue(upos, out var usel) || usel.Type != BuildingType.University)
                foreach (var kv in g.Buildings) if (kv.Value.Type == BuildingType.University) { upos = kv.Key; usel = kv.Value; break; }
            if (usel != null && usel.Type == BuildingType.University)
            {
                var info = g.PreviewUpgrade(JobKind.Building, upos, SoldierType.Swordsman);
                var ub = new Rect(r.xMax - 330, r.y - 8, 220, 34);
                uiRects.Add(ub);
                string ue;
                if (GUI.Button(ub, "<size=12>" + (info.Block ?? "Akademi Sv " + info.TargetLevel + "  " + info.Cost + " | " + Dur(info.Seconds)) + "</size>", info.Block == null ? UiTheme.BtnGreen : GUI.skin.button) && info.Block == null)
                { if (g.TryStartUpgrade(JobKind.Building, upos, SoldierType.Swordsman, out ue)) Toast("Akademi yükseltmesi başladı"); else Toast(ue); }
            }
            float bw = Mathf.Min(110f, (r.width - 16f) / TreeNames.Length - 2f);
            for (int i = 0; i < TreeNames.Length; i++)
            {
                if (SelBtn(new Rect(r.x + 8 + i * (bw + 2), r.y + 30, bw, 32), TreeNames[i], researchTree == i)) { researchTree = i; scrollPos = Vector2.zero; }
            }

            string head = "Akademi Sv " + g.UniversityLevel + (g.Level < ResearchState.UniversityUnlockLevel ? " (Sv5'te açılır, sonra Akademi kur)" : "") + "   Yuva " + g.Research.Active.Count + "/" + g.Research.Slots;
            foreach (var a in g.Research.Active) head += "   | " + ResearchCatalog.Get(a.Id).Name + " " + Dur(a.RemainingSeconds);
            GUI.Label(new Rect(r.x + 12, r.y + 68, r.width - 700, 24), head, UiTheme.Small);
            // yol geliştirme araştırması (approved #22)
            string rtxt = g.RoadResearchDone ? "Yol geliştirme: AÇIK" : (g.RoadResearchRemaining > 0 ? "Yol araştırması: " + Dur(g.RoadResearchRemaining) : "Yol geliştirme araştır  " + RoadRules.ResearchCost);
            var rrb = new Rect(r.xMax - 340, r.y + 66, 326, 30);
            uiRects.Add(rrb);
            if (GUI.Button(rrb, "<size=13>" + rtxt + "</size>", g.RoadResearchDone ? UiTheme.BtnGreen : GUI.skin.button) && !g.RoadResearchDone)
            { string re; if (g.TryStartRoadResearch(out re)) Toast("Yol geliştirme araştırması başladı (" + Dur(RoadRules.ResearchDurationSeconds) + ")"); else Toast(re); }
            string ttxt = g.TrapResearchDone ? "Tuzak Ustalığı: 6 tuzak hakkı" : (g.TrapResearchRemaining > 0 ? "Tuzak Ustalığı: " + Dur(g.TrapResearchRemaining) : "Tuzak Ustalığı araştır (4→6)  " + GameRules.TrapResearchCost);
            var trb = new Rect(r.xMax - 680, r.y + 66, 330, 30);
            uiRects.Add(trb);
            if (GUI.Button(trb, "<size=13>" + ttxt + "</size>", g.TrapResearchDone ? UiTheme.BtnGreen : GUI.skin.button) && !g.TrapResearchDone)
            { string te; if (g.TryStartTrapResearch(out te)) Toast("Tuzak Ustalığı başladı (" + Dur(GameRules.TrapResearchSeconds) + ")"); else Toast(te); }

            GUILayout.BeginArea(new Rect(r.x + 8, r.y + 100, r.width - 16, r.height - 202));
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
            var r = ModalRect(w, h);
            Modal(r, "GÖREVLER");
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 48, r.width - 24, r.height - 58));
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            var list = g.Quests.Visible(g.Level);
            string claimId = null, academyId = null;
            GUILayout.Label("AKADEMİ GÖREVLERİ  <size=13><color=#d8c9a8>(yapı hakkı kazandırır" + (g.CountBuildings(BuildingType.University) == 0 ? " — önce Akademi kur" : "") + ")</color></size>", UiTheme.Header);
            foreach (var at in AcademyTasks.All)
            {
                bool claimed = g.AcademyClaimed.Contains(at.Id), ok = g.AcademyTaskComplete(at);
                GUILayout.BeginHorizontal(GUILayout.Height(36));
                GUILayout.Label((claimed ? "<color=#8fe07a>✔ </color>" : "") + at.Name + "   " + System.Math.Min(at.Progress(g), at.Target) + "/" + at.Target + "\n<size=13><color=#ffd560>Ödül: " + at.RewardText + "</color></size>", GUILayout.Width(w - 260));
                GUI.enabled = !claimed && ok;
                if (GUILayout.Button(claimed ? "Alındı" : "AL", !claimed && ok ? UiTheme.BtnGreen : GUI.skin.button, GUILayout.Width(100), GUILayout.Height(36))) academyId = at.Id;
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10);
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
            if (academyId != null)
            {
                string aerr;
                if (g.TryClaimAcademy(academyId, out aerr)) { popups.Add("YENİ HAK!\n" + AcademyTasks.Get(academyId).RewardText + "\nKöylü evi: " + g.MaxVillagerHouses + "/" + GameRules.MaxVillagerHouses + "   Kule: " + g.MaxTowers + "/" + GameRules.MaxTowerSlots + "\nYeni yapıyı KÖY DÜZENİ'nden kur."); dirty = true; }
                else Toast(aerr);
            }
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
            var r = ModalRect(w, h);
            Modal(r, "SALDIR  —  " + g.ReadySoldiers + " asker hazır (" + g.ReadyHousing + " yer)");
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
            GUI.Label(new Rect(r.x + 12, r.y + 334, r.width - 24, 120), "<color=#d8c9a8>İpucu: askerleri kırmızı alana dokunarak (basılı tutarak) gönder. Kılıçlı en yakın yapıya saldırır. Duvarlar yer askerlerini üstüne çeker (uçanlar geçer), Elf kulesi periye, Top kalabalığa güçlüdür; Buz yavaşlatır, Şimşek zincirleme vurur. Zafer için yapıların ortalama hasarı %51'i geçmeli.</color>");
        }

        // ---------- Menü ----------
        private void DrawMenu(float w, float h)
        {
            var r = ModalRect(w, h);
            Modal(r, "AYARLAR");
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
            y += 42;
            if (Btn(new Rect(x, y, 300, 34), "Kendi köyüne saldır (test)")) StartTestBattle();
            GUI.Label(new Rect(x + 310, y + 6, r.width - 340, 24), "<size=12><color=#d8c9a8>Kurduğun savunmayı dene: test ordusu (kapasite kadar), ödül yok, ordun harcanmaz</color></size>");
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
            GUI.Label(new Rect(box.x, box.y + 12, box.width, 40), (testBattle ? "TEST: " : "") + (r.Victory ? "ZAFER!" : "YENİLGİ"), head);
            for (int i = 0; i < 3; i++)
                UiTheme.Dot(new Rect(box.x + box.width * 0.5f - 66 + i * 46, box.y + 56, 40, 40), i < r.Stars ? new Color(1f, 0.82f, 0.2f) : new Color(0.55f, 0.48f, 0.40f));
            float y = box.y + 104f;
            Line(box, ref y, "Genel hasar: %" + r.OverallDamagePct + (testBattle ? "   (test: ödül yok)" : "   XP: +" + (lastOutcome != null ? lastOutcome.XpGained : r.Xp)));
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
