using System.Collections.Generic;
using UnityEngine;

// Top-down "see-through walls": SphereCasts from the camera to the target (player); any renderer on the
// occluder layers between them fades out (via the DitherFadeLit shader's _Fade, set per-renderer through a
// MaterialPropertyBlock — no material instances). Restores smoothly when no longer blocking the view.
public class CameraOcclusionFader : MonoBehaviour
{
    public Transform target;                 // the player
    public LayerMask occluderMask = ~0;      // set to the "Occluder" layer
    [Range(0f, 1f)] public float fadedAmount = 0.25f;   // _Fade while occluding (0 = invisible, 1 = solid)
    public float fadeSpeed = 8f;
    public float castRadius = 0.4f;

    static readonly int FadeID = Shader.PropertyToID("_Fade");

    readonly Dictionary<Renderer, float> managed = new();   // renderer -> current _Fade
    readonly HashSet<Renderer> occluding = new();
    MaterialPropertyBlock mpb;

    void Awake() { mpb = new MaterialPropertyBlock(); }

    void LateUpdate()
    {
        occluding.Clear();

        if (target != null)
        {
            Vector3 origin = transform.position;
            Vector3 dir = target.position - origin;
            float dist = dir.magnitude;
            if (dist > 0.01f)
            {
                var hits = Physics.SphereCastAll(origin, castRadius, dir.normalized, dist - 0.5f,
                                                 occluderMask, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    var r = h.collider.GetComponent<Renderer>();
                    if (r == null) r = h.collider.GetComponentInParent<Renderer>();
                    if (r != null) { occluding.Add(r); if (!managed.ContainsKey(r)) managed[r] = 1f; }
                }
            }
        }

        if (managed.Count == 0) return;

        // Lerp each managed renderer toward its target fade; drop ones fully restored.
        var keys = new List<Renderer>(managed.Keys);
        foreach (var r in keys)
        {
            if (r == null) { managed.Remove(r); continue; }
            float goal = occluding.Contains(r) ? fadedAmount : 1f;
            float val = Mathf.MoveTowards(managed[r], goal, fadeSpeed * Time.deltaTime);
            managed[r] = val;

            r.GetPropertyBlock(mpb);
            mpb.SetFloat(FadeID, val);
            r.SetPropertyBlock(mpb);

            if (val >= 0.999f && !occluding.Contains(r)) managed.Remove(r);   // fully solid again
        }
    }
}
