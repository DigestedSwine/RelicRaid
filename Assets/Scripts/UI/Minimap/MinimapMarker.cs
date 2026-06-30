using System.Collections.Generic;
using UnityEngine;

// What kind of blip this is. Drives default color/size/visibility. Extend freely.
public enum MinimapMarkerType
{
    Self, PartyMember, RealmAlly, Enemy, Boss, MobCamp, Landmark, Objective, Resource, Ping
}

// Anything that should show on the minimap carries one of these. It auto-registers to a static list the
// Minimap reads each frame — so players/enemies get one at spawn, designers drop them on camps/landmarks,
// and a future ping system just Instantiates one at a world position. No minimap code changes needed.
public class MinimapMarker : MonoBehaviour
{
    public MinimapMarkerType type = MinimapMarkerType.Landmark;
    public string label;
    [Tooltip("Stay pinned to the minimap rim when out of range (party members, objectives).")]
    public bool clampToEdge = false;
    [Tooltip("Always shown regardless of range (landmarks/objectives across the whole map).")]
    public bool alwaysVisible = false;
    public Color colorOverride = new Color(0, 0, 0, 0);   // alpha>0 → use this instead of the type default

    static readonly List<MinimapMarker> all = new List<MinimapMarker>();
    public static IReadOnlyList<MinimapMarker> All => all;

    void OnEnable() { if (!all.Contains(this)) all.Add(this); }
    void OnDisable() { all.Remove(this); }

    public Color Color()
    {
        if (colorOverride.a > 0.01f) return colorOverride;
        switch (type)
        {
            case MinimapMarkerType.Self:        return new Color(0.45f, 0.92f, 1f);
            case MinimapMarkerType.PartyMember: return new Color(0.35f, 0.88f, 0.45f);
            case MinimapMarkerType.RealmAlly:   return new Color(0.40f, 0.70f, 1f);
            case MinimapMarkerType.Enemy:       return new Color(0.92f, 0.28f, 0.22f);
            case MinimapMarkerType.Boss:        return new Color(1f, 0.40f, 0.12f);
            case MinimapMarkerType.MobCamp:     return new Color(0.95f, 0.55f, 0.22f);
            case MinimapMarkerType.Landmark:    return new Color(0.95f, 0.88f, 0.50f);
            case MinimapMarkerType.Objective:   return new Color(1f, 0.90f, 0.30f);
            case MinimapMarkerType.Resource:    return new Color(0.50f, 0.85f, 1f);
            case MinimapMarkerType.Ping:        return new Color(1f, 0.85f, 0.20f);
            default: return UnityEngine.Color.white;
        }
    }

    public float Size()
    {
        switch (type)
        {
            case MinimapMarkerType.Self:     return 12f;
            case MinimapMarkerType.Boss:     return 13f;
            case MinimapMarkerType.MobCamp:  return 13f;
            case MinimapMarkerType.Landmark: return 11f;
            default: return 9f;
        }
    }
}
