using UnityEngine;

// Defines an area that NPCs wander inside while idle. Drop one in the scene, size it (box or circle),
// and assign it to each roaming NPC's NPCController.roamZone. Gizmo shows the area in the Scene view.
public class RoamZone : MonoBehaviour
{
    public enum Shape { Box, Circle }
    public Shape shape = Shape.Box;
    public Vector2 boxSize = new Vector2(10f, 10f);
    public float radius = 6f;

    public Vector3 RandomPoint()
    {
        if (shape == Shape.Box)
        {
            float x = Random.Range(-boxSize.x * 0.5f, boxSize.x * 0.5f);
            float z = Random.Range(-boxSize.y * 0.5f, boxSize.y * 0.5f);
            return transform.position + new Vector3(x, 0f, z);
        }
        Vector2 c = Random.insideUnitCircle * radius;
        return transform.position + new Vector3(c.x, 0f, c.y);
    }

    public bool Contains(Vector3 p)
    {
        Vector3 d = p - transform.position; d.y = 0f;
        if (shape == Shape.Box) return Mathf.Abs(d.x) <= boxSize.x * 0.5f && Mathf.Abs(d.z) <= boxSize.y * 0.5f;
        return d.sqrMagnitude <= radius * radius;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.55f);
        if (shape == Shape.Box) Gizmos.DrawWireCube(transform.position, new Vector3(boxSize.x, 0.5f, boxSize.y));
        else Gizmos.DrawWireSphere(transform.position, radius);
    }
}
