using UnityEngine;

public class BlendShapeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("BlendShape Names (7 tane)")]
    [SerializeField] private string[] blendShapeNames = new string[7];

    [Header("Mode")]
    [SerializeField] private bool useRandomOnStart = true;
    [SerializeField] private int selectedIndex = 0;

    [Header("Value")]
    [SerializeField][Range(0f, 100f)] private float targetWeight = 100f;
    [SerializeField] private bool resetOthers = true;

    private void Start()
    {
        if (skinnedMeshRenderer == null)
        {
            Debug.LogWarning($"{name}: SkinnedMeshRenderer atanmadý.");
            return;
        }

        if (blendShapeNames == null || blendShapeNames.Length == 0)
        {
            Debug.LogWarning($"{name}: Blendshape listesi boþ.");
            return;
        }

        if (useRandomOnStart)
        {
            ApplyRandomBlendShape();
        }
        else
        {
            ApplySelectedBlendShape();
        }
    }

    [ContextMenu("Apply Selected BlendShape")]
    public void ApplySelectedBlendShape()
    {
        if (!IsRendererValid()) return;

        if (selectedIndex < 0 || selectedIndex >= blendShapeNames.Length)
        {
            Debug.LogWarning($"{name}: selectedIndex geçersiz.");
            return;
        }

        ApplyBlendShapeByName(blendShapeNames[selectedIndex]);
    }

    [ContextMenu("Apply Random BlendShape")]
    public void ApplyRandomBlendShape()
    {
        if (!IsRendererValid()) return;

        int randomIndex = Random.Range(0, blendShapeNames.Length);
        ApplyBlendShapeByName(blendShapeNames[randomIndex]);
    }

    public void ApplyBlendShapeByName(string blendShapeName)
    {
        if (!IsRendererValid()) return;

        int blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);

        if (blendShapeIndex < 0)
        {
            Debug.LogWarning($"{name}: '{blendShapeName}' adlý blendshape bulunamadý.");
            return;
        }

        if (resetOthers)
        {
            ResetAllBlendShapes();
        }

        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, targetWeight);
    }

    [ContextMenu("Reset All BlendShapes")]
    public void ResetAllBlendShapes()
    {
        if (!IsRendererValid()) return;

        int count = skinnedMeshRenderer.sharedMesh.blendShapeCount;

        for (int i = 0; i < count; i++)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(i, 0f);
        }
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
}