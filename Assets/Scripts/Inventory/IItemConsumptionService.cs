namespace SunnysideIsland.Inventory
{
    public interface IItemConsumptionService
    {
        bool CanConsume(string itemId);
        bool TryConsume(string itemId, int quantity = 1);
        bool TryGetRestoreAmounts(string itemId, out int hungerRestore, out int healthRestore, out float staminaRestore);
    }
}
