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
            return GUI.Button(Pressed(r), text);
        }

        // ---------- Menü efektleri (kullanıcı isteği 27.11.2026) ----------
        private bool uiPressing; private Vector2 uiPressPos;
        private Tab animTab = (Tab)(-1); private float tabOpenTime, popupOpenTime; private int lastPopupCount;

        /// <summary>Basılı tutulan düğme biraz içe göçer (dokunma geri bildirimi).</summary>
        private Rect Pressed(Rect r)
        {
            if (!uiPressing || !r.Contains(uiPressPos)) return r;
            float dx = r.width * 0.035f, dy = r.height * 0.06f;
            return new Rect(r.x + dx, r.y + dy, r.width - 2f * dx, r.height - 2f * dy);
        }

        private void TrackPress()
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown) { uiPressing = true; uiPressPos = e.mousePosition; }
            else if (e.type == EventType.MouseUp) uiPressing = false;
        }

        /// <summary>Pencere açılışı: 0,2 sn içinde hafif büyüyerek ve belirerek gelir.</summary>
        private void PopIn(float openTime, float w, float h, Matrix4x4 baseM)
        {
            float k = Mathf.Clamp01((Time.unscaledTime - openTime) / 0.2f);
            float ease = 1f - (1f - k) * (1f - k) * (1f - k);
            if (ease >= 1f) return;
            float sc = 0.9f + 0.1f * ease;
            var c = new Vector3(w * 0.5f, h * 0.5f, 0f);
            GUI.matrix = baseM * Matrix4x4.TRS(c, Quaternion.identity, Vector3.one) * Matrix4x4.Scale(new Vector3(sc, sc, 1f)) * Matrix4x4.TRS(-c, Quaternion.identity, Vector3.one);
            GUI.color = new Color(1f, 1f, 1f, ease);
        }

        /// <summary>Seçili durumu altın renkle gösteren düğme.</summary>
        private bool SelBtn(Rect r, string text, bool on)
        {
            uiRects.Add(r);
            return GUI.Button(Pressed(r), text, on ? UiTheme.BtnOn : GUI.skin.button);
        }

        /// <summary>Büyük yuvarlak ana ekran düğmesi (renkli daire + yazı); badge = kırmızı ünlem.</summary>
        private bool RoundBtn(Rect r, string label, Color c, bool badge = false)
        {
            uiRects.Add(r);
            var hit = r;
            r = Pressed(r);
            if (badge) { float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.03f; r = new Rect(r.center.x - r.width * pulse * 0.5f, r.center.y - r.height * pulse * 0.5f, r.width * pulse, r.height * pulse); } // bildirimli düğme hafifçe atar
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
            if (e.type == EventType.MouseDown && hit.Contains(e.mousePosition)) { e.Use(); return true; }
            return false;
        }

        /// <summary>Basıldığı anda tetiklenen düğme (GUI.Button bazı durumlarda bırakma olayını kaçırıyor; RoundBtn ile aynı yöntem).</summary>
        private bool PressBtn(Rect r, string text, GUIStyle style)
        {
            uiRects.Add(r);
            GUI.Label(Pressed(r), text, style);
            var e = Event.current;
            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition)) { e.Use(); return true; }
            return false;
        }

        /// <summary>Telefonda listeyi parmakla sürükleyerek kaydır (IMGUI kaydırma çubuğu dokunmatikte çok ince). Sürükleme basılı düğmeyi iptal eder.</summary>
        private static void DragScroll(Rect area, ref Vector2 pos)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDrag || !area.Contains(e.mousePosition)) return;
            pos.y = Mathf.Max(0f, pos.y - e.delta.y); // üst sınırı BeginScrollView kırpar
            GUIUtility.hotControl = 0;
            e.Use();
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
            // Telefon uyumu: arayüz güvenli alana (çentik dışı) çizilir; mantıksal tuval en az 1136x640 olacak şekilde ölçeklenir
            var sa = Screen.safeArea;
            uiScale = Mathf.Clamp(Mathf.Min(sa.height / 640f, sa.width / 1136f), 0.5f, 4f);
            uiOffset = new Vector2(sa.x, Screen.height - sa.yMax);
            GUI.matrix = Matrix4x4.TRS(new Vector3(uiOffset.x, uiOffset.y, 0f), Quaternion.identity, new Vector3(uiScale, uiScale, 1f));
            uiRects.Clear();
            float w = sa.width / uiScale, h = sa.height / uiScale;
            var baseM = GUI.matrix;
            TrackPress();
            if (popups.Count != lastPopupCount) { if (popups.Count > lastPopupCount) popupOpenTime = Time.unscaledTime; lastPopupCount = popups.Count; }
            if (tab != animTab) { animTab = tab; tabOpenTime = Time.unscaledTime; }

            if (popups.Count > 0) { DrawHud(w, h, false); PopIn(popupOpenTime, w, h, baseM); DrawPopup(w, h); GUI.matrix = baseM; GUI.color = Color.white; return; }
            if (sim != null) { DrawHud(w, h, true); DrawBattleGui(w, h); return; }
            if (tab == Tab.Village) DrawJobTimers();
            DrawHud(w, h, false);
            if (LayoutMode && tab == Tab.Village) { DrawLayoutMode(w, h); return; }
            if (tab != Tab.Village) PopIn(tabOpenTime, w, h, baseM);
            switch (tab)
            {
                case Tab.Village: DrawVillage(w, h); break;
                case Tab.Barracks: DrawBarracks(w, h); break;
                case Tab.Research: DrawResearch(w, h); break;
                case Tab.Quests: DrawQuests(w, h); break;
                case Tab.Attack: DrawAttack(w, h); break;
                case Tab.TestArmy: DrawTestArmy(w, h); break;
                default: DrawMenu(w, h); break;
            }
            GUI.matrix = baseM; GUI.color = Color.white;
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
                var gp = ScreenToGui(sp);
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

        /// <summary>Saldırı hızı: iki vuruş arası saniye ve saniyedeki vuruş (10 tick = 1 sn).</summary>
        private static string AtkSpeed(int intervalTicks)
        {
            int it = System.Math.Max(1, intervalTicks);
            float sec = RealSeconds(it); // ekrandaki gerçek süre (savaş %40 yavaş)
            return "Saldırı " + sec.ToString("0.0") + " sn (" + (1f / sec).ToString("0.#") + "/sn)";
        }


        // ---------- Seçili yapı (alt orta kart) ----------
        private void DrawSelectPanel(float w, float h)
        {
            float ph = towerElemMenu ? 222f : 184f; // element menüsü açıkken bir satır daha
            var r = new Rect(w * 0.5f - 230f, h - ph - 12f, 460f, ph);
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
                var d = ContentCatalog.TowerFor(t.Type, t.Element);
                bool wall = t.Type == TowerType.Wall;
                GUI.Label(new Rect(x, y, iw - 150, 26), ContentCatalog.Tower(t.Type).Name + "  <size=14>Sv " + t.Level + (wall ? "" : "   Element: " + ContentCatalog.ElementName(t.Element)) + (t.DefElement != TowerElement.None ? "   Savunma: " + ContentCatalog.ElementName(t.DefElement) : "") + "</size>", UiTheme.Header);
                {
                    var eb = new Rect(r.xMax - 150, r.y + 8, 100, 30);
                    uiRects.Add(eb);
                    if (GUI.Button(eb, towerElemMenu ? "Geri" : "Element", towerElemMenu ? GUI.skin.button : UiTheme.BtnGreen)) towerElemMenu = !towerElemMenu;
                }
                y += 28;
                if (towerElemMenu) { DrawElementMenu(x, y, iw, p, wall); return; }
                var m = g.Research.Mods();
                int hp = CombatMods.Apply(ContentCatalog.Scaled(d.Hp, t.Level) * (wall ? t.WallHpPct : 100) / 100, m.TowerHp);
                int atk = CombatMods.Apply(ContentCatalog.Scaled(d.Attack, t.Level), m.TowerDamage) * t.DamagePct / 100;
                string info = wall ? "Can " + hp + "   Zırh %" + t.WallArmorPct + "   (can " + t.BranchA + ", zırh " + t.BranchB + " puan)   Saldırmaz; askerleri üstüne çeker"
                    : "Can " + hp + "   Hasar " + atk + "   " + AtkSpeed(System.Math.Max(1, d.IntervalTicks * t.IntervalPct / 100)) + "   Menzil " + (d.RangeMilli / 1000f).ToString("0.0")
                      + "   (hız " + t.BranchA + ", hasar " + t.BranchB + " puan)\n" + ContentCatalog.ElementEffect(t.Element);
                if (g.Wears.TryGetValue(p, out var wr) && wr.PermanentDamagePct > 0)
                {
                    info += "   <color=#ffb070>Hasar %" + wr.PermanentDamagePct + (wr.IsDisabled ? " (DEVRE DIŞI)" : "") + "</color>";
                    string e;
                    var rb = new Rect(r.xMax - 260, r.y + 8, 100, 30);
                    uiRects.Add(rb);
                    if (GUI.Button(rb, "Onar", UiTheme.BtnRed)) { if (g.TryRepair(p, out e)) { Toast("Onarıldı"); dirty = true; } else Toast(e); }
                }
                GUI.Label(new Rect(x, y, iw, 40), "<size=12>" + info + "</size>"); y += 40;
                DrawTowerUpgrade(x, y, iw, p, t.Type);
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

        private bool towerElemMenu;

        /// <summary>Kule yükseltmesi iki dal: Okçu (saldırı hızı / hasar), Duvar (can / zırh). Her yükseltme seviye +1.</summary>
        private void DrawTowerUpgrade(float x, float y, float iw, GridPos p, TowerType type)
        {
            var info = g.PreviewUpgrade(JobKind.Tower, p, SoldierType.Swordsman);
            if (info.Block != null && info.TargetLevel == 0) { GUI.Label(new Rect(x, y, iw, 24), info.Block); return; }
            GUI.Label(new Rect(x, y, iw - 230, 60), "<size=12>Sv " + info.TargetLevel + ": " + info.Cost + "\n" + Dur(info.Seconds) + (info.Block != null ? "\n<color=#ffb070>" + info.Block + "</color>" : "") + "</size>");
            for (int b = 1; b <= 2; b++)
            {
                var rb = new Rect(x + iw - 230 + (b - 1) * 116, y + 4, 110, 44);
                uiRects.Add(rb);
                GUI.enabled = info.Block == null;
                string err;
                if (GUI.Button(rb, "<size=12>+ " + GameState.BranchName(type, b) + "</size>", UiTheme.BtnGreen))
                { if (g.TryStartTowerUpgrade(p, b, out err)) Toast("Yükseltme başladı: " + GameState.BranchName(type, b)); else Toast(err); }
                GUI.enabled = true;
            }
        }

        /// <summary>Element menüsü: araştırılmış elementler kuleye uygulanır (90 sn + biraz kaynak).</summary>
        private void DrawElementMenu(float x, float y, float iw, GridPos p, bool wall)
        {
            // savunma elementi (Ateş/Buz/Zehir Kalkanı) yalnızca Duvarda; Okçu Kulesi yalnızca saldırı elementi alır (27.11.2026)
            if (wall)
            {
                g.DefElementBlock(p, TowerElement.Fire, out var dcost);
                GUI.Label(new Rect(x, y, iw, 20), "<size=12>Uygulama: " + dcost + "  " + Dur(GameRules.ElementApplySeconds) + "   Duvara saldıran o elementteki askerler %35 az hasar verir</size>");
                y += 24;
            }
            float dy = y;
            float dbw = (iw - 10) / 3f;
            var defs = new[] { TowerElement.Fire, TowerElement.Ice, TowerElement.Poison };
            for (int i = 0; wall && i < 3; i++)
            {
                var e = defs[i];
                string block = g.DefElementBlock(p, e, out _);
                bool locked = !g.Unlocked(ResearchStat.TowerDefenseElement, e);
                var rb = new Rect(x + i * (dbw + 5), dy, dbw, 30);
                uiRects.Add(rb);
                if (GUI.Button(rb, "<size=12>Savunma: " + ContentCatalog.ElementName(e) + (locked ? " (kilitli)" : "") + "</size>", block == null ? UiTheme.BtnGreen : GUI.skin.button))
                { string err; if (g.TryApplyDefElement(p, e, out err)) { Toast(ContentCatalog.ElementName(e) + " savunması uygulanıyor"); towerElemMenu = false; } else Toast(err); }
            }
            if (wall) return;
            var els = new[] { TowerElement.Arrow, TowerElement.Fire, TowerElement.Ice, TowerElement.Poison, TowerElement.Magic, TowerElement.Lightning };
            g.ElementBlock(p, TowerElement.Arrow, out var cost);
            GUI.Label(new Rect(x, y, iw, 20), "<size=12>Uygulama: " + cost + "  " + Dur(GameRules.ElementApplySeconds) + "   (önce Akademi > Element ağacı Sv1'de araştır)</size>");
            y += 22;
            float bw = (iw - 10) / 3f;
            for (int i = 0; i < els.Length; i++)
            {
                var e = els[i];
                string block = g.ElementBlock(p, e, out _);
                bool locked = !g.ElementUnlocked(e);
                var rb = new Rect(x + (i % 3) * (bw + 5), y + (i / 3) * 46, bw, 42);
                uiRects.Add(rb);
                string label = "<size=12><b>" + ContentCatalog.ElementName(e) + "</b>" + (locked ? " (kilitli)" : "") + "\n<size=11>" + ElementShort(e) + "</size></size>";
                if (GUI.Button(rb, label, block == null ? UiTheme.BtnGreen : GUI.skin.button))
                { string err; if (g.TryApplyElement(p, e, out err)) { Toast(ContentCatalog.ElementName(e) + " uygulanıyor"); towerElemMenu = false; } else Toast(err); }
            }
        }

        private static string ElementShort(TowerElement e)
        {
            switch (e)
            {
                case TowerElement.Arrow: return "Kritik, hızlı"; case TowerElement.Fire: return "Alan + yanma"; case TowerElement.Ice: return "Yavaşlatır, donma";
                case TowerElement.Poison: return "Zehir, zayıflatır"; case TowerElement.Magic: return "Uçana +%100"; default: return "2 askere sıçrar";
            }
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
                GUI.Label(new Rect(r.x + 20, top, 300, 24), "Kışla Sv " + bsel.Level + "   <size=12>(" + g.CountBuildings(BuildingType.Barracks) + "/" + g.MaxCountOf(BuildingType.Barracks) + " kışla; 2. ve 3. Akademi araştırmasıyla)</size>", UiTheme.Header);
                DrawUpgradeRow(r.x + 20, top + 24, r.width - 40, JobKind.Building, bpos, SoldierType.Swordsman);
                top += 88;
            }
            else GUI.Label(new Rect(r.x + 20, top, r.width - 40, 22), "<color=#ffb070>Önce KÖY DÜZENİ'nden Kışla kur</color>", UiTheme.Small);
            { var area = new Rect(r.x + 12, top, r.width - 24, r.yMax - top - 10); DragScroll(area, ref scrollPos); GUILayout.BeginArea(area); }
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            string err;
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
            {
                var t = (SoldierType)i; var d = ContentCatalog.Soldier(t);
                int lvl = g.SoldierLevel(t);
                bool locked = !g.SoldierUnlocked(t);
                GUILayout.BeginHorizontal(GUILayout.Height(44));
                { // askerin 3B önizlemesi (kendi seviyesindeki görünümüyle)
                    var pv = PreviewIcons.Soldier(t, lvl);
                    var pr = GUILayoutUtility.GetRect(44f, 44f, GUILayout.Width(44f), GUILayout.Height(44f));
                    GUI.color = locked ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
                    if (pv != null) GUI.DrawTexture(pr, pv, ScaleMode.ScaleToFit, true);
                    else UiTheme.Dot(new Rect(pr.x + 6f, pr.y + 6f, 32f, 32f), new Color(0.4f, 0.35f, 0.3f));
                    GUI.color = Color.white;
                }
                if (GUILayout.Button((locked ? "<color=#ffb070>[" + (ResearchCatalog.FindSpecial(ResearchStat.SoldierUnlock, i) != null ? "Akademi araştırması" : "Sv " + d.UnlockLevel + "'de açılır") + "]</color> " : "") + "<b>" + d.Name + "</b>  <color=#ffd560>" + VillageArt.RankName(lvl) + "</color>  Sv " + lvl + "  adet " + g.Inventory.Get(t) + "  <color=#9fd0ff>" + GameRules.Housing(t) + " yer</color>" + (detailSoldier == i ? "  ▲" : "  ▼") + "\n<size=13>Can " + ContentCatalog.Scaled(d.Hp, lvl) + "  Hasar " + ContentCatalog.Scaled(d.Attack, lvl) + "  " + AtkSpeed(d.IntervalTicks) + "  Hız " + d.SpeedMilli + "</size>", UiTheme.Skin.label, GUILayout.Width(320)))
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
                    // element seçimi (Element ağacında araştırılan Ateş/Buz/Zehir; ücretsiz, sonraki savaşlarda geçerli)
                    for (int pass = 0; pass < 2; pass++)
                    {
                        bool atk = pass == 0;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("<size=13>" + (atk ? "Saldırı elementi:" : "Savunma elementi:") + "</size>", GUILayout.Width(140));
                        var cur = atk ? g.SoldierAtkElement(t) : g.SoldierDefElement(t);
                        foreach (var e in new[] { TowerElement.None, TowerElement.Fire, TowerElement.Ice, TowerElement.Poison })
                        {
                            bool ok = g.Unlocked(atk ? ResearchStat.SoldierAttackElement : ResearchStat.SoldierDefenseElement, e);
                            string lab = (e == TowerElement.None ? "Yok" : ContentCatalog.ElementName(e)) + (ok ? "" : " (kilitli)");
                            if (GUILayout.Button("<size=12>" + lab + "</size>", cur == e ? UiTheme.BtnOn : (ok ? UiTheme.BtnGreen : GUI.skin.button), GUILayout.Width(110), GUILayout.Height(28)))
                            { if (g.TrySetSoldierElement(t, atk, e, out err)) { LastSave(); } else Toast(err); }
                        }
                        GUILayout.EndHorizontal();
                    }
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
            GUILayout.Label("YÜKSELTMELER (" + g.Jobs.Count + "/" + g.Builders * g.JobQueueDepth + "; " + g.Builders + " işçi" + (g.JobQueueDepth > 1 ? ", köylü başına " + g.JobQueueDepth + " iş" : "") + ")", UiTheme.Header);
            for (int ji = 0; ji < g.Jobs.Count; ji++)
            {
                var j = g.Jobs[ji];
                if (ji >= g.Builders) GUILayout.Label("<size=12><color=#b0a090>(sırada)</color></size>");
                if (j.Kind == JobKind.TowerElement) { GUILayout.Label("Kule " + (j.TargetLevel >= GameState.DefElementJobOffset ? "savunma elementi: " + ContentCatalog.ElementName((TowerElement)(j.TargetLevel - GameState.DefElementJobOffset)) : "elementi: " + ContentCatalog.ElementName((TowerElement)j.TargetLevel)) + "  kalan " + Dur(j.RemainingSeconds)); continue; }
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

            { var area = new Rect(r.x + 8, r.y + 100, r.width - 16, r.height - 202); DragScroll(area, ref scrollPos); GUILayout.BeginArea(area); }
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            for (int lv = 1; lv <= ResearchCatalog.MaxLevel; lv++)
            {
                bool any = false;
                for (int c = 1; c <= 6; c++) if (ResearchCatalog.Find((ResearchTree)researchTree, lv, c) != null) any = true;
                if (!any) continue;
                GUILayout.BeginHorizontal(GUILayout.Height(40));
                GUILayout.Label("Sv " + lv, GUILayout.Width(44));
                int cols = 1;
                foreach (var rd in ResearchCatalog.All) if ((int)rd.Tree == researchTree && rd.Column > cols) cols = rd.Column;
                float colW = cols <= 2 ? 300f : cols == 3 ? 230f : 180f; // 1. sütun bonus, sonrakiler özel araştırmalar
                for (int c = 1; c <= cols; c++)
                {
                    var d = ResearchCatalog.Find((ResearchTree)researchTree, lv, c);
                    if (d == null) { GUILayout.Label("", GUILayout.Width(colW)); continue; }
                    string st = g.Research.IsDone(d.Id) ? "[OK]" : (g.Research.IsActive(d.Id) ? "[...]" : (g.Research.LockReason(d, g.Level, g.UniversityLevel) == null ? "[>]" : "[X]"));
                    var style = selResearch == d.Id ? UiTheme.BtnOn : (g.Research.IsDone(d.Id) ? UiTheme.BtnGreen : GUI.skin.button);
                    if (GUILayout.Button(st + " " + d.Name, style, GUILayout.Width(colW), GUILayout.Height(38))) selResearch = d.Id;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Seçili araştırma ayrıntısı
            var sel = selResearch != null ? ResearchCatalog.Get(selResearch) : null;
            var dr = new Rect(r.x + 6, r.y + r.height - 94, r.width - 12, 88);
            GUI.Box(dr, "");
            if (sel == null) { GUI.Label(new Rect(dr.x + 8, dr.y + 6, dr.width - 16, 70), "Bir araştırma seç. 1. sütun: bonus (bir önceki seviyeyi ister). Diğer sütunlar: haklar, askerler, elementler (aynı adlı bir öncekini ister)."); return; }
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
                case ResearchStat.VillagerHouseSlot: return "+1 Köylü Evi hakkı";
                case ResearchStat.TowerSlot: return "+1 Kule hakkı";
                case ResearchStat.BarracksSlot: return "+1 Kışla hakkı";
                case ResearchStat.AcademySlot: return "+1 Akademi hakkı (+1 araştırma yuvası)";
                case ResearchStat.WoodSlot: return "+1 Orman ve +1 Kereste Deposu hakkı";
                case ResearchStat.StoneSlot: return "+1 Taş Ocağı ve +1 Taş Deposu hakkı";
                case ResearchStat.GoldSlot: return "+1 Altın Madeni ve +1 Banka hakkı";
                case ResearchStat.IronSlot: return "+1 Demir Madeni ve +1 Demir Deposu hakkı";
                case ResearchStat.RoadSlot: return "+" + d.Permille + " yol hakkı";
                case ResearchStat.AutoRepair: return "Kuleler savunmadan sonra kendiliğinden onarılır";
                case ResearchStat.JobQueue: return "Köylü başına +1 sıralı iş";
                case ResearchStat.SoldierArmor: return "Saldıran askerlere +" + d.Permille + " zırh";
                case ResearchStat.TowerArmor: return "Kuleler %" + d.Permille + " daha az hasar alır";
                case ResearchStat.SoldierUnlock: return "Yeni asker: " + d.Name;
                case ResearchStat.TowerAttackElement: return "Kule elementi: " + d.Name;
                case ResearchStat.TowerDefenseElement: return "Duvar savunma elementi: " + d.Name;
                case ResearchStat.SoldierAttackElement: return "Asker saldırı elementi: " + d.Name;
                case ResearchStat.SoldierDefenseElement: return "Asker savunma elementi: " + d.Name;
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
            { var area = new Rect(r.x + 12, r.y + 48, r.width - 24, r.height - 58); DragScroll(area, ref scrollPos); GUILayout.BeginArea(area); }
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
            var r = ModalRect(w, h);
            Modal(r, "SALDIR  —  " + g.ReadySoldiers + " asker hazır (" + g.ReadyHousing + " yer)");
            string[] dn = { "KOLAY", "NORMAL", "ZOR" };
            var diffCol = new[] { new Color(0.2f, 0.5f, 0.15f), new Color(0.6f, 0.38f, 0.05f), new Color(0.7f, 0.15f, 0.1f) };
            float cw = (r.width - 40f) / 3f;
            float ch = Mathf.Min(r.height - 96f, 430f);
            var dark = new GUIStyle(UiTheme.Skin.label) { normal = { textColor = new Color(0.25f, 0.15f, 0.07f) }, fontSize = 13 };
            for (int d = 0; d < 3; d++)
            {
                var t = targets[d];
                var cr = new Rect(r.x + 10 + d * (cw + 10), r.y + 38, cw, ch);
                GUI.Box(cr, "", UiTheme.Parchment);
                var head = new GUIStyle(UiTheme.Title) { fontSize = 19, normal = { textColor = diffCol[d] } };
                GUI.Label(new Rect(cr.x + 8, cr.y + 6, cr.width - 16, 26), dn[d] + "  <size=13>Sv " + t.Level + "</size>", head);
                // hedef köyün 3B önizlemesi (keşif: tuzaklar görünmez)
                float pw = cr.width - 16f, ph = Mathf.Min(pw * 0.66f, ch * 0.52f);
                var pr = new Rect(cr.x + 8f, cr.y + 34f, pw, ph);
                GUI.Box(pr, "", UiTheme.Chip);
                var pv = PreviewIcons.Village(t.Seed + "_" + t.Level, t.Defense);
                if (pv != null) GUI.DrawTexture(new Rect(pr.x + 2f, pr.y + 2f, pr.width - 4f, pr.height - 4f), pv, ScaleMode.ScaleToFit, true);
                else GUI.Label(pr, "<color=#d8c9a8>Keşif yapılıyor...</color>", UiTheme.Center);
                float iy = pr.yMax + 6f;
                GUI.Label(new Rect(cr.x + 12, iy, cr.width - 24, ch - (iy - cr.y) - 52f),
                    "<b>" + t.Name + "</b>\nKule " + t.Defense.Towers.Count + "   Bina " + t.Defense.Buildings.Count + "   Giriş " + t.Defense.Layout.EntranceCount + "   Tuzak ?" +
                    "\nYağma: " + t.Pool + "\nXP x" + ((100 + GameState.BattleXpPctPerTargetLevel * (t.Level - 1)) / 100f).ToString("0.00"), dark);
                var bb = new Rect(cr.x + 12, cr.yMax - 48, cr.width - 24, 40);
                uiRects.Add(bb);
                if (GUI.Button(Pressed(bb), "SALDIR", UiTheme.BtnRed)) StartBattle(t);
            }
            float by = r.y + 38 + ch + 10;
            if (Btn(new Rect(r.x + 10, by, 180, 34), "Hedefleri yenile")) { targetSeed++; targets = null; }
            GUI.Label(new Rect(r.x + 200, by - 2, r.width - 210, r.yMax - by), "<size=12><color=#d8c9a8>İpucu: askerleri kırmızı alana dokunarak (basılı tutarak) gönder. Tuzaklar keşifte görünmez. Duvarlar yer askerlerini üstüne çeker (uçanlar geçer); Ateş alan hasarı, Buz yavaşlatma, Zehir zayıflatma, Şimşek zincir vurur. Zafer için %75 (%90: 2 yıldız, %100: 3 yıldız).</color></size>");
        }

        // ---------- Test ordusu ----------
        private void DrawTestArmy(float w, float h)
        {
            var r = ModalRect(w, h);
            int cnt = TestArmyCount();
            int housing = 0;
            foreach (var kv in testArmy) housing += kv.Value * GameRules.Housing(kv.Key);
            Modal(r, "TEST ORDUSU  —  " + cnt + " asker, " + housing + " yer");
            GUI.Label(new Rect(r.x + 20, r.y + 30, r.width - 40, 22), "<size=13><color=#d8c9a8>Kendi köyüne saldırı testi: istediğin askeri seç (en çok " + TestArmyMaxSoldiers + " asker). Seviyeler senin asker seviyelerin; ordun harcanmaz, ödül yok.</color></size>");
            // askerler kaydırılabilir liste; başlat düğmesi her ekranda pencerenin altında sabit
            float by = r.yMax - 48;
            var listR = new Rect(r.x + 10, r.y + 56, r.width - 20, by - r.y - 62);
            int rows = ContentCatalog.SoldierTypeCount;
            uiRects.Add(listR);
            DragScroll(listR, ref scrollPos);
            scrollPos = GUI.BeginScrollView(listR, scrollPos, new Rect(0, 0, listR.width - 20, rows * 38f));
            var rowSt = new GUIStyle(GUI.skin.label) { wordWrap = false, clipping = TextClipping.Clip, richText = true }; // uzun ad (Atlı Kılıçlı) sarılıp kaybolmasın
            float y = 0;
            for (int i = 0; i < rows; i++)
            {
                var t = (SoldierType)i; var d = ContentCatalog.Soldier(t);
                int n = testArmy.TryGetValue(t, out var v) ? v : 0;
                GUI.Label(new Rect(10, y + 6, 336, 26), "<b>" + d.Name + "</b>  <size=13>Sv " + g.SoldierLevel(t) + "  " + GameRules.Housing(t) + " yer  " + AtkSpeed(d.IntervalTicks) + (g.Level < d.UnlockLevel ? "  <color=#ffb070>(senin için Sv " + d.UnlockLevel + "'de açılır; testte serbest)</color>" : "") + "</size>", rowSt);
                float bx = 350;
                if (Btn(new Rect(bx, y, 48, 32), "-10")) n = Mathf.Max(0, n - 10);
                if (Btn(new Rect(bx + 52, y, 40, 32), "-")) n = Mathf.Max(0, n - 1);
                GUI.Label(new Rect(bx + 96, y + 4, 50, 26), n.ToString(), UiTheme.Center);
                int room = TestArmyMaxSoldiers - cnt;
                if (Btn(new Rect(bx + 150, y, 40, 32), "+") && room > 0) n += 1;
                if (Btn(new Rect(bx + 194, y, 48, 32), "+10") && room > 0) n += Mathf.Min(10, room);
                testArmy[t] = n;
                cnt = TestArmyCount();
                y += 38;
            }
            GUI.EndScrollView();
            y = by;
            if (Btn(new Rect(r.x + 20, y, 200, 38), "Kapasite kadar doldur")) FillTestArmy();
            if (Btn(new Rect(r.x + 228, y, 120, 38), "Temizle")) testArmy.Clear();
            var go = new Rect(r.x + 360, y, 240, 38);
            uiRects.Add(go);
            if (PressBtn(go, "TEST SALDIRISI", UiTheme.BtnRed)) StartTestBattle();
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
            if (Btn(new Rect(r.x + 180, y, 230, 34), "Grafik: " + (GraphicsFx.High ? "Yüksek" : "Düşük (pil dostu)"))) GraphicsFx.High = !GraphicsFx.High;
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
            if (Btn(new Rect(x, y, 300, 34), "Kendi köyüne saldır (test)")) { if (TestArmyCount() == 0) FillTestArmy(); tab = Tab.TestArmy; scrollPos = Vector2.zero; }
            GUI.Label(new Rect(x + 310, y + 6, r.width - 340, 24), "<size=12><color=#d8c9a8>Kurduğun savunmayı dene: askerleri sen seç, ödül yok, ordun harcanmaz</color></size>");
            y += 42;
            // Elementler (kullanıcı isteği 25.11.2026): test düğmeleri + bilgi bölümü
            if (Btn(new Rect(x, y, 220, 34), "Tüm elementleri aç (test)"))
            {
                int n = 0;
                foreach (var d in ResearchCatalog.All)
                    if ((d.Stat == ResearchStat.TowerAttackElement || d.Stat == ResearchStat.TowerDefenseElement || d.Stat == ResearchStat.SoldierAttackElement || d.Stat == ResearchStat.SoldierDefenseElement) && !g.Research.IsDone(d.Id))
                    { g.Research.ForceComplete(d.Id); n++; }
                LastSave(); Toast(n + " element araştırması açıldı (test)");
            }
            if (Btn(new Rect(x + 226, y, 290, 34), (testTowerElements ? "[x]" : "[ ]") + " Test saldırısında kulelere element")) testTowerElements = !testTowerElements;
            if (Btn(new Rect(x + 522, y, 150, 34), showElementInfo ? "Bilgiyi kapat" : "Element bilgisi")) showElementInfo = !showElementInfo;
            y += 42;
            if (showElementInfo) { DrawElementInfo(new Rect(r.x + 10, y, r.width - 20, r.yMax - y - 8)); return; }
            uiRects.Add(new Rect(x, y, 300, 38));
            if (GUI.Button(new Rect(x, y, 300, 38), confirmNew ? "EMİN MİSİN? Tekrar bas: tüm ilerleme silinir" : "Yeni oyun (sil)", UiTheme.BtnRed)) { if (confirmNew) { confirmNew = false; NewGameConfirmed(); } else confirmNew = true; }
            y += 48;
            var sb = new StringBuilder("Son olaylar:\n");
            foreach (var l in log) sb.Append("• ").Append(l).Append('\n');
            GUI.Label(new Rect(r.x + 10, y, r.width - 20, 160), sb.ToString());
        }

        private bool testTowerElements, showElementInfo;

        /// <summary>Ayarlar > Element bilgisi: tüm elementlerin etkileri, döngü ve direnç.</summary>
        private void DrawElementInfo(Rect r)
        {
            var sb = new StringBuilder("<size=14><b>KULE SALDIRI ELEMENTLERİ</b> (Okçu Kulesi; Akademi > Element ağacında araştır, kuleye 90 sn ile uygula)</size>\n<size=13>");
            foreach (var e in new[] { TowerElement.Arrow, TowerElement.Fire, TowerElement.Ice, TowerElement.Poison, TowerElement.Magic, TowerElement.Lightning })
            {
                var rd = ResearchCatalog.FindSpecial(ResearchStat.TowerAttackElement, (int)e);
                sb.Append("• <b>").Append(ContentCatalog.ElementName(e)).Append("</b>").Append(rd != null ? " (Sv" + rd.Level + ")" : "").Append(g.ElementUnlocked(e) ? " <color=#8fe07a>açık</color>" : "").Append(": ").Append(ContentCatalog.ElementEffect(e)).Append("\n");
            }
            sb.Append("</size>\n<size=14><b>SAVUNMA VE ASKER ELEMENTLERİ</b> (Ateş, Buz, Zehir)</size>\n<size=13>");
            sb.Append("• Duvar savunma elementi: Ateş/Buz/Zehir Kalkanı (yalnızca Duvara, 90 sn ile uygulanır; Okçu Kulesi savunma elementi almaz)\n");
            sb.Append("• Asker saldırı elementi: Ateş/Buz/Zehir Silahları — asker savunma elementi: Ateş/Buz/Zehir Zırhı (kışlada asker türüne seçilir)\n");
            sb.Append("</size>\n<size=14><b>KURALLAR</b></size>\n<size=13>");
            sb.Append("• Döngü: <color=#ff9a50>Ateş</color> > <color=#8fe07a>Zehir</color> > <color=#9fe0ff>Buz</color> > <color=#ff9a50>Ateş</color>\n");
            sb.Append("• Zayıf elementle saldıran <b>%40 az</b> hasar verir (ör. zehirli asker Ateş savunmalı kuleye). Üstün olana ek hasar yok.\n");
            sb.Append("• Direnç: hedefin savunma elementi saldırı elementiyle aynıysa <b>%35 az</b> hasar (ör. Ateş Zırhlı asker Ateş kulesinden).\n");
            sb.Append("• Ok, Büyü ve Şimşek nötrdür (döngüye girmez).\n");
            sb.Append("• Ayrıca: Ateş yakar, Buz yavaşlatır ve üst üste donma hasarı verir, Zehir zamanla hasar verir ve askerin hasarını %30 düşürür.</size>");
            uiRects.Add(r);
            { var area = r; DragScroll(area, ref elemInfoScroll); GUILayout.BeginArea(area); }
            elemInfoScroll = GUILayout.BeginScrollView(elemInfoScroll); // pencereye sığmazsa kaydırılır
            var st = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true };
            st.normal.textColor = new Color(0.96f, 0.92f, 0.82f);
            GUILayout.Label(sb.ToString(), st, GUILayout.Width(r.width - 24));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private Vector2 elemInfoScroll;

        // ---------- Savaş arayüzü ----------
        private void DrawBattleGui(float w, float h)
        {
            DrawFloatTexts(w, h); // hasar sayıları, "YIKILDI", yıldız duyuruları
            // yalnızca orduda olan türler (15 tür sığmaz): stokta kalan ya da sahaya inmiş
            var shown = new List<SoldierType>();
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
            {
                var st = (SoldierType)i;
                bool used = sim.StockOf(st) > 0 || st == selSoldier;
                if (!used) foreach (var s in sim.Soldiers) if (s.Type == st) { used = true; break; }
                if (used) shown.Add(st);
            }
            int n = Mathf.Max(1, shown.Count);
            float bw = Mathf.Min(120f, (w - 8f) / n - 3f);
            for (int i = 0; i < shown.Count; i++)
            {
                var t = shown[i];
                if (sim.StockOf(t) <= 0 && t != selSoldier) GUI.color = new Color(1f, 1f, 1f, 0.45f);
                var br = new Rect(4 + i * (bw + 3), 50, bw, 52);
                if (SelBtn(br, ContentCatalog.Soldier(t).Name + "\n" + sim.StockOf(t), t == selSoldier)) selSoldier = t;
                var sp = PreviewIcons.Soldier(t, g.SoldierLevel(t)); // askerin küçük 3B görseli
                if (sp != null) GUI.DrawTexture(new Rect(br.x + 2, br.y + 2, 26, 26), sp, ScaleMode.ScaleToFit, true);
                else UiTheme.Dot(new Rect(br.x + 4, br.y + 4, 12, 12), VillageArt.TunicOf(t));
                GUI.color = Color.white;
            }
            if (!battleOver)
            {
                uiRects.Add(new Rect(4, 108, 120, 36));
                if (GUI.Button(new Rect(4, 108, 120, 36), "Geri Çek", UiTheme.BtnRed) && !sim.Finished) sim.Retreat();
                if (Btn(new Rect(128, 108, 100, 36), "Hız " + speed + "x")) speed = speed == 1 ? 3 : (speed == 3 ? 6 : 1);
                bool anyStock = false;
                for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++) if (sim.StockOf((SoldierType)i) > 0) { anyStock = true; break; }
                if (anyStock) { var da = new Rect(232, 108, 150, 36); uiRects.Add(da); if (GUI.Button(Pressed(da), "Hepsini Gönder", UiTheme.BtnGreen) && !sim.Finished) DeployAll(); }
            }
            int aliveSoldiers = 0;
            foreach (var s in sim.Soldiers) if (s.Alive) aliveSoldiers++;
            var rs = new Rect(6, h - 46, w - 12, 40);
            string msg = Time.time < toastUntil ? toast : curTarget.Name + " (Sv " + curTarget.Level + ")  |  " + (int)RealSeconds(sim.Tick) + " sn / " + (int)RealSeconds(BattleSim.TimeLimitTicks) + "  |  Hasar %" + LiveDamagePct() + " (zafer %75)  |  Sahada " + aliveSoldiers + "   [kırmızı alana dokun/basılı tut]";
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
