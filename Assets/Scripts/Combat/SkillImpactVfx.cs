using UnityEngine;

// Lightweight coded impact effect: an emissive sphere that expands and fades, then self-destroys.
// A stand-in until real VFX prefabs exist (Bloom gives it the glow). Spawn via the static helper.
public class SkillImpactVfx : MonoBehaviour
{
    public float duration = 0.45f;
    public float maxScale = 2.5f;
    public Color color = Color.white;

    Renderer rend;
    MaterialPropertyBlock mpb;
    float t;
    static readonly int EmId = Shader.PropertyToID("_EmissionColor");

    public static void Spawn(Vector3 pos, Color color, float maxScale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SkillVfx";
        var col = go.GetComponent<Collider>(); if (col) Destroy(col);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.2f;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // hint transparent-ish (best effort)
        go.GetComponent<Renderer>().sharedMaterial = mat;

        var v = go.AddComponent<SkillImpactVfx>();
        v.color = color; v.maxScale = maxScale;
        v.rend = go.GetComponent<Renderer>();
        v.mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        t += Time.deltaTime;
        float f = t / duration;
        if (f >= 1f) { Destroy(gameObject); return; }
        transform.localScale = Vector3.one * Mathf.Lerp(0.2f, maxScale, f);
        if (rend != null)
        {
            rend.GetPropertyBlock(mpb);
            mpb.SetColor(EmId, color * Mathf.Lerp(5f, 0f, f));
            rend.SetPropertyBlock(mpb);
        }
    }
}
