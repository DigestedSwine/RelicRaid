using UnityEngine;

// Glowing resource / relic node. Pulses emission (needs Bloom in the scene Volume to actually "glow"),
// and visually depletes as it's mined: the glow dims and shard meshes vanish.
// For real gameplay, set autoDemo = false and call Mine(amount) from your mining logic.
[DisallowMultipleComponent]
public class RelicNode : MonoBehaviour
{
    [Header("Glow")]
    public Renderer glowRenderer;                       // the core crystal
    public Color glowColor = new Color(0.25f, 0.9f, 1f);
    public float baseIntensity = 4f;                    // HDR emission multiplier (>1 to bloom)
    public float pulseSpeed = 2.5f;
    public float pulseAmount = 0.35f;

    [Header("Depletion")]
    public Renderer[] shards;                           // disappear one-by-one as resource drops
    [Range(0f, 1f)] public float resource = 1f;

    [Header("Idle motion")]
    public float spinSpeed = 30f;
    public float bobAmount = 0.12f;
    public float bobSpeed = 1.5f;

    [Header("Demo")]
    public bool autoDemo = true;                        // oscillate resource so deplete/refill is visible; off for gameplay

    MaterialPropertyBlock mpb;
    static readonly int EmId = Shader.PropertyToID("_EmissionColor");
    Vector3 startPos;
    float t;

    void Start()
    {
        mpb = new MaterialPropertyBlock();
        startPos = transform.position;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (autoDemo) resource = 0.5f + 0.5f * Mathf.Cos(t * 0.3f);  // starts full, slow 1→0→1 sweep

        // Pulsing emission, scaled by remaining resource.
        float pulse = 1f + Mathf.Sin(t * pulseSpeed) * pulseAmount;
        float intensity = baseIntensity * resource * pulse;
        if (glowRenderer != null)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();   // robust to mid-play recompiles
            glowRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(EmId, glowColor * intensity);
            glowRenderer.SetPropertyBlock(mpb);
        }

        // Shards vanish as the node depletes.
        if (shards != null)
        {
            for (int i = 0; i < shards.Length; i++)
            {
                if (shards[i] == null) continue;
                bool on = resource > (i + 1f) / (shards.Length + 1f);
                if (shards[i].gameObject.activeSelf != on)
                    shards[i].gameObject.SetActive(on);
            }
        }

        // Idle motion.
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = startPos + Vector3.up * Mathf.Sin(t * bobSpeed) * bobAmount;
    }

    // Hook from gameplay (with autoDemo = false). Returns the amount actually removed.
    public float Mine(float amount)
    {
        float before = resource;
        resource = Mathf.Clamp01(resource - amount);
        return before - resource;
    }
}
