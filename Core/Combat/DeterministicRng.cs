namespace GameCore.Combat
{
    /// <summary>Tohumlu, platformdan bağımsız basit rastgele sayı üretici (xorshift32). Sunucu ve istemci aynı sonucu alır.</summary>
    public sealed class DeterministicRng
    {
        private uint state;
        public DeterministicRng(int seed) { state = (uint)(seed == 0 ? 0x6D2B79F5 : seed); }

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            state = x;
            return x;
        }

        /// <summary>0 .. max-1</summary>
        public int Next(int max) => max <= 0 ? 0 : (int)(NextUInt() % (uint)max);
    }
}
