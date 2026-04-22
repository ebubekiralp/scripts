using UnityEngine;
using RootMotion.FinalIK;

public partial class WanderBot
{
    [Header("Final IK Components")]
    [SerializeField] private LookAtIK lookAtIK;
    [SerializeField] private FullBodyBipedIK fbbIK;

    private Transform ikSmoothLookTarget;

    private void SetupFinalIK()
    {
        if (lookAtIK == null) lookAtIK = GetComponent<LookAtIK>();
        if (fbbIK == null) fbbIK = GetComponent<FullBodyBipedIK>();

        if (lookAtIK != null)
        {
            // Sahnede çöp yaratmamak için script hedefini kodla yaratıyoruz
            GameObject ikTargetObj = new GameObject("IK_SmoothLookTarget");
            ikTargetObj.transform.SetParent(transform);
            ikSmoothLookTarget = ikTargetObj.transform;
            
            // Başlangıç pozisyonu olarak hafif önü ayarlıyoruz
            ikSmoothLookTarget.position = transform.position + transform.forward * 2f;
            lookAtIK.solver.target = ikSmoothLookTarget;
        }
    }

    private void HandleFinalIK()
    {
        Transform currentLookTarget = activeLookTargetOverride != null ? activeLookTargetOverride : lookTarget;

        if (lookAtIK == null || !enableLookAt || currentLookTarget == null)
        {
            currentLookWeight = Mathf.Lerp(currentLookWeight, 0f, Time.deltaTime * lookSmoothSpeed);
            if (lookAtIK != null) lookAtIK.solver.IKPositionWeight = currentLookWeight;
            return;
        }

        float distance = Vector3.Distance(transform.position, currentLookTarget.position);
        float targetLookWeight = 0f;

        // Hedef menzilde ve açıda mı kontrolü
        if (distance <= maxLookDistance)
        {
            Vector3 directionToTarget = (currentLookTarget.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle <= maxLookAngle)
            {
                targetLookWeight = lookWeight; // WanderBot.cs içindeki değişkeni kullanıyoruz
            }
        }

        // Hedef objeyi pürüzsüzce gerçek hedefe taşı (Robotik hareketi engeller)
        if (targetLookWeight > 0.01f)
        {
            ikSmoothLookTarget.position = Vector3.Lerp(ikSmoothLookTarget.position, currentLookTarget.position, Time.deltaTime * lookSmoothSpeed);
        }

        // Ağırlığı pürüzsüzce uyarla
        currentLookWeight = Mathf.Lerp(currentLookWeight, targetLookWeight, Time.deltaTime * lookSmoothSpeed);
        lookAtIK.solver.IKPositionWeight = currentLookWeight;
    }
}
