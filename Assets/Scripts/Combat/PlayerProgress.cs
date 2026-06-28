using System;
using System.Collections.Generic;
using UnityEngine;

// Per-character progression + tallies. Tracks mined resources, XP/level/skill points, and kills (by
// category AND by name, so "bears defeated", "bosses", "uniques", and future "player kills" all work).
// Source of truth for the HUD; later this is what Nakama persists (xp, level, skillPoints, counts).
[RequireComponent(typeof(HealthComponent))]
public class PlayerProgress : MonoBehaviour
{
    [Header("Level")]
    public int level = 1;
    public int xp = 0;
    public int skillPoints = 0;
    public int maxLevel = 60;            // GDD
    public int baseXpToLevel = 100;      // XP for 1 -> 2
    public float xpCurve = 1.15f;        // each level needs ~15% more

    HealthComponent health;
    public HealthComponent Health => health;
    readonly Dictionary<ResourceType, int> resources = new();
    readonly Dictionary<EnemyKind, int> killsByKind = new();
    readonly Dictionary<string, int> killsByName = new();

    public int TotalKills { get; private set; }

    public event Action<int> OnLevelUp;                 // (newLevel)
    public event Action<int, int> OnXpChanged;          // (xp, xpToNext)
    public event Action<ResourceType, int> OnResourceChanged;  // (type, newTotal)
    public event Action<EnemyIdentity> OnKill;

    void Awake() { health = GetComponent<HealthComponent>(); }

    // Resources and kills are both granted by the Party (group-wide), not subscribed to here.

    // ---------- resources ----------
    public void AddResource(ResourceType type, int amount)
    {
        if (amount == 0) return;
        resources[type] = GetResource(type) + amount;
        OnResourceChanged?.Invoke(type, resources[type]);
    }
    public int GetResource(ResourceType type) => resources.TryGetValue(type, out var v) ? v : 0;

    // ---------- kills ----------
    // Called by Party for every group member when the group gets a kill.
    public void RecordKill(EnemyIdentity id)
    {
        TotalKills++;
        EnemyKind kind = id != null ? id.kind : EnemyKind.Normal;
        string name = id != null ? id.displayName : "Unknown";
        killsByKind[kind] = GetKills(kind) + 1;
        killsByName[name] = GetKills(name) + 1;
        OnKill?.Invoke(id);
        if (id != null && id.xpReward > 0) AddXP(id.xpReward);
    }

    public int GetKills(EnemyKind kind) => killsByKind.TryGetValue(kind, out var v) ? v : 0;
    public int GetKills(string name) => killsByName.TryGetValue(name, out var v) ? v : 0;

    // ---------- xp / level ----------
    public int XpToNext() => Mathf.RoundToInt(baseXpToLevel * Mathf.Pow(xpCurve, level - 1));

    public void AddXP(int amount)
    {
        if (amount <= 0 || level >= maxLevel) return;
        xp += amount;
        int need = XpToNext();
        while (xp >= need && level < maxLevel)
        {
            xp -= need;
            level++;
            skillPoints++;
            OnLevelUp?.Invoke(level);
            need = XpToNext();
        }
        if (level >= maxLevel) xp = 0;
        OnXpChanged?.Invoke(xp, XpToNext());
    }
}
