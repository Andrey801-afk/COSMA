using UnityEngine;

public sealed class EarthCloudDrift : MonoBehaviour
{
    private const float AxisEpsilon = 0.0001f;
    private const string GeneratedLayerPrefix = "UpperCloudLayer_Runtime_";

    [SerializeField] private Vector3 localAxis = Vector3.up;
    [SerializeField, Min(0f)] private float degreesPerSecond = 0.075f;
    [SerializeField, Range(0f, 0.95f)] private float vectorBreakupStrength = 0f;
    [SerializeField, Min(0f)] private float vectorBreakupSpeed = 0.175f;
    [SerializeField] private float vectorBreakupPhase = 0f;

    [Header("Upper Cloud Layers")]
    [SerializeField] private bool createUpperCloudLayers = true;
    [SerializeField, Range(0, 6)] private int upperCloudLayerCount = 4;
    [SerializeField, Min(0f)] private float upperCloudFirstScaleOffset = 0.0015f;
    [SerializeField, Min(0f)] private float upperCloudScaleStep = 0.0015f;
    [SerializeField, Min(0f)] private float upperCloudBaseSpeed = 0.09f;
    [SerializeField, Min(0f)] private float upperCloudSpeedStep = 0.0225f;
    [SerializeField, Range(0f, 1f)] private float upperCloudOpacity = 0.18f;
    [SerializeField, Range(0f, 1f)] private float upperCloudCoverage = 0.38f;
    [SerializeField, Range(0f, 0.95f)] private float upperCloudVectorBreakup = 0.18f;
    [SerializeField, Min(0f)] private float upperCloudVectorBreakupSpeed = 0.16f;
    [SerializeField] private Vector2 upperCloudTextureScale = Vector2.one;

    private Material[] ownedRuntimeMaterials = System.Array.Empty<Material>();
    private bool upperLayersCreated;

    private void Start()
    {
        if (createUpperCloudLayers)
        {
            EnsureUpperCloudLayers();
        }
    }

    private void Update()
    {
        if (ExecutionPauseController.IsPaused)
        {
            return;
        }

        if (degreesPerSecond <= 0f)
        {
            return;
        }

        Vector3 axis = localAxis;
        if (axis.sqrMagnitude <= AxisEpsilon)
        {
            return;
        }

        transform.Rotate(ResolveDriftAxis(axis), degreesPerSecond * Time.deltaTime, Space.Self);
    }

    private void OnDisable()
    {
        CleanupGeneratedUpperLayers();
    }

    private void OnDestroy()
    {
        CleanupOwnedMaterials();
    }

    private Vector3 ResolveDriftAxis(Vector3 axis)
    {
        Vector3 normalizedAxis = axis.normalized;
        if (vectorBreakupStrength <= 0f || vectorBreakupSpeed <= 0f)
        {
            return normalizedAxis;
        }

        Vector3 tangentA = Vector3.Cross(normalizedAxis, Vector3.right);
        if (tangentA.sqrMagnitude <= AxisEpsilon)
        {
            tangentA = Vector3.Cross(normalizedAxis, Vector3.forward);
        }

        tangentA.Normalize();
        Vector3 tangentB = Vector3.Cross(normalizedAxis, tangentA).normalized;
        float time = Time.time * vectorBreakupSpeed + vectorBreakupPhase;
        Vector3 breakup =
            tangentA * Mathf.Sin(time) +
            tangentB * Mathf.Cos(time * 0.73f + vectorBreakupPhase * 0.37f);

        return (normalizedAxis + breakup.normalized * vectorBreakupStrength).normalized;
    }

    private void EnsureUpperCloudLayers()
    {
        if (upperLayersCreated || upperCloudLayerCount <= 0)
        {
            return;
        }

        MeshFilter sourceFilter = GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = GetComponent<MeshRenderer>();
        if (sourceFilter == null || sourceRenderer == null || sourceFilter.sharedMesh == null)
        {
            return;
        }

        Transform layerParent = transform.parent != null ? transform.parent : transform;
        CleanupGeneratedUpperLayers(layerParent);

        for (int i = 0; i < upperCloudLayerCount; i++)
        {
            CreateUpperCloudLayer(layerParent, sourceFilter, sourceRenderer, i);
        }

        upperLayersCreated = true;
    }

    private void CreateUpperCloudLayer(
        Transform layerParent,
        MeshFilter sourceFilter,
        MeshRenderer sourceRenderer,
        int layerIndex)
    {
        GameObject layerObject = new GameObject($"{GeneratedLayerPrefix}{layerIndex + 1:00}");
        layerObject.layer = gameObject.layer;

        Transform layerTransform = layerObject.transform;
        layerTransform.SetParent(layerParent, false);
        layerTransform.localPosition = transform.localPosition;
        layerTransform.localRotation = transform.localRotation * Quaternion.Euler(0f, layerIndex * 17f, layerIndex * 9f);
        layerTransform.localScale = transform.localScale * (1f + upperCloudFirstScaleOffset + upperCloudScaleStep * layerIndex);

        MeshFilter layerFilter = layerObject.AddComponent<MeshFilter>();
        layerFilter.sharedMesh = sourceFilter.sharedMesh;

        MeshRenderer layerRenderer = layerObject.AddComponent<MeshRenderer>();
        CopyRendererSettings(sourceRenderer, layerRenderer);
        layerRenderer.sharedMaterials = CreateUpperLayerMaterials(sourceRenderer.sharedMaterials, layerIndex);

        EarthCloudDrift layerDrift = layerObject.AddComponent<EarthCloudDrift>();
        layerDrift.createUpperCloudLayers = false;
        layerDrift.localAxis = BuildUpperLayerAxis(layerIndex);
        layerDrift.degreesPerSecond = BuildUpperLayerSpeed(layerIndex);
        layerDrift.vectorBreakupStrength = upperCloudVectorBreakup;
        layerDrift.vectorBreakupSpeed = upperCloudVectorBreakupSpeed * (1f + layerIndex * 0.19f);
        layerDrift.vectorBreakupPhase = layerIndex * 1.618f;
        layerDrift.ownedRuntimeMaterials = layerRenderer.sharedMaterials;
    }

    private Material[] CreateUpperLayerMaterials(Material[] sourceMaterials, int layerIndex)
    {
        if (sourceMaterials == null || sourceMaterials.Length == 0)
        {
            return System.Array.Empty<Material>();
        }

        Material[] materials = new Material[sourceMaterials.Length];
        for (int i = 0; i < sourceMaterials.Length; i++)
        {
            Material source = sourceMaterials[i];
            if (source == null)
            {
                continue;
            }

            Material material = new Material(source)
            {
                name = $"{source.name}_UpperCloud_{layerIndex + 1:00}"
            };

            float layerFade = Mathf.Clamp01(1f - layerIndex * 0.18f);
            if (material.HasProperty("_Opacity"))
            {
                material.SetFloat("_Opacity", Mathf.Clamp01(upperCloudOpacity * layerFade));
            }

            if (material.HasProperty("_Coverage"))
            {
                material.SetFloat("_Coverage", Mathf.Clamp01(upperCloudCoverage + layerIndex * 0.035f));
            }

            if (material.HasProperty("_Softness"))
            {
                material.SetFloat("_Softness", 0.14f + layerIndex * 0.018f);
            }

            if (material.HasProperty("_PanSpeed"))
            {
                float direction = layerIndex % 2 == 0 ? 1f : -1f;
                material.SetFloat("_PanSpeed", direction * (0.004f + layerIndex * 0.00125f));
            }

            if (material.HasProperty("_DetailStrength"))
            {
                material.SetFloat("_DetailStrength", 0.20f + layerIndex * 0.025f);
            }

            if (material.HasProperty("_VortexStrength"))
            {
                material.SetFloat("_VortexStrength", 0f);
            }

            if (material.HasProperty("_VortexScale"))
            {
                material.SetFloat("_VortexScale", 1.02f + layerIndex * 0.08f);
            }

            if (material.HasProperty("_CloudColor"))
            {
                material.SetColor("_CloudColor", Color.Lerp(new Color(0.94f, 0.97f, 1f, 1f), Color.white, layerFade));
            }

            material.SetTextureScale(
                "_CloudTex",
                new Vector2(
                    upperCloudTextureScale.x,
                    upperCloudTextureScale.y));
            material.SetTextureOffset("_CloudTex", Vector2.zero);
            materials[i] = material;
        }

        return materials;
    }

    private Vector3 BuildUpperLayerAxis(int layerIndex)
    {
        float direction = layerIndex % 2 == 0 ? 1f : -1f;
        Vector3 axis = localAxis.sqrMagnitude > AxisEpsilon ? localAxis.normalized : Vector3.up;
        Vector3 lateral = new Vector3(0.34f * direction, 0.08f * (layerIndex + 1), 0.22f * (layerIndex % 3 - 1));
        return (axis * direction + lateral).normalized;
    }

    private float BuildUpperLayerSpeed(int layerIndex)
    {
        float direction = layerIndex % 2 == 0 ? 1f : -1f;
        return direction * (upperCloudBaseSpeed + upperCloudSpeedStep * layerIndex);
    }

    private static void CopyRendererSettings(MeshRenderer source, MeshRenderer target)
    {
        target.shadowCastingMode = source.shadowCastingMode;
        target.receiveShadows = source.receiveShadows;
        target.lightProbeUsage = source.lightProbeUsage;
        target.reflectionProbeUsage = source.reflectionProbeUsage;
        target.motionVectorGenerationMode = source.motionVectorGenerationMode;
        target.allowOcclusionWhenDynamic = source.allowOcclusionWhenDynamic;
        target.renderingLayerMask = source.renderingLayerMask;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder;
    }

    private void CleanupGeneratedUpperLayers()
    {
        if (!createUpperCloudLayers)
        {
            CleanupOwnedMaterials();
            return;
        }

        Transform layerParent = transform.parent != null ? transform.parent : transform;
        CleanupGeneratedUpperLayers(layerParent);
        upperLayersCreated = false;
    }

    private static void CleanupGeneratedUpperLayers(Transform layerParent)
    {
        if (layerParent == null)
        {
            return;
        }

        for (int i = layerParent.childCount - 1; i >= 0; i--)
        {
            Transform child = layerParent.GetChild(i);
            if (child != null && child.name.StartsWith(GeneratedLayerPrefix, System.StringComparison.Ordinal))
            {
                DestroyObject(child.gameObject);
            }
        }
    }

    private void CleanupOwnedMaterials()
    {
        for (int i = 0; i < ownedRuntimeMaterials.Length; i++)
        {
            Material material = ownedRuntimeMaterials[i];
            if (material != null)
            {
                DestroyObject(material);
            }
        }

        ownedRuntimeMaterials = System.Array.Empty<Material>();
    }

    private static void DestroyObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
            return;
        }

        DestroyImmediate(target);
    }
}
