using UnityEngine;
using UnityEngine.InputSystem;

// Top-down follow camera with zoom (dolly along the view direction) and 360° orbit (right-click hold):
// dragging horizontally pivots the camera around the character while keeping the SAME fixed tilt/height.
// The yaw is held after you let go (free orbit), not re-centered. Orbit is fed through Orbit(screenDelta),
// so 3-finger touch can drive the same path later.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 12f, -7f);   // height + back tilt; defines the FIXED angle that orbits
    public float smoothTime = 0.15f;

    [Header("Zoom (offset multiplier)")]
    [Range(0.5f, 1.5f)] public float zoom = 1f;
    public float zoomMin = 0.5f;     // 50% closer
    public float zoomMax = 1.5f;     // 50% farther

    [Header("Orbit (right-click hold now; touch later)")]
    public float orbitSpeed = 0.25f;     // degrees of yaw per screen pixel dragged
    public bool invertOrbit = false;     // flip drag direction if desired

    Vector3 vel;
    public float yaw;                    // current orbit angle around the character (degrees), persists

    // ---- Zoom (slider hook): t 0..1 → [zoomMin, zoomMax]. t=0.5 ≈ current. ----
    public void SetZoomNormalized(float t) => zoom = Mathf.Lerp(zoomMin, zoomMax, Mathf.Clamp01(t));
    public float GetZoomNormalized() => Mathf.InverseLerp(zoomMin, zoomMax, zoom);

    // ---- Orbit (input-agnostic): feed a screen-space drag delta (mouse or future touch). Horizontal only;
    // pitch stays fixed so the camera keeps its angle while pivoting 360° around the character. ----
    public void Orbit(Vector2 screenDelta)
    {
        yaw += screenDelta.x * orbitSpeed * (invertOrbit ? -1f : 1f);
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.isPressed) Orbit(mouse.delta.ReadValue());
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 focus = target.position;
        // Rotate the fixed offset around Y → orbit at constant tilt/height/distance.
        Vector3 rotatedOffset = Quaternion.Euler(0f, yaw, 0f) * offset;
        Vector3 desired = focus + rotatedOffset * zoom;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
        transform.LookAt(focus + Vector3.up * 1f);
    }
}
