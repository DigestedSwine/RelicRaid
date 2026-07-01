using UnityEngine;

// A "sticky note in the world." Drop one wherever you want Claude to do something and type the instruction
// in the Note field. Claude reads every ClaudeMarker over the MCP bridge — position + note + done flag — so
// you point in the scene and describe the task, instead of trying to explain coordinates in chat.
//
// Place one via  Tools > Claude > Drop Marker Here  (Ctrl+Shift+M), or add this component to any GameObject.
[ExecuteAlways]
[DisallowMultipleComponent]
public class ClaudeMarker : MonoBehaviour
{
    [TextArea(2, 6)]
    public string note = "Describe what to place / do at this spot.";

    [Tooltip("Optional tag to group markers, e.g. 'loot', 'spawn', 'camp', 'fix'.")]
    public string kind = "";

    [Tooltip("Claude flips this on once handled (or you can, to mute it).")]
    public bool done = false;

    public Color color = new Color(1f, 0.4f, 0.1f);
    public float blockSize = 1.5f;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Color c = done ? new Color(0.4f, 0.5f, 0.4f) : color;
        Gizmos.color = c;
        Gizmos.DrawWireCube(transform.position, Vector3.one * blockSize);
        Gizmos.color = new Color(c.r, c.g, c.b, 0.18f);
        Gizmos.DrawCube(transform.position, Vector3.one * blockSize);

        var style = new GUIStyle { normal = { textColor = Color.white }, fontStyle = FontStyle.Bold };
        string label = (done ? "✓ " : "► ") + (string.IsNullOrEmpty(kind) ? "" : "[" + kind + "] ") + note;
        UnityEditor.Handles.Label(transform.position + Vector3.up * (blockSize * 0.5f + 0.4f), label, style);
    }
#endif
}
