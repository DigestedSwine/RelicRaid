using System.Collections.Generic;
using UnityEngine;

public static class PolygonUtils
{
    // Ray-casting point-in-polygon test (Jordan curve theorem)
    public static bool Contains(List<Vector2> polygon, Vector2 point)
    {
        bool inside = false;
        int n = polygon.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = polygon[i], b = polygon[j];
            if ((a.y > point.y) != (b.y > point.y) &&
                point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    public static float Perimeter(List<Vector2> polygon)
    {
        float p = 0f;
        for (int i = 0; i < polygon.Count; i++)
            p += Vector2.Distance(polygon[i], polygon[(i + 1) % polygon.Count]);
        return p;
    }
}
