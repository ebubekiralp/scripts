using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonControllerMod : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private float acceleration = 2.0f;
    [SerializeField] private float deceleration = 2.0f;
    [SerializeField] private float backwardDeceleration = 1.0f;
    [SerializeField] private float maximumWalkVelocity = 1.0f;

    private float velocityX = 0.0f;
    private float velocityY = 0.0f;


    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3f;

    [Header("Look Paramteters")]
    [SerializeField] private float mouseSensivity = 0.1f;
    [SerializeField] private float upDownLookRange = 80f;


    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInputHendler playerInputHendler;

    private Vector3 currentMovement;
    private float verticalRotation;
    private float CurrentSpeed => walkSpeed;



    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleAnimation();
    }

    private Vector3 CalculateWorldDirection()
    {
        Vector3 inputDirection = new Vector3(playerInputHendler.MovementInput.x, 0f, playerInputHendler.MovementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        return worldDirection.normalized;
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovement.x = worldDirection.x * CurrentSpeed;
        currentMovement.z = worldDirection.z * CurrentSpeed;

        characterController.Move(currentMovement * Time.deltaTime);
    }

    private void ApplyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }

    private void ApplyVerticalRotation(float rotationAmount)
    {
        verticalRotation = Mathf.Clamp(verticalRotation - rotationAmount, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);

    }

    private void HandleRotation()
    {
        float mouseXRotation = playerInputHendler.RotationInput.x * mouseSensivity;
        float mouseYRotation = playerInputHendler.RotationInput.y * mouseSensivity;

        ApplyHorizontalRotation(mouseXRotation);
        ApplyVerticalRotation(mouseYRotation);


    }

    private void ChangeAnimationVelocity(bool forwardPressed, bool backwardPressed, bool leftPressed, bool rightPressed)
    {
        if (forwardPressed && velocityY < maximumWalkVelocity)
            velocityY += Time.deltaTime * acceleration;

        if (!forwardPressed && !backwardPressed && velocityY > 0.0f)
            velocityY -= Time.deltaTime * deceleration;

        if (backwardPressed && velocityY > -maximumWalkVelocity)
            velocityY -= Time.deltaTime * acceleration;

        if (!backwardPressed && velocityY < 0.0f)
            velocityY += Time.deltaTime * backwardDeceleration;

        if (leftPressed && velocityX > -maximumWalkVelocity)
            velocityX -= Time.deltaTime * acceleration;

        if (rightPressed && velocityX < maximumWalkVelocity)
            velocityX += Time.deltaTime * acceleration;

        if (!leftPressed && velocityX < 0.0f)
            velocityX += Time.deltaTime * deceleration;

        if (!rightPressed && velocityX > 0.0f)
            velocityX -= Time.deltaTime * deceleration;
    }

    private void LockOrResetAnimationVelocity(bool forwardPressed, bool backwardPressed, bool leftPressed, bool rightPressed)
    {
        if (!forwardPressed && !backwardPressed && velocityY > -0.05f && velocityY < 0.05f)
            velocityY = 0.0f;

        if (!leftPressed && !rightPressed && velocityX > -0.05f && velocityX < 0.05f)
            velocityX = 0.0f;

        if (forwardPressed && velocityY > maximumWalkVelocity)
            velocityY = maximumWalkVelocity;

        if (backwardPressed && velocityY < -maximumWalkVelocity)
            velocityY = -maximumWalkVelocity;

        if (leftPressed && velocityX < -maximumWalkVelocity)
            velocityX = -maximumWalkVelocity;

        if (rightPressed && velocityX > maximumWalkVelocity)
            velocityX = maximumWalkVelocity;
    }

    private void HandleAnimation()
    {
        bool forwardPressed = playerInputHendler.MovementInput.y > 0.1f;
        bool backwardPressed = playerInputHendler.MovementInput.y < -0.1f;
        bool leftPressed = playerInputHendler.MovementInput.x < -0.1f;
        bool rightPressed = playerInputHendler.MovementInput.x > 0.1f;
        bool isWalking = forwardPressed || backwardPressed || leftPressed || rightPressed;
        
        ChangeAnimationVelocity(forwardPressed, backwardPressed, leftPressed, rightPressed);
        LockOrResetAnimationVelocity(forwardPressed, backwardPressed, leftPressed, rightPressed);

        animator.SetFloat("MoveX", velocityX);
        animator.SetFloat("MoveY", velocityY);
        animator.SetBool("isWalking", isWalking);
    }

}