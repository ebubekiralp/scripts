using UnityEngine;
using UnityEngine.AI;

public partial class WanderBot : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;

    [Header("Patrol Points")]
    [SerializeField] private bool usePatrolPoints;
    [SerializeField] private PatrolPoint[] patrolPoints;
    [SerializeField] private bool loopPatrol = true;

    [Header("Animation")]
    [SerializeField] private float animationDamp = 0.15f;
    [SerializeField] private float rotationSmoothSpeed = 8f;

    [Header("Turn Animations")]
    [SerializeField] private bool useTurnAnimations = true;
    [SerializeField] private float turnTriggerAngle = 60f;
    [SerializeField] private float turnLeftDuration = 0.75f;
    [SerializeField] private float turnRightDuration = 0.75f;
    [SerializeField] private float turnBackDuration = 0.9f;
    [SerializeField][Range(0f, 1f)] private float turnRotationStartNormalized = 0.18f;
    [SerializeField] private float backTurnThreshold = 150f;

    [Header("Follow Mode")]
    [SerializeField] private bool enableFollowMode;
    [SerializeField] private Transform followTarget;
    [SerializeField] private float followStopDistance = 1.5f;
    [SerializeField] private float followRefreshDistance = 0.35f;

    [Header("Look At")]
    [SerializeField] private Transform lookTarget;
    [SerializeField] private bool enableLookAt = true;
    [SerializeField][Range(0f, 1f)] private float lookWeight = 0.7f;
    [SerializeField][Range(0f, 1f)] private float bodyWeight = 0.1f;
    [SerializeField][Range(0f, 1f)] private float headWeight = 1f;
    [SerializeField][Range(0f, 1f)] private float eyesWeight = 0.5f;
    [SerializeField][Range(0f, 1f)] private float clampWeight = 0.6f;
    [SerializeField] private float maxLookDistance = 7f;
    [SerializeField] private float maxLookAngle = 60f;
    [SerializeField] private float lookSmoothSpeed = 5f;

    private float waitTimer;
    private bool waiting;
    private float currentLookWeight;
    private bool isTurningInPlace;
    private float turnTimer;
    private float turnDuration;
    private Vector3 pendingDestination;
    private Quaternion turnStartRotation;
    private Quaternion turnTargetRotation;
    private bool wasFollowModeEnabled;
    private Vector3 lastFollowDestination;
    private int currentPatrolIndex;
    private Transform activeLookTargetOverride;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int LeftTurnHash = Animator.StringToHash("TurnLeft90");
    private static readonly int RightTurnHash = Animator.StringToHash("TurnRight90");
    private static readonly int BackTurnHash = Animator.StringToHash("Turn180");

    private enum TurnType
    {
        Left90,
        Right90,
        Back180
    }

    private void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent != null)
            agent.updateRotation = false;

        wasFollowModeEnabled = enableFollowMode;

        SetupFinalIK();
        SetupFaceSystem(); // Yüz sistemini hazırlayıp döngüyü başlatıyoruz

        if (enableFollowMode)
        {
            UpdateFollowDestination(true);
            return;
        }

        BeginAutonomousMovement();
    }

    private void Update()
    {
        HandleModeSwitch();
        HandleTurning();
        HandleIdleTurning(); // Hedef etrafında dönerken ayakların ayak uydurması
        HandleFollowMode();
        HandleAgentRotation();
        HandleMovementAnimation();
        HandleWanderLogic();
        HandleFinalIK();
    }

    private void HandleModeSwitch()
    {
        bool modeChanged = enableFollowMode != wasFollowModeEnabled;

        if (!modeChanged)
            return;

        wasFollowModeEnabled = enableFollowMode;
        isTurningInPlace = false;
        waiting = false;
        activeLookTargetOverride = null;

        if (enableFollowMode)
        {
            UpdateFollowDestination(true);
            return;
        }

        BeginAutonomousMovement();
    }
}
