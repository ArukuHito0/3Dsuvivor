using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using static PlayerInput;

[CreateAssetMenu(fileName = "InputReader", menuName = "Player/InputReader")]
public class InputReader : ScriptableObject, IPlayerActions
{
    public event UnityAction<Vector2> Move = delegate { };
    public event UnityAction<Vector2, bool> Look = delegate { };
    public event UnityAction Attack = delegate { };
    public event UnityAction Dodge = delegate { };
    public event UnityAction Jump = delegate { };
    public event UnityAction EnableGuard = delegate { };
    public event UnityAction DisableGuard = delegate { };
    public event UnityAction<bool> Sprint = delegate { };

    PlayerInput inputActions;

    public Vector3 Direction => inputActions.Player.Move.ReadValue<Vector2>();

    private bool IsDeviceMouse(InputAction.CallbackContext context) => context.control.device.name == "Mouse";

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new PlayerInput();
            inputActions.Player.SetCallbacks(this);
        }
        inputActions.Enable();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        Attack.Invoke();
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        Dodge.Invoke();
    }

    public void OnGuard(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                EnableGuard.Invoke(); break;
            case InputActionPhase.Canceled:
                DisableGuard.Invoke(); break;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        Jump.Invoke();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Look.Invoke(context.ReadValue<Vector2>(), IsDeviceMouse(context));
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Move.Invoke(context.ReadValue<Vector2>());
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        Sprint.Invoke(context.ReadValue<bool>());
    }
}