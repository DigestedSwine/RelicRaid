using UnityEditor;
using UnityEngine;

// Menu + hotkey to drop a ClaudeMarker at the Scene view's focus point (or origin).
public static class ClaudeMarkerMenu
{
    // Ctrl+Shift+M (Cmd+Shift+M on Mac)
    [MenuItem("Tools/Claude/Drop Marker Here %#m")]
    public static void DropMarker()
    {
        var go = new GameObject("ClaudeMarker");
        var sv = SceneView.lastActiveSceneView;
        go.transform.position = sv != null ? sv.pivot : Vector3.zero;
        go.AddComponent<ClaudeMarker>();

        Undo.RegisterCreatedObjectUndo(go, "Drop Claude Marker");
        Selection.activeGameObject = go;
        if (sv != null) sv.FrameSelected();
    }

    [MenuItem("Tools/Claude/Select All Markers")]
    public static void SelectAll()
    {
        var all = Object.FindObjectsByType<ClaudeMarker>(FindObjectsSortMode.None);
        Selection.objects = System.Array.ConvertAll(all, m => (Object)m.gameObject);
        Debug.Log($"[Claude] {all.Length} marker(s) in the scene.");
    }
}
