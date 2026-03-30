namespace SunnysideIsland.Events
{
    public class BuildingPlacementStartedEvent
    {
        public string BuildingId { get; set; }
    }

    public class BuildingPlaceRequestedEvent
    {
        public string BuildingId { get; set; }
        public UnityEngine.Vector3Int GridPosition { get; set; }
    }

    public class BuildingPlaceConfirmEvent
    {
        public string BuildingId { get; set; }
        public UnityEngine.Vector3Int GridPosition { get; set; }
    }

    public class BuildingPlacedEvent
    {
        public string BuildingId { get; set; }
        public UnityEngine.Vector3 Position { get; set; }
    }

    public class BuildingCompletedEvent
    {
        public string BuildingId { get; set; }
        public string BuildingName { get; set; }
    }

    public class BuildingUpgradedEvent
    {
        public string BuildingId { get; set; }
        public int NewLevel { get; set; }
    }

    public class BuildingDemolishedEvent
    {
        public string BuildingId { get; set; }
    }

    public class PlacementFailedEvent
    {
        public string Message { get; set; }
        public string Reason { get; set; }
    }

    public class ConstructionStartedEvent
    {
        public string BuildingId { get; set; }
        public string BuildingName { get; set; }
    }

    public class ConstructionProgressEvent
    {
        public string BuildingId { get; set; }
        public int CurrentProgress { get; set; }
        public int RequiredProgress { get; set; }
    }

    public class ConstructionCancelledEvent
    {
        public string BuildingId { get; set; }
        public int RefundedWood { get; set; }
    }

    public class BuildingPlacementCancelledEvent
    {
    }

    public class InsufficientResourcesEvent
    {
        public string ResourceId { get; set; }
        public int Required { get; set; }
        public int Current { get; set; }
    }
}