using UnityEngine;

// Blocks progression until a condition is met (GDD "gate opens on condition — TBD by designer").
// Pick the condition in the inspector; assign a `barrier` object (door/wall/forcefield) that gets
// disabled when the gate opens. Other systems can also just call Open() (e.g. a switch trigger).
public class Gate : MonoBehaviour
{
    public enum Condition { PacksCleared, CrystalsMined, Manual }

    public Condition condition = Condition.PacksCleared;

    [Header("Packs Cleared")]
    public EncounterPack[] requiredPacks;
    public int packsRequired = 0;            // 0 = all listed packs

    [Header("Crystals Mined (by anyone)")]
    public ResourceType crystalType = ResourceType.WellspringCrystal;
    public int crystalsRequired = 10;

    [Header("On Open")]
    public GameObject barrier;               // the physical blocker, disabled when the gate opens

    public bool IsOpen { get; private set; }
    public System.Action OnOpened;

    int packsCleared;
    int crystalBaseline;   // WorldEvents.TotalMined(crystalType) captured when this gate starts watching

    // Pack-clear uses the (reliable) instance-event hookup. Crystal-mined POLLS WorldEvents.TotalMined in
    // Update — static-event subscription proved unreliable across domain reloads, so we don't depend on it.
    void OnEnable()
    {
        if (condition == Condition.PacksCleared && requiredPacks != null)
            foreach (var p in requiredPacks) if (p != null) p.OnCleared += OnPackCleared;
    }

    void OnDisable()
    {
        if (requiredPacks != null)
            foreach (var p in requiredPacks) if (p != null) p.OnCleared -= OnPackCleared;
    }

    void Start()
    {
        if (condition == Condition.CrystalsMined) crystalBaseline = WorldEvents.TotalMined(crystalType);
    }

    void Update()
    {
        if (!IsOpen && condition == Condition.CrystalsMined
            && WorldEvents.TotalMined(crystalType) - crystalBaseline >= crystalsRequired)
            Open();
    }

    void OnPackCleared(EncounterPack p)
    {
        packsCleared++;
        int need = packsRequired > 0 ? packsRequired : (requiredPacks != null ? requiredPacks.Length : 0);
        if (packsCleared >= need) Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        if (barrier != null) barrier.SetActive(false);
        OnOpened?.Invoke();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsOpen ? new Color(0.3f, 0.9f, 0.3f, 0.8f) : new Color(0.9f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, new Vector3(3f, 3f, 0.4f));
        if (condition == Condition.PacksCleared && requiredPacks != null)
            foreach (var p in requiredPacks) if (p != null) Gizmos.DrawLine(transform.position, p.transform.position);
    }
}
