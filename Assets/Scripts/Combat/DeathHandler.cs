using UnityEngine;

// Generic reaction to a HealthComponent death: trigger a death animation (or freeze), disable AI/attack
// behaviours, and optionally clean up the GameObject.
[RequireComponent(typeof(HealthComponent))]
public class DeathHandler : MonoBehaviour
{
    public Animator animator;
    public string deathBool = "";        // e.g. "isDead" if the rig has a death clip; empty otherwise
    public string speedFloat = "Speed";  // zeroed on death when there's no death clip (freezes locomotion)
    public bool disableColliders = true;
    public float destroyDelay = -1f;     // <0 = leave the corpse in place
    public MonoBehaviour[] disableOnDeath; // AI / attacker components to switch off

    HealthComponent health;

    void Awake() { health = GetComponent<HealthComponent>(); }
    void OnEnable()  { health.OnDeath += HandleDeath; }
    void OnDisable() { health.OnDeath -= HandleDeath; }

    void HandleDeath()
    {
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(deathBool)) animator.SetBool(deathBool, true);
            else if (!string.IsNullOrEmpty(speedFloat)) animator.SetFloat(speedFloat, 0f);
        }

        if (disableColliders)
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

        if (disableOnDeath != null)
            foreach (var m in disableOnDeath) if (m != null) m.enabled = false;

        if (destroyDelay >= 0f) Destroy(gameObject, destroyDelay);
    }
}
