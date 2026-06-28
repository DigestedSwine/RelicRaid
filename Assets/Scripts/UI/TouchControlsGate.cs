using UnityEngine;
using UnityEngine.InputSystem;

// Shows the touch-controls canvas only on touch/mobile devices; hidden on desktop (WASD/gamepad there).
// forceShow lets you preview the layout in the editor / Device Simulator.
public class TouchControlsGate : MonoBehaviour
{
    public bool forceShow = false;

    void Awake()
    {
        bool touch = forceShow || Application.isMobilePlatform || Touchscreen.current != null;
        gameObject.SetActive(touch);
    }
}
