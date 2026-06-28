using System.Collections;
using UnityEngine;

// Brings an entity back after death: waits respawnDelay, returns it to its spawn point, restores full
// HP/MP, clears the death animation, re-enables colliders and the listed behaviours (controllers/AI).
// Presence of this component = "this entity respawns". Omit it (e.g. on a boss) for permanent death.
[RequireComponent(typeof(HealthComponent))]
public class Respawner : MonoBehaviour
{
    public float respawnDelayMin = 15f;     // randomized respawn window (mobs); ~3-4x the old 5s
    public float respawnDelayMax = 20f;
    public Transform spawnPoint;            // null → use the position/rotation captured at Awake
    public Behaviour[] reEnableOnRespawn;   // HeroController / NPCController / MeleeAttacker, etc.
    public Animator animator;
    public string deathBool = "";           // cleared on respawn (e.g. "isDead")
    public bool reEnableColliders = true;
    public bool hideWhileDead = true;       // hide the mesh during the dead window (clear respawn delay when there's no death anim)

    HealthComponent health;
    Vector3 startPos;
    Quaternion startRot;

    void Awake()
    {
        health = GetComponent<HealthComponent>();
        startPos = transform.position;
        startRot = transform.rotation;
    }

    void OnEnable() { health.OnDeath += OnDied; }
    void OnDisable() { health.OnDeath -= OnDied; }

    void OnDied()
    {
        if (hideWhileDead) SetRenderers(false);   // vanish immediately so the dead window is visible
        StartCoroutine(RespawnRoutine());
    }

    void SetRenderers(bool on)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = on;
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(Random.Range(respawnDelayMin, respawnDelayMax));

        // Return to spawn.
        Vector3 p = spawnPoint != null ? spawnPoint.position : startPos;
        Quaternion r = spawnPoint != null ? spawnPoint.rotation : startRot;
        transform.SetPositionAndRotation(p, r);

        // Reset visuals.
        if (animator != null && !string.IsNullOrEmpty(deathBool)) animator.SetBool(deathBool, false);
        if (reEnableColliders)
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = true;

        if (hideWhileDead) SetRenderers(true);    // reappear

        // Restore health (fires OnRevived → AI resets to Idle, etc.).
        health.Revive();

        // Hand control back.
        if (reEnableOnRespawn != null)
            foreach (var b in reEnableOnRespawn) if (b != null) b.enabled = true;

        // Also re-enable whatever DeathHandler switched off on death (AI/attacker), so the list never has to
        // be kept in sync by hand — otherwise a mob that dies once respawns brain-dead and never aggros again.
        var dh = GetComponent<DeathHandler>();
        if (dh != null && dh.disableOnDeath != null)
            foreach (var m in dh.disableOnDeath) if (m != null) m.enabled = true;
    }
}
