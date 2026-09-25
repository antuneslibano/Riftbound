namespace Riftbound.Simulation
{
    /// <summary>Counters used by the debug overlay and the end screen. Allocation free.</summary>
    public class MatchStats
    {
        const int Buckets = 5; // seconds of DPS window

        public readonly int[] CardsPlayed = new int[2];
        public readonly int[] UnitsSpawned = new int[2];
        public readonly int[] UnitsLost = new int[2];
        public readonly float[] TotalDamage = new float[2];
        public readonly float[] CoreDamageDealt = new float[2];

        readonly float[,] buckets = new float[2, Buckets];
        int bucket;
        float bucketTime;

        public void RecordDamage(Team team, float amount)
        {
            TotalDamage[(int)team] += amount;
            buckets[(int)team, bucket] += amount;
        }

        public void Tick(float dt)
        {
            bucketTime += dt;
            while (bucketTime >= 1f)
            {
                bucketTime -= 1f;
                bucket = (bucket + 1) % Buckets;
                buckets[0, bucket] = 0f;
                buckets[1, bucket] = 0f;
            }
        }

        /// <summary>Average damage per second of a team over the last few seconds.</summary>
        public float Dps(Team team)
        {
            float sum = 0f;
            for (int i = 0; i < Buckets; i++) sum += buckets[(int)team, i];
            return sum / (Buckets - 1 + bucketTime);
        }
    }
}
