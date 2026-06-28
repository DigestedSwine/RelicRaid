using UnityEngine;

// Persistent across scene loads. Carries the player's menu selections into gameplay (chosen dungeon now;
// faction/class/party later). Created in the Boot scene and survives via DontDestroyOnLoad.
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public DungeonInfo SelectedDungeon { get; set; }
    // Later: public Faction Faction; public CharacterClass Class; public party info, etc.

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
