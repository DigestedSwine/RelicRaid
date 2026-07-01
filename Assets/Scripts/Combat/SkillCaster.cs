using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Casts abilities from skill slots. Listens to InputReader.AbilityPressed(slot), checks cooldown + MP,
// plays the cast animation, waits the cast time, then resolves the effect (damage / DoT / AOE / VFX)
// through CombatManager. Auto-targets the nearest enemy (no aim system yet).
public class SkillCaster : MonoBehaviour
{
    public InputReader input;
    public HealthComponent self;
    public Animator animator;
    public Team hostileTo = Team.Hostiles;

    [Tooltip("Ability slots — index matches InputReader ability keys (1,2,3,4).")]
    public SkillData[] skills = new SkillData[4];

    float[] readyAt;
    NetworkPlayer netPlayer;

    void Awake()
    {
        if (self == null) self = GetComponent<HealthComponent>();
        if (animator == null) animator = GetComponent<Animator>();
        netPlayer = GetComponent<NetworkPlayer>();
        readyAt = new float[skills.Length];
    }

    void OnEnable()  { if (input != null) input.AbilityPressed += OnAbility; }
    void OnDisable() { if (input != null) input.AbilityPressed -= OnAbility; }

    void OnAbility(int slot)
    {
        if (slot < 0 || slot >= skills.Length) return;
        // The InputReader is a SHARED ScriptableObject, so its AbilityPressed event can reach more than one
        // caster (a proxy that stayed subscribed, or two players in one process). Only the LOCAL player — the
        // one with state authority — may cast; proxies are network-driven. This keeps players independent.
        if (netPlayer != null && !netPlayer.HasStateAuthority) return;
        TryCast(skills[slot], slot);
    }

    public float CooldownRemaining(int slot)
    {
        if (readyAt == null || slot < 0 || slot >= readyAt.Length) return 0f;
        return Mathf.Max(0f, readyAt[slot] - Time.time);
    }

    void TryCast(SkillData skill, int slot)
    {
        if (skill == null) return;
        if (self != null && !self.IsAlive) return;
        if (Time.time < readyAt[slot]) return;                    // on cooldown
        if (self != null && !self.SpendMP(skill.mpCost)) return;  // not enough MP
        readyAt[slot] = Time.time + skill.cooldown;
        StartCoroutine(CastRoutine(skill));
    }

    IEnumerator CastRoutine(SkillData skill)
    {
        // Networked → replicate the cast/attack/aoe trigger so other players see it; else play locally.
        if (netPlayer != null) netPlayer.FireAction(skill.animTrigger);
        else if (animator != null && !string.IsNullOrEmpty(skill.animTrigger)) animator.SetTrigger(skill.animTrigger);
        if (skill.castTime > 0f) yield return new WaitForSeconds(skill.castTime);
        if (self != null && !self.IsAlive) yield break;
        Resolve(skill);
    }

    void Resolve(SkillData skill)
    {
        Color c = ColorFor(skill.damageType);
        switch (skill.targetType)
        {
            case SkillTargetType.Self:
            case SkillTargetType.Ally:
                ApplyTo(skill, self);
                SpawnVfx(skill, transform.position + Vector3.up, c);
                break;

            case SkillTargetType.Single:
            {
                var t = NearestEnemy(skill.range);
                if (t != null) { ApplyTo(skill, t); SpawnVfx(skill, AimPoint(t), c); }
                break;
            }

            case SkillTargetType.AOE:
            {
                Vector3 cpos;
                if (skill.selfCentered) cpos = transform.position;     // close-range nova around the caster
                else
                {
                    var center = NearestEnemy(skill.range);
                    cpos = center != null ? center.transform.position
                                          : transform.position + transform.forward * (skill.range * 0.5f);
                }
                foreach (var h in EnemiesInRadius(cpos, skill.aoeRadius)) ApplyTo(skill, h);
                SpawnVfx(skill, cpos + Vector3.up, c);
                break;
            }
        }
    }

    void ApplyTo(SkillData skill, HealthComponent target)
    {
        if (target == null) return;
        bool offensive = target != self && target.team != self.team;
        if (offensive && skill.damageMultiplier > 0f)
            CombatManager.ProcessAttack(self, target, skill.damageMultiplier, skill.damageType);
        if (offensive && skill.dotDamage > 0f && skill.dotDuration > 0f)
            target.ApplyDoT(skill.dotDamage, skill.dotDuration, skill.dotTickRate, skill.damageType, self);
        if (offensive && skill.statusEffect != StatusEffectType.None && skill.statusDuration > 0f)
            ApplyStatus(target, skill.statusEffect, skill.statusDuration);
    }

    // Crowd control onto a target's AI. Stun = full freeze; Root/Slow map to stun for now (movement-only
    // variants can split out when locomotion CC is needed). No-op on things without an NPCController.
    void ApplyStatus(HealthComponent target, StatusEffectType type, float duration)
    {
        var npc = target.GetComponent<NPCController>();
        if (npc == null) return;
        if (type == StatusEffectType.Stun || type == StatusEffectType.Root || type == StatusEffectType.Slow)
            npc.ApplyStun(duration);
    }

    HealthComponent NearestEnemy(float range)
    {
        float best = range * range;
        HealthComponent found = null;
        foreach (var h in UnityEngine.Object.FindObjectsByType<HealthComponent>(FindObjectsSortMode.None))
        {
            if (h == null || h == self || !h.IsAlive || h.team != hostileTo) continue;
            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; found = h; }
        }
        return found;
    }

    List<HealthComponent> EnemiesInRadius(Vector3 center, float radius)
    {
        var list = new List<HealthComponent>();
        float r2 = radius * radius;
        foreach (var h in UnityEngine.Object.FindObjectsByType<HealthComponent>(FindObjectsSortMode.None))
        {
            if (h == null || h == self || !h.IsAlive || h.team != hostileTo) continue;
            if ((h.transform.position - center).sqrMagnitude <= r2) list.Add(h);
        }
        return list;
    }

    static Vector3 AimPoint(HealthComponent t) => t.transform.position + Vector3.up;

    static Color ColorFor(DamageType t)
    {
        switch (t)
        {
            case DamageType.Holy:   return new Color(1f, 0.9f, 0.4f);
            case DamageType.Nature: return new Color(0.4f, 0.9f, 0.3f);
            case DamageType.Frost:  return new Color(0.5f, 0.85f, 1f);
            case DamageType.Storm:  return new Color(0.7f, 0.5f, 1f);
            case DamageType.Spirit: return new Color(0.6f, 1f, 0.9f);
            default:                return new Color(1f, 0.8f, 0.6f);
        }
    }

    void SpawnVfx(SkillData skill, Vector3 pos, Color c)
    {
        if (skill.vfxPrefab != null) Instantiate(skill.vfxPrefab, pos, Quaternion.identity);
        else SkillImpactVfx.Spawn(pos, c, Mathf.Max(1.2f, skill.aoeRadius > 0f ? skill.aoeRadius * 2f : 1.6f));
    }
}
