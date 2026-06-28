using System.Collections;
using UnityEngine;

// Death feedback for enemies that have NO death-animation clip (bear, crystal spiders, any future creature
// without one). On death: topple to a death pose, hold a beat, then sink through the floor while fading out.
// On revive (driven by Respawner) it restores pose + opacity; the Respawner restores world position/rotation.
// One reusable component — drop it on any clip-less mob. Set Respawner.hideWhileDead = false so this controls
// the visuals through the dead window instead of the mesh vanishing instantly.
[RequireComponent(typeof(HealthComponent))]
public class DeathFall : MonoBehaviour
{
    [Header("Topple (local euler added on death)")]
    public Vector3 toppleEuler = new Vector3(0f, 0f, 165f);  // spider: roll legs-up; quadruped: tip on its side
    public float toppleTime = 0.45f;

    [Header("Hold, then sink + fade")]
    public float holdTime = 1.2f;
    public float sinkDepth = 2f;        // far enough to submerge through the floor
    public float sinkTime = 1.6f;
    public bool fade = true;            // fade materials to transparent during the sink

    HealthComponent health;
    Quaternion baseLocalRot;
    Renderer[] renderers;
    Material[][] originalMats;          // cached shared materials, re-applied on revive (restores opaque + full alpha)
    Coroutine routine;

    void Awake()
    {
        health = GetComponent<HealthComponent>();
        baseLocalRot = transform.localRotation;
        renderers = GetComponentsInChildren<Renderer>(true);
        originalMats = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++) originalMats[i] = renderers[i].sharedMaterials;
    }

    void OnEnable() { health.OnDeath += OnDeath; health.OnRevived += OnRevived; }
    void OnDisable() { health.OnDeath -= OnDeath; health.OnRevived -= OnRevived; }

    void OnDeath()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Fall());
    }

    IEnumerator Fall()
    {
        // 1) topple to the death pose
        Quaternion from = transform.localRotation;
        Quaternion to = baseLocalRot * Quaternion.Euler(toppleEuler);
        for (float t = 0f; t < toppleTime; t += Time.deltaTime)
        {
            transform.localRotation = Quaternion.Slerp(from, to, t / toppleTime);
            yield return null;
        }
        transform.localRotation = to;

        // 2) lie there a moment
        yield return new WaitForSeconds(holdTime);

        // 3) sink through the floor (+ optional fade) using per-renderer material instances
        Material[][] inst = null;
        if (fade)
        {
            inst = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                inst[i] = renderers[i].materials;            // instances (won't touch shared assets)
                foreach (var m in inst[i]) MakeTransparent(m);
            }
        }
        Vector3 startPos = transform.position;
        for (float t = 0f; t < sinkTime; t += Time.deltaTime)
        {
            float k = t / sinkTime;
            transform.position = startPos + Vector3.down * (sinkDepth * k);
            if (inst != null)
                for (int i = 0; i < inst.Length; i++)
                    if (inst[i] != null) foreach (var m in inst[i]) SetAlpha(m, 1f - k);
            yield return null;
        }
        routine = null;   // submerged; Respawner's dead-window timer continues, then revives us
    }

    void OnRevived()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        transform.localRotation = baseLocalRot;                       // upright (Respawner also resets world pos/rot)
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].sharedMaterials = originalMats[i];   // back to opaque originals
    }

    static void MakeTransparent(Material m)
    {
        if (m == null) return;
        m.SetFloat("_Surface", 1f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    static void SetAlpha(Material m, float a)
    {
        if (m == null) return;
        if (m.HasProperty("_BaseColor")) { var c = m.GetColor("_BaseColor"); c.a = a; m.SetColor("_BaseColor", c); }
        if (m.HasProperty("_Color")) { var c = m.GetColor("_Color"); c.a = a; m.SetColor("_Color", c); }
    }
}
