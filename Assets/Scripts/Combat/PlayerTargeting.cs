using UnityEngine;

// Foundation for target selection. For now it AUTO-targets the nearest living enemy in range so the HUD
// can show a target frame. A real targeting UX (tap-to-target / tab / lock-on) plugs in later by calling
// SetTarget() and flipping autoTarget off — the HUD and anything else read CurrentTarget regardless.
public class PlayerTargeting : MonoBehaviour
{
    public HealthComponent self;            // the player (excluded; its team filters out allies)
    public float targetRange = 14f;
    public bool autoTarget = true;          // turn off when manual targeting lands
    public float scanInterval = 0.25f;

    public HealthComponent CurrentTarget { get; private set; }
    public event System.Action<HealthComponent> OnTargetChanged;

    float nextScan;

    void Update()
    {
        // Drop a target that died or wandered out of range.
        if (CurrentTarget != null && (!CurrentTarget.IsAlive || Dist(CurrentTarget) > targetRange * 1.4f))
            SetTarget(null);

        if (autoTarget && Time.time >= nextScan)
        {
            nextScan = Time.time + scanInterval;
            AutoAcquire();
        }
    }

    void AutoAcquire()
    {
        // Keep the current target if it's still valid and in range; otherwise pick the nearest enemy.
        if (CurrentTarget != null && CurrentTarget.IsAlive && Dist(CurrentTarget) <= targetRange) return;

        HealthComponent best = null;
        float bestD = targetRange;
        foreach (var hc in Object.FindObjectsByType<HealthComponent>(FindObjectsSortMode.None))
        {
            if (hc == null || hc == self || !hc.IsAlive) continue;
            if (self != null && hc.team == self.team) continue;   // enemies only
            float d = Dist(hc);
            if (d < bestD) { bestD = d; best = hc; }
        }
        if (best != CurrentTarget) SetTarget(best);
    }

    public void SetTarget(HealthComponent t)
    {
        if (t == CurrentTarget) return;
        CurrentTarget = t;
        OnTargetChanged?.Invoke(t);
    }

    float Dist(HealthComponent h) => Vector3.Distance(transform.position, h.transform.position);
}
