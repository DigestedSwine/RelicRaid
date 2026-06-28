using UnityEngine;

// Describes one dungeon for the Level Select screen. Make one asset per dungeon
// (Create ▸ RelicRaid ▸ Dungeon Info) and list them on the MenuFlow.
[CreateAssetMenu(menuName = "RelicRaid/Dungeon Info", fileName = "Dungeon")]
public class DungeonInfo : ScriptableObject
{
    public string displayName = "Wellspring Hollow";
    public string sceneName;                 // the scene to load (must be in Build Settings)
    [TextArea] public string description = "A bear-infested wood hiding a Wellspring node.";
    public int recommendedLevel = 1;
    public Sprite thumbnail;                 // optional; falls back to a colored card
}
