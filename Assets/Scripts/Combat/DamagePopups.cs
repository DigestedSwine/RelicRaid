using UnityEngine;

// Global listener: spawns a floating "-N" number above any unit that takes damage. One instance in the
// scene. Reuses FloatingText. Red on the player (you got hit), warm yellow on enemies (you hit them).
public class DamagePopups : MonoBehaviour
{
    public float headMargin = 0.35f;

    static readonly Color PlayerHit = new Color(1f, 0.35f, 0.30f);
    static readonly Color EnemyHit  = new Color(1f, 0.90f, 0.45f);

    void OnEnable()  { CombatEvents.UnitDamaged += OnDamaged; }
    void OnDisable() { CombatEvents.UnitDamaged -= OnDamaged; }

    void OnDamaged(HealthComponent victim, float amount, DamageType type)
    {
        if (victim == null) return;
        int dmg = Mathf.Max(1, Mathf.RoundToInt(amount));

        // Place just above the victim's visual top (animation/scale safe).
        var r = victim.GetComponentInChildren<Renderer>();
        float topY = r != null ? r.bounds.max.y : victim.transform.position.y + 1.5f;
        Vector3 pos = new Vector3(victim.transform.position.x, topY + headMargin, victim.transform.position.z)
                    + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0f, 0.25f), 0f);

        FloatingText.Spawn(pos, "-" + dmg, victim.team == Team.Players ? PlayerHit : EnemyHit);
    }
}
