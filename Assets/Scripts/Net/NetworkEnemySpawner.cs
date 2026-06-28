using UnityEngine;

// Spawns networked enemies once the session is up — ONLY on the Shared-Mode master client (the spawned
// NetworkObjects then replicate to everyone). Non-master clients do nothing. Drop in the scene, assign the
// networked enemy prefab + count; placed at this transform with a scatter radius.
public class NetworkEnemySpawner : MonoBehaviour
{
    public NetworkBootstrap net;
    public Fusion.NetworkObject enemyPrefab;
    public int count = 3;
    public float radius = 4f;

    bool done;

    void Update()
    {
        if (done || net == null || !net.Connected || net.Runner == null || enemyPrefab == null) return;
        if (!net.Runner.IsSharedModeMasterClient) { done = true; return; }   // only the master spawns

        for (int i = 0; i < count; i++)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            net.Runner.Spawn(enemyPrefab, transform.position + new Vector3(r.x, 0.5f, r.y), Quaternion.identity);
        }
        done = true;
    }
}
