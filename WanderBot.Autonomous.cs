using UnityEngine;
using UnityEngine.AI;

public partial class WanderBot
{
    private void HandleWanderLogic()
    {
        if (enableFollowMode || agent == null || isTurningInPlace)
            return;

        if (agent.pathPending)
            return;

        if (!waiting && agent.remainingDistance <= agent.stoppingDistance)
        {
            waiting = true;
            waitTimer = GetWaitDuration();
            HandleArrival();
        }

        if (!waiting)
            return;

        waitTimer -= Time.deltaTime;

        if (waitTimer > 0f)
            return;

        waiting = false;
        activeLookTargetOverride = null;
        MoveToNextAutonomousDestination();
    }

    private void BeginAutonomousMovement()
    {
        waiting = false;
        activeLookTargetOverride = null;

        if (HasPatrolPoints())
        {
            currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, patrolPoints.Length - 1);
            MoveToCurrentPatrolPoint();
            return;
        }

        PickNewDestination();
    }

    private void MoveToNextAutonomousDestination()
    {
        if (!HasPatrolPoints())
        {
            PickNewDestination();
            return;
        }

        if (!loopPatrol && currentPatrolIndex >= patrolPoints.Length - 1)
        {
            StopAgent();
            return;
        }

        currentPatrolIndex = loopPatrol
            ? (currentPatrolIndex + 1) % patrolPoints.Length
            : Mathf.Min(currentPatrolIndex + 1, patrolPoints.Length - 1);

        MoveToCurrentPatrolPoint();
    }

    private void MoveToCurrentPatrolPoint()
    {
        if (!TryGetCurrentPatrolPoint(out PatrolPoint patrolPoint))
            return;

        Vector3 destination = patrolPoint.point.position;
        destination.y = transform.position.y;
        MoveToDestination(destination);
    }

    private bool HasPatrolPoints()
    {
        return usePatrolPoints && patrolPoints != null && patrolPoints.Length > 0;
    }

    private bool TryGetCurrentPatrolPoint(out PatrolPoint patrolPoint)
    {
        patrolPoint = null;

        if (!HasPatrolPoints())
            return false;

        patrolPoint = patrolPoints[currentPatrolIndex];
        return patrolPoint != null && patrolPoint.point != null;
    }

    private float GetWaitDuration()
    {
        if (!TryGetCurrentPatrolPoint(out PatrolPoint patrolPoint))
            return Random.Range(minWaitTime, maxWaitTime);

        return Mathf.Max(0f, patrolPoint.waitTime);
    }

    private void HandleArrival()
    {
        if (!TryGetCurrentPatrolPoint(out PatrolPoint patrolPoint))
            return;

        activeLookTargetOverride = patrolPoint.lookTarget;

        if (animator == null || string.IsNullOrWhiteSpace(patrolPoint.animationTrigger))
            return;

        animator.SetTrigger(patrolPoint.animationTrigger);
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
}
