using UnityEngine;

// Fires OnCleared once every enemy in its room area is dead. Counts living hostiles within clearRadius of
// the pack center (NOT the master's spawner list) — so the clear is detected on EVERY client from the
// replicated/networked enemies, letting each client's Gate open in co-op. Spawners stay set for spawning.
public class EncounterPack : MonoBehaviour
{
    public EnemySpawner[] spawners;
    public float clearRadius = 14f;       // covers one ~16m gauntlet room; rooms are 28m apart so no bleed
    public bool alertTogether = true;     // GDD "pack alert" (wiring is a future hook)

    public System.Action<EncounterPack> OnCleared;
    public bool IsCleared { get; private set; }

    bool everPopulated;

    public int AliveCount()
    {
        int n = 0;
        float r2 = clearRadius * clearRadius;
        foreach (var h in Object.FindObjectsByType<HealthComponent>(FindObjectsSortMode.None))
        {
            if (h == null || !h.IsAlive || h.team != Team.Hostiles) continue;
            if ((h.transform.position - transform.position).sqrMagnitude <= r2) n++;
        }
        return n;
    }

    void Update()
    {
        if (IsCleared) return;
        int alive = AliveCount();
        if (alive > 0) everPopulated = true;
        else if (everPopulated) { IsCleared = true; OnCleared?.Invoke(this); }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, clearRadius);
        if (spawners != null)
            foreach (var s in spawners)
                if (s != null) Gizmos.DrawLine(transform.position, s.transform.position);
    }
}
