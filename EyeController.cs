using System.Collections;
using UnityEngine;

public class EyeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("Blink")]
    [SerializeField] private string blinkBlendShapeName = "Inori_Blink";
    [SerializeField] private bool winkEnabled = true;

    [Header("Blink Timing")]
    [SerializeField] private Vector2 blinkIntervalRange = new Vector2(2.5f, 5.5f);
    [SerializeField] private Vector2 closeDurationRange = new Vector2(0.04f, 0.08f);
    [SerializeField] private Vector2 openDurationRange = new Vector2(0.05f, 0.1f);
    [SerializeField] private Vector2 holdClosedRange = new Vector2(0.02f, 0.05f);

    [Header("Double Blink Chance")]
    [SerializeField][Range(0f, 1f)] private float doubleBlinkChance = 0.18f;
    [SerializeField] private Vector2 doubleBlinkGapRange = new Vector2(0.06f, 0.14f);

    [Header("Debug Buttons")]
    [SerializeField] private bool showDebugButtons = true;

    private int blinkIndex = -1;
    private Coroutine blinkRoutine;

    private void Start()
    {
        if (!IsRendererValid())
            return;

        blinkIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blinkBlendShapeName);

        if (blinkIndex < 0)
        {
            Debug.LogWarning($"{name}: '{blinkBlendShapeName}' adlý blendshape bulunamadý.");
            return;
        }

        SetBlinkWeight(0f);

        if (winkEnabled)
            StartBlinkRoutine();
    }

    private void OnDisable()
    {
        StopBlinkRoutine();

        if (IsRendererValid() && blinkIndex >= 0)
            SetBlinkWeight(0f);
    }

    [ContextMenu("Wink Açýk")]
    public void EnableWink()
    {
        winkEnabled = true;
        StartBlinkRoutine();
    }

    [ContextMenu("Wink Kapalý")]
    public void DisableWink()
    {
        winkEnabled = false;
        StopBlinkRoutine();

        if (IsRendererValid() && blinkIndex >= 0)
            SetBlinkWeight(0f);
    }

    private void StartBlinkRoutine()
    {
        if (!IsRendererValid() || blinkIndex < 0)
            return;

        StopBlinkRoutine();
        blinkRoutine = StartCoroutine(BlinkLoop());
    }

    private void StopBlinkRoutine()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
    }

    private IEnumerator BlinkLoop()
    {
        while (winkEnabled)
        {
            float wait = Random.Range(blinkIntervalRange.x, blinkIntervalRange.y);
            yield return new WaitForSeconds(wait);

            yield return BlinkOnce();

            if (Random.value < doubleBlinkChance)
            {
                float gap = Random.Range(doubleBlinkGapRange.x, doubleBlinkGapRange.y);
                yield return new WaitForSeconds(gap);
                yield return BlinkOnce();
            }
        }

        SetBlinkWeight(0f);
    }

    private IEnumerator BlinkOnce()
    {
        float closeDuration = Random.Range(closeDurationRange.x, closeDurationRange.y);
        float holdClosed = Random.Range(holdClosedRange.x, holdClosedRange.y);
        float openDuration = Random.Range(openDurationRange.x, openDurationRange.y);

        yield return AnimateBlink(0f, 100f, closeDuration);
        yield return new WaitForSeconds(holdClosed);
        yield return AnimateBlink(100f, 0f, openDuration);
    }

    private IEnumerator AnimateBlink(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            // daha doðal his için smooth easing
            t = t * t * (3f - 2f * t);

            float weight = Mathf.Lerp(from, to, t);
            SetBlinkWeight(weight);

            yield return null;
        }

        SetBlinkWeight(to);
    }

    private void SetBlinkWeight(float value)
    {
        if (!IsRendererValid() || blinkIndex < 0)
            return;

        skinnedMeshRenderer.SetBlendShapeWeight(blinkIndex, value);
    }

    private bool IsRendererValid()
    {
        if (skinnedMeshRenderer == null)
        {
            Debug.LogWarning($"{name}: SkinnedMeshRenderer yok.");
            return false;
        }

        if (skinnedMeshRenderer.sharedMesh == null)
        {
            Debug.LogWarning($"{name}: sharedMesh yok.");
            return false;
        }

        return true;
    }

    private void OnGUI()
    {
        if (!showDebugButtons)
            return;

        const int width = 140;
        const int height = 40;
        const int x = 20;
        const int y1 = 20;
        const int y2 = 70;

        if (GUI.Button(new Rect(x, y1, width, height), "Wink Açýk"))
        {
            EnableWink();
        }

        if (GUI.Button(new Rect(x, y2, width, height), "Wink Kapalý"))
        {
            DisableWink();
        }
    }
}