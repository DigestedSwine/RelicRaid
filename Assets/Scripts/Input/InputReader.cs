using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Input abstraction layer. Gameplay reads MoveInput / SprintHeld / AttackPressed from here and never
// touches a device binding. WHERE the input comes from (WASD, gamepad, or a touch On-Screen Stick) lives
// entirely in the InputSystem_Actions asset below — so swapping to touch later is a binding/UI change,
// not a code change.
[CreateAssetMenu(menuName = "RelicRaid/Input Reader", fileName = "InputReader")]
public class InputReader : ScriptableObject
{
    [Tooltip("The InputSystem_Actions asset. All bindings (keyboard / gamepad / touch joystick) live here.")]
    public InputActionAsset actions;

    [Header("Names inside the asset")]
    public string actionMap = "Player";
    public string moveActionName = "Move";
    public string sprintActionName = "Sprint";
    public string attackActionName = "Attack";

    [Header("Abilities")]
    [Tooltip("Ability slots bound to keyboard 1..N for now; on-screen buttons call PressAbility(slot).")]
    public int abilitySlots = 4;

    InputActionMap _map;
    InputAction _move, _sprint, _attack;
    InputAction[] _abilities;
    InputAction _interact;
    bool _enabled;

    // --- Abstract API the gameplay layer consumes (device-agnostic) ---
    public Vector2 MoveInput => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
    public bool SprintHeld => _sprint != null && _sprint.IsPressed();
    public bool InteractHeld => _interact != null && _interact.IsPressed();   // hold-to-mine, etc.
    public event Action AttackPressed;
    public event Action<int> AbilityPressed;   // (slot index)

    // Touch / on-screen ability buttons call this so they go through the same path as keys.
    public void PressAbility(int slot) => AbilityPressed?.Invoke(slot);

    public void Enable()
    {
        if (_enabled) return;
        if (actions == null) { Debug.LogWarning("[InputReader] No InputActionAsset assigned."); return; }

        _map = actions.FindActionMap(actionMap, true);
        _move   = _map.FindAction(moveActionName, true);
        _sprint = _map.FindAction(sprintActionName, false);   // optional
        _attack = _map.FindAction(attackActionName, false);   // optional

        if (_attack != null) _attack.performed += OnAttack;

        // Ability slots: code-bound to number keys 1..N for now (no asset edit needed). Touch buttons
        // can call PressAbility(slot) to reach the same AbilityPressed event.
        _abilities = new InputAction[Mathf.Max(0, abilitySlots)];
        for (int i = 0; i < _abilities.Length; i++)
        {
            var a = new InputAction("Ability" + (i + 1), InputActionType.Button, "<Keyboard>/" + (i + 1));
            int slot = i;
            a.performed += _ => AbilityPressed?.Invoke(slot);
            a.Enable();
            _abilities[i] = a;
        }

        // Interact / hold-to-mine: code-bound to E + gamepad West (touch button can add a binding later).
        _interact = new InputAction("Interact", InputActionType.Button);
        _interact.AddBinding("<Keyboard>/e");
        _interact.AddBinding("<Gamepad>/buttonWest");
        _interact.Enable();

        _map.Enable();
        _enabled = true;
    }

    public void Disable()
    {
        if (!_enabled) return;
        if (_attack != null) _attack.performed -= OnAttack;
        if (_abilities != null)
            foreach (var a in _abilities) { if (a != null) { a.Disable(); a.Dispose(); } }
        _abilities = null;
        if (_interact != null) { _interact.Disable(); _interact.Dispose(); _interact = null; }
        if (_map != null) _map.Disable();
        _enabled = false;
    }

    void OnAttack(InputAction.CallbackContext ctx) => AttackPressed?.Invoke();
}
