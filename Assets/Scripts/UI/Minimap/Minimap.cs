using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Blip-based circular minimap (UI Toolkit). Reads MinimapMarker.All each frame, centers on the local player
// (the Self marker), projects every other marker into the disc, rotates to match the camera, edge-clamps or
// culls by range. Pure overlay — a top-down terrain render-texture backdrop can drop in behind the blips later.
[RequireComponent(typeof(UIDocument))]
public class Minimap : MonoBehaviour
{
    [Header("Coverage / look")]
    public float worldRange = 55f;          // world half-extent the disc covers (meters from player to rim)
    public float diameter = 168f;
    public bool rotateWithCamera = true;    // disc spins so camera-forward is up; false = north-up
    public CameraFollow cameraFollow;       // supplies yaw for rotate-with-camera

    VisualElement disc, blipLayer, selfBlip;
    readonly List<VisualElement> pool = new List<VisualElement>();

    void OnEnable() { TryBuild(); }

    void TryBuild()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc != null ? doc.rootVisualElement : null;   // null until the panel initializes — retried in Update
        if (root == null) return;
        root.Clear();
        root.pickingMode = PickingMode.Ignore;
        Build(root);
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
    }

    void Build(VisualElement root)
    {
        float rad = diameter * 0.5f;

        disc = new VisualElement();
        disc.style.position = Position.Absolute;
        disc.style.left = 16; disc.style.top = 16;
        disc.style.width = diameter; disc.style.height = diameter;
        disc.style.backgroundColor = new Color(0.05f, 0.07f, 0.11f, 0.62f);
        Round(disc, rad);
        disc.style.overflow = Overflow.Hidden;
        SetBorder(disc, 3f, new Color(0.62f, 0.72f, 0.88f, 0.85f));
        disc.pickingMode = PickingMode.Ignore;
        root.Add(disc);

        blipLayer = new VisualElement();
        blipLayer.style.position = Position.Absolute;
        blipLayer.style.left = 0; blipLayer.style.top = 0;
        blipLayer.style.right = 0; blipLayer.style.bottom = 0;
        blipLayer.pickingMode = PickingMode.Ignore;
        disc.Add(blipLayer);

        // self at the center, on top of the blips
        selfBlip = MakeBlip();
        Style(selfBlip, new Color(0.45f, 0.92f, 1f), 12f);
        selfBlip.style.left = rad - 6f; selfBlip.style.top = rad - 6f;
        disc.Add(selfBlip);
    }

    void Update()
    {
        if (disc == null) TryBuild();
        if (disc == null) return;

        Transform center = FindSelf();
        if (center == null) { disc.style.display = DisplayStyle.None; return; }
        disc.style.display = DisplayStyle.Flex;

        float rad = diameter * 0.5f;
        float edge = rad - 7f;
        float yaw = (rotateWithCamera && cameraFollow != null) ? cameraFollow.yaw : 0f;
        float a = -yaw * Mathf.Deg2Rad;
        float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
        Vector3 c = center.position;

        int used = 0;
        var markers = MinimapMarker.All;
        for (int i = 0; i < markers.Count; i++)
        {
            var m = markers[i];
            if (m == null || m.type == MinimapMarkerType.Self) continue;

            Vector3 rel = m.transform.position - c;
            // world XZ → disc px; rotate by -yaw so camera-forward (=+Z) points up. Screen y grows downward.
            float px = (rel.x * ca - rel.z * sa) / worldRange * rad;
            float py = -(rel.x * sa + rel.z * ca) / worldRange * rad;

            float dist = Mathf.Sqrt(px * px + py * py);
            if (dist > edge)
            {
                if (m.clampToEdge || m.alwaysVisible) { float s = edge / dist; px *= s; py *= s; }
                else continue;   // out of range and not pinned → skip
            }

            var blip = GetBlip(used++);
            float sz = m.Size();
            Style(blip, m.Color(), sz);
            blip.style.left = rad + px - sz * 0.5f;
            blip.style.top = rad + py - sz * 0.5f;
            blip.style.display = DisplayStyle.Flex;
        }

        for (int i = used; i < pool.Count; i++) pool[i].style.display = DisplayStyle.None;
    }

    Transform FindSelf()
    {
        var markers = MinimapMarker.All;
        for (int i = 0; i < markers.Count; i++)
            if (markers[i] != null && markers[i].type == MinimapMarkerType.Self) return markers[i].transform;
        return null;
    }

    VisualElement GetBlip(int i)
    {
        while (pool.Count <= i) { var b = MakeBlip(); blipLayer.Add(b); pool.Add(b); }
        return pool[i];
    }

    static VisualElement MakeBlip()
    {
        var b = new VisualElement();
        b.style.position = Position.Absolute;
        b.pickingMode = PickingMode.Ignore;
        return b;
    }

    static void Style(VisualElement b, Color c, float sz)
    {
        b.style.width = sz; b.style.height = sz;
        b.style.backgroundColor = c;
        Round(b, sz * 0.5f);
        SetBorder(b, 1.5f, new Color(0f, 0f, 0f, 0.55f));
    }

    static void Round(VisualElement e, float r)
    {
        e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
        e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
    }

    static void SetBorder(VisualElement e, float w, Color c)
    {
        e.style.borderTopWidth = w; e.style.borderBottomWidth = w;
        e.style.borderLeftWidth = w; e.style.borderRightWidth = w;
        e.style.borderTopColor = c; e.style.borderBottomColor = c;
        e.style.borderLeftColor = c; e.style.borderRightColor = c;
    }
}
