namespace GameCore.Economy
{
    /// <summary>Oyundaki 4 kaynak. Yiyecek kaynağı YOKTUR.</summary>
    public enum ResourceType { Wood = 0, Stone = 1, Gold = 2, Iron = 3 }

    /// <summary>Kaynağın harcanabileceği alanlar.</summary>
    [System.Flags]
    public enum SpendArea
    {
        None = 0, Building = 1, Upgrade = 2, Research = 4, Repair = 8, Road = 16,
        All = Building | Upgrade | Research | Repair | Road
    }

    /// <summary>Yağmalanma kuralı (ayrıntılar dengeleme/PvP aşamasında).</summary>
    public enum LootRule { Lootable = 0, Protected = 1 }
}
