using UnityEngine;
using DarkMagic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// InputStarter: a tiny, student-friendly wrapper around a small Input Actions setup.
/// Import this sample, drop this component on a GameObject, and you can read:
/// - Move (Vector2)
/// - Look (Vector2)
/// - JumpDown / JumpHeld
/// - FireDown / FireHeld
/// - SprintHeld
/// - PauseDown
///
/// This is OPTIONAL. It does not affect I.
/// </summary>
public class InputStarter : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [Header("Optional (leave empty to auto-load from Resources/V/V_InputStarter)")]
    public InputActionAsset actions;

    private InputActionMap _player;

    private InputAction _move;
    private InputAction _look;
    private InputAction _jump;
    private InputAction _fire;
    private InputAction _sprint;
    private InputAction _pause;

    public Vector2 Move => _move?.ReadValue<Vector2>() ?? default;
    public Vector2 Look => _look?.ReadValue<Vector2>() ?? default;

    public bool JumpHeld => _jump?.IsPressed() ?? false;
    public bool JumpDown => _jump?.WasPressedThisFrame() ?? false;

    public bool FireHeld => _fire?.IsPressed() ?? false;
    public bool FireDown => _fire?.WasPressedThisFrame() ?? false;

    public bool SprintHeld => _sprint?.IsPressed() ?? false;

    public bool PauseDown => _pause?.WasPressedThisFrame() ?? false;

    private void Awake()
    {
        if (actions == null)
            actions = Resources.Load<InputActionAsset>("V/V_InputStarter");

        if (actions == null)
        {
            Debug.LogWarning("[V/InputStarter] Could not load InputActionAsset at Resources/V/V_InputStarter. Make sure the sample was imported.");
            enabled = false;
            return;
        }

        _player = actions.FindActionMap("Player", throwIfNotFound: true);

        _move = _player.FindAction("Move", throwIfNotFound: true);
        _look = _player.FindAction("Look", throwIfNotFound: true);
        _jump = _player.FindAction("Jump", throwIfNotFound: true);
        _fire = _player.FindAction("Fire", throwIfNotFound: true);
        _sprint = _player.FindAction("Sprint", throwIfNotFound: true);
        _pause = _player.FindAction("Pause", throwIfNotFound: true);
    }

    private void OnEnable()
    {
        actions?.Enable();
    }

    private void OnDisable()
    {
        actions?.Disable();
    }
#else
    private void Awake()
    {
        Debug.LogWarning("[V/InputStarter] ENABLE_INPUT_SYSTEM is not defined. Install/enable the new Input System to use this sample.");
        enabled = false;
    }
#endif
}