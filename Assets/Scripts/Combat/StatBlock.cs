using UnityEngine;

// Per-character stats (GDD Shared Stat Framework). Serializable so it shows in the Inspector now;
// graduates to a field on CharacterData (ScriptableObject) once we have multiple classes.
[System.Serializable]
public class StatBlock
{
    public float maxHP = 100f;
    public float maxMP = 50f;
    public float atk = 20f;     // physical power
    public float matk = 20f;    // magic power
    public float def = 10f;     // physical mitigation
    public float mdef = 10f;    // magic mitigation
    public float spd = 1f;      // move + attack speed modifier
    public float mpRegenPerSecond = 6f;
    [Range(0f, 1f)] public float crit = 0.1f;
    [Range(0f, 1f)] public float dodge = 0.05f;

    public float AttackFor(DamageType type)  => type == DamageType.Physical ? atk : matk;
    public float DefenseFor(DamageType type) => type == DamageType.Physical ? def : mdef;

    // Display / AI heuristic ONLY — never feed this into the damage formula (DEF is already mitigated there).
    public float EffectiveHP => maxHP * (1f + def / 100f);
}
