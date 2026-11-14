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
        private static readonly string[] ShopCats = { "Kaynak", "Köy", "Savunma" };
        private static readonly BuildingType[][] ShopBuildings =
        {
            new[] { BuildingType.Forest, BuildingType.LumberDepot, BuildingType.StoneQuarry, BuildingType.StoneDepot, BuildingType.GoldMine, BuildingType.Bank, BuildingType.IronMine, BuildingType.IronDepot },
            new[] { BuildingType.VillagerHouse, BuildingType.Barracks, BuildingType.University },
        };
        private Vector2 scrollPos;
        private int researchTree, shopCat;
        private string selResearch;
        private TargetInfo[] targets;
        private int targetSeed = 1, targetsLevel = -1;
        private bool confirmNew, editMode;

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
            if (GUI.Button(new Rect(r.xMax - 50f, r.y - 14f, 44f, 44f), "X", UiTheme.BtnRed)) { tab = Tab.Village; dirty = true; }
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
            DrawHud(w, h, false);
            switch (tab)
            {
                case Tab.Village: DrawVillage(w, h); break;
                case Tab.Shop: DrawShop(w, h); break;
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

            // işçi + ordu (üst orta)
            var br = new Rect(w * 0.5f - 150f, 10, 140, 34);
            GUI.Label(br, "İşçi  " + g.FreeBuilders + "/" + g.Builders, UiTheme.Chip);
            UiTheme.Dot(new Rect(br.x + 5, br.y + 6, 22, 22), new Color(0.45f, 0.78f, 0.4f));
            var ar = new Rect(w * 0.5f + 10f, 10, 140, 34);
            GUI.Label(ar, "Ordu  " + g.ArmySize + "/" + g.ArmyCapacity, UiTheme.Chip);
            UiTheme.Dot(new Rect(ar.x + 5, ar.y + 6, 22, 22), new Color(0.88f, 0.3f, 0.25f));
            uiRects.Add(br); uiRects.Add(ar);

            // kaynaklar (sağ üst, alt alta)
            float y = 8f;
            foreach (var t in ResourceRules.Unlocked(g.Config, g.Level))
            {
                int have = g.Wallet.Get(t), cap = g.CapacityOf(t);
                var rr = new Rect(w - 238f, y, 230f, 40f);
                uiRects.Add(rr);
                GUI.Label(new Rect(rr.x, rr.y + 4, 196, 32), "", UiTheme.Chip);
                UiTheme.Bar(new Rect(rr.x + 8, rr.y + 20, 180, 12), cap > 0 ? (float)have / cap : 0f, ResColor(t));
                GUI.Label(new Rect(rr.x + 10, rr.y + 1, 180, 20), "<b>" + have + "</b> <size=11><color=#d8c9a8>/ " + cap + "   +" + g.PerHour(t) + "/sa</color></size>", UiTheme.Small);
                UiTheme.Dot(new Rect(rr.xMax - 40, rr.y, 40, 40), new Color(0.12f, 0.08f, 0.04f));
                UiTheme.Dot(new Rect(rr.xMax - 36, rr.y + 4, 32, 32), ResColor(t));
                GUI.Label(new Rect(rr.xMax - 36, rr.y + 4, 32, 32), ResName(t).Substring(0, 1), UiTheme.Center);
                y += 44f;
            }
        }

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
            // yerleştirme şeridi (mağazadan seçilince)
            if (tool == Tool.Building || tool == Tool.Tower)
            {
                string nm, cost;
                if (tool == Tool.Building) { var bd = ContentCatalog.Building(selBuilding); nm = bd.Name; cost = GameRules.Scale(GameRules.BuildCostOf(selBuilding), g.Config, g.Level, 100).ToString(); }
                else { var td = ContentCatalog.Tower(selTower); nm = td.Name; cost = GameRules.Scale(td.BuildCost, g.Config, g.Level, 100).ToString(); }
                var br = new Rect(w * 0.5f - 280f, 56f, 560f, 52f);
                uiRects.Add(br);
                GUI.Box(br, "", UiTheme.Parchment);
                GUI.Label(new Rect(br.x + 16, br.y + 6, 400, 40), "<b>" + nm + "</b>  " + cost + "\n<size=12>Yerleştirmek için bir hücreye dokun" + (tool == Tool.Tower ? " (yola komşu)" : "") + "</size>",
                    new GUIStyle(UiTheme.Skin.label) { normal = { textColor = new Color(0.25f, 0.15f, 0.07f) } });
                if (GUI.Button(new Rect(br.xMax - 110, br.y + 8, 100, 36), "Bitti", UiTheme.BtnGreen)) { tool = Tool.Select; dirty = true; }
            }
            if (editMode) DrawEditBar(w, h);

            // sol alt: SALDIR + Düzenle
            if (RoundBtn(new Rect(16, h - 132, 116, 116), "SALDIR", new Color(0.85f, 0.25f, 0.15f))) { tab = Tab.Attack; editMode = false; tool = Tool.Select; }
            if (RoundBtn(new Rect(144, h - 84, 68, 68), editMode ? "Bitti" : "Düzen", editMode ? new Color(0.3f, 0.65f, 0.25f) : new Color(0.55f, 0.4f, 0.25f)))
            { editMode = !editMode; tool = Tool.Select; selCell = new GridPos(-1, -1); dirty = true; }

            // sağ alt: MAĞAZA + yan düğmeler
            if (RoundBtn(new Rect(w - 132, h - 132, 116, 116), "MAĞAZA", new Color(0.95f, 0.68f, 0.15f))) { tab = Tab.Shop; scrollPos = Vector2.zero; editMode = false; }
            float cx = w - 104, cy = h - 212;
            if (RoundBtn(new Rect(cx, cy, 70, 70), "Ordu", new Color(0.7f, 0.25f, 0.2f))) { tab = Tab.Barracks; scrollPos = Vector2.zero; }
            if (RoundBtn(new Rect(cx, cy - 80, 70, 70), "Akademi", new Color(0.25f, 0.45f, 0.8f))) { tab = Tab.Research; scrollPos = Vector2.zero; }
            if (RoundBtn(new Rect(cx, cy - 160, 70, 70), "Görev", new Color(0.35f, 0.62f, 0.3f), HasClaimable())) { tab = Tab.Quests; scrollPos = Vector2.zero; }
            if (RoundBtn(new Rect(w - 214, h - 84, 68, 68), "Ayar", new Color(0.45f, 0.45f, 0.5f))) tab = Tab.Menu;

            if (g.DamagedTowerCount() > 0)
            {
                string e;
                if (Btn(new Rect(16, h - 182, 200, 40), "Kuleleri onar (" + g.DamagedTowerCount() + ")")) { if (g.TryRepairAll(out e)) { Toast("Kuleler onarıldı"); dirty = true; } else Toast(e); }
            }
            if (tool == Tool.Select && layout.InBounds(selCell)) DrawSelectPanel(w, h);

            // son baskın (sol, seviyenin altında)
            if (g.LastRaid != null)
            {
                var rr = g.LastRaid;
                var r = new Rect(8, 66, 214, 40);
                GUI.Label(r, "<size=12>Son baskın: %" + rr.Result.OverallDamagePct + " " + (rr.Result.Victory ? "kaybettin" : "savundun") + "</size>", rr.Result.Victory ? UiTheme.PillBad : UiTheme.Pill);
                uiRects.Add(r);
            }
            DrawStatusLine(w, h);
        }

        /// <summary>Durum: yalnızca bildirim veya savunma geçersizken görünür (ana ekran sade kalsın).</summary>
        private void DrawStatusLine(float w, float h)
        {
            bool showToast = Time.time < toastUntil;
            if (!showToast && valid && !editMode) return;
            string msg = showToast ? toast : (valid ? "Savunma hazır" : validationError);
            if (editMode || !valid) msg += "     Yol " + layout.RoadCount + "/" + layout.RoadLimit + "   Giriş " + layout.EntranceCount + "   Kule " + g.Towers.Count + "/" + g.MaxTowers;
            float tw = Mathf.Min(w - 520f, 620f);
            if (tw < 300f) tw = w - 20f;
            var rs = new Rect((w - tw) * 0.5f, (tool == Tool.Building || tool == Tool.Tower) ? 114f : 52f, tw, 34);
            GUI.Label(rs, msg, showToast || valid ? UiTheme.Pill : UiTheme.PillBad);
            uiRects.Add(rs);
        }

        /// <summary>Düzenleme araçları (sol dikey şerit): yol, yol geliştirme, tuzak, giriş, sil.</summary>
        private void DrawEditBar(float w, float h)
        {
            var items = new[] { (Tool.Select, "Seç / Kaydır"), (Tool.Road, "Yol"), (Tool.RoadUp, "Yol Geliştir"), (Tool.Trap, "Tuzak"), (Tool.Entrance, "Giriş"), (Tool.Erase, "Sil") };
            float y = 116f;
            var bg = new Rect(8, y - 8, 160, items.Length * 46 + 16);
            Panel(bg);
            foreach (var (t, n) in items)
            {
                if (SelBtn(new Rect(16, y, 144, 40), n, tool == t)) { tool = t; if (t != Tool.Select) selCell = new GridPos(-1, -1); dirty = true; }
                y += 46f;
            }
            if (tool == Tool.Trap && Btn(new Rect(176, 116 + 3 * 46, 190, 40), "< " + TrapName(selTrap) + " >")) selTrap = (TrapType)(((int)selTrap % 3) + 1);
            if (tool == Tool.RoadUp)
            {
                string info = g.RoadResearchDone ? "Yola dokun: Tahta → Altın+Tahta (" + RoadRules.UpgradeCost(RoadTier.Reinforced) + ") → Demir (" + RoadRules.UpgradeCost(RoadTier.Iron) + ")"
                    : "Önce Akademi'de yol geliştirme araştırması gerekli";
                var r = new Rect(176, 116 + 2 * 46, Mathf.Min(420f, w - 400f), 52);
                GUI.Label(r, "<size=12>" + info + "</size>", g.RoadResearchDone ? UiTheme.Pill : UiTheme.PillBad);
                uiRects.Add(r);
            }
        }

        // ---------- Mağaza ----------
        private void DrawShop(float w, float h)
        {
            var r = ModalRect(w, h);
            Modal(r, "MAĞAZA");
            float bw = 150f;
            for (int i = 0; i < ShopCats.Length; i++)
                if (SelBtn(new Rect(r.x + 20 + i * (bw + 8), r.y + 30, bw, 38), ShopCats[i], shopCat == i)) { shopCat = i; scrollPos = Vector2.zero; }

            float cw = 200f, ch = 232f;
            int perRow = Mathf.Max(1, (int)((r.width - 30f) / (cw + 10f)));
            int count = shopCat < 2 ? ShopBuildings[shopCat].Length : BattleSim.TowerTypeCount;
            var inner = new Rect(0, 0, r.width - 50f, Mathf.Ceil(count / (float)perRow) * (ch + 10f));
            scrollPos = GUI.BeginScrollView(new Rect(r.x + 14, r.y + 78, r.width - 28, r.height - 90), scrollPos, inner);
            for (int i = 0; i < count; i++)
            {
                var cr = new Rect((i % perRow) * (cw + 10f), (i / perRow) * (ch + 10f), cw, ch);
                string name, sub; int have, limit, unlock; ResourceCost cost;
                if (shopCat < 2)
                {
                    var bt = ShopBuildings[shopCat][i]; var d = ContentCatalog.Building(bt);
                    name = d.Name; unlock = d.UnlockLevel; have = g.CountBuildings(bt); limit = g.MaxCountOf(bt);
                    cost = GameRules.Scale(GameRules.BuildCostOf(bt), g.Config, g.Level, 100);
                    sub = bt == BuildingType.VillagerHouse ? "+1 işçi" : (GameRules.ProducedBy(bt).HasValue ? "Üretim" : (d.IsDepot ? "Depo" : (bt == BuildingType.Barracks ? "Asker eğitir" : "Araştırma + görev")));
                    if (bt == BuildingType.VillagerHouse && limit < GameRules.MaxVillagerHouses) sub += " (görevle " + GameRules.MaxVillagerHouses + ")";
                }
                else
                {
                    var tt = (TowerType)i; var d = ContentCatalog.Tower(tt);
                    name = d.Name; unlock = d.UnlockLevel; have = g.Towers.Count; limit = g.MaxTowers;
                    cost = GameRules.Scale(d.BuildCost, g.Config, g.Level, 100);
                    sub = TowerRole(tt) + "\nKule hakkı " + have + "/" + limit + (limit < GameRules.MaxTowerSlots ? " (görevle " + GameRules.MaxTowerSlots + ")" : "");
                }
                bool locked = g.Level < unlock, full = have >= limit;
                GUI.Box(cr, "", UiTheme.Parchment);
                var dark = new GUIStyle(UiTheme.Skin.label) { normal = { textColor = new Color(0.25f, 0.15f, 0.07f) }, alignment = TextAnchor.UpperCenter };
                GUI.Label(new Rect(cr.x + 8, cr.y + 10, cw - 16, 26), "<b>" + name + "</b>", dark);
                UiTheme.Dot(new Rect(cr.x + cw * 0.5f - 34, cr.y + 40, 68, 68), shopCat == 0 ? ResColor(i < 2 ? ResourceType.Wood : (i < 4 ? ResourceType.Stone : (i < 6 ? ResourceType.Gold : ResourceType.Iron))) : (shopCat == 1 ? new Color(0.6f, 0.45f, 0.3f) : new Color(0.55f, 0.58f, 0.65f)));
                GUI.Label(new Rect(cr.x + 8, cr.y + 112, cw - 16, 44), "<size=12>" + sub + "</size>", dark);
                GUI.Label(new Rect(cr.x + 8, cr.y + 158, cw - 16, 22), "<size=13>" + (locked ? "Sv " + unlock + "'de açılır" : (shopCat < 2 ? have + "/" + limit + "   " : "") + cost) + "</size>", dark);
                if (!locked && !full)
                {
                    if (GUI.Button(new Rect(cr.x + 20, cr.y + ch - 46, cw - 40, 36), "Kur", UiTheme.BtnGreen))
                    {
                        if (shopCat < 2) { selBuilding = ShopBuildings[shopCat][i]; tool = Tool.Building; }
                        else { selTower = (TowerType)i; tool = Tool.Tower; }
                        tab = Tab.Village; selCell = new GridPos(-1, -1); dirty = true;
                    }
                }
                else GUI.Label(new Rect(cr.x + 20, cr.y + ch - 46, cw - 40, 36), locked ? "Kilitli" : "Sınırda", UiTheme.PillBad);
            }
            GUI.EndScrollView();
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
                if (b.Type == BuildingType.Barracks) info += "   Ordu +" + (GameRules.ArmyCapacity(g.BarracksLevelSum()) - GameRules.ArmyCapacity(g.BarracksLevelSum() - b.Level));
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
                    var rb = new Rect(r.xMax - 150, r.y + 8, 136, 30);
                    uiRects.Add(rb);
                    if (GUI.Button(rb, "Onar", UiTheme.BtnRed)) { if (g.TryRepair(p, out e)) { Toast("Onarıldı"); dirty = true; } else Toast(e); }
                }
                GUI.Label(new Rect(x, y, iw, 22), "<size=13>" + info + "</size>"); y += 26;
                DrawUpgradeRow(x, y, iw, JobKind.Tower, p, SoldierType.Swordsman);
            }
            else if (layout.Get(p) == CellKind.Road)
            {
                string tierName = layout.GetRoadTier(p) == RoadTier.Iron ? "Demir" : (layout.GetRoadTier(p) == RoadTier.Reinforced ? "Altın+Tahta" : "Tahta");
                GUI.Label(new Rect(x, y, iw, 26), "Yol  <size=14>" + tierName + "</size>", UiTheme.Header); y += 30;
                GUI.Label(new Rect(x, y, iw, 44), (layout.IsEntrance(p) ? "GİRİŞ   " : "") + (g.Traps.TryGetValue(p, out var tr) ? TrapName(tr) + " tuzak   " : "") + "\n<size=12>Geliştirmek için Düzen → Yol Geliştir</size>");
            }
            else GUI.Label(new Rect(x, y, iw, 24), "Boş alan — Mağaza'dan yapı kur");
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
            Modal(r, "ORDU  " + g.ArmySize + "/" + g.ArmyCapacity);
            if (g.BarracksLevelSum() == 0) GUI.Label(new Rect(r.x + 20, r.y + 26, r.width - 40, 22), "<color=#ffb070>Önce Mağaza → Köy'den Kışla kur</color>", UiTheme.Small);
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 48, r.width - 24, r.height - 58));
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            string err;
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
            {
                var t = (SoldierType)i; var d = ContentCatalog.Soldier(t);
                int lvl = g.SoldierLevel(t);
                bool locked = g.Level < d.UnlockLevel;
                GUILayout.BeginHorizontal(GUILayout.Height(44));
                GUILayout.Label((locked ? "<color=#ffb070>[Sv " + d.UnlockLevel + "'de açılır]</color> " : "") + "<b>" + d.Name + "</b>  <color=#ffd560>" + VillageArt.RankName(lvl) + "</color>  Sv " + lvl + "  adet " + g.Inventory.Get(t) + "\n<size=13>Can " + ContentCatalog.Scaled(d.Hp, lvl) + "  Hasar " + ContentCatalog.Scaled(d.Attack, lvl) + "  Hız " + d.SpeedMilli + "</size>", GUILayout.Width(320));
                GUI.enabled = !locked;
                int ts = Mathf.Max(5, (int)((long)d.TrainSeconds * (1000 - g.Research.Total(ResearchStat.TrainTime)) / 1000));
                if (GUILayout.Button("Eğit\n" + d.Cost + " | " + Dur(ts), GUILayout.Width(210))) { if (g.TryTrain(t, out err)) AddLog(d.Name + " eğitimi sıraya alındı"); else Toast(err); }
                var up = g.PreviewUpgrade(JobKind.Soldier, new GridPos(0, 0), t);
                string vis = VillageArt.LevelVisualName(up.TargetLevel);
                string utxt = (up.Block != null ? "Yükselt: " + up.Block : "Sv " + up.TargetLevel + " yükselt  " + up.Cost + " | " + Dur(up.Seconds)) + (vis.Length > 0 ? "\n<size=12><color=#ffd560>+ " + vis + "</color></size>" : "");
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
            var r = ModalRect(w, h);
            Modal(r, "AKADEMİ");
            float bw = Mathf.Min(110f, (r.width - 16f) / TreeNames.Length - 2f);
            for (int i = 0; i < TreeNames.Length; i++)
            {
                if (SelBtn(new Rect(r.x + 8 + i * (bw + 2), r.y + 30, bw, 32), TreeNames[i], researchTree == i)) { researchTree = i; scrollPos = Vector2.zero; }
            }

            string head = "Akademi Sv " + g.UniversityLevel + (g.Level < ResearchState.UniversityUnlockLevel ? " (Sv5'te açılır, sonra Akademi kur)" : "") + "   Yuva " + g.Research.Active.Count + "/" + g.Research.Slots;
            foreach (var a in g.Research.Active) head += "   | " + ResearchCatalog.Get(a.Id).Name + " " + Dur(a.RemainingSeconds);
            GUI.Label(new Rect(r.x + 12, r.y + 68, r.width - 360, 24), head, UiTheme.Small);
            // yol geliştirme araştırması (approved #22)
            string rtxt = g.RoadResearchDone ? "Yol geliştirme: AÇIK" : (g.RoadResearchRemaining > 0 ? "Yol araştırması: " + Dur(g.RoadResearchRemaining) : "Yol geliştirme araştır  " + RoadRules.ResearchCost);
            var rrb = new Rect(r.xMax - 340, r.y + 66, 326, 30);
            uiRects.Add(rrb);
            if (GUI.Button(rrb, "<size=13>" + rtxt + "</size>", g.RoadResearchDone ? UiTheme.BtnGreen : GUI.skin.button) && !g.RoadResearchDone)
            { string re; if (g.TryStartRoadResearch(out re)) Toast("Yol geliştirme araştırması başladı (" + Dur(RoadRules.ResearchDurationSeconds) + ")"); else Toast(re); }

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
                if (g.TryClaimAcademy(academyId, out aerr)) { popups.Add("YENİ HAK!\n" + AcademyTasks.Get(academyId).RewardText + "\nKöylü evi: " + g.MaxVillagerHouses + "/" + GameRules.MaxVillagerHouses + "   Kule: " + g.MaxTowers + "/" + GameRules.MaxTowerSlots); dirty = true; }
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
            Modal(r, "SALDIR  —  " + (g.ArmySize - g.Queue.Items.Count) + " asker hazır");
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
            GUI.Label(new Rect(r.x + 12, r.y + 334, r.width - 24, 120), "<color=#d8c9a8>İpucu: askerleri kırmızı alana dokunarak (basılı tutarak) gönder. Duvarlar yer askerlerini üstüne çeker (uçanlar geçer), Elf kulesi periye, Top kalabalığa güçlüdür; Buz yavaşlatır, Şimşek zincirleme vurur. Zafer için yapıların ortalama hasarı %51'i geçmeli.</color>");
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
