using System;
using System.Collections.Generic;

// Global world signals (mining, objectives, ...), decoupled like CombatEvents. The Party listens so a
// mined node rewards the whole group (and later, multiple parties tagging a shared node).
public static class WorldEvents
{
    // (miner's HealthComponent, resource type, amount). The miner identifies which party gets credit.
    public static event Action<HealthComponent, ResourceType, int> ResourceMined;

    // Running per-type tally since domain load (resets each play). Lets consumers POLL totals reliably
    // instead of depending on event-subscription timing (which can be fragile for objectives/gates).
    static readonly Dictionary<ResourceType, int> minedTotals = new Dictionary<ResourceType, int>();
    public static int TotalMined(ResourceType type) => minedTotals.TryGetValue(type, out int v) ? v : 0;

    public static void RaiseResourceMined(HealthComponent miner, ResourceType type, int amount)
    {
        minedTotals.TryGetValue(type, out int cur);
        minedTotals[type] = cur + amount;
        ResourceMined?.Invoke(miner, type, amount);
    }
}
