using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// On attack input, after a short wind-up, damages living enemies in a frontal arc via CombatManager.
// Uses a HealthComponent scan (no colliders) — fine for the vertical slice; swap to physics-layer
// queries when enemy counts grow.
public class MeleeAttacker : MonoBehaviour
{
    [Header("Wiring")]
    public InputReader input;          // same asset HeroController uses
    public HealthComponent self;

    [Header("Swing")]
    public float range = 3.5f;
    public float halfAngle = 70f;      // degrees off forward that count as "in front"
    public float damageMultiplier = 1.5f;
    public DamageType damageType = DamageType.Physical;
    public float hitDelay = 0.3f;      // sync with the animation contact frame
    public float cooldown = 0.6f;

    float nextReady;

    void OnEnable()  { if (input != null) input.AttackPressed += OnAttack; }
    void OnDisable() { if (input != null) input.AttackPressed -= OnAttack; }

    void OnAttack()
    {
        if (Time.time < nextReady) return;
        if (self != null && !self.IsAlive) return;
        nextReady = Time.time + cooldown;
        StartCoroutine(Swing());
    }

    IEnumerator Swing()
    {
        yield return new WaitForSeconds(hitDelay);
        if (self != null && !self.IsAlive) yield break;

        float cosHalf = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        float r2 = range * range;
        var all = Object.FindObjectsByType<HealthComponent>(FindObjectsSortMode.None);
        foreach (var hc in all)
        {
            if (hc == null || hc == self || !hc.IsAlive) continue;
            if (self != null && hc.team == self.team) continue;     // don't hit allies

            Vector3 to = hc.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > r2) continue;                     // out of range
            if (to.sqrMagnitude > 0.001f && Vector3.Dot(transform.forward, to.normalized) < cosHalf) continue; // not in front

            CombatManager.ProcessAttack(self, hc, damageMultiplier, damageType);
        }
    }
}
