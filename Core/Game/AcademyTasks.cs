using System;
using System.Collections.Generic;
using GameCore.Combat;

namespace GameCore.Game
{
    public enum AcademyReward { VillagerHouse, TowerSlot }

    /// <summary>Akademi görevi: tek seferlik, ilerlemesi oyun durumundan okunur (sayaç tutulmaz), ödülü kalıcı bir yapı hakkıdır.</summary>
    public sealed class AcademyTask
    {
        public string Id, Name;
        public AcademyReward Reward;
        public int Target;
        public Func<GameState, int> Progress;
        public string RewardText => Reward == AcademyReward.VillagerHouse ? "+1 Köylü Evi hakkı" : "+1 Kule hakkı";
    }

    /// <summary>
    /// Akademi görevleri (kullanıcı kararı 14.11.2026, approved-mechanics #37-#39):
    /// köylü evi başta 1 → görevlerle en çok 3; kule başta 5 → görevlerle en çok 8.
    /// Görevler yalnızca Akademi kuruluyken alınabilir. Hedef sayıları GEÇİCİDİR.
    /// </summary>
    public static class AcademyTasks
    {
        public static readonly IReadOnlyList<AcademyTask> All = new List<AcademyTask>
        {
            new AcademyTask { Id = "a_house1", Name = "Akademi'yi kur", Reward = AcademyReward.VillagerHouse, Target = 1, Progress = g => g.CountBuildings(BuildingType.University) },
            new AcademyTask { Id = "a_house2", Name = "3 araştırma tamamla", Reward = AcademyReward.VillagerHouse, Target = 3, Progress = g => g.Research.Done.Count },
            new AcademyTask { Id = "a_tower1", Name = "Akademi'yi Sv3'e yükselt", Reward = AcademyReward.TowerSlot, Target = 3, Progress = g => g.UniversityLevel },
            new AcademyTask { Id = "a_tower2", Name = "5 savaş kazan", Reward = AcademyReward.TowerSlot, Target = 5, Progress = g => g.BattlesWon },
            new AcademyTask { Id = "a_tower3", Name = "15 araştırma tamamla", Reward = AcademyReward.TowerSlot, Target = 15, Progress = g => g.Research.Done.Count },
        };

        public static AcademyTask Get(string id)
        {
            foreach (var t in All) if (t.Id == id) return t;
            return null;
        }
    }
}
