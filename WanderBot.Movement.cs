using UnityEngine;

public partial class WanderBot
{
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

        if (agent != null && pendingDestination != transform.position)
        {
            agent.isStopped = false;
            agent.SetDestination(pendingDestination);
        }
    }

    private void HandleIdleTurning()
    {
        // Sadece duruyorsak ve o an dönmüyorsak çalışsın
        if (isTurningInPlace || (agent != null && !agent.isStopped && agent.velocity.sqrMagnitude > 0.01f))
            return;

        Transform currentLookTarget = activeLookTargetOverride != null ? activeLookTargetOverride : lookTarget;
        if (currentLookTarget == null || !enableLookAt)
            return;

        Vector3 direction = currentLookTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        float signedAngle = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up);
        float absoluteAngle = Mathf.Abs(signedAngle);

        // Eğer hedef çok fazla yana/arkaya kaymışsa, olduğu yerde ayaklarıyla ona dönsün
        if (useTurnAnimations && absoluteAngle >= turnTriggerAngle && animator != null)
        {
            TurnType turnType = absoluteAngle >= backTurnThreshold
                ? TurnType.Back180
                : (signedAngle < 0f ? TurnType.Left90 : TurnType.Right90);

            pendingDestination = transform.position; // Sadece olduğu yerde dönüyor
            turnStartRotation = transform.rotation;
            turnTargetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            turnDuration = GetTurnDuration(turnType);
            turnTimer = turnDuration;
            isTurningInPlace = true;
            waiting = false;
            
            if (agent != null) agent.isStopped = true;
            
            TriggerTurnAnimation(turnType);
        }
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
}
