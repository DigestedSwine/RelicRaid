using System.Collections.Generic;
using UnityEngine;

// A group of players that shares kill rewards (co-op: tank/healer/3 DPS — everyone gets XP + the kill
// tally regardless of who landed the blow or dealt damage). When any member kills an enemy, every member
// is rewarded. Scales from 1 player today to a full 5-player party under multiplayer later.
public class Party : MonoBehaviour
{
    public List<PlayerProgress> members = new List<PlayerProgress>();

    void OnEnable()
    {
        CombatEvents.UnitKilled += OnUnitKilled;
        WorldEvents.ResourceMined += OnResourceMined;
    }
    void OnDisable()
    {
        CombatEvents.UnitKilled -= OnUnitKilled;
        WorldEvents.ResourceMined -= OnResourceMined;
    }

    void OnResourceMined(HealthComponent miner, ResourceType type, int amount)
    {
        if (!IsMember(miner)) return;                 // only if one of ours mined it
        foreach (var m in members)
            if (m != null) m.AddResource(type, amount);   // whole group shares the haul
    }

    public void Add(PlayerProgress p) { if (p != null && !members.Contains(p)) members.Add(p); }
    public void Remove(PlayerProgress p) { members.Remove(p); }

    bool IsMember(HealthComponent h)
    {
        if (h == null) return false;
        foreach (var m in members) if (m != null && m.Health == h) return true;
        return false;
    }

    void OnUnitKilled(HealthComponent victim, HealthComponent killer, IReadOnlyList<HealthComponent> contributors)
    {
        if (IsMember(victim)) return;                  // no rewards for a member dying

        // Did anyone in THIS party deal damage? If so, the whole party shares the reward. Each party
        // checks independently, so multiple parties that tagged the same target all get rewarded.
        bool participated = false;
        if (contributors != null)
            foreach (var c in contributors) if (IsMember(c)) { participated = true; break; }
        if (!participated) return;

        var id = victim != null ? victim.GetComponent<EnemyIdentity>() : null;
        foreach (var m in members)
            if (m != null) m.RecordKill(id);   // RecordKill also grants XP
    }
}
