using UnityEngine;

// Spawns networked enemies once the session is up — ONLY on the Shared-Mode master client (spawned
// NetworkObjects replicate to everyone). Supports a single prefab × count, OR a mix (one of EACH prefab
// per count). Optionally assigns a RoamZone so the master's AI wanders the group's area.
public class NetworkEnemySpawner : MonoBehaviour
{
    public NetworkBootstrap net;
    public Fusion.NetworkObject enemyPrefab;            // single-type spawner
    public Fusion.NetworkObject[] enemyPrefabs;         // optional mix: one of each per count (overrides enemyPrefab)
    public RoamZone roamZone;                           // optional: spawned NPCs wander this zone
    public int count = 3;
    public float radius = 4f;

    bool done;

    void Update()
    {
        if (done || net == null || !net.Connected || net.Runner == null) return;
        if (!net.Runner.IsSharedModeMasterClient) { done = true; return; }   // only the master spawns

        var prefabs = (enemyPrefabs != null && enemyPrefabs.Length > 0) ? enemyPrefabs : new[] { enemyPrefab };
        for (int c = 0; c < count; c++)
            foreach (var pf in prefabs)
            {
                if (pf == null) continue;
                Vector2 r = Random.insideUnitCircle * radius;
                var no = net.Runner.Spawn(pf, transform.position + new Vector3(r.x, 0.5f, r.y), Quaternion.identity);
                if (roamZone != null && no != null)
                {
                    var npc = no.GetComponent<NPCController>();
                    if (npc != null) npc.roamZone = roamZone;
                }
            }
        done = true;
    }
}
