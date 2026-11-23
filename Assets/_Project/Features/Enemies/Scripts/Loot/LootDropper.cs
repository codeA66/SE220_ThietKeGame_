using UnityEngine;

namespace DeadCells.Enemies
{
    /// <summary>
    /// Turns a dead enemy into coins and gems. Loot scales with the enemy's max health and its
    /// EnemyData loot multiplier, so tougher enemy types are worth more.
    /// </summary>
    public static class LootDropper
    {
        private const float GoldPerHealth = 0.1f;
        private const float GemChancePerHealth = 1f / 200f;
        private const float BonusGemHealth = 150f;
        private const int MaxCoinPieces = 8;

        public static void Drop(EnemyData data, Vector3 position, Transform player, LayerMask groundMask)
        {
            float worth = data.MaxHealth * data.LootMultiplier;

            int gold = Mathf.Max(1, Mathf.RoundToInt(worth * GoldPerHealth * Random.Range(0.8f, 1.2f)));
            int pieces = Mathf.Min(gold, MaxCoinPieces);
            for (int i = 0; i < pieces; i++)
            {
                // Spread the total evenly; the first pieces take the remainder.
                int value = gold / pieces + (i < gold % pieces ? 1 : 0);
                LootPickup.SpawnGold(value, position, RandomBurst(), player, groundMask);
            }

            int gems = 0;
            if (Random.value < worth * GemChancePerHealth)
                gems++;
            if (worth >= BonusGemHealth && Random.value < 0.5f)
                gems++;
            for (int i = 0; i < gems; i++)
                LootPickup.SpawnGem(RollGemTier(worth), position, RandomBurst() * 1.15f, player, groundMask);
        }

        // Weighted roll; rarer tiers gain weight with enemy worth. Rat (20): ~76% sapphire, 20% emerald,
        // 4% amethyst. Zombie (120): ~51% sapphire, 30% emerald, 15% amethyst, 3% ruby.
        private static GemTier RollGemTier(float worth)
        {
            float sapphire = 10f;
            float emerald = 2f + worth / 30f;
            float amethyst = worth / 40f;
            float ruby = Mathf.Max(0f, (worth - 80f) / 60f);

            float roll = Random.value * (sapphire + emerald + amethyst + ruby);
            if ((roll -= sapphire) < 0f)
                return GemTier.Sapphire;
            if ((roll -= emerald) < 0f)
                return GemTier.Emerald;
            if ((roll -= amethyst) < 0f)
                return GemTier.Amethyst;
            return GemTier.Ruby;
        }

        private static Vector2 RandomBurst() => new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(4f, 7f));
    }
}
