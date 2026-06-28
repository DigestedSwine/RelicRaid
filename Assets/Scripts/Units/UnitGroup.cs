using System.Collections.Generic;
using UnityEngine;

public class UnitGroup : MonoBehaviour
{
    public Color GroupColor { get; private set; }
    public IReadOnlyList<Unit> Units => units;

    readonly List<Unit> units = new();

    static readonly Color[] palette =
    {
        new Color(0.2f, 0.8f, 1.0f),
        new Color(1.0f, 0.8f, 0.2f),
        new Color(0.2f, 1.0f, 0.4f),
        new Color(1.0f, 0.4f, 0.8f),
        new Color(0.8f, 0.4f, 1.0f),
    };
    static int paletteIndex;

    public static UnitGroup Create(List<Unit> selected)
    {
        var go = new GameObject("UnitGroup");
        var group = go.AddComponent<UnitGroup>();
        group.GroupColor = palette[paletteIndex % palette.Length];
        paletteIndex++;

        foreach (var unit in selected)
        {
            unit.Group?.RemoveUnit(unit);
            group.AddUnit(unit);
        }

        return group;
    }

    public void AddUnit(Unit unit)
    {
        units.Add(unit);
        unit.Group = this;
    }

    public void RemoveUnit(Unit unit)
    {
        units.Remove(unit);
        unit.Group = null;
        if (units.Count == 0)
            Destroy(gameObject);
    }

    public void MoveTo(Vector3 worldPosition)
    {
        foreach (var unit in units)
            unit.MoveTo(worldPosition);
    }

    public void Dissolve()
    {
        for (int i = units.Count - 1; i >= 0; i--)
            units[i].Group = null;
        units.Clear();
        Destroy(gameObject);
    }
}
