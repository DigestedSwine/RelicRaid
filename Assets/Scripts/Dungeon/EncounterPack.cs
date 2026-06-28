using UnityEngine;

// Groups several EnemySpawners into one "pack". Fires OnCleared once every enemy it spawned is dead — a
// Gate can require N packs cleared to open. NOTE: with respawning enemies a pack never stays cleared, so
// packs used to gate progression should set their spawners' enemiesRespawn = false.
public class EncounterPack : MonoBehaviour
{
    public EnemySpawner[] spawners;
    public bool alertTogether = true;     // GDD "pack alert": one aggro'd → all aggro (wiring is a future hook)

    public System.Action<EncounterPack> OnCleared;
    public bool IsCleared { get; private set; }

    bool everPopulated;

    public int AliveCount()
    {
        int n = 0;
        if (spawners != null)
            foreach (var s in spawners) if (s != null) n += s.AliveCount();
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
        if (spawners == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.7f);
        foreach (var s in spawners)
            if (s != null) Gizmos.DrawLine(transform.position, s.transform.position);
    }
}
