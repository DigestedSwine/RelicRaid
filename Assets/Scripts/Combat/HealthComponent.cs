using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// HP/MP container + DoT runner + events. Sits on every player and NPC.
// Pure state: it never computes damage (CombatManager does that) — it just applies and broadcasts.
public class HealthComponent : MonoBehaviour
{
    public Team team = Team.Hostiles;
    public StatBlock stats = new StatBlock();

    [Header("Playtest")]
    public bool indestructible = false;      // takes damage normally but HP floors at 1 (never dies) — godmode toggle

    [Header("Out-of-combat regen (player + mobs)")]
    public float outOfCombatDelay = 10f;     // wait this long after last combat before HP regen begins
    public float fullHealSeconds = 35f;      // seconds to refill a full bar once regen kicks in (0 = disabled)

    public float CurrentHP { get; private set; }
    public float CurrentMP { get; private set; }
    public float MaxHP => stats.maxHP;
    public float MaxMP => stats.maxMP;
    public float HPFraction => stats.maxHP > 0f ? CurrentHP / stats.maxHP : 0f;
    public bool IsAlive => CurrentHP > 0f;

    // For network proxies to mirror the authority's HP (display only — doesn't run death logic).
    public void NetSetHP(float hp) => CurrentHP = hp;

    public event Action<float, DamageType> OnDamaged;  // (amount, type)
    public event Action<float> OnHealed;               // (amount)
    public event Action OnDeath;
    public event Action OnRevived;

    // Who last damaged this unit (last hit) — set by CombatManager. Cleared on revive.
    public HealthComponent LastAttacker { get; set; }

    // Combat timer for out-of-combat regen. Bumped on taking OR dealing damage.
    public float LastCombatTime { get; private set; }
    public bool InCombat => Time.time - LastCombatTime < outOfCombatDelay;
    public void MarkInCombat() => LastCombatTime = Time.time;

    // Everyone who dealt damage to this unit this life — used so every participating party shares the kill.
    readonly HashSet<HealthComponent> contributors = new HashSet<HealthComponent>();
    public void RegisterDamage(HealthComponent attacker) { if (attacker != null) contributors.Add(attacker); }

    readonly List<Coroutine> dots = new List<Coroutine>();

    void Awake()
    {
        CurrentHP = stats.maxHP;
        CurrentMP = stats.maxMP;
    }

    void Update()
    {
        if (IsAlive && CurrentMP < stats.maxMP && stats.mpRegenPerSecond > 0f)
            CurrentMP = Mathf.Min(stats.maxMP, CurrentMP + stats.mpRegenPerSecond * Time.deltaTime);

        // Out-of-combat HP regen (player + mobs): begins after outOfCombatDelay, fills in fullHealSeconds.
        if (IsAlive && fullHealSeconds > 0f && CurrentHP < stats.maxHP
            && Time.time - LastCombatTime >= outOfCombatDelay)
            CurrentHP = Mathf.Min(stats.maxHP, CurrentHP + (stats.maxHP / fullHealSeconds) * Time.deltaTime);
    }

    public void TakeDamage(float amount, DamageType type)
    {
        if (!IsAlive || amount <= 0f) return;
        MarkInCombat();
        CurrentHP = Mathf.Max(0f, CurrentHP - amount);
        if (indestructible && CurrentHP <= 0f) CurrentHP = 1f;   // godmode: damage shows, but survive at 1 HP
        OnDamaged?.Invoke(amount, type);
        if (CurrentHP <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        CurrentHP = Mathf.Min(stats.maxHP, CurrentHP + amount);
        OnHealed?.Invoke(amount);
    }

    // Top off both bars (playtest convenience / full-restore pickups later).
    public void RefillToFull()
    {
        CurrentHP = stats.maxHP;
        CurrentMP = stats.maxMP;
        OnHealed?.Invoke(stats.maxHP);
    }

    public bool SpendMP(float amount)
    {
        if (CurrentMP < amount) return false;
        CurrentMP -= amount;
        return true;
    }

    // Damage-over-time. perTick is already the final per-tick amount (CombatManager pre-mitigates).
    // source credits the DoT's kill to that unit's group (e.g. the wizard's Creeping Decay).
    public void ApplyDoT(float perTick, float duration, float tickRate, DamageType type, HealthComponent source = null)
    {
        if (!IsAlive || perTick <= 0f || tickRate <= 0f) return;
        RegisterDamage(source);     // casting a DoT counts as damaging/tagging this target
        dots.Add(StartCoroutine(DoTRoutine(perTick, duration, tickRate, type, source)));
    }

    IEnumerator DoTRoutine(float perTick, float duration, float tickRate, DamageType type, HealthComponent source)
    {
        float elapsed = 0f;
        while (elapsed < duration && IsAlive)
        {
            yield return new WaitForSeconds(tickRate);
            elapsed += tickRate;
            if (source != null) LastAttacker = source;   // so a DoT kill is credited correctly
            TakeDamage(perTick, type);
        }
    }

    void Die()
    {
        foreach (var d in dots) if (d != null) StopCoroutine(d);
        dots.Clear();
        OnDeath?.Invoke();

        var list = new List<HealthComponent>();
        foreach (var c in contributors) if (c != null) list.Add(c);
        CombatEvents.RaiseUnitKilled(this, LastAttacker, list);   // killer + all damage contributors
        contributors.Clear();
    }

    // Bring a dead entity back to full HP/MP. Used by Respawner.
    public void Revive()
    {
        CurrentHP = stats.maxHP;
        CurrentMP = stats.maxMP;
        LastAttacker = null;
        contributors.Clear();
        OnRevived?.Invoke();
    }
}
