using UnityEngine;

// Spawned in the boss room on boss death. Detects a player standing in its trigger and raises events;
// the HUD shows the "Exit dungeon?" confirm, and DungeonManager handles the actual exit. Each player
// exits independently, so this just reports presence — it doesn't decide.
[RequireComponent(typeof(Collider))]
public class ExitPortal : MonoBehaviour
{
    [Header("Behaviour")]
    [Tooltip("Placeholder until the lobby/Fusion exit flow exists: stepping on the portal teleports the player back to the dungeon entry/spawn point.")]
    public bool returnToEntryOnEnter = true;
    public Transform destinationOverride;       // null → DungeonManager.entryPoint

    public System.Action<HealthComponent> OnPlayerEntered;
    public System.Action<HealthComponent> OnPlayerExited;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var h = other.GetComponentInParent<HealthComponent>();
        if (!IsPlayer(h)) return;
        OnPlayerEntered?.Invoke(h);
        if (returnToEntryOnEnter) ReturnToEntry(h);
    }

    // Teleport the player back to the dungeon entry point (CharacterController-safe).
    void ReturnToEntry(HealthComponent player)
    {
        Transform dest = destinationOverride;
        if (dest == null)
        {
            var dm = Object.FindFirstObjectByType<DungeonManager>();
            if (dm != null) dest = dm.entryPoint;
        }
        if (dest == null) { Debug.LogWarning("[ExitPortal] No destination (DungeonManager.entryPoint unset).", this); return; }

        var go = player.gameObject;
        var cc = go.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;                 // CC fights direct moves
        go.transform.SetPositionAndRotation(dest.position, dest.rotation);
        if (cc != null) cc.enabled = true;
    }

    void OnTriggerExit(Collider other)
    {
        var h = other.GetComponentInParent<HealthComponent>();
        if (IsPlayer(h)) OnPlayerExited?.Invoke(h);
    }

    static bool IsPlayer(HealthComponent h) => h != null && h.team == Team.Players;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 1.2f);
    }
}
