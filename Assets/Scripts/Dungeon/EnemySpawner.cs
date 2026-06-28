using System.Collections.Generic;
using UnityEngine;

// Designer tool: spawns enemies at / scattered around this point. Drop in the scene, assign the enemy
// prefab, set count + scatter. Respawn is handled by the enemy prefab's own Respawner — turn
// enemiesRespawn OFF for boss-room enemies (GDD doesRespawn = false). Gizmos show the spawn area.
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int count = 1;
    public float scatterRadius = 2f;
    public bool spawnOnStart = true;
    public bool enemiesRespawn = true;     // false in the boss room (clean encounter)

    readonly List<GameObject> spawned = new List<GameObject>();
    public IReadOnlyList<GameObject> Spawned => spawned;

    void Start() { if (spawnOnStart) Spawn(); }

    public void Spawn()
    {
        if (enemyPrefab == null) { Debug.LogWarning($"[EnemySpawner] {name}: no enemyPrefab.", this); return; }
        for (int i = 0; i < count; i++)
        {
            Vector2 r = scatterRadius > 0f ? Random.insideUnitCircle * scatterRadius : Vector2.zero;
            Vector3 pos = transform.position + new Vector3(r.x, 0f, r.y);
            var go = Instantiate(enemyPrefab, pos, transform.rotation);
            if (!enemiesRespawn)
            {
                var resp = go.GetComponent<Respawner>();
                if (resp != null) Destroy(resp);
            }
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
