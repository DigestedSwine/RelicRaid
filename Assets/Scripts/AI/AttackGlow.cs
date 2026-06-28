using System.Collections;
using UnityEngine;

// Telegraphs an NPC attack with a glowing maw — a stand-in for a missing attack animation. Ramps up over
// the wind-up, flashes on the strike, then fades. Needs Bloom in the scene Volume for the halo.
// The glow is an independent world-scale object (so the entity's tiny scale can't shrink it) positioned
// between the head joint and the snout tip, so it sits INSIDE the mouth rather than out past the nose.
public class AttackGlow : MonoBehaviour
{
    public NPCController npc;

    [Header("Placement")]
    public Transform anchor;            // head joint, inside the head (e.g. head0)
    public Transform forwardAnchor;     // snout/nose helper that defines forward (e.g. head0_end)
    [Range(0f, 1f)] public float forwardAmount = 0.45f;  // 0 = head center, 1 = snout tip
    public Vector3 extraOffset = Vector3.zero;           // world-space fine-tune

    [Header("Look")]
    public float size = 0.3f;           // world meters (sphere diameter)
    public Color color = new Color(1f, 0.15f, 0.05f);
    public float maxIntensity = 7f;     // HDR multiplier (>1 to bloom)
    public float fadeOut = 0.35f;

    Renderer glow;
    MaterialPropertyBlock mpb;
    float intensity;
    Coroutine routine;
    static readonly int BaseId = Shader.PropertyToID("_BaseColor");

    void Start()
    {
        Build();
        if (npc != null) { npc.AttackWindup += OnWindup; npc.AttackStrike += OnStrike; }
        Apply();
    }

    void OnDestroy()
    {
        if (npc != null) { npc.AttackWindup -= OnWindup; npc.AttackStrike -= OnStrike; }
        if (glow != null) Destroy(glow.gameObject);
    }

    void Build()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "MawGlow_" + gameObject.name;
        var col = go.GetComponent<Collider>(); if (col) Destroy(col);
        go.transform.localScale = Vector3.one * size;

        // URP Unlit: always shows _BaseColor (never renders black from missing lighting), and HDR-bright
        // values bloom. More reliable than Lit + emission-via-PropertyBlock.
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);   // opaque
        // Draw OVER the bear mesh so the attack tell reads from any angle (top-down it was buried in the head).
        if (mat.HasProperty("_ZTest")) mat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        mat.renderQueue = 4000;

        glow = go.GetComponent<Renderer>();
        glow.sharedMaterial = mat;
        mpb = new MaterialPropertyBlock();
    }

    void OnWindup(float windup)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Ramp(windup));
    }

    IEnumerator Ramp(float windup)
    {
        float t = 0f;
        while (t < windup) { t += Time.deltaTime; intensity = Mathf.Clamp01(t / Mathf.Max(0.01f, windup)); Apply(); yield return null; }
        intensity = 1f; Apply();
    }

    void OnStrike()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        intensity = 1.3f; Apply();
        float t = 0f;
        while (t < fadeOut) { t += Time.deltaTime; intensity = Mathf.Lerp(1.3f, 0f, t / fadeOut); Apply(); yield return null; }
        intensity = 0f; Apply();
    }

    void Apply()
    {
        if (glow == null) return;
        glow.GetPropertyBlock(mpb);
        mpb.SetColor(BaseId, color * (intensity * maxIntensity));
        glow.SetPropertyBlock(mpb);
        glow.enabled = intensity > 0.01f;   // fully hidden when not attacking
    }

    void LateUpdate()
    {
        if (glow == null || anchor == null) return;
        Vector3 p = forwardAnchor != null
            ? Vector3.Lerp(anchor.position, forwardAnchor.position, forwardAmount)
            : anchor.position;
        glow.transform.position = p + extraOffset;
    }
}
