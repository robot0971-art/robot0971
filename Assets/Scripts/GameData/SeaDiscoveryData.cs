using System;
using UnityEngine;
using SunnysideIsland.Events;

namespace SunnysideIsland.GameData
{
    [Serializable]
    public class SeaDiscoveryData
    {
        [Header("=== Discovery Settings ===")]
        [Tooltip("Item unlocked when the player discovers something in the sea.")]
        public string itemId;

        [Tooltip("Relative weight for random selection. Higher values increases odds.")]
        public float weight = 1f;

        [Tooltip("Minimum quantity granted when this entry is picked.")]
        public int minQuantity = 1;

        [Tooltip("Maximum quantity granted when this entry is picked.")]
        public int maxQuantity = 1;

        [Tooltip("If specified, the entry is only valid during these seasons.")]
        public Season[] availableSeasons;

        public bool IsAvailable(Season currentSeason)
        {
            if (availableSeasons == null || availableSeasons.Length == 0)
            {
                return true;
            }

            foreach (var season in availableSeasons)
            {
                if (season == currentSeason)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetRandomQuantity()
        {
            int min = Mathf.Max(1, minQuantity);
            int max = Mathf.Max(min, maxQuantity);
            return UnityEngine.Random.Range(min, max + 1);
        }
    }
}
