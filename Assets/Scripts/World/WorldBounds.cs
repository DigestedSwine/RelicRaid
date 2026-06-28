using System.Collections.Generic;
using UnityEngine;

// Keeps players on the playable area. Builds an invisible BoxCollider ring around a rectangular region so
// the CharacterController can't walk off, AND catches anyone who still ends up below killY (clipped through,
// launched, etc.), returning them to their last grounded position. Gizmo shows the bounds.
public class WorldBounds : MonoBehaviour
{
    [Header("Play area (world XZ)")]
    public Vector2 center = Vector2.zero;
    public Vector2 size = new Vector2(58f, 58f);
    public float wallHeight = 8f;
    public float wallThickness = 2f;
    public bool buildInvisibleWalls = true;

    [Header("Fall failsafe")]
    public float killY = -5f;
    public Vector3 fallbackRespawn = new Vector3(0f, 3f, 0f);

    readonly List<CharacterController> players = new();
    readonly Dictionary<CharacterController, Vector3> lastSafe = new();

    void Start()
    {
        if (buildInvisibleWalls) BuildWalls();
        foreach (var h in Object.FindObjectsByType<HealthComponent>(FindObjectsSortMode.None))
            if (h.team == Team.Players)
            {
                var cc = h.GetComponent<CharacterController>();
                if (cc != null) { players.Add(cc); lastSafe[cc] = h.transform.position; }
            }
    }

    void BuildWalls()
    {
        float hx = size.x * 0.5f, hz = size.y * 0.5f;
        float spanX = size.x + wallThickness * 2f;
        float spanZ = size.y + wallThickness * 2f;
        MakeWall("Bound_N", new Vector3(center.x, wallHeight * 0.5f, center.y + hz), new Vector3(spanX, wallHeight, wallThickness));
        MakeWall("Bound_S", new Vector3(center.x, wallHeight * 0.5f, center.y - hz), new Vector3(spanX, wallHeight, wallThickness));
        MakeWall("Bound_E", new Vector3(center.x + hx, wallHeight * 0.5f, center.y), new Vector3(wallThickness, wallHeight, spanZ));
        MakeWall("Bound_W", new Vector3(center.x - hx, wallHeight * 0.5f, center.y), new Vector3(wallThickness, wallHeight, spanZ));
    }

    void MakeWall(string n, Vector3 pos, Vector3 boxSize)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, true);
        go.transform.position = pos;
        go.AddComponent<BoxCollider>().size = boxSize;   // transform scale 1 → size is world units
    }

    void LateUpdate()
    {
        for (int i = 0; i < players.Count; i++)
        {
            var cc = players[i];
            if (cc == null) continue;
            if (cc.isGrounded) lastSafe[cc] = cc.transform.position;
            if (cc.transform.position.y < killY)
            {
                Vector3 safe = lastSafe.TryGetValue(cc, out var s) ? s : fallbackRespawn;
                cc.enabled = false;                       // CharacterController fights direct moves
                cc.transform.position = safe + Vector3.up * 0.5f;
                cc.enabled = true;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.7f);
        Gizmos.DrawWireCube(new Vector3(center.x, wallHeight * 0.5f, center.y), new Vector3(size.x, wallHeight, size.y));
    }
}
