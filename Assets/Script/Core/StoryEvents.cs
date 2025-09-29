using ARKOM.Player;

namespace ARKOM.Core
{
    // Custom story progression events
    public readonly struct FlashlightAcquiredEvent { }
    public readonly struct PowerRestoredEvent { }
    public readonly struct PlayerSeatedEvent { public readonly string SeatId; public PlayerSeatedEvent(string id){ SeatId=id; } }
    public readonly struct BlackoutStartedEvent { }
    public readonly struct TimeSkipFinishedEvent { }

    // Post-TimeSkip sequence events (placeholders)
    public readonly struct PlatesCleanedEvent { public readonly int Total; public PlatesCleanedEvent(int total){ Total=total; } }
    public readonly struct FridgeScareDoneEvent { }
    public readonly struct OoyCheckedEvent { }
    public readonly struct SweepCompleteEvent { }
    public readonly struct AnomalyFirstSeenEvent { public readonly string AnomalyId; public AnomalyFirstSeenEvent(string id){ AnomalyId = id; } }
    public readonly struct GhostSpawnedEvent { public readonly int Index; public GhostSpawnedEvent(int index){ Index=index; } }
    public readonly struct PlayerInBedEvent { }
    public readonly struct PrayerFinishedEvent { }
    public readonly struct KitchenEnteredEvent { }
}
