using UnityEngine;

public enum SkillTargetType { Single, AOE, Self, Ally }

// Data-driven ability definition (GDD Skill Architecture). One ScriptableObject per ability; the
// SkillCaster reads these. Values are first-pass scaffolding — tune in playtesting.
[CreateAssetMenu(menuName = "RelicRaid/Skill", fileName = "Skill")]
public class SkillData : ScriptableObject
{
    public string skillName = "New Skill";
    [TextArea] public string description;
    public Faction faction;
    public CharacterClass characterClass;

    [Header("Targeting")]
    public DamageType damageType = DamageType.Nature;
    public SkillTargetType targetType = SkillTargetType.Single;
    public float range = 12f;
    public float aoeRadius = 0f;
    public bool selfCentered = false;   // AOE: center the burst on the caster (close-range nova) instead of on an enemy

    [Header("Casting")]
    public float castTime = 0f;        // 0 = instant
    public float cooldown = 4f;
    public float mpCost = 15f;
    public string animTrigger = "Cast"; // Animator trigger to play (Cast / Attack / AoE)

    [Header("Direct damage")]
    public float damageMultiplier = 1f; // 0 = no direct hit (pure DoT/utility)

    [Header("DoT (0 = none)")]
    public float dotDamage = 0f;        // per tick
    public float dotDuration = 0f;
    public float dotTickRate = 1f;
    [Range(0f, 1f)] public float dotSpreadChance = 0f;  // TODO: spreading not wired yet

    [Header("Status (reserved — not applied yet)")]
    public StatusEffectType statusEffect = StatusEffectType.None;
    public float statusDuration = 0f;

    [Header("VFX")]
    public GameObject vfxPrefab;        // null → coded impact pop colored by damageType
}
