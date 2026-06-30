using UnityEngine;

// Single-hero top-down movement. Reads input through an InputReader abstraction, so it has no idea
// whether the input came from WASD, a gamepad, or a touch joystick. Swapping control schemes is a
// binding/UI change in the InputSystem_Actions asset, not a change here.
// Drives the Animator built in Assets/Animations/Wizard/WizardController.controller.
[RequireComponent(typeof(CharacterController))]
public class HeroController : MonoBehaviour
{
    [Header("Input")]
    public InputReader input;            // assign the InputReader asset
    public Transform cameraTransform;    // movement is relative to this (defaults to Camera.main) so WASD follows the orbited camera

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 6f;
    public float rotationSpeed = 720f;   // deg/sec to face travel direction
    public float gravity = -20f;

    [Header("Animator")]
    public float animDamp = 0.1f;        // smoothing on the Speed param

    [Header("Networking")]
    [Tooltip("When true, movement is driven by NetworkPlayer.FixedUpdateNetwork (so NetworkTransform syncs it). Update() stops moving.")]
    public bool networkDriven = false;

    CharacterController cc;
    Animator animator;
    float verticalVel;

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void OnEnable()
    {
        if (input != null) input.Enable();
    }

    void OnDisable()
    {
        if (input != null) input.Disable();
    }

    void Update()
    {
        if (!networkDriven) DoMove(Time.deltaTime);   // single-player path; networked players call DoMove from the net tick
    }

    // One movement+animation step. Called from Update (local) or NetworkPlayer.FixedUpdateNetwork (networked).
    public void DoMove(float dt)
    {
        Vector2 move = input != null ? input.MoveInput : Vector2.zero;

        // Camera-relative: W = away from the camera, D = camera-right — so WASD always matches the
        // screen regardless of how the camera has been orbited. Falls back to world axes if no camera.
        Vector3 fwd = Vector3.forward, right = Vector3.right;
        if (cameraTransform != null)
        {
            fwd = cameraTransform.forward; fwd.y = 0f; fwd.Normalize();
            right = cameraTransform.right; right.y = 0f; right.Normalize();
        }
        Vector3 dir = right * move.x + fwd * move.y;
        float mag = Mathf.Clamp01(dir.magnitude);              // joystick deflection 0..1
        if (mag < 0.1f) mag = 0f;                              // small deadzone (no center creep)
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();       // keep direction, drop magnitude

        // Analog: speed scales with how far the stick is pushed, maxing at runSpeed (sprint) at full tilt.
        // Sprint button still forces max for keyboard/gamepad.
        bool sprinting = input != null && input.SprintHeld;
        float targetSpeed = sprinting ? runSpeed : mag * runSpeed;
        Vector3 horizontal = dir * targetSpeed;

        // Gravity so the CharacterController stays grounded.
        if (cc.isGrounded && verticalVel < 0f) verticalVel = -2f;
        verticalVel += gravity * dt;

        Vector3 velocity = horizontal + Vector3.up * verticalVel;
        cc.Move(velocity * dt);

        // Face travel direction.
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, look, rotationSpeed * dt);
        }

        // Feed the locomotion blend tree (planar speed only).
        if (animator != null)
            animator.SetFloat(SpeedHash, horizontal.magnitude, animDamp, dt);
    }
}
