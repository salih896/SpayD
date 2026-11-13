using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Grid;
using GameCore.Military;
using GameCore.Progression;
using GameCore.Village;

namespace GameCore.Game
{
    /// <summary>
    /// Oyun durumunu düz metne yazar/okur (sürümlü, bağımlılıksız). İstemci yerel kayıt için kullanır;
    /// gerçek oyuncu verisi sunucuda tutulacağında aynı alanlar veritabanına eşlenir.
    /// </summary>
    public static class GameSave
    {
        public const int Version = 1;
        private static string I(long v) => v.ToString(CultureInfo.InvariantCulture);

        public static string Save(GameState g, long unixSeconds)
        {
            var sb = new StringBuilder();
            sb.AppendLine("v " + Version);
            sb.AppendLine("t " + I(unixSeconds));
            sb.AppendLine("xp " + I(g.TotalXp));
            sb.AppendLine("w " + g.Wallet.Get(ResourceType.Wood) + " " + g.Wallet.Get(ResourceType.Stone) + " " + g.Wallet.Get(ResourceType.Gold) + " " + g.Wallet.Get(ResourceType.Iron));
            sb.AppendLine("acc " + I(g.ProdAccumMilli[0]) + " " + I(g.ProdAccumMilli[1]) + " " + I(g.ProdAccumMilli[2]) + " " + I(g.ProdAccumMilli[3]));
            sb.AppendLine("stats " + g.BattlesFought + " " + g.BattlesWon + " " + g.TotalStars + " " + g.LastRaidDay);
            sb.AppendLine("layout " + g.Layout.Size + " " + g.Layout.RoadLimit + " " + g.Layout.EntranceLimit);
            foreach (var p in g.Layout.CellsOf(CellKind.Road)) sb.AppendLine("road " + p.X + " " + p.Y + " " + (int)g.Layout.GetRoadTier(p));
            foreach (var p in g.Layout.EntrancePositions()) sb.AppendLine("ent " + p.X + " " + p.Y);
            foreach (var kv in g.Towers) sb.AppendLine("tower " + kv.Key.X + " " + kv.Key.Y + " " + (int)kv.Value.Type + " " + kv.Value.Level);
            foreach (var kv in g.Buildings) sb.AppendLine("build " + kv.Key.X + " " + kv.Key.Y + " " + (int)kv.Value.Type + " " + kv.Value.Level);
            foreach (var kv in g.Traps) sb.AppendLine("trap " + kv.Key.X + " " + kv.Key.Y + " " + (int)kv.Value);
            foreach (var kv in g.Wears) sb.AppendLine("wear " + kv.Key.X + " " + kv.Key.Y + " " + kv.Value.PermanentDamagePct + " " + kv.Value.AttackCount);
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
            {
                var t = (SoldierType)i;
                if (g.Inventory.Get(t) > 0) sb.AppendLine("inv " + i + " " + g.Inventory.Get(t));
                if (g.SoldierLevel(t) > 1) sb.AppendLine("slv " + i + " " + g.SoldierLevel(t));
            }
            foreach (var it in g.Queue.Items)
                sb.AppendLine("q " + (int)it.Type + " " + it.RemainingSeconds + " " + (it.Started ? 1 : 0) + " " + it.Cost.Wood + " " + it.Cost.Stone + " " + it.Cost.Gold + " " + it.Cost.Iron);
            foreach (var id in g.Research.Done) sb.AppendLine("rdone " + id);
            foreach (var a in g.Research.Active) sb.AppendLine("ract " + a.Id + " " + a.RemainingSeconds);
            foreach (var kv in g.Quests.Progress) sb.AppendLine("qp " + kv.Key + " " + kv.Value + " " + (g.Quests.IsClaimed(kv.Key) ? 1 : 0));
            sb.AppendLine("qd " + g.Quests.DailyDay + " " + string.Join(",", g.Quests.DailyIds));
            if (g.LastRaid != null && g.LastRaid.Result != null)
            {
                var rr = g.LastRaid; var lo = rr.LootLost;
                sb.AppendLine("raid " + rr.Day + " " + (rr.Result.Victory ? 1 : 0) + " " + rr.Result.Stars + " " + rr.Result.OverallDamagePct + " "
                    + lo.Wood + " " + lo.Stone + " " + lo.Gold + " " + lo.Iron + " " + rr.TowersDamaged);
            }
            foreach (var j in g.Jobs) sb.AppendLine("job " + (int)j.Kind + " " + j.Pos.X + " " + j.Pos.Y + " " + (int)j.Soldier + " " + j.TargetLevel + " " + j.RemainingSeconds);
            return sb.ToString();
        }

        /// <summary>Kaydı okur. Bozuk/boş içerikte null döner (istemci yeni oyun başlatır). unixSeconds: kaydın alındığı an.</summary>
        public static GameState Load(string text, ResourceConfig cfg, out long unixSeconds)
        {
            unixSeconds = 0;
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                var g = new GameState(cfg);
                var lines = text.Split('\n');
                VillageLayout layout = null;
                var roadRows = new List<int[]>();
                var entRows = new List<int[]>();
                var towerRows = new List<int[]>();
                var buildRows = new List<int[]>();
                var daily = new List<string>();
                int dailyDay = -1;
                var qpRows = new List<string[]>();
                bool versionOk = false;
                foreach (var raw in lines)
                {
                    var l = raw.Trim();
                    if (l.Length == 0) continue;
                    var p = l.Split(' ');
                    switch (p[0])
                    {
                        case "v": versionOk = int.Parse(p[1]) == Version; break;
                        case "t": unixSeconds = long.Parse(p[1], CultureInfo.InvariantCulture); break;
                        case "xp": g.TotalXp = long.Parse(p[1], CultureInfo.InvariantCulture); break;
                        case "w":
                            g.Wallet.SetAmount(ResourceType.Wood, int.Parse(p[1])); g.Wallet.SetAmount(ResourceType.Stone, int.Parse(p[2]));
                            g.Wallet.SetAmount(ResourceType.Gold, int.Parse(p[3])); g.Wallet.SetAmount(ResourceType.Iron, int.Parse(p[4])); break;
                        case "acc": for (int i = 0; i < 4; i++) g.ProdAccumMilli[i] = long.Parse(p[i + 1], CultureInfo.InvariantCulture); break;
                        case "stats": g.BattlesFought = int.Parse(p[1]); g.BattlesWon = int.Parse(p[2]); g.TotalStars = int.Parse(p[3]); g.LastRaidDay = int.Parse(p[4]); break;
                        case "layout": layout = new VillageLayout(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3])); break;
                        case "road": roadRows.Add(new[] { int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]) }); break;
                        case "ent": entRows.Add(new[] { int.Parse(p[1]), int.Parse(p[2]) }); break;
                        case "tower": towerRows.Add(new[] { int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]) }); break;
                        case "build": buildRows.Add(new[] { int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]) }); break;
                        case "trap": g.Traps[new GridPos(int.Parse(p[1]), int.Parse(p[2]))] = (TrapType)int.Parse(p[3]); break;
                        case "wear": g.Wears[new GridPos(int.Parse(p[1]), int.Parse(p[2]))] = new TowerWear(int.Parse(p[3]), int.Parse(p[4])); break;
                        case "inv": g.Inventory.Add((SoldierType)int.Parse(p[1]), int.Parse(p[2])); break;
                        case "slv": g.SoldierLevels[(SoldierType)int.Parse(p[1])] = int.Parse(p[2]); break;
                        case "q":
                            g.Queue.RestoreItem(new ProductionItem
                            {
                                Type = (SoldierType)int.Parse(p[1]), RemainingSeconds = int.Parse(p[2]), Started = p[3] == "1",
                                Cost = new ResourceCost(int.Parse(p[4]), int.Parse(p[5]), int.Parse(p[6]), int.Parse(p[7]))
                            }); break;
                        case "rdone": g.Research.ForceComplete(p[1]); break;
                        case "ract": g.Research.ForceActive(p[1], int.Parse(p[2])); break;
                        case "qp": qpRows.Add(p); break;
                        case "qd":
                            dailyDay = int.Parse(p[1]);
                            if (p.Length > 2 && p[2].Length > 0) daily.AddRange(p[2].Split(','));
                            break;
                        case "raid": // yalnızca rapor özeti saklanır (ayrıntılı savaş kaydı sunucu aşamasında)
                            g.LastRaid = new RaidReport
                            {
                                Day = int.Parse(p[1]),
                                Result = new BattleResult { Victory = p[2] == "1", Stars = int.Parse(p[3]), OverallDamagePct = int.Parse(p[4]) },
                                LootLost = new ResourceCost(int.Parse(p[5]), int.Parse(p[6]), int.Parse(p[7]), int.Parse(p[8])),
                                TowersDamaged = int.Parse(p[9])
                            };
                            break;
                        case "job":
                            g.Jobs.Add(new UpgradeJob { Kind = (JobKind)int.Parse(p[1]), Pos = new GridPos(int.Parse(p[2]), int.Parse(p[3])), Soldier = (SoldierType)int.Parse(p[4]), TargetLevel = int.Parse(p[5]), RemainingSeconds = int.Parse(p[6]) });
                            break;
                    }
                }
                if (!versionOk || layout == null) return null;
                string e;
                foreach (var r in roadRows) layout.TryPlace(new GridPos(r[0], r[1]), CellKind.Road, out e, (RoadTier)r[2]);
                foreach (var r in entRows) layout.ToggleEntrance(new GridPos(r[0], r[1]), out e);
                foreach (var r in towerRows) { var pos = new GridPos(r[0], r[1]); if (layout.TryPlace(pos, CellKind.Tower, out e)) g.Towers[pos] = new TowerSpec((TowerType)r[2], r[3], 0); }
                foreach (var r in buildRows) { var pos = new GridPos(r[0], r[1]); if (layout.TryPlace(pos, CellKind.Building, out e)) g.Buildings[pos] = new BuildingSpec((BuildingType)r[2], r[3]); }
                g.Layout = layout;
                foreach (var kv in new List<GridPos>(g.Towers.Keys)) if (!g.Wears.ContainsKey(kv)) g.Wears[kv] = new TowerWear();
                foreach (var row in qpRows) g.Quests.Restore(row[1], int.Parse(row[2]), row[3] == "1");
                g.Quests.RestoreDaily(dailyDay, daily);
                g.RecomputeCapacity();
                return g;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
