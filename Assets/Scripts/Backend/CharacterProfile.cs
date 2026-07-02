using System;

// The persisted character record. Serialized to JSON and stored as a user-owned Nakama storage object
// (collection "profile", key "character"). Kept deliberately small for #1 (persistence); skill points,
// allocated skill nodes, inventory and equipment get added here in focus areas #3 and #4.
//
// schemaVersion lets us migrate old saves when fields change (bump it + handle upgrades in the loader).
[Serializable]
public class CharacterProfile
{
    public int schemaVersion = 1;

    public string classId = "wizard";   // which of the 12 classes (id string; flexible until a class registry exists)
    public int level = 1;
    public long xp = 0;                  // total lifetime XP; level thresholds resolve in focus area #2
    public long currency = 0;            // soft currency (gold)

    public string displayName = "";      // optional, shown on nameplates/roster later

    public static CharacterProfile NewDefault(string name)
    {
        return new CharacterProfile { displayName = name ?? "" };
    }

    public string ToJson() => UnityEngine.JsonUtility.ToJson(this);

    public static CharacterProfile FromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        var p = UnityEngine.JsonUtility.FromJson<CharacterProfile>(json);
        // room for schemaVersion migrations here as the record grows
        return p;
    }
}
