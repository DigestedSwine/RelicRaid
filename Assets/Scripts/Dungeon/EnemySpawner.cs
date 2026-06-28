using System.Collections.Generic;
using UnityEngine;

// Designer tool: spawns enemies at / scattered around this point. If `net` is assigned it MASTER-spawns
// networked enemies (Shared-Mode synced); otherwise it Instantiates locally (single-player). Respawn is
// handled by the enemy's own Respawner — enemiesRespawn=false strips it (boss room / gated packs).
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public NetworkBootstrap net;           // assign → networked master-spawn; null → local Instantiate
    public int count = 1;
    public float scatterRadius = 2f;
    public bool spawnOnStart = true;
    public bool enemiesRespawn = true;     // false in the boss room / gated packs (clean encounter)
    public float leashOverride = -1f;      // >0 → set spawned NPCs' leashRadius (keeps gated packs in their room)

    readonly List<GameObject> spawned = new List<GameObject>();
    public IReadOnlyList<GameObject> Spawned => spawned;
    bool autoDone;

    void Start() { if (spawnOnStart && net == null) Spawn(); }   // single-player path

    void Update()   // networked path: spawn once, on the master, after the session is up
    {
        if (autoDone || !spawnOnStart || net == null) return;
        if (!net.Connected || net.Runner == null) return;
        if (net.Runner.IsSharedModeMasterClient) Spawn();
        autoDone = true;   // proxies don't spawn (enemies replicate from the master)
    }

    public void Spawn()
    {
        if (enemyPrefab == null) { Debug.LogWarning($"[EnemySpawner] {name}: no enemyPrefab.", this); return; }
        bool networked = net != null && net.Runner != null && net.Runner.IsSharedModeMasterClient;
        for (int i = 0; i < count; i++)
        {
            Vector2 r = scatterRadius > 0f ? Random.insideUnitCircle * scatterRadius : Vector2.zero;
            Vector3 pos = transform.position + new Vector3(r.x, networked ? 0.5f : 0f, r.y);
            GameObject go;
            if (networked)
            {
                var no = net.Runner.Spawn(enemyPrefab.GetComponent<Fusion.NetworkObject>(), pos, transform.rotation);
                go = no != null ? no.gameObject : null;
            }
            else go = Instantiate(enemyPrefab, pos, transform.rotation);
            if (go == null) continue;

            if (!enemiesRespawn) { var resp = go.GetComponent<Respawner>(); if (resp != null) Destroy(resp); }
            if (leashOverride > 0f) { var npc = go.GetComponent<NPCController>(); if (npc != null) npc.leashRadius = leashOverride; }
            spawned.Add(go);
        }
    }

    public int AliveCount()
    {
        int n = 0;
        foreach (var g in spawned)
        {
            if (g == null) continue;
            var h = g.GetComponent<HealthComponent>();
            if (h != null && h.IsAlive) n++;
        }
        return n;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.3f, scatterRadius));
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
    }
}
