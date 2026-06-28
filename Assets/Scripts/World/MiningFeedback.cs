using UnityEngine;

// Juice for the mining loop: while the local player channels a node, emit periodic sparks at it; on a
// successful extract, pop a burst + a floating "+N ◆". Reads the Miner (local-authority only — proxies'
// Miner is disabled so this no-ops for them). Networked mining VFX for other players is a later pass.
public class MiningFeedback : MonoBehaviour
{
    public Miner miner;
    public Color crystalColor = new Color(0.4f, 0.85f, 1f);
    public float sparkInterval = 0.16f;

    float nextSpark;
    Vector3 lastNodePos;

    void Awake() { if (miner == null) miner = GetComponent<Miner>(); }
    void OnEnable()  { if (miner != null) miner.OnResourceGained += OnGained; }
    void OnDisable() { if (miner != null) miner.OnResourceGained -= OnGained; }

    void Update()
    {
        if (miner == null || !miner.IsMining || miner.CurrentNode == null) return;
        lastNodePos = miner.CurrentNode.transform.position;
        if (Time.time >= nextSpark)
        {
            nextSpark = Time.time + sparkInterval;
            Vector3 p = lastNodePos + Vector3.up * 0.6f + Random.insideUnitSphere * 0.45f;
            SkillImpactVfx.Spawn(p, crystalColor, 0.55f);
        }
    }

    void OnGained(ResourceType type, int amount)
    {
        SkillImpactVfx.Spawn(lastNodePos + Vector3.up * 0.8f, crystalColor, 2.4f);   // reward burst
        FloatingText.Spawn(lastNodePos + Vector3.up * 1.3f, "+" + amount + " ◆", crystalColor);
    }
}
