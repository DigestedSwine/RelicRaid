using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Attach to a GameObject in the scene with a LineRenderer component.
// The LineRenderer needs a material — assign Sprites/Default or any unlit material.
[RequireComponent(typeof(LineRenderer))]
public class LassoController : MonoBehaviour
{
    [SerializeField] float minPointSpacing = 0.15f;
    // A lasso smaller than this perimeter is treated as a tap (move command)
    [SerializeField] float minLassoPerimeter = 1.5f;

    LineRenderer line;
    Camera cam;
    readonly List<Vector2> points = new();
    bool drawing;

    // All active groups persist here; groups self-destruct when empty
    readonly List<UnitGroup> activeGroups = new();

    void Awake()
    {
        cam = Camera.main;
        line = GetComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = true;
        line.startWidth = 0.08f;
        line.endWidth = 0.08f;
        line.positionCount = 0;

        // Fallback material so the line is visible without manual setup
        if (line.sharedMaterial == null)
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(1f, 0.9f, 0.2f, 0.85f);
            line.endColor   = new Color(1f, 0.9f, 0.2f, 0.85f);
        }
    }

    void Update()
    {
        bool pressed = Pointer.current?.press.isPressed ?? false;
        Vector2 screenPos = Pointer.current?.position.ReadValue() ?? Vector2.zero;
        Vector3 world = ScreenToWorld(screenPos);

        if (pressed && !drawing)
            BeginLasso(world);
        else if (pressed && drawing)
            ContinueLasso(world);
        else if (!pressed && drawing)
            EndLasso(world);
    }

    void BeginLasso(Vector3 world)
    {
        drawing = true;
        points.Clear();
        points.Add(world);
        line.positionCount = 1;
        line.SetPosition(0, world);
    }

    void ContinueLasso(Vector3 world)
    {
        if (Vector2.Distance(points[points.Count - 1], world) < minPointSpacing) return;

        points.Add(world);
        line.positionCount = points.Count;
        line.SetPosition(points.Count - 1, world);
    }

    void EndLasso(Vector3 world)
    {
        drawing = false;
        line.positionCount = 0;

        if (PolygonUtils.Perimeter(points) < minLassoPerimeter)
        {
            HandleTap(world);
        }
        else
        {
            SelectUnitsInLasso();
        }

        points.Clear();
    }

    void SelectUnitsInLasso()
    {
        var selected = new List<Unit>();
        foreach (var unit in UnitRegistry.All)
        {
            if (PolygonUtils.Contains(points, unit.transform.position))
                selected.Add(unit);
        }

        if (selected.Count == 0) return;

        var group = UnitGroup.Create(selected);
        activeGroups.Add(group);
    }

    void HandleTap(Vector3 world)
    {
        // Prune destroyed groups before issuing move commands
        activeGroups.RemoveAll(g => g == null);

        foreach (var group in activeGroups)
            group.MoveTo(world);
    }

    Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 p = cam.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        p.z = 0f;
        return p;
    }
}
