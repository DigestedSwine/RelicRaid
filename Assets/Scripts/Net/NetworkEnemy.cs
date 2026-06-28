using Fusion;
using UnityEngine;

// Makes an NPC networked for Shared-Mode co-op. The master client (state authority) runs the NPCController AI;
// proxies disable the AI and just receive synced position (NetworkTransform), locomotion animation (NetSpeed),
// and HP (NetHP). Any client's hit is routed to the authority via ApplyDamage/RPC so damage replicates.
public class NetworkEnemy : NetworkBehaviour
{
    [Networked] public float NetSpeed { get; set; }
    [Networked] public float NetHP { get; set; }

    NPCController npc;
    HealthComponent health;
    Animator animator;
    static readonly int SpeedHash = Animator.StringToHash("Speed");

    public override void Spawned()
    {
        npc = GetComponent<NPCController>();
        health = GetComponent<HealthComponent>();
        animator = GetComponentInChildren<Animator>();
        if (npc != null) { npc.enabled = HasStateAuthority; npc.networkDriven = HasStateAuthority; }   // AI only on the authority, driven by the net tick
        if (HasStateAuthority && health != null) NetHP = health.CurrentHP;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (npc != null && npc.enabled) npc.Tick(Runner.DeltaTime);   // run AI/move on the net tick so NetworkTransform syncs it
        if (animator != null) NetSpeed = animator.GetFloat(SpeedHash);
        if (health != null) NetHP = health.CurrentHP;
    }

    public override void Render()
    {
        if (HasStateAuthority) return;
        if (animator != null) animator.SetFloat(SpeedHash, NetSpeed);
        if (health != null) health.NetSetHP(NetHP);                 // proxies mirror authority HP for display
    }

    // Apply a computed hit: on the authority directly, otherwise ask the authority via RPC.
    public void ApplyDamage(float amount, DamageType type)
    {
        if (HasStateAuthority) { if (health != null) health.TakeDamage(amount, type); }
        else RPC_Damage(amount, (int)type);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_Damage(float amount, int type)
    {
        if (health != null) health.TakeDamage(amount, (DamageType)type);
    }

    // One-shot attack-trigger replication — NPCController.Swing calls this so proxies see the enemy attack.
    public void FireAction(string trigger)
    {
        if (string.IsNullOrEmpty(trigger)) return;
        if (animator != null) animator.SetTrigger(trigger);          // authority, immediate
        if (Object != null && Object.IsValid) RPC_Action(TriggerId(trigger));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_Action(int id)
    {
        if (HasStateAuthority) return;
        string t = TriggerName(id);
        if (animator != null && t != null) animator.SetTrigger(t);
    }

    static int TriggerId(string t) { switch (t) { case "Cast": return 1; case "Attack": return 2; case "AoE": return 3; default: return 0; } }
    static string TriggerName(int id) { switch (id) { case 1: return "Cast"; case 2: return "Attack"; case 3: return "AoE"; default: return null; } }
}
