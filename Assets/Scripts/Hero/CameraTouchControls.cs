using UnityEngine;
using UnityEngine.InputSystem;

// Touch camera control for tablets: two-finger PINCH to zoom + two-finger TWIST to orbit (drives CameraFollow).
// Touches inside the joystick zone (bottom-left) are ignored, so you can move with the left thumb and adjust
// the camera with two fingers on the right at the same time. No-ops on desktop (no touchscreen).
public class CameraTouchControls : MonoBehaviour
{
    public CameraFollow cam;
    public float rotateSensitivity = 1f;       // degrees of orbit per degree of finger twist
    public float zoomSensitivity = 0.0016f;    // normalized zoom per pixel of pinch
    [Tooltip("Fraction of the screen (from bottom-left) treated as the joystick zone and ignored for camera gestures.")]
    public Vector2 joystickZone = new Vector2(0.4f, 0.55f);

    float prevDist, prevAngle;
    bool hasPrev;

    void Awake() { if (cam == null) cam = GetComponent<CameraFollow>(); }

    void Update()
    {
        var ts = Touchscreen.current;
        if (ts == null || cam == null) { hasPrev = false; return; }

        // Collect active touches outside the joystick zone.
        Vector2 a = Vector2.zero, b = Vector2.zero;
        int n = 0;
        foreach (var t in ts.touches)
        {
            var phase = t.phase.ReadValue();
            if (phase != UnityEngine.InputSystem.TouchPhase.Began &&
                phase != UnityEngine.InputSystem.TouchPhase.Moved &&
                phase != UnityEngine.InputSystem.TouchPhase.Stationary) continue;

            Vector2 p = t.position.ReadValue();
            if (p.x < Screen.width * joystickZone.x && p.y < Screen.height * joystickZone.y) continue;  // joystick

            if (n == 0) a = p; else if (n == 1) b = p;
            n++;
            if (n >= 2) break;
        }

        if (n == 2)
        {
            float dist = Vector2.Distance(a, b);
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            if (hasPrev)
            {
                // pinch apart (dist up) → zoom in (closer = lower normalized)
                float z = Mathf.Clamp01(cam.GetZoomNormalized() - (dist - prevDist) * zoomSensitivity);
                cam.SetZoomNormalized(z);
                // twist → orbit
                cam.yaw += Mathf.DeltaAngle(prevAngle, angle) * rotateSensitivity;
            }
            prevDist = dist; prevAngle = angle; hasPrev = true;
        }
        else hasPrev = false;
    }
}
