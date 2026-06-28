using UnityEngine;

// Single source of truth for damage. Runs locally now; when Photon Fusion lands, the authority check
// wraps these calls (server computes, clients receive the result) — the math itself doesn't change.
public static class CombatManager
{
    public struct AttackResult
    {
        public bool connected;   // landed on a valid, living target
        public bool dodged;
        public bool crit;
        public float damage;
    }

    // GDD formula:
    // damage = power * mult * (1 - DEF/(DEF+100)) * critMult * variance(0.9-1.1)
    public static float ComputeDamage(StatBlock attacker, StatBlock target, float skillMultiplier, DamageType type, out bool crit)
    {
        float power = attacker.AttackFor(type);
        float def = target.DefenseFor(type);
        float baseDamage = power * skillMultiplier;
        float mitigation = 1f - def / (def + 100f);
        crit = Random.value < attacker.crit;
        float critMult = crit ? 1.5f : 1f;
        float variance = Random.Range(0.9f, 1.1f);
        return baseDamage * mitigation * critMult * variance;
    }

    public static AttackResult ProcessAttack(HealthComponent attacker, HealthComponent target, float skillMultiplier, DamageType type)
    {
        var r = new AttackResult();
        if (attacker == null || target == null || !target.IsAlive) return r;
        attacker.MarkInCombat();            // dealing damage keeps the attacker in combat (no regen)
        if (Random.value < target.stats.dodge) { r.dodged = true; return r; }

        float dmg = ComputeDamage(attacker.stats, target.stats, skillMultiplier, type, out bool crit);
        target.LastAttacker = attacker;     // last hit
        target.RegisterDamage(attacker);    // contributor -> attacker's party shares the reward
        // In networked play, route the hit to the enemy's authority so HP changes replicate; else apply locally.
        var netEnemy = target.GetComponent<NetworkEnemy>();
        if (netEnemy != null) netEnemy.ApplyDamage(dmg, type);
        else target.TakeDamage(dmg, type);
        r.connected = true;
        r.crit = crit;
        r.damage = dmg;
        return r;
    }

    public static bool ValidateHit(Transform attacker, Transform target, float range)
    {
        if (target == null) return false;
        return (target.position - attacker.position).sqrMagnitude <= range * range;
    }
}
