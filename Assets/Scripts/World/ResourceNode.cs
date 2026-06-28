using System;
using System.Collections;
using UnityEngine;

// Mining behavior you can drop on ANY static asset (rock, ore vein, crystal pylon, tree...) to make it
// mineable. This is PURE LOGIC + a state machine — it carries no visuals of its own. Anything that wants
// to react (a glow, a mesh swap, a sound) subscribes to OnStateChanged / OnMined, so the same component
// works on every future asset with no per-asset code (see ResourceNodeVisual).
public class ResourceNode : MonoBehaviour
{
    public enum NodeState { Full, Depleted, Respawning }

    [Header("Resource")]
    public ResourceType resourceType = ResourceType.WellspringCrystal;
    public int yieldMin = 1;
    public int yieldMax = 3;

    [Header("Timing")]
    public float channelTime = 2.5f;   // seconds of mining to extract (the Miner reads this)
    public float respawnTime = 8f;     // seconds before a depleted node refills

    public NodeState State { get; private set; } = NodeState.Full;
    public bool CanMine => State == NodeState.Full;

    // Visuals / audio subscribe here. No per-asset code needed if you use ResourceNodeVisual.
    public event Action<NodeState> OnStateChanged;
    public event Action<int> OnMined;     // amount yielded this extraction

    // Called by the Miner when a channel completes. Returns the amount yielded (0 if not currently mineable).
    public int Extract()
    {
        if (State != NodeState.Full) return 0;
        int amount = UnityEngine.Random.Range(yieldMin, yieldMax + 1);
        OnMined?.Invoke(amount);
        SetState(NodeState.Depleted);
        StartCoroutine(RespawnRoutine());
        return amount;
    }

    IEnumerator RespawnRoutine()
    {
        SetState(NodeState.Respawning);
        yield return new WaitForSeconds(respawnTime);
        SetState(NodeState.Full);
    }

    void SetState(NodeState s)
    {
        State = s;
        OnStateChanged?.Invoke(s);
    }
}
