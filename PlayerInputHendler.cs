using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHendler : MonoBehaviour
{
    [Header("Input Action Assets")]
    [SerializeField] private InputActionAsset playerControls;

    [Header("Action Map Name Reference")]
    [SerializeField] private string actionMapname = "Player";

    [Header("Action Name References")]
    [SerializeField] private string movement = "Movement";
    [SerializeField] private string rotation = "Rotation";

    private InputAction movementAction;
    private InputAction rotationAction;

    public Vector2 MovementInput { get; private set; }
    public Vector2 RotationInput { get; private set; }


    public void Awake()
    {
        InputActionMap mapReference = playerControls.FindActionMap(actionMapname);

        movementAction = mapReference.FindAction(movement);
        rotationAction = mapReference.FindAction(rotation);

        SubscribeActionValuesToInputEvents();

    }

    private void SubscribeActionValuesToInputEvents()
    {
        movementAction.performed += inputInfo => MovementInput = inputInfo.ReadValue<Vector2>();
        movementAction.canceled += inputInfo => MovementInput = Vector2.zero;

        rotationAction.performed += inputInfo => RotationInput = inputInfo.ReadValue<Vector2>();
        rotationAction.canceled += inputInfo => RotationInput = Vector2.zero;

    }

    private void OnEnable()
    {
        playerControls.FindActionMap(actionMapname).Enable();
    }

    private void OnDisable()
    {
        playerControls.FindActionMap(actionMapname).Disable();

    }
}