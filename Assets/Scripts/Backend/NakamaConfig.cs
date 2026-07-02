using UnityEngine;

// Connection settings for the Nakama server. Kept in a ScriptableObject so the same client code points at
// whatever server you stand up — swap the asset's values, no code change. For Heroic Cloud: scheme "https",
// port 443, and the host + server key from the Heroic Cloud console.
//
// NOTE: the server key is a public CLIENT key (safe to ship in the build), not an admin/console secret.
[CreateAssetMenu(fileName = "NakamaConfig", menuName = "RelicRaid/Nakama Config")]
public class NakamaConfig : ScriptableObject
{
    [Header("Heroic Cloud → Configuration → connection details")]
    public string scheme = "https";     // Heroic Cloud uses TLS
    public string host = "";            // e.g. <your-project>.nakamacloud.io
    public int port = 443;              // Heroic Cloud = 443
    public string serverKey = "defaultkey";

    [Header("Behaviour")]
    public bool verboseLogging = true;  // log auth/save/load steps while we're bringing this up
}
