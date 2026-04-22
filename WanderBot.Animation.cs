using UnityEngine;

public partial class WanderBot
{
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

    private void HandleMovementAnimation()
    {
        if (animator == null || agent == null)
            return;

        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
        float moveX = localVelocity.x / Mathf.Max(agent.speed, 0.01f);
        float moveY = localVelocity.z / Mathf.Max(agent.speed, 0.01f);

        animator.SetFloat(MoveXHash, moveX, animationDamp, Time.deltaTime);
        animator.SetFloat(MoveYHash, moveY, animationDamp, Time.deltaTime);
    }

}
