namespace GameCore.Economy
{
    /// <summary>Dört kaynaktan oluşan maliyet. Değerler dengeleme aşamasında veri dosyasına taşınacak.</summary>
    public readonly struct ResourceCost
    {
        public readonly int Wood, Stone, Gold, Iron;
        public ResourceCost(int wood, int stone = 0, int gold = 0, int iron = 0)
        { Wood = wood; Stone = stone; Gold = gold; Iron = iron; }

        public int Get(ResourceType t)
        {
            switch (t)
            {
                case ResourceType.Wood: return Wood;
                case ResourceType.Stone: return Stone;
                case ResourceType.Gold: return Gold;
                default: return Iron;
            }
        }

        public bool IsEmpty => Wood == 0 && Stone == 0 && Gold == 0 && Iron == 0;

        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (Wood > 0) parts.Add(Wood + " odun");
            if (Stone > 0) parts.Add(Stone + " taş");
            if (Gold > 0) parts.Add(Gold + " altın");
            if (Iron > 0) parts.Add(Iron + " demir");
            return parts.Count == 0 ? "ücretsiz" : string.Join(", ", parts);
        }
    }
}
