using System;
using System.Collections.Generic;

// Global combat signals. Kept separate so HealthComponent / CombatManager have no dependency on the
// progression layer — anything (Party, quests, achievements, kill feed) can subscribe.
public static class CombatEvents
{
    // (victim, killer, contributors). killer = last hit (may be null); contributors = everyone who dealt
    // damage. Each Party checks whether any of its members contributed and rewards itself, so multiple
    // parties that tagged the same target both get the reward.
    public static event Action<HealthComponent, HealthComponent, IReadOnlyList<HealthComponent>> UnitKilled;

    public static void RaiseUnitKilled(HealthComponent victim, HealthComponent killer, IReadOnlyList<HealthComponent> contributors)
        => UnitKilled?.Invoke(victim, killer, contributors);

    // (victim, amount, type) — fired on every damage tick. Floating combat numbers / hit feedback subscribe.
    public static event Action<HealthComponent, float, DamageType> UnitDamaged;
    public static void RaiseUnitDamaged(HealthComponent victim, float amount, DamageType type)
        => UnitDamaged?.Invoke(victim, amount, type);
}
