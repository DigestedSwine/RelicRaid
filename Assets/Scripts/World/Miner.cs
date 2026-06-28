using UnityEngine;

// Player-side mining: finds the nearest mineable ResourceNode in range, channels it (auto, or hold-to-mine
// through the InputReader), cancels the channel if the player takes damage, and yields resources on
// completion. Exposes progress + state for the HUD. Resource amounts are placeholder counters for now
// (per the GDD: build the hooks, defer the values).
[RequireComponent(typeof(HealthComponent))]
public class Miner : MonoBehaviour
{
    public InputReader input;
    public float range = 3.5f;
    public bool holdToMine = false;     // false = auto-mine when in range; true = hold Interact (E / gamepad)
    public bool cancelOnDamage = true;  // GDD open Q: full cancel (chosen) vs delay

    HealthComponent health;
    ResourceNode current;
    float progress;                      // 0..1 of the current channel
    bool mining;

    public bool IsMining => mining;
    public float Progress => progress;
    public ResourceNode CurrentNode => current;
    public int Crystals { get; private set; }            // placeholder inventory counter
    public event System.Action<ResourceType, int> OnResourceGained;

    void Awake() { health = GetComponent<HealthComponent>(); }
    void OnEnable() { if (health != null) health.OnDamaged += OnDamaged; }
    void OnDisable() { if (health != null) health.OnDamaged -= OnDamaged; }

    void OnDamaged(float amount, DamageType type) { if (cancelOnDamage) Cancel(); }

    void Update()
    {
        if (!health.IsAlive) { Cancel(); return; }

        ResourceNode node = FindNode();
        bool wantMine = node != null && (!holdToMine || (input != null && input.InteractHeld));

        if (!wantMine) { Cancel(); return; }

        if (current != node) { current = node; progress = 0f; }
        mining = true;
        progress += Time.deltaTime / Mathf.Max(0.01f, node.channelTime);

        if (progress >= 1f)
        {
            int got = node.Extract();
            if (got > 0)
            {
                Crystals += got;
                OnResourceGained?.Invoke(node.resourceType, got);
                WorldEvents.RaiseResourceMined(health, node.resourceType, got);   // -> Party shares to the group
            }
            Cancel();   // node is now depleted; stop channeling
        }
    }

    ResourceNode FindNode()
    {
        ResourceNode best = null;
        float bestSqr = range * range;
        foreach (var n in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
        {
            if (!n.CanMine) continue;
            float d = (n.transform.position - transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = n; }
        }
        return best;
    }

    void Cancel()
    {
        mining = false;
        progress = 0f;
        current = null;
    }
}
