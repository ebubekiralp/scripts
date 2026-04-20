using UnityEngine;
using UnityEngine.AI;

public class WanderBot : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;

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
    [SerializeField] private float ninetyTurnTolerance = 20f;
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

        if (enableFollowMode)
        {
            UpdateFollowDestination(true);
            return;
        }

        PickNewDestination();
    }

    private void Update()
    {
        HandleModeSwitch();
        HandleTurning();
        HandleFollowMode();
        HandleAgentRotation();
        HandleMovementAnimation();
        HandleWanderLogic();
    }

    private void HandleModeSwitch()
    {
        bool modeChanged = enableFollowMode != wasFollowModeEnabled;

        if (!modeChanged)
            return;

        wasFollowModeEnabled = enableFollowMode;
        isTurningInPlace = false;
        waiting = false;

        if (enableFollowMode)
        {
            UpdateFollowDestination(true);
            return;
        }

        PickNewDestination();
    }

    private void HandleWanderLogic()
    {
        if (enableFollowMode || agent == null || isTurningInPlace)
            return;

        if (agent.pathPending)
            return;

        if (!waiting && agent.remainingDistance <= agent.stoppingDistance)
        {
            waiting = true;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }

        if (waiting)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                waiting = false;
                PickNewDestination();
            }
        }
    }

    private void PickNewDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * wanderRadius;
            randomPoint.y = transform.position.y;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                MoveToDestination(hit.position);
                return;
            }
        }
    }

    private void MoveToDestination(Vector3 destination)
    {
        if (agent == null)
            return;

        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        float signedAngle = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up);
        float absoluteAngle = Mathf.Abs(signedAngle);

        if (useTurnAnimations && absoluteAngle >= turnTriggerAngle && animator != null)
        {
            TurnType turnType = absoluteAngle >= backTurnThreshold
                ? TurnType.Back180
                : (signedAngle < 0f ? TurnType.Left90 : TurnType.Right90);

            pendingDestination = destination;
            turnStartRotation = transform.rotation;
            turnTargetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            turnDuration = GetTurnDuration(turnType);
            turnTimer = turnDuration;
            isTurningInPlace = true;
            waiting = false;
            agent.isStopped = true;
            agent.ResetPath();
            TriggerTurnAnimation(turnType);
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    private void StopAgent()
    {
        if (agent == null)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private float GetTurnDuration(TurnType turnType)
    {
        if (turnType == TurnType.Back180)
            return turnBackDuration;

        return turnType == TurnType.Left90 ? turnLeftDuration : turnRightDuration;
    }

    private void TriggerTurnAnimation(TurnType turnType)
    {
        if (turnType == TurnType.Back180)
        {
            animator.SetTrigger(BackTurnHash);
            return;
        }

        if (turnType == TurnType.Left90)
            animator.SetTrigger(LeftTurnHash);

        if (turnType == TurnType.Right90)
            animator.SetTrigger(RightTurnHash);
    }

    private void HandleTurning()
    {
        if (!isTurningInPlace)
            return;

        turnTimer -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(turnTimer / Mathf.Max(turnDuration, 0.01f));
        float rotationProgress = Mathf.InverseLerp(turnRotationStartNormalized, 1f, progress);
        transform.rotation = Quaternion.Slerp(turnStartRotation, turnTargetRotation, rotationProgress);

        if (turnTimer > 0f)
            return;

        transform.rotation = turnTargetRotation;
        isTurningInPlace = false;

        if (enableFollowMode)
        {
            UpdateFollowDestination(true);
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(pendingDestination);
    }

    private void HandleFollowMode()
    {
        if (!enableFollowMode || agent == null || isTurningInPlace)
            return;

        UpdateFollowDestination(false);
    }

    private void UpdateFollowDestination(bool forceUpdate)
    {
        if (agent == null)
            return;

        if (followTarget == null)
        {
            StopAgent();
            return;
        }

        Vector3 toTarget = followTarget.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance <= followStopDistance)
        {
            StopAgent();
            return;
        }

        Vector3 destination = followTarget.position - toTarget.normalized * followStopDistance;
        destination.y = transform.position.y;

        bool destinationChanged = Vector3.Distance(lastFollowDestination, destination) >= followRefreshDistance;

        if (!forceUpdate && !destinationChanged && !agent.isStopped)
            return;

        lastFollowDestination = destination;
        MoveToDestination(destination);
    }

    private void StartTurnInPlace(Vector3 direction, float signedAngle, TurnType turnType)
    {
        pendingDestination = transform.position;
        turnStartRotation = transform.rotation;
        turnTargetRotation = GetTurnTargetRotation(direction, turnType);
        turnDuration = GetTurnDuration(turnType);
        turnTimer = turnDuration;
        isTurningInPlace = true;
        waiting = false;
        StopAgent();
        TriggerTurnAnimation(turnType);
    }

    private Quaternion GetTurnTargetRotation(Vector3 direction, TurnType turnType)
    {
        if (turnType == TurnType.Back180)
            return Quaternion.LookRotation(direction, Vector3.up);

        float yAngle = turnType == TurnType.Left90 ? -90f : 90f;
        return Quaternion.Euler(0f, transform.eulerAngles.y + yAngle, 0f);
    }

    private void HandleAgentRotation()
    {
        if (agent == null || isTurningInPlace)
            return;

        Vector3 desiredDirection = agent.desiredVelocity;
        desiredDirection.y = 0f;

        if (desiredDirection.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
    }

    private void HandleMovementAnimation()
    {
        if (animator == null)
            return;

        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
        float moveX = localVelocity.x / Mathf.Max(agent.speed, 0.01f);
        float moveY = localVelocity.z / Mathf.Max(agent.speed, 0.01f);

        animator.SetFloat(MoveXHash, moveX, animationDamp, Time.deltaTime);
        animator.SetFloat(MoveYHash, moveY, animationDamp, Time.deltaTime);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !enableLookAt || lookTarget == null)
        {
            if (animator != null)
                animator.SetLookAtWeight(0f);
            return;
        }

        float distance = Vector3.Distance(transform.position, lookTarget.position);
        float targetLookWeight = 0f;

        if (distance <= maxLookDistance)
        {
            Vector3 directionToTarget = (lookTarget.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle <= maxLookAngle)
            {
                targetLookWeight = lookWeight;
            }
        }

        currentLookWeight = Mathf.Lerp(currentLookWeight, targetLookWeight, Time.deltaTime * lookSmoothSpeed);
        animator.SetLookAtWeight(currentLookWeight, bodyWeight, headWeight, eyesWeight, clampWeight);
        animator.SetLookAtPosition(lookTarget.position);
    }
}
