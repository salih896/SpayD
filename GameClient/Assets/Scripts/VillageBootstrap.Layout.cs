using System.Collections.Generic;
using UnityEngine;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Game;
using GameCore.Grid;
using GameCore.Progression;
using GameCore.Village;

namespace GameClient
{
    /// <summary>
    /// Köy Düzeni modu (kullanıcı kararı 15.11.2026): yollar dahil her şey envanterde; sürükle-bırak ile yerleştirilir.
    /// Bu moda girmeden hiçbir yapı kurulamaz. "Onayla" kuralları denetler (yollar bağlı, yapılar yola bağlı, sınırlar,
    /// maliyet) ve geçerse yenilikleri köye aktarır; geçmezse ekranın ortasında bildirim çıkar.
    /// </summary>
    public sealed partial class VillageBootstrap
    {
        private static readonly string[] InvTabs = { "Envanter", "Bina", "Kule", "Tuzak" };
        private static readonly BuildingType[] ShopBuildingOrder =
        {
            BuildingType.Forest, BuildingType.LumberDepot, BuildingType.VillagerHouse, BuildingType.Barracks, BuildingType.StoneQuarry, BuildingType.StoneDepot,
            BuildingType.University, BuildingType.GoldMine, BuildingType.Bank, BuildingType.IronMine, BuildingType.IronDepot
        };

        private LayoutDraft draft;
        private bool LayoutMode => draft != null;
        private int invTab;
        private float invScroll;
        private DraftItem dragItem;
        private bool dragFromCell;
        private GridPos dragOrigin;
        private bool entranceMode;
        private Rect invPanelGui, sellZoneGui;
        private Transform ghostRoot;
        private int ghostId = -1;
        private HashSet<GridPos> problemCells = new HashSet<GridPos>();
        private string draftError = "";
        private bool draftOk;

        private const float InvPanelH = 168f;

        /// <summary>Zıplayan aşağı ok (arayüz): tip noktası p; üstünde kısa açıklama.</summary>
        private void UiArrow(Vector2 p, string label)
        {
            float bob = Mathf.Abs(Mathf.Sin(Time.time * 5f)) * 8f;
            var r = new Rect(p.x - 16f, p.y - 34f - bob, 32f, 32f);
            GUI.DrawTexture(r, UiTheme.ArrowDown);
            if (!string.IsNullOrEmpty(label))
            {
                var st = new GUIStyle(UiTheme.PillBad) { fontSize = 12 };
                float tw = st.CalcSize(new GUIContent(label)).x + 20f;
                GUI.Label(new Rect(p.x - tw * 0.5f, r.y - 24f, tw, 22f), label, st);
            }
        }

        /// <summary>Köyde oklarla gösterilecek hücreler: kurala uymayanlar + giriş yoksa alt kenar.</summary>
        private List<GridPos> ArrowCells()
        {
            var list = new List<GridPos>(problemCells);
            if (draft != null && draft.Entrances.Count == 0)
            {
                bool any = false;
                foreach (var kv in draft.Cells) if (kv.Key.Y == 0 && kv.Value.Kind == DraftKind.Road) { list.Add(kv.Key); any = true; }
                if (!any) list.Add(new GridPos(draft.Size / 2, 0));
            }
            return list;
        }

        private void EnterLayout()
        {
            draft = new LayoutDraft(g);
            tab = Tab.Village;
            selCell = new GridPos(-1, -1);
            invTab = draft.Inventory.Count > 0 ? 0 : 1;
            invScroll = 0f;
            entranceMode = false;
            dirty = true;
        }

        private void ExitLayout()
        {
            draft = null;
            dragItem = null;
            entranceMode = false;
            HideGhost();
            dirty = true;
        }

        private void ConfirmLayout()
        {
            string err;
            if (g.TryApplyLayout(draft, out err))
            {
                ExitLayout();
                FlushLevelUps();
                Toast("Köy düzeni kaydedildi");
                LastSave();
            }
            else popups.Add("DÜZEN KABUL EDİLMEDİ\n" + err);
        }

        // ---------- Girdi ----------
        private void HandleLayoutInput(Vector2 pos, bool down, bool held, bool pinching)
        {
            draft.SyncRoadLimit();
            if (dragItem != null)
            {
                UpdateGhost(pos);
                if (held) return;
                DropDragged(pos);
                return;
            }
            if (pinching) return;
            if (down)
            {
                dragging = false;
                pressStart = pos;
                if (dragBlocked) return;
                if (!TryGetCell(pos, out var cell)) return;
                if (entranceMode)
                {
                    string e;
                    if (draft.ToggleEntrance(cell, out e)) dirty = true; else Toast(e);
                    dragBlocked = true;
                    return;
                }
                if (draft.At(cell) != null)
                {
                    string e;
                    var it = draft.PickUp(cell, out e);
                    if (it == null) { Toast(e); dragBlocked = true; return; }
                    dragItem = it; dragFromCell = true; dragOrigin = cell;
                    dirty = true;
                    UpdateGhost(pos);
                }
                return;
            }
            // boş zemin sürükleme = kaydır
            if (held && !dragBlocked)
            {
                if (!dragging && (pos - pressStart).sqrMagnitude > 144f) dragging = true;
                if (dragging)
                {
                    var ptr = UnityEngine.InputSystem.Pointer.current;
                    Vector2 delta = ptr != null ? ptr.delta.ReadValue() : Vector2.zero;
                    if (RayToPlane(pos, out var cur) && RayToPlane(pos - delta, out var prev)) target += prev - cur;
                }
            }
        }

        private bool OverGui(Rect r, Vector2 screen)
        {
            var gp = ScreenToGui(screen);
            return r.Contains(gp);
        }

        private void DropDragged(Vector2 pos)
        {
            var it = dragItem;
            dragItem = null;
            HideGhost();
            string err = null;
            bool placed = false;
            if (OverGui(sellZoneGui, pos))
            {
                if (draft.Sell(it, out err)) { Toast(it.IsNew ? "Satın alma iptal edildi" : LayoutDraft.NameOf(it) + " satılacak (onayda %50 iade)"); dirty = true; return; }
            }
            else if (!OverGui(invPanelGui, pos) && TryGetCell(pos, out var cell))
            {
                placed = draft.Place(it, cell, out err);
                if (placed) { FxKit.Construction(null, new Vector3(cell.X, 0f, cell.Y)); DropPuff(new Vector3(cell.X, 0f, cell.Y)); } // yerleşti: toz + halka
                else Shake(0.06f, 0.2f); // yerleşmedi: hafif sarsıntı
                if (!placed && dragFromCell) draft.Place(it, dragOrigin, out _); // geçersizse eski yerine dön
            }
            else if (dragFromCell && !OverGui(invPanelGui, pos)) draft.Place(it, dragOrigin, out _);
            // yeni satın alma yerleşmediyse iptal (envanterde ödenmemiş öğe kalmasın)
            if (!placed && it.IsNew && draft.Inventory.Contains(it)) draft.Sell(it, out _);
            if (!string.IsNullOrEmpty(err)) Toast(err);
            dirty = true;
        }

        // ---------- Hayalet (sürüklenen öğenin önizlemesi) ----------
        private void UpdateGhost(Vector2 screen)
        {
            if (dragItem == null) { HideGhost(); return; }
            if (ghostRoot == null || ghostId != dragItem.Id)
            {
                HideGhost();
                ghostRoot = new GameObject("Ghost").transform;
                ghostId = dragItem.Id;
                var p = Vector3.zero;
                switch (dragItem.Kind)
                {
                    case DraftKind.Building: VillageArt.BuildBuilding(ghostRoot, dragItem.Building, draft.LevelOf(dragItem), 7, p); break;
                    case DraftKind.Tower: VillageArt.BuildTower(ghostRoot, dragItem.Tower, draft.LevelOf(dragItem), false, p); break;
                    case DraftKind.Trap: VillageArt.BuildTrap(ghostRoot, p, dragItem.Trap); break;
                    default: VillageArt.BuildRoad(ghostRoot, p, dragItem.Tier, new[] { false, false, false, false }, 7); break;
                }
                VillageArt.BuildSelection(ghostRoot, p);
            }
            bool overUi = OverGui(invPanelGui, screen);
            if (!overUi && RayToPlane(screen, out var hit))
            {
                ghostRoot.gameObject.SetActive(true);
                var cell = new Vector3(Mathf.Round(hit.x), 0.06f, Mathf.Round(hit.z));
                ghostRoot.position = layout.InBounds(new GridPos((int)cell.x, (int)cell.z)) ? cell : new Vector3(hit.x, 0.06f, hit.z);
            }
            else ghostRoot.gameObject.SetActive(false);
        }

        private void HideGhost()
        {
            if (ghostRoot != null) Destroy(ghostRoot.gameObject);
            ghostRoot = null;
            ghostId = -1;
        }

        // ---------- Arayüz ----------
        private struct InvGroup { public DraftItem First; public int Count; }

        private List<InvGroup> InventoryGroups()
        {
            var list = new List<InvGroup>();
            var idx = new Dictionary<string, int>();
            foreach (var it in draft.Inventory)
            {
                int sub = it.Kind == DraftKind.Building ? (int)it.Building : it.Kind == DraftKind.Tower ? (int)it.Tower : it.Kind == DraftKind.Trap ? (int)it.Trap : (int)it.Tier;
                string key = it.Kind + ":" + sub + ":" + (it.HasOrigin ? draft.LevelOf(it) : 0);
                if (idx.TryGetValue(key, out int i)) { var gr = list[i]; gr.Count++; list[i] = gr; }
                else { idx[key] = list.Count; list.Add(new InvGroup { First = it, Count = 1 }); }
            }
            return list;
        }

        private static Color ItemColor(DraftItem it)
        {
            switch (it.Kind)
            {
                case DraftKind.Building: return BuildingColor(it.Building);
                case DraftKind.Tower: return TowerColor(it.Tower);
                case DraftKind.Trap: return TrapColor(it.Trap);
                default: return new Color(0.62f, 0.48f, 0.32f);
            }
        }

        private void DrawLayoutMode(float w, float h)
        {
            var dark = new GUIStyle(UiTheme.Skin.label) { normal = { textColor = new Color(0.25f, 0.15f, 0.07f) }, alignment = TextAnchor.UpperCenter, fontSize = 13 };

            // üst bilgi şeridi
            string status = "KÖY DÜZENİ   Yol " + draft.PlacedRoads + "/" + draft.RoadLimit + "   Kule " + draft.TowerCount + "/" + g.MaxTowers + "   Tuzak " + draft.TrapCount + "/" + g.MaxTraps
                + "   Kışla " + draft.CountBuildings(BuildingType.Barracks) + "/" + g.MaxCountOf(BuildingType.Barracks);
            var cost = draft.NewCost();
            if (cost.Wood + cost.Stone + cost.Gold + cost.Iron > 0) status += "   Ödenecek: " + cost;
            var refund = draft.Refund();
            if (refund.Wood + refund.Stone + refund.Gold + refund.Iron > 0) status += "   İade: " + refund;
            var sr = new Rect(Mathf.Max(8f, w * 0.5f - 420f), 104f, Mathf.Min(w - 16f, 840f), 30f);
            GUI.Label(sr, "<size=13>" + status + "</size>", UiTheme.Pill); uiRects.Add(sr);
            bool ok = draftOk; // Rebuild'de (düzen değişince) hesaplanır
            var vr = new Rect(sr.x, sr.yMax + 4f, sr.width, 26f);
            bool showToast = Time.time < toastUntil;
            GUI.Label(vr, "<size=12>" + (showToast ? toast : (ok ? "Düzen geçerli — Onayla ile köye aktar" : draftError)) + "</size>", ok && !showToast ? UiTheme.Pill : UiTheme.PillBad); uiRects.Add(vr);

            // alt panel: sekmeler + kartlar + düğmeler
            invPanelGui = new Rect(0f, h - InvPanelH, w, InvPanelH);
            uiRects.Add(invPanelGui);
            GUI.Box(invPanelGui, "");
            float tx = 10f;
            for (int i = 0; i < InvTabs.Length; i++)
            {
                string label = InvTabs[i] + (i == 0 ? " " + draft.Inventory.Count : "");
                float tw = i == 0 ? 136f : 100f;
                if (SelBtn(new Rect(tx, h - InvPanelH + 6f, tw, 30f), label, invTab == i)) { invTab = i; invScroll = 0f; }
                tx += tw + 4f;
            }
            if (PressBtn(new Rect(tx + 8f, h - InvPanelH + 6f, 170f, 30f), "<size=13>Tümünü envantere al</size>", UiTheme.BtnRed))
            {
                int n = draft.PickUpAll();
                invTab = 0; invScroll = 0f; entranceMode = false; dirty = true;
                Toast(n > 0 ? n + " öğe envantere alındı" + (draft.Cells.Count > 0 ? " (geliştirilen yollar yerinde kaldı)" : "") : "Alınacak bir şey yok");
            }
            // eksik: envanterde yerleştirilmemiş öğe varsa Envanter sekmesini ok ile göster
            if (draft.Inventory.Count > 0)
                UiArrow(new Vector2(10f + 68f, h - InvPanelH - 2f), "Yerleştir");
            float btnW = 120f, bx = w - btnW - 10f;
            if (GUI.Button(new Rect(bx, h - InvPanelH + 8f, btnW, 46f), "ONAYLA", UiTheme.BtnGreen)) { ConfirmLayout(); return; }
            if (GUI.Button(new Rect(bx, h - InvPanelH + 60f, btnW, 40f), "İptal", UiTheme.BtnRed)) { ExitLayout(); Toast("Değişiklikler atıldı"); return; }
            if (GUI.Button(new Rect(bx, h - InvPanelH + 106f, btnW, 40f), entranceMode ? "Giriş: AÇIK" : "Giriş", entranceMode ? UiTheme.BtnOn : GUI.skin.button)) entranceMode = !entranceMode;
            sellZoneGui = new Rect(bx - 96f, h - InvPanelH + 8f, 88f, 138f);
            GUI.Box(sellZoneGui, dragItem != null ? "<b>SAT</b>\n\nburaya\nbırak" : "<size=12>Satmak\niçin\nsürükle\nbırak</size>", dragItem != null ? UiTheme.PillBad : UiTheme.Skin.box);

            // kart alanı
            var area = new Rect(10f, h - InvPanelH + 42f, sellZoneGui.x - 20f, InvPanelH - 50f);
            float cw = 112f, ch = area.height - 4f;
            var groups = InventoryGroups();
            var shopTowers = new List<TowerType>(); // kaldırılmış eski kule türleri (Sv 999) listelenmez
            for (int ti = 0; ti < BattleSim.TowerTypeCount; ti++) if (ContentCatalog.Tower((TowerType)ti).UnlockLevel <= ProgressionRules.MaxLevel) shopTowers.Add((TowerType)ti);
            int count = invTab == 0 ? groups.Count : (invTab == 1 ? ShopBuildingOrder.Length : invTab == 2 ? shopTowers.Count : TrapCatalog.Count);
            float content = count * (cw + 6f);
            float maxScroll = Mathf.Max(0f, content - (area.width - 60f));
            invScroll = Mathf.Clamp(invScroll, 0f, maxScroll);
            if (maxScroll > 0f)
            {
                if (Btn(new Rect(area.x, area.y + ch * 0.5f - 22f, 26f, 44f), "<")) invScroll -= cw * 2f;
                if (Btn(new Rect(area.xMax - 26f, area.y + ch * 0.5f - 22f, 26f, 44f), ">")) invScroll += cw * 2f;
            }
            var inner = new Rect(area.x + 30f, area.y, area.width - 60f, area.height);
            GUI.BeginGroup(inner);
            if (invTab == 0 && groups.Count == 0)
                GUI.Label(new Rect(0, 30, inner.width, 40), "<color=#d8c9a8>Envanter boş. Köydeki bir yapıyı sürükleyip buraya bırakabilir, Bina/Kule/Tuzak sekmelerinden yeni yapı alabilirsin.</color>");
            for (int i = 0; i < count; i++)
            {
                var cr = new Rect(i * (cw + 6f) - invScroll, 0f, cw, ch);
                if (cr.xMax < 0f || cr.x > inner.width) continue;
                string title, sub; Color col; bool enabled = true;
                DraftKind kind = DraftKind.Building; int type = 0;
                DraftItem invItem = null;
                if (invTab == 0)
                {
                    var gr = groups[i]; invItem = gr.First;
                    title = LayoutDraft.NameOf(gr.First) + (gr.Count > 1 ? "  x" + gr.Count : "");
                    sub = gr.First.Kind == DraftKind.Road ? "Yol hakkı" : (gr.First.HasOrigin ? "Sv " + draft.LevelOf(gr.First) : "Yeni: " + draft.CostOf(gr.First));
                    col = ItemColor(gr.First);
                }
                else if (invTab == 1)
                {
                    var bt = ShopBuildingOrder[i]; var d = ContentCatalog.Building(bt);
                    kind = DraftKind.Building; type = (int)bt;
                    int have = draft.CountBuildings(bt), lim = g.MaxCountOf(bt);
                    bool locked = g.Level < d.UnlockLevel;
                    title = d.Name;
                    sub = locked ? "Sv " + d.UnlockLevel + "'de açılır" : have + "/" + lim + "\n" + GameRules.Scale(GameRules.BuildCostOf(bt), g.Config, g.Level, 100);
                    enabled = !locked && have < lim;
                    col = BuildingColor(bt);
                }
                else if (invTab == 2)
                {
                    var tt = shopTowers[i]; var d = ContentCatalog.Tower(tt);
                    kind = DraftKind.Tower; type = (int)tt;
                    bool locked = g.Level < d.UnlockLevel;
                    title = d.Name;
                    sub = locked ? "Sv " + d.UnlockLevel + "'de açılır" : "Kule " + draft.TowerCount + "/" + g.MaxTowers + "\n" + GameRules.Scale(d.BuildCost, g.Config, g.Level, 100);
                    enabled = !locked && draft.TowerCount < g.MaxTowers;
                    col = TowerColor(tt);
                }
                else
                {
                    var tp = (TrapType)(i + 1); var d = TrapCatalog.Get(tp);
                    kind = DraftKind.Trap; type = (int)tp;
                    bool locked = g.Level < d.UnlockLevel;
                    title = d.Name;
                    sub = locked ? "Sv " + d.UnlockLevel + "'de açılır" : "Tuzak " + draft.TrapCount + "/" + g.MaxTraps + "\n" + GameRules.Scale(d.Cost, g.Config, g.Level, 100);
                    enabled = !locked && draft.TrapCount < g.MaxTraps;
                    col = TrapColor(tp);
                }
                GUI.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                GUI.Box(cr, "", UiTheme.Parchment);
                GUI.color = Color.white;
                // 3B önizleme (hazır değilse renkli daire)
                Texture prev = null;
                if (invTab == 0 && invItem != null)
                {
                    int lv = invItem.HasOrigin ? draft.LevelOf(invItem) : 1;
                    if (invItem.Kind == DraftKind.Building) prev = PreviewIcons.Building(invItem.Building, lv);
                    else if (invItem.Kind == DraftKind.Tower) prev = PreviewIcons.Tower(invItem.Tower, TowerElement.None, lv);
                    else if (invItem.Kind == DraftKind.Trap) prev = PreviewIcons.Trap(invItem.Trap);
                }
                else if (invTab == 1) prev = PreviewIcons.Building((BuildingType)type, 1);
                else if (invTab == 2) prev = PreviewIcons.Tower((TowerType)type, TowerElement.None, 1);
                else if (invTab == 3) prev = PreviewIcons.Trap((TrapType)type);
                if (prev != null)
                {
                    GUI.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                    GUI.DrawTexture(new Rect(cr.x + cw * 0.5f - 22f, cr.y + 1f, 44f, 44f), prev, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                }
                else UiTheme.Dot(new Rect(cr.x + cw * 0.5f - 18f, cr.y + 6f, 36f, 36f), col);
                GUI.Label(new Rect(cr.x + 4f, cr.y + 42f, cw - 8f, 22f), "<b>" + title + "</b>", dark);
                GUI.Label(new Rect(cr.x + 4f, cr.y + 62f, cw - 8f, ch - 62f), "<size=11>" + sub + "</size>", dark);
                // basınca sürükleme başlar
                var e = Event.current;
                if (e.type == EventType.MouseDown && cr.Contains(e.mousePosition) && dragItem == null)
                {
                    e.Use();
                    if (invTab == 0) { dragItem = invItem; dragFromCell = false; }
                    else
                    {
                        string err;
                        var it = draft.AddNew(kind, type, out err);
                        if (it == null) Toast(err);
                        else { dragItem = it; dragFromCell = false; }
                    }
                }
            }
            GUI.EndGroup();

            // sürüklenen öğe etiketi (parmağın yanında)
            if (dragItem != null)
            {
                var mp = Event.current.mousePosition;
                GUI.Label(new Rect(mp.x + 18f, mp.y - 30f, 220f, 26f), "<size=12>" + LayoutDraft.NameOf(dragItem) + "</size>", UiTheme.Pill);
            }
            if (entranceMode)
            {
                var r = new Rect(w * 0.5f - 230f, h - InvPanelH - 40f, 460f, 32f);
                GUI.Label(r, "<size=12>Alt kenardaki yola dokun: giriş aç / kapat</size>", UiTheme.Pill); uiRects.Add(r);
            }
        }
    }
}
