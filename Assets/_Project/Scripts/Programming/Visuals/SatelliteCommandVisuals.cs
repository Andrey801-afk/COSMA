using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SatelliteCommandVisuals : MonoBehaviour
{
    private const string BatteryIndicatorSceneName = "Point Light Indicator battery";
    private const float BlinkSpeed = 8f;
    private const float PulseSpeed = 4.5f;

    private static readonly Color BatteryOk = new(0.30f, 1f, 0.42f, 1f);
    private static readonly Color BatteryLow = new(1f, 0.18f, 0.08f, 1f);
    private static readonly Color CameraReady = new(0.28f, 0.95f, 1f, 1f);
    private static readonly Color PhotoReady = new(0.34f, 1f, 0.74f, 1f);
    private static readonly Color GyroReady = new(0.42f, 0.68f, 1f, 1f);
    private static readonly Color Stabilized = new(0.64f, 1f, 0.45f, 1f);
    private static readonly Color LinkReady = new(0.26f, 0.86f, 1f, 1f);
    private static readonly Color DataSent = new(0.86f, 1f, 0.36f, 1f);
    private static readonly Color Dim = new(0.05f, 0.07f, 0.08f, 1f);

    [SerializeField] private SatelliteStateController stateController;
    [SerializeField] private SatelliteController satelliteController;

    private Indicator batteryIndicator;
    private Indicator cameraIndicator;
    private Indicator gyroIndicator;
    private Indicator linkIndicator;
    private SatelliteState currentState;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureInScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInScene();
    }

    private static void EnsureInScene()
    {
        SatelliteController satellite = Object.FindFirstObjectByType<SatelliteController>(FindObjectsInactive.Include);
        if (satellite == null || satellite.GetComponent<SatelliteCommandVisuals>() != null)
        {
            return;
        }

        satellite.gameObject.AddComponent<SatelliteCommandVisuals>();
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureIndicators();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (stateController != null)
        {
            stateController.StateChanged += HandleStateChanged;
            HandleStateChanged(stateController.State, string.Empty);
        }
    }

    private void OnDisable()
    {
        if (stateController != null)
        {
            stateController.StateChanged -= HandleStateChanged;
        }
    }

    private void Update()
    {
        if (stateController == null || satelliteController == null)
        {
            ResolveReferences();
        }

        if (currentState == null && stateController != null)
        {
            currentState = stateController.State;
        }

        UpdateVisuals();
    }

    private void HandleStateChanged(SatelliteState state, string message)
    {
        currentState = state;
        UpdateVisuals();
    }

    private void ResolveReferences()
    {
        if (satelliteController == null)
        {
            satelliteController = GetComponent<SatelliteController>();
        }

        if (stateController == null)
        {
            stateController = Object.FindFirstObjectByType<SatelliteStateController>(FindObjectsInactive.Include);
        }
    }

    private void EnsureIndicators()
    {
        batteryIndicator ??= FindExistingIndicator(BatteryIndicatorSceneName, BatteryOk, 1.6f)
            ?? CreateIndicator("Battery_Indicator", new Vector3(-0.42f, 0.28f, 0.22f), BatteryOk, 1.6f);
        cameraIndicator ??= CreateIndicator("Camera_Indicator", new Vector3(0f, -0.34f, 0.52f), CameraReady, 1.4f);
        gyroIndicator ??= CreateIndicator("Gyro_Indicator", new Vector3(0.42f, 0.28f, 0.22f), GyroReady, 1.4f);
        linkIndicator ??= CreateIndicator("Link_Indicator", new Vector3(0f, 0.45f, -0.44f), LinkReady, 1.9f);
    }

    private Indicator FindExistingIndicator(string objectName, Color color, float fallbackRange)
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Light fallback = null;
        for (int i = 0; i < lights.Length; i++)
        {
            Light candidate = lights[i];
            if (candidate == null)
            {
                continue;
            }

            bool nameMatches = candidate.name == objectName || candidate.name.StartsWith(objectName + " (");
            if (!nameMatches)
            {
                continue;
            }

            if (candidate.transform.IsChildOf(transform))
            {
                return ConfigureExistingIndicator(candidate, color, fallbackRange);
            }

            fallback ??= candidate;
        }

        return fallback != null ? ConfigureExistingIndicator(fallback, color, fallbackRange) : null;
    }

    private static Indicator ConfigureExistingIndicator(Light light, Color color, float fallbackRange)
    {
        if (light == null)
        {
            return null;
        }

        light.gameObject.SetActive(true);
        light.type = LightType.Point;
        light.shadows = LightShadows.None;
        light.color = color;
        float range = light.range > 0f ? light.range : fallbackRange;
        light.range = range;

        Renderer renderer = light.GetComponentInChildren<Renderer>(true);
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        return new Indicator(light, renderer, range);
    }

    private Indicator CreateIndicator(string name, Vector3 localPosition, Color color, float range)
    {
        GameObject root = new(name);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.identity;

        Light pointLight = root.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = color;
        pointLight.range = range;
        pointLight.intensity = 0f;
        pointLight.shadows = LightShadows.None;

        return new Indicator(pointLight, null, range);
    }

    private void UpdateVisuals()
    {
        EnsureIndicators();
        SatelliteState state = currentState;
        if (state == null)
        {
            SetIndicator(batteryIndicator, Dim, 0f);
            SetIndicator(cameraIndicator, Dim, 0f);
            SetIndicator(gyroIndicator, Dim, 0f);
            SetIndicator(linkIndicator, Dim, 0f);
            return;
        }

        UpdateBatteryIndicator(state);
        UpdateCameraIndicator(state);
        UpdateGyroIndicator(state);
        UpdateLinkIndicator(state);
    }

    private void UpdateBatteryIndicator(SatelliteState state)
    {
        if (state.batteryCharge <= SatelliteState.BatteryLowThreshold)
        {
            float blink = Mathf.PingPong(Time.time * BlinkSpeed, 1f);
            SetIndicator(batteryIndicator, BatteryLow, Mathf.Lerp(0.15f, 2.8f, blink));
            return;
        }

        SetIndicator(batteryIndicator, state.powerOn ? BatteryOk : Dim, state.powerOn ? 0.85f : 0.08f);
    }

    private void UpdateCameraIndicator(SatelliteState state)
    {
        if (state.photoTaken)
        {
            SetIndicator(cameraIndicator, PhotoReady, 0.9f + Mathf.PingPong(Time.time * 2.5f, 0.5f));
            return;
        }

        SetIndicator(cameraIndicator, state.cameraCoverOpen ? CameraReady : Dim, state.cameraCoverOpen ? 1.05f : 0.03f);
    }

    private void UpdateGyroIndicator(SatelliteState state)
    {
        if (state.isStabilized)
        {
            SetIndicator(gyroIndicator, Stabilized, 1.1f);
            return;
        }

        SetIndicator(gyroIndicator, state.gyrosCalibrated ? GyroReady : Dim, state.gyrosCalibrated ? 0.85f : 0.03f);
    }

    private void UpdateLinkIndicator(SatelliteState state)
    {
        if (state.dataSent)
        {
            float pulse = Mathf.PingPong(Time.time * PulseSpeed, 1f);
            SetIndicator(linkIndicator, DataSent, Mathf.Lerp(0.7f, 2.2f, pulse));
            return;
        }

        if (state.communicationLinkAvailable)
        {
            float pulse = Mathf.PingPong(Time.time * PulseSpeed, 1f);
            SetIndicator(linkIndicator, LinkReady, Mathf.Lerp(0.45f, 1.8f, pulse));
            return;
        }

        SetIndicator(linkIndicator, state.antennaFacingEarth ? LinkReady : Dim, state.antennaFacingEarth ? 0.35f : 0.03f);
    }

    private static void SetIndicator(Indicator indicator, Color color, float intensity)
    {
        if (indicator == null)
        {
            return;
        }

        if (indicator.Light != null)
        {
            indicator.Light.color = color;
            indicator.Light.intensity = intensity;
            indicator.Light.range = indicator.Range;
        }

        if (indicator.Renderer != null)
        {
            ApplyMaterialColor(indicator.Renderer.material, Color.Lerp(Dim, color, Mathf.Clamp01(intensity)));
        }
    }

    private static Material CreateIndicatorMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new(shader);
        ApplyMaterialColor(material, color);
        return material;
    }

    private static void ApplyMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.6f);
        }
    }

    private sealed class Indicator
    {
        public readonly Light Light;
        public readonly Renderer Renderer;
        public readonly float Range;

        public Indicator(Light light, Renderer renderer, float range)
        {
            Light = light;
            Renderer = renderer;
            Range = range;
        }
    }
}
