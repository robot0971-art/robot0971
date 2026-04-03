using DI;
using SunnysideIsland.GameData;
using SunnysideIsland.Survival;
using UnityEngine;
using GameDataClass = SunnysideIsland.GameData.GameData;

namespace SunnysideIsland.Inventory
{
    public class ItemConsumptionService : IItemConsumptionService
    {
        public bool CanConsume(string itemId)
        {
            return TryGetRestoreAmounts(itemId, out _, out _, out _);
        }

        public bool TryConsume(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            {
                return false;
            }

            if (!TryGetRestoreAmounts(itemId, out int hungerRestore, out int healthRestore, out float staminaRestore))
            {
                return false;
            }

            if (!DIContainer.TryResolve<IInventorySystem>(out var inventorySystem) ||
                !inventorySystem.RemoveItem(itemId, quantity))
            {
                return false;
            }

            if (hungerRestore != 0 && DIContainer.TryResolve<HungerSystem>(out var hungerSystem))
            {
                hungerSystem.Modify(hungerRestore * quantity);
            }

            if (healthRestore != 0 && DIContainer.TryResolve<HealthSystem>(out var healthSystem))
            {
                healthSystem.Heal(healthRestore * quantity, $"Consumed {itemId}");
            }

            if (!Mathf.Approximately(staminaRestore, 0f) &&
                DIContainer.TryResolve<StaminaSystem>(out var staminaSystem))
            {
                staminaSystem.Restore(staminaRestore * quantity);
            }

            return true;
        }

        public bool TryGetRestoreAmounts(
            string itemId,
            out int hungerRestore,
            out int healthRestore,
            out float staminaRestore)
        {
            hungerRestore = 0;
            healthRestore = 0;
            staminaRestore = 0f;

            if (string.IsNullOrEmpty(itemId) || !DIContainer.TryResolve<GameDataClass>(out var gameData))
            {
                return false;
            }

            var itemData = gameData.GetItem(itemId);
            if (itemData == null || itemData.itemType != ItemType.Consumable)
            {
                return false;
            }

            hungerRestore = itemData.hungerRestore;
            healthRestore = itemData.healthRestore;
            staminaRestore = itemData.staminaRestore;

            if (hungerRestore == 0 && healthRestore == 0 && Mathf.Approximately(staminaRestore, 0f))
            {
                var recipe = gameData.recipes?.Find(x => x.resultItemId == itemId);
                if (recipe != null)
                {
                    hungerRestore = recipe.hungerRestore;
                }

                if (hungerRestore == 0)
                {
                    var fish = gameData.fishData?.Find(x => x.itemId == itemId);
                    if (fish != null)
                    {
                        hungerRestore = fish.hungerRestore;
                    }
                }
            }

            return hungerRestore != 0 || healthRestore != 0 || !Mathf.Approximately(staminaRestore, 0f);
        }
    }
}
