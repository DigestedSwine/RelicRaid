using UnityEngine;

// Boss behaviour layered on a HealthComponent (+ optional NPCController). Triggers phases as HP drops
// past thresholds (GDD: 50% / 25%) — each phase can change move speed, attack damage, and enable a VFX.
// On death it spawns the exit portal and signals the DungeonManager. Give the boss NO Respawner.
[RequireComponent(typeof(HealthComponent))]
public class BossController : MonoBehaviour
{
    [System.Serializable]
    public class Phase
    {
        public string name = "Phase";
        [Range(0f, 1f)] public float hpThreshold = 0.5f;  // enters when HP fraction drops to/below this
        public float moveSpeed = -1f;                      // <0 = leave unchanged
        public float attackMultiplier = -1f;               // <0 = leave unchanged
        public GameObject phaseVfx;                        // enabled on entering the phase
    }

    [Tooltip("Order high → low threshold, e.g. 0.5 then 0.25.")]
    public Phase[] phases;

    [Header("On Death")]
    public GameObject exitPortalPrefab;
    public Transform portalSpawnPoint;     // null → boss position

    public System.Action<int> OnPhaseChanged;   // phase index
    public int CurrentPhase { get; private set; } = -1;

    HealthComponent health;
    NPCController npc;

    void Awake()
    {
        health = GetComponent<HealthComponent>();
        npc = GetComponent<NPCController>();
    }

    void OnEnable()
    {
        health.OnDamaged += OnDamaged;
        health.OnDeath += OnDeath;
    }

    void OnDisable()
    {
        health.OnDamaged -= OnDamaged;
        health.OnDeath -= OnDeath;
    }

    void OnDamaged(float amount, DamageType type)
    {
        float frac = health.HPFraction;
        // Advance through any phases we've dropped past (ordered high→low).
        for (int i = CurrentPhase + 1; i < phases.Length; i++)
        {
            if (frac <= phases[i].hpThreshold) EnterPhase(i);
            else break;
        }
    }

    void EnterPhase(int i)
    {
        CurrentPhase = i;
        var p = phases[i];
        if (npc != null)
        {
            if (p.moveSpeed >= 0f) npc.moveSpeed = p.moveSpeed;
            if (p.attackMultiplier >= 0f) npc.attackMultiplier = p.attackMultiplier;
        }
        if (p.phaseVfx != null) p.phaseVfx.SetActive(true);
        OnPhaseChanged?.Invoke(i);
    }

    void OnDeath()
    {
        Vector3 pos = portalSpawnPoint != null ? portalSpawnPoint.position : transform.position;
        if (exitPortalPrefab != null)
        {
            // Networked: only the authority (master) spawns it → one portal replicated to all clients.
            var no = GetComponent<Fusion.NetworkObject>();
            if (no != null && no.Runner != null)
            {
                if (no.HasStateAuthority)
                    no.Runner.Spawn(exitPortalPrefab.GetComponent<Fusion.NetworkObject>(), pos, Quaternion.identity);
            }
            else Instantiate(exitPortalPrefab, pos, Quaternion.identity);   // single-player fallback
        }
        DungeonEvents.RaiseBossDefeated(this);
    }
}
