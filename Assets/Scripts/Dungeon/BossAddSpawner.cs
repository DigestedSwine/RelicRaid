using UnityEngine;

// Bridges BossController phase changes to EnemySpawners: when the boss enters phase i, fires spawnersByPhase[i].
// Lets a melee boss call in reinforcements at HP thresholds (GDD boss "summon" beats) with zero code per fight —
// designer assigns one spawner (or a small array) per phase index. Spawners should have spawnOnStart = false.
public class BossAddSpawner : MonoBehaviour
{
    public BossController boss;
    [Tooltip("Index aligns with BossController.phases — element i fires when the boss enters phase i.")]
    public EnemySpawner[] spawnersByPhase;

    void Awake() { if (boss == null) boss = GetComponent<BossController>(); }
    void OnEnable()  { if (boss != null) boss.OnPhaseChanged += OnPhase; }
    void OnDisable() { if (boss != null) boss.OnPhaseChanged -= OnPhase; }

    void OnPhase(int i)
    {
        if (spawnersByPhase == null || i < 0 || i >= spawnersByPhase.Length) return;
        if (spawnersByPhase[i] != null) spawnersByPhase[i].Spawn();
    }
}
