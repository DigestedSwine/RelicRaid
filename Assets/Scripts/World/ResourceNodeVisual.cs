using UnityEngine;

// Generic, zero-code visual reaction to a ResourceNode's state. Reuse the mining behavior on ANY future
// asset by dropping ResourceNode + this on the prefab, then assigning the asset's "full" and (optional)
// "depleted" GameObjects in the Inspector — no scripting per asset.
//   • Full        -> fullVisual on,  depletedVisual off
//   • Depleted /  -> fullVisual off, depletedVisual on (or just hidden if no depleted version)
//     Respawning
public class ResourceNodeVisual : MonoBehaviour
{
    public ResourceNode node;
    [Tooltip("Shown while the node is mineable (Full).")]
    public GameObject fullVisual;
    [Tooltip("Optional: shown while Depleted/Respawning (e.g. a broken/empty version). Leave null to just hide.")]
    public GameObject depletedVisual;

    void Awake() { if (node == null) node = GetComponentInParent<ResourceNode>(); }

    void OnEnable() { if (node != null) node.OnStateChanged += Apply; }
    void OnDisable() { if (node != null) node.OnStateChanged -= Apply; }
    void Start() { if (node != null) Apply(node.State); }

    void Apply(ResourceNode.NodeState s)
    {
        bool full = s == ResourceNode.NodeState.Full;
        if (fullVisual != null) fullVisual.SetActive(full);
        if (depletedVisual != null) depletedVisual.SetActive(!full);
    }
}
