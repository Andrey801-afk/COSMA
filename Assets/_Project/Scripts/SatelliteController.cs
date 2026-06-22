using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SatelliteController : MonoBehaviour
{
    private const float DirectionEpsilon = 0.0001f;
    private const float AxisEpsilon = 0.0001f;
    private static readonly Color PhotoSpaceBackgroundColor = new Color(0.003f, 0.005f, 0.014f, 1f);
    private static readonly WaitForFixedUpdate WaitForFixedStep = new WaitForFixedUpdate();

    [Header("Orbit Settings")]
    [FormerlySerializedAs("_orbitCenter")]
    [SerializeField] private Transform orbitCenter;
    [FormerlySerializedAs("_orbitSpeed")]
    [SerializeField] private float orbitSpeed = 5f;
    [FormerlySerializedAs("_orbitRadius")]
    [SerializeField] private float orbitRadius = 50f;
    [FormerlySerializedAs("_orbitInclination")]
    [SerializeField] private float orbitInclination = 25f;

    [Header("Rotation Settings")]
    [FormerlySerializedAs("_rotationForce")]
    [SerializeField] private float rotationForce = 1f;
    [SerializeField] private Transform earthTarget;
    [FormerlySerializedAs("_sunTarget")]
    [SerializeField] private Transform sunTarget;
    [FormerlySerializedAs("_antennaTransform")]
    [SerializeField] private Transform antennaTransform;
    [SerializeField] private Transform transmissionRayOrigin;
    [SerializeField] private Transform[] solarPanelTransforms;
    [FormerlySerializedAs("_sunSensor")]
    [SerializeField] private SunSensor sunSensor;
    [SerializeField, Range(0f, 1f)] private float sunDetectionThreshold = 0.02f;
    [SerializeField, Min(1f)] private float defaultRotationSpeedDegreesPerSecond = 30f;
    [SerializeField, Min(0.01f)] private float rotationCompletionAngleDegrees = 0.25f;
    [SerializeField] private Vector3 earthAimLocalAxis = Vector3.up;
    [SerializeField] private Vector3 earthAntennaAimLocalAxis = new Vector3(0f, -1f, 0f);
    [SerializeField] private Vector3 sunAimLocalAxis = Vector3.forward;
    [SerializeField] private Vector3 antennaAimLocalAxis = Vector3.back;

    [Header("Transmission")]
    [FormerlySerializedAs("transmitToEarthLocalAxis")]
    [SerializeField] private Vector3 transmissionFallbackLocalAxis = Vector3.forward;
    [SerializeField, Min(0.1f)] private float defaultMessageTransmissionDurationSeconds = 1f;
    [SerializeField, Min(0.002f)] private float transmissionBeamWidth = 0.018f;
    [SerializeField, Min(1f)] private float transmissionBeamLength = 140f;
    [SerializeField, Min(0f)] private float transmissionBeamSourceOffset = 0.75f;
    [SerializeField, Min(0f)] private float transmissionBeamPulseAmplitude = 0.05f;
    [SerializeField, Min(0.1f)] private float transmissionBeamPulseSpeed = 6f;
    [SerializeField] private Color transmissionBeamStartColor = new Color(0.75f, 1f, 0.78f, 0.95f);
    [SerializeField] private Color transmissionBeamEndColor = new Color(0.68f, 1f, 0.74f, 0.08f);

    [Header("Planet Destruction")]
    [SerializeField, Min(1f)] private float planetDestructionTurnSpeedDegreesPerSecond = 55f;
    [FormerlySerializedAs("planetDestructionWarmupSeconds")]
    [SerializeField, Min(0.1f)] private float planetDestructionBeamSeconds = 2f;
    [SerializeField, Min(0.1f)] private float planetDestructionExplosionSeconds = 2.25f;
    [SerializeField, Min(0.01f)] private float planetDestructionBeamWidth = 2.2f;
    [SerializeField, Min(1f)] private float planetDestructionShockwaveScale = 460f;
    [SerializeField] private Color planetDestructionBeamStartColor = new Color(0.45f, 1f, 0.55f, 1f);
    [SerializeField] private Color planetDestructionBeamEndColor = new Color(0.05f, 1f, 0.35f, 0.72f);
    [SerializeField] private Color planetDestructionFlashColor = new Color(1f, 0.38f, 0.04f, 0.98f);
    [SerializeField] private Color planetDestructionShockwaveColor = new Color(1f, 0.9f, 0.18f, 0.58f);
    [SerializeField] private Color planetDestructionDebrisColor = new Color(1f, 0.22f, 0.02f, 0.88f);

    [Header("Photo Capture")]
    [SerializeField] private Camera satelliteCamera;
    [SerializeField] private SatellitePhotoPreviewController photoPreviewController;
    [SerializeField, Min(128)] private int photoResolution = 1024;
    [SerializeField, Min(0.1f)] private float photoCameraForwardOffset = 3f;
    [SerializeField, Range(10f, 100f)] private float photoCameraFieldOfView = 35f;
    [SerializeField, Min(0.01f)] private float photoCameraNearClipPlane = 0.1f;
    [SerializeField, Min(10f)] private float photoCameraFarClipPlane = 5000f;

    [Header("Camera Cover Visual")]
    [SerializeField] private Transform cameraCoverTransform;
    [SerializeField] private string cameraCoverObjectName = "SphereOpenCloseCamera";
    [SerializeField, Min(0.01f)] private float cameraCoverMoveDurationSeconds = 0.45f;
    [SerializeField] private Vector3 cameraCoverOpenLocalPosition = Vector3.zero;

    private Rigidbody attachedRigidbody;
    private Vector3 torqueInput = Vector3.zero;
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private Quaternion defaultAntennaLocalRotation = Quaternion.identity;
    private Quaternion[] defaultSolarPanelLocalRotations = System.Array.Empty<Quaternion>();
    private Vector3 defaultCameraCoverLocalPosition;
    private float orbitAngle;
    private bool defaultPoseCached;
    private bool cameraCoverDefaultPositionCached;
    private RenderTexture photoRenderTexture;
    private LineRenderer transmissionBeam;
    private Material transmissionBeamMaterial;
    private bool transmissionRayOriginResolvedAutomatically;
    private LineRenderer planetDestructionBeam;
    private Material planetDestructionBeamMaterial;
    private GameObject planetDestructionEffect;
    private Material planetDestructionFlashMaterial;
    private Material planetDestructionShockwaveMaterial;
    private Material planetDestructionDebrisMaterial;
    private Transform cachedPlanetVisualRoot;
    private Renderer[] cachedPlanetRenderers = System.Array.Empty<Renderer>();
    private bool[] cachedPlanetRendererEnabled = System.Array.Empty<bool>();
    private Coroutine cameraCoverAnimation;
    private Coroutine attitudeHoldRoutine;

    public float OrbitSpeed => orbitSpeed;
    public float OrbitRadius => orbitRadius;
    public float OrbitInclination => orbitInclination;
    public Transform OrbitCenter => orbitCenter;
    public Transform EarthTarget => earthTarget;
    public Transform SunTarget => sunTarget;

    protected virtual void Awake()
    {
        attachedRigidbody = GetComponent<Rigidbody>();
        ResolveEarthTarget();
        ResolveSunTarget();
        ResolveAntennaTransform();
        ResolveTransmissionRayOrigin();
        ResolveSolarPanelTransforms();
        ResolvePhotoCamera();
        ResolveCameraCoverTransform();
        DetachFromOrbitCenterParent();
        CacheDefaultPose();
    }

    protected virtual void Start()
    {
        ResolveEarthTarget();
        ResolveSunTarget();
        ResolveAntennaTransform();
        ResolveTransmissionRayOrigin();
        ResolveSolarPanelTransforms();
        ResolvePhotoCamera();
        ResolveCameraCoverTransform();
        DetachFromOrbitCenterParent();
        CacheDefaultPose();
        if (!IsOrbitDrivenExternally())
        {
            SyncOrbitFromCurrentPosition();
        }

        // The first play session should start from the same clean pose as a manual reset.
        ResetSatellitePose();
    }

    protected virtual void FixedUpdate()
    {
        if (ExecutionPauseController.IsPaused)
        {
            return;
        }

        if (!IsOrbitDrivenExternally())
        {
            AdvanceOrbit(Time.fixedDeltaTime, attachedRigidbody != null);
        }

        if (attachedRigidbody == null || torqueInput == Vector3.zero)
        {
            return;
        }

        if (attachedRigidbody.isKinematic)
        {
            Quaternion rotationStep = Quaternion.Euler(torqueInput * rotationForce * Time.fixedDeltaTime);
            ApplyRotation(transform, transform.rotation * rotationStep);
        }
        else
        {
            attachedRigidbody.AddRelativeTorque(torqueInput * rotationForce);
        }

        torqueInput = Vector3.zero;
    }

    public void RotateRight() => SetTorqueInput(Vector3.up);
    public void RotateLeft() => SetTorqueInput(Vector3.down);
    public void RotateUp() => SetTorqueInput(Vector3.right);
    public void RotateDown() => SetTorqueInput(Vector3.left);

    public bool TryReadSunSensors(out bool sunDetected)
    {
        Vector3 directionToSun = GetSunDirection();
        if (directionToSun.sqrMagnitude <= DirectionEpsilon)
        {
            sunDetected = false;
            return false;
        }

        bool sensorHasSignal = sunSensor != null && sunSensor.GetValue() >= sunDetectionThreshold;
        sunDetected = sensorHasSignal || directionToSun.sqrMagnitude > DirectionEpsilon;
        return true;
    }

    public bool TryReadMagnetometer(out bool earthDetected)
    {
        earthDetected = GetEarthDirection(transform).sqrMagnitude > DirectionEpsilon;
        return true;
    }

    public IEnumerator RotateToEarth(float rotationSpeedDegreesPerSecond = 30f)
    {
        yield return RotateToEarth(EarthFacingSide.Camera, rotationSpeedDegreesPerSecond);
    }

    public IEnumerator RotateToEarth(float rotationSpeedDegreesPerSecond, float trackingDurationSeconds)
    {
        yield return RotateToEarth(EarthFacingSide.Camera, rotationSpeedDegreesPerSecond, trackingDurationSeconds);
    }

    public IEnumerator RotateToEarth(
        EarthFacingSide facingSide,
        float rotationSpeedDegreesPerSecond = 30f,
        float trackingDurationSeconds = 0f)
    {
        yield return RotateTransformTowardDirection(
            transform,
            () => GetEarthDirection(transform),
            ResolveEarthFacingAxis(facingSide),
            rotationSpeedDegreesPerSecond,
            trackingDurationSeconds);
    }

    public IEnumerator RotateToSun(float rotationSpeedDegreesPerSecond = 30f, float trackingDurationSeconds = 0f)
    {
        if (trackingDurationSeconds > 0f)
        {
            yield return TrackSunForDuration(rotationSpeedDegreesPerSecond, trackingDurationSeconds);
            yield break;
        }

        yield return RotateTransformTowardDirection(
            transform,
            GetSunDirection,
            NormalizeAxis(sunAimLocalAxis, Vector3.forward),
            rotationSpeedDegreesPerSecond);
        yield return RotateSolarPanelsToSun(rotationSpeedDegreesPerSecond);
    }

    public IEnumerator RotateAntennaToEarth(float rotationSpeedDegreesPerSecond = 30f)
    {
        Transform antenna = ResolveAntennaTransform();
        if (antenna == null)
        {
            yield break;
        }

        yield return RotateTransformTowardDirection(
            antenna,
            () => GetEarthDirection(antenna),
            NormalizeAxis(antennaAimLocalAxis, Vector3.back),
            rotationSpeedDegreesPerSecond);
    }

    public IEnumerator SendMessage(float transmissionDurationSeconds = 1f)
    {
        yield return TransmitMessage(transmissionDurationSeconds);
    }

    public IEnumerator DestroyPlanet()
    {
        Transform planetRoot = ResolvePlanetVisualRoot();
        if (planetRoot == null)
        {
            yield break;
        }

        RestorePlanetVisual();
        CachePlanetRenderers(planetRoot);

        yield return RotateTransformTowardDirection(
            transform,
            () => GetEarthDirection(transform),
            ResolveEarthFacingAxis(EarthFacingSide.Camera),
            planetDestructionTurnSpeedDegreesPerSecond);

        LineRenderer beam = ResolvePlanetDestructionBeam();
        float beamSeconds = Mathf.Max(0.1f, planetDestructionBeamSeconds);
        float elapsed = 0f;
        while (elapsed < beamSeconds)
        {
            yield return ExecutionPauseController.WaitWhilePaused();

            UpdatePlanetDestructionBeam(beam, planetRoot, elapsed, beamSeconds);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        HidePlanetDestructionBeam();
        yield return RunPlanetExplosionVisual(planetRoot);
    }

    public void BeginRotateToEarthHold(
        EarthFacingSide facingSide,
        float rotationSpeedDegreesPerSecond,
        float trackingDurationSeconds)
    {
        StartAttitudeHold(RotateToEarth(facingSide, rotationSpeedDegreesPerSecond, trackingDurationSeconds));
    }

    public void BeginRotateToSunHold(float rotationSpeedDegreesPerSecond, float trackingDurationSeconds)
    {
        StartAttitudeHold(RotateToSun(rotationSpeedDegreesPerSecond, trackingDurationSeconds));
    }

    public void BeginStabilizeHold(float durationSeconds, float rotationSpeedDegreesPerSecond)
    {
        StartAttitudeHold(StabilizeSatelliteForSeconds(durationSeconds, rotationSpeedDegreesPerSecond));
    }

    public bool CanRotateToEarth()
    {
        return GetEarthDirection(transform).sqrMagnitude > DirectionEpsilon;
    }

    public bool CanRotateToSun()
    {
        return GetSunDirection().sqrMagnitude > DirectionEpsilon;
    }

    public bool CanRotateAntennaToEarth()
    {
        Transform antenna = ResolveAntennaTransform();
        return antenna != null && GetEarthDirection(antenna).sqrMagnitude > DirectionEpsilon;
    }

    public bool CanSendMessage()
    {
        return GetTransmissionDirection().sqrMagnitude > DirectionEpsilon;
    }

    public bool CanDestroyPlanet()
    {
        return ResolvePlanetVisualRoot() != null;
    }

    public void StabilizeSatellite()
    {
        StopActiveMotion();
        torqueInput = Vector3.zero;

        if (attachedRigidbody != null && !attachedRigidbody.isKinematic)
        {
            attachedRigidbody.linearVelocity = Vector3.zero;
            attachedRigidbody.angularVelocity = Vector3.zero;
            attachedRigidbody.WakeUp();
        }

        Physics.SyncTransforms();
    }

    public IEnumerator StabilizeSatelliteForSeconds(float durationSeconds, float rotationSpeedDegreesPerSecond = 30f)
    {
        float elapsed = 0f;
        durationSeconds = Mathf.Max(0f, durationSeconds);
        float resolvedRotationSpeed = ResolveRotationSpeed(rotationSpeedDegreesPerSecond);

        if (!TryBuildEarthRelativeFrame(out Quaternion startFrame))
        {
            yield break;
        }

        Quaternion earthRelativeRotation = Quaternion.Inverse(startFrame) * transform.rotation;

        while (elapsed < durationSeconds)
        {
            yield return WaitForCommandPhysicsStep();
            float stepDeltaTime = GetCommandFixedDeltaTime();

            StabilizeSatellite();
            if (TryBuildEarthRelativeFrame(out Quaternion currentFrame))
            {
                Quaternion desiredRotation = currentFrame * earthRelativeRotation;
                ApplyRotation(
                    transform,
                    Quaternion.RotateTowards(
                        transform.rotation,
                        desiredRotation,
                        resolvedRotationSpeed * stepDeltaTime));
            }

            elapsed += stepDeltaTime;
        }

        StabilizeSatellite();
    }

    public bool IsEarthInPhotoFrame()
    {
        Camera resolvedPhotoCamera = ResolvePhotoCamera();
        Transform resolvedEarthTarget = ResolveEarthTarget();
        if (resolvedPhotoCamera == null || resolvedEarthTarget == null)
        {
            return false;
        }

        SyncPhotoCameraPose(resolvedPhotoCamera);
        ConfigurePhotoCamera(resolvedPhotoCamera);

        return IsEarthVisibleInPhotoFrame(resolvedPhotoCamera, resolvedEarthTarget);
    }

    public bool CaptureEarthPhoto(out RenderTexture capturedTexture, out string message)
    {
        return CaptureEarthPhoto(true, out capturedTexture, out message, out _);
    }

    public bool CaptureEarthPhoto(
        bool cameraCoverOpen,
        out RenderTexture capturedTexture,
        out string message,
        out bool earthInFrame)
    {
        capturedTexture = null;
        earthInFrame = false;

        EnsurePhotoRenderTexture();
        if (photoRenderTexture == null)
        {
            message = "RenderTexture для фотографии недоступен.";
            return false;
        }

        if (!cameraCoverOpen)
        {
            ClearPhotoRenderTexture(Color.black);
            capturedTexture = photoRenderTexture;
            message = $"Снимок сделан: крышка камеры закрыта, кадр черный ({photoRenderTexture.width}x{photoRenderTexture.height}).";
            return true;
        }

        Camera resolvedPhotoCamera = ResolvePhotoCamera();
        if (resolvedPhotoCamera == null)
        {
            message = "Камера спутника недоступна.";
            return false;
        }

        SyncPhotoCameraPose(resolvedPhotoCamera);
        ConfigurePhotoCamera(resolvedPhotoCamera);
        earthInFrame = IsEarthVisibleInPhotoFrame(resolvedPhotoCamera, ResolveEarthTarget());

        RenderTexture previousActiveRenderTexture = RenderTexture.active;
        RenderTexture previousCameraTargetTexture = resolvedPhotoCamera.targetTexture;
        resolvedPhotoCamera.targetTexture = photoRenderTexture;

        try
        {
            resolvedPhotoCamera.Render();
        }
        finally
        {
            resolvedPhotoCamera.targetTexture = previousCameraTargetTexture;
            RenderTexture.active = previousActiveRenderTexture;
        }

        capturedTexture = photoRenderTexture;
        message = earthInFrame
            ? $"Снимок сделан: в кадре видна Земля ({photoRenderTexture.width}x{photoRenderTexture.height})."
            : $"Снимок сделан: камера смотрит в звездное небо ({photoRenderTexture.width}x{photoRenderTexture.height}).";
        return true;
    }

    public IEnumerator ShowEarthPhotoPreview(RenderTexture capturedTexture, float durationSeconds)
    {
        SatellitePhotoPreviewController resolvedPreviewController = ResolvePhotoPreviewController();
        if (resolvedPreviewController == null || capturedTexture == null)
        {
            yield break;
        }

        yield return resolvedPreviewController.ShowPreview(capturedTexture, durationSeconds);
    }

    public void ClearEarthPhotoPreview()
    {
        SatellitePhotoPreviewController existingPreviewController = FindExistingPhotoPreviewController();
        if (existingPreviewController != null)
        {
            existingPreviewController.HidePreview();
        }
    }

    private IEnumerator RunPlanetExplosionVisual(Transform planetRoot)
    {
        if (planetRoot == null)
        {
            yield break;
        }

        CleanupPlanetDestructionEffect();

        planetDestructionEffect = new GameObject("PlanetDestructionExplosion");
        planetDestructionEffect.transform.position = ResolvePlanetCenter(planetRoot);

        GameObject coreFlash = CreateExplosionSphere("PlanetDestructionCore", planetDestructionEffect.transform);
        GameObject shockwave = CreateExplosionSphere("PlanetDestructionShockwave", planetDestructionEffect.transform);
        LineRenderer[] debrisRays = CreateExplosionDebrisRays(planetDestructionEffect.transform, 18);
        Vector3[] debrisDirections = CreateExplosionDirections(debrisRays.Length);

        Renderer coreRenderer = coreFlash != null ? coreFlash.GetComponent<Renderer>() : null;
        planetDestructionFlashMaterial = CreateVisualMaterial("PlanetDestructionFlashMaterial", planetDestructionFlashColor);
        if (coreRenderer != null)
        {
            coreRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coreRenderer.receiveShadows = false;
            if (planetDestructionFlashMaterial != null)
            {
                coreRenderer.sharedMaterial = planetDestructionFlashMaterial;
            }
        }

        Renderer shockwaveRenderer = shockwave != null ? shockwave.GetComponent<Renderer>() : null;
        planetDestructionShockwaveMaterial = CreateVisualMaterial("PlanetDestructionShockwaveMaterial", planetDestructionShockwaveColor);
        if (shockwaveRenderer != null)
        {
            shockwaveRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shockwaveRenderer.receiveShadows = false;
            if (planetDestructionShockwaveMaterial != null)
            {
                shockwaveRenderer.sharedMaterial = planetDestructionShockwaveMaterial;
            }
        }

        float planetDiameter = ResolvePlanetDiameter(planetRoot);
        float startScale = Mathf.Max(8f, planetDiameter * 0.25f);
        float endScale = Mathf.Max(planetDestructionShockwaveScale, planetDiameter * 1.75f);
        float duration = Mathf.Max(0.1f, planetDestructionExplosionSeconds);
        float elapsed = 0f;
        bool planetHidden = false;

        while (elapsed < duration)
        {
            yield return ExecutionPauseController.WaitWhilePaused();

            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 center = ResolvePlanetCenter(planetRoot);
            planetDestructionEffect.transform.position = center;

            if (coreFlash != null)
            {
                float coreScale = Mathf.Lerp(startScale, endScale * 0.72f, eased);
                coreFlash.transform.localScale = Vector3.one * coreScale;
            }

            if (shockwave != null)
            {
                float shockScale = Mathf.Lerp(planetDiameter * 0.55f, endScale, eased);
                shockwave.transform.localScale = Vector3.one * shockScale;
            }

            Color flashColor = planetDestructionFlashColor;
            flashColor.a = Mathf.Lerp(planetDestructionFlashColor.a, 0f, Mathf.Clamp01(t * 1.25f));
            ApplyMaterialColor(planetDestructionFlashMaterial, flashColor);

            Color shockwaveColor = planetDestructionShockwaveColor;
            shockwaveColor.a = Mathf.Lerp(planetDestructionShockwaveColor.a, 0f, Mathf.Clamp01(t * 0.95f));
            ApplyMaterialColor(planetDestructionShockwaveMaterial, shockwaveColor);

            UpdateExplosionDebrisRays(debrisRays, debrisDirections, center, planetDiameter, endScale, t);

            if (!planetHidden && t >= 0.18f)
            {
                SetCachedPlanetRenderersEnabled(false);
                planetHidden = true;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SetCachedPlanetRenderersEnabled(false);
        CleanupPlanetDestructionEffect();
    }

    private void RestorePlanetVisual()
    {
        HidePlanetDestructionBeam();
        CleanupPlanetDestructionEffect();

        int count = Mathf.Min(cachedPlanetRenderers.Length, cachedPlanetRendererEnabled.Length);
        for (int i = 0; i < count; i++)
        {
            Renderer renderer = cachedPlanetRenderers[i];
            if (renderer != null)
            {
                renderer.enabled = cachedPlanetRendererEnabled[i];
            }
        }

        cachedPlanetVisualRoot = null;
        cachedPlanetRenderers = System.Array.Empty<Renderer>();
        cachedPlanetRendererEnabled = System.Array.Empty<bool>();
    }

    private void CachePlanetRenderers(Transform planetRoot)
    {
        if (planetRoot == null)
        {
            cachedPlanetVisualRoot = null;
            cachedPlanetRenderers = System.Array.Empty<Renderer>();
            cachedPlanetRendererEnabled = System.Array.Empty<bool>();
            return;
        }

        if (cachedPlanetVisualRoot == planetRoot && cachedPlanetRenderers.Length > 0)
        {
            return;
        }

        cachedPlanetVisualRoot = planetRoot;
        cachedPlanetRenderers = planetRoot.GetComponentsInChildren<Renderer>(true);
        cachedPlanetRendererEnabled = new bool[cachedPlanetRenderers.Length];
        for (int i = 0; i < cachedPlanetRenderers.Length; i++)
        {
            cachedPlanetRendererEnabled[i] = cachedPlanetRenderers[i] != null && cachedPlanetRenderers[i].enabled;
        }
    }

    private void SetCachedPlanetRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < cachedPlanetRenderers.Length; i++)
        {
            Renderer renderer = cachedPlanetRenderers[i];
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }
    }

    private void CleanupPlanetDestructionEffect()
    {
        if (planetDestructionEffect != null)
        {
            Destroy(planetDestructionEffect);
            planetDestructionEffect = null;
        }

        if (planetDestructionFlashMaterial != null)
        {
            Destroy(planetDestructionFlashMaterial);
            planetDestructionFlashMaterial = null;
        }

        if (planetDestructionShockwaveMaterial != null)
        {
            Destroy(planetDestructionShockwaveMaterial);
            planetDestructionShockwaveMaterial = null;
        }

        if (planetDestructionDebrisMaterial != null)
        {
            Destroy(planetDestructionDebrisMaterial);
            planetDestructionDebrisMaterial = null;
        }
    }

    private static GameObject CreateExplosionSphere(string objectName, Transform parent)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = objectName;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localRotation = Quaternion.identity;
        sphere.transform.localScale = Vector3.one;

        if (sphere.TryGetComponent(out Collider collider))
        {
            Destroy(collider);
        }

        return sphere;
    }

    private LineRenderer[] CreateExplosionDebrisRays(Transform parent, int rayCount)
    {
        rayCount = Mathf.Max(0, rayCount);
        LineRenderer[] rays = new LineRenderer[rayCount];
        if (rayCount == 0)
        {
            return rays;
        }

        if (planetDestructionDebrisMaterial == null)
        {
            planetDestructionDebrisMaterial = CreateVisualMaterial("PlanetDestructionDebrisMaterial", planetDestructionDebrisColor);
        }

        for (int i = 0; i < rayCount; i++)
        {
            GameObject rayObject = new GameObject($"PlanetDestructionDebris_{i:00}");
            rayObject.transform.SetParent(parent, false);
            LineRenderer ray = rayObject.AddComponent<LineRenderer>();
            ray.enabled = true;
            ray.positionCount = 2;
            ray.useWorldSpace = true;
            ray.alignment = LineAlignment.View;
            ray.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ray.receiveShadows = false;
            ray.textureMode = LineTextureMode.Stretch;
            ray.numCapVertices = 4;
            ray.numCornerVertices = 2;
            ray.startWidth = 2.4f;
            ray.endWidth = 0.25f;
            ray.startColor = planetDestructionDebrisColor;
            ray.endColor = new Color(planetDestructionDebrisColor.r, planetDestructionDebrisColor.g, planetDestructionDebrisColor.b, 0f);

            if (planetDestructionDebrisMaterial != null)
            {
                ray.sharedMaterial = planetDestructionDebrisMaterial;
            }

            rays[i] = ray;
        }

        return rays;
    }

    private static Vector3[] CreateExplosionDirections(int rayCount)
    {
        Vector3[] directions = new Vector3[Mathf.Max(0, rayCount)];
        if (directions.Length == 0)
        {
            return directions;
        }

        const float goldenAngle = 137.50776f * Mathf.Deg2Rad;
        for (int i = 0; i < directions.Length; i++)
        {
            float fraction = directions.Length <= 1 ? 0.5f : i / (float)(directions.Length - 1);
            float y = 1f - fraction * 2f;
            float radius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float angle = i * goldenAngle;
            directions[i] = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius).normalized;
        }

        return directions;
    }

    private void UpdateExplosionDebrisRays(
        LineRenderer[] rays,
        Vector3[] directions,
        Vector3 center,
        float planetDiameter,
        float endScale,
        float normalizedTime)
    {
        if (rays == null || directions == null)
        {
            return;
        }

        float t = Mathf.Clamp01(normalizedTime);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        float alpha = Mathf.Lerp(planetDestructionDebrisColor.a, 0f, Mathf.Clamp01(t * 1.15f));
        Color startColor = planetDestructionDebrisColor;
        startColor.a = alpha;
        Color endColor = planetDestructionDebrisColor;
        endColor.a = 0f;

        int count = Mathf.Min(rays.Length, directions.Length);
        for (int i = 0; i < count; i++)
        {
            LineRenderer ray = rays[i];
            if (ray == null)
            {
                continue;
            }

            Vector3 direction = directions[i].sqrMagnitude > DirectionEpsilon ? directions[i].normalized : Vector3.up;
            float innerDistance = Mathf.Lerp(planetDiameter * 0.25f, endScale * 0.18f, eased);
            float outerDistance = Mathf.Lerp(planetDiameter * 0.55f, endScale * 0.82f, eased);
            ray.startWidth = Mathf.Lerp(2.8f, 0.35f, t);
            ray.endWidth = Mathf.Lerp(0.65f, 0.04f, t);
            ray.startColor = startColor;
            ray.endColor = endColor;
            ray.SetPosition(0, center + direction * innerDistance);
            ray.SetPosition(1, center + direction * outerDistance);
        }
    }

    private LineRenderer ResolvePlanetDestructionBeam()
    {
        if (planetDestructionBeam == null)
        {
            Transform existingBeamTransform = transform.Find("PlanetDestructionBeam");
            if (existingBeamTransform != null)
            {
                planetDestructionBeam = existingBeamTransform.GetComponent<LineRenderer>();
            }
        }

        if (planetDestructionBeam == null)
        {
            GameObject beamObject = new GameObject("PlanetDestructionBeam");
            beamObject.transform.SetParent(transform, false);
            planetDestructionBeam = beamObject.AddComponent<LineRenderer>();
        }

        ConfigurePlanetDestructionBeam(planetDestructionBeam);
        return planetDestructionBeam;
    }

    private void ConfigurePlanetDestructionBeam(LineRenderer beam)
    {
        if (beam == null)
        {
            return;
        }

        if (planetDestructionBeamMaterial == null)
        {
            planetDestructionBeamMaterial = CreateVisualMaterial("PlanetDestructionBeamMaterial", planetDestructionBeamStartColor, true);
        }

        beam.enabled = false;
        beam.positionCount = 2;
        beam.useWorldSpace = true;
        beam.alignment = LineAlignment.View;
        beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beam.receiveShadows = false;
        beam.textureMode = LineTextureMode.Stretch;
        beam.numCapVertices = 8;
        beam.numCornerVertices = 4;
        beam.startColor = planetDestructionBeamStartColor;
        beam.endColor = planetDestructionBeamEndColor;
        beam.startWidth = planetDestructionBeamWidth;
        beam.endWidth = planetDestructionBeamWidth * 0.35f;
        beam.widthMultiplier = 1f;

        if (planetDestructionBeamMaterial != null)
        {
            beam.sharedMaterial = planetDestructionBeamMaterial;
        }
    }

    private void UpdatePlanetDestructionBeam(LineRenderer beam, Transform planetRoot, float elapsedTime, float durationSeconds)
    {
        if (beam == null || planetRoot == null)
        {
            return;
        }

        Vector3 targetPosition = ResolvePlanetCenter(planetRoot);
        Vector3 directionToPlanet = targetPosition - transform.position;
        if (directionToPlanet.sqrMagnitude <= DirectionEpsilon)
        {
            return;
        }

        Vector3 normalizedDirection = directionToPlanet.normalized;
        Vector3 sourcePosition = GetTransmissionOriginWorldPosition(normalizedDirection);
        float normalizedTime = Mathf.Clamp01(elapsedTime / Mathf.Max(0.1f, durationSeconds));
        float charge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedTime * 2.5f));
        float pulse = 1f + Mathf.Sin(elapsedTime * 18f) * 0.22f;
        float width = Mathf.Max(0.05f, planetDestructionBeamWidth * Mathf.Lerp(0.72f, 1.2f, charge));

        beam.enabled = true;
        beam.startWidth = width;
        beam.endWidth = width * 0.65f;
        beam.widthMultiplier = Mathf.Max(0.25f, pulse);
        beam.SetPosition(0, sourcePosition);
        beam.SetPosition(1, targetPosition);
    }

    private void HidePlanetDestructionBeam()
    {
        if (planetDestructionBeam != null)
        {
            planetDestructionBeam.enabled = false;
        }
    }

    private Material CreateVisualMaterial(string materialName, Color color, bool additiveBlend = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader)
        {
            name = materialName
        };
        ConfigureMaterialTransparency(material, additiveBlend);
        ApplyMaterialColor(material, color);
        return material;
    }

    private static void ConfigureMaterialTransparency(Material material, bool additiveBlend)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt(
            "_DstBlend",
            (int)(additiveBlend
                ? UnityEngine.Rendering.BlendMode.One
                : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void ApplyMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color);
        }
    }

    private Transform ResolvePlanetVisualRoot()
    {
        Transform resolvedEarthTarget = ResolveEarthTarget();
        if (HasRenderableChildren(resolvedEarthTarget))
        {
            return resolvedEarthTarget;
        }

        GameObject earthObject = GameObject.Find("Earth");
        if (earthObject != null && HasRenderableChildren(earthObject.transform))
        {
            return earthObject.transform;
        }

        return resolvedEarthTarget;
    }

    private static bool HasRenderableChildren(Transform root)
    {
        return root != null && root.GetComponentsInChildren<Renderer>(true).Length > 0;
    }

    private static Vector3 ResolvePlanetCenter(Transform planetRoot)
    {
        if (planetRoot == null)
        {
            return Vector3.zero;
        }

        return TryGetTargetBounds(planetRoot, out Bounds bounds)
            ? bounds.center
            : planetRoot.position;
    }

    private static float ResolvePlanetDiameter(Transform planetRoot)
    {
        if (planetRoot == null)
        {
            return 1f;
        }

        if (TryGetTargetBounds(planetRoot, out Bounds bounds))
        {
            return Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        }

        Vector3 scale = planetRoot.lossyScale;
        return Mathf.Max(1f, Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)));
    }

    public void OpenCameraCoverVisual()
    {
        SetCameraCoverVisualOpen(true);
    }

    public void CloseCameraCoverVisual()
    {
        SetCameraCoverVisualOpen(false);
    }

    public IEnumerator OpenCameraCoverVisualRoutine()
    {
        yield return RunCameraCoverVisualRoutine(true);
    }

    public IEnumerator CloseCameraCoverVisualRoutine()
    {
        yield return RunCameraCoverVisualRoutine(false);
    }

    public void CancelCommandRotation()
    {
        CancelAttitudeHold();
        StopActiveMotion();
        HideTransmissionBeam();
    }

    private void StopActiveMotion()
    {
        torqueInput = Vector3.zero;

        if (attachedRigidbody != null && !attachedRigidbody.isKinematic)
        {
            attachedRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void StartAttitudeHold(IEnumerator routine)
    {
        if (routine == null)
        {
            return;
        }

        CancelAttitudeHold();
        StopActiveMotion();
        HideTransmissionBeam();
        attitudeHoldRoutine = StartCoroutine(RunAttitudeHold(routine));
    }

    private IEnumerator RunAttitudeHold(IEnumerator routine)
    {
        yield return routine;
        attitudeHoldRoutine = null;
    }

    private void CancelAttitudeHold()
    {
        if (attitudeHoldRoutine == null)
        {
            return;
        }

        StopCoroutine(attitudeHoldRoutine);
        attitudeHoldRoutine = null;
    }

    public void ResetSatellitePose()
    {
        ResolveEarthTarget();
        ResolveSunTarget();
        ResolveAntennaTransform();
        ResolveTransmissionRayOrigin();
        ResolveSolarPanelTransforms();
        ResolveCameraCoverTransform();
        CacheDefaultPose();
        CancelCommandRotation();
        RestorePlanetVisual();
        bool externalOrbitDriven = IsOrbitDrivenExternally();
        Vector3 resetPosition = externalOrbitDriven ? transform.position : defaultPosition;

        if (attachedRigidbody != null && !attachedRigidbody.isKinematic)
        {
            attachedRigidbody.linearVelocity = Vector3.zero;
            attachedRigidbody.angularVelocity = Vector3.zero;
        }

        if (attachedRigidbody != null)
        {
            attachedRigidbody.position = resetPosition;
            attachedRigidbody.rotation = defaultRotation;
            attachedRigidbody.WakeUp();
        }

        transform.position = resetPosition;
        transform.rotation = defaultRotation;
        Physics.SyncTransforms();
        if (!externalOrbitDriven)
        {
            SyncOrbitFromCurrentPosition();
        }

        Transform antenna = ResolveAntennaTransform();
        if (antenna != null)
        {
            antenna.localRotation = defaultAntennaLocalRotation;
        }

        RestoreSolarPanelLocalRotations();
        RestoreCameraCoverLocalPosition();
        ClearEarthPhotoPreview();
    }

    public void Reset()
    {
        ResetSatellitePose();
    }

    public void ConfigureTargets(Transform earth, Transform sun)
    {
        if (earth != null)
        {
            earthTarget = earth;
        }

        if (sun != null)
        {
            sunTarget = sun;
        }
    }

    private void OnDestroy()
    {
        RestorePlanetVisual();
        HideTransmissionBeam();

        if (photoRenderTexture != null)
        {
            photoRenderTexture.Release();
            Destroy(photoRenderTexture);
            photoRenderTexture = null;
        }

        if (transmissionBeamMaterial != null)
        {
            Destroy(transmissionBeamMaterial);
            transmissionBeamMaterial = null;
        }

        if (planetDestructionBeamMaterial != null)
        {
            Destroy(planetDestructionBeamMaterial);
            planetDestructionBeamMaterial = null;
        }
    }

    private void SetTorqueInput(Vector3 value)
    {
        torqueInput = value;
        if (attachedRigidbody != null)
        {
            attachedRigidbody.WakeUp();
        }
    }

    private void CacheDefaultPose()
    {
        CacheCameraCoverDefaultPosition();

        if (defaultPoseCached)
        {
            return;
        }

        defaultPosition = transform.position;
        defaultRotation = transform.rotation;

        Transform antenna = ResolveAntennaTransform();
        defaultAntennaLocalRotation = antenna != null ? antenna.localRotation : Quaternion.identity;

        Transform[] solarPanels = ResolveSolarPanelTransforms();
        defaultSolarPanelLocalRotations = new Quaternion[solarPanels.Length];
        for (int i = 0; i < solarPanels.Length; i++)
        {
            defaultSolarPanelLocalRotations[i] = solarPanels[i] != null
                ? solarPanels[i].localRotation
                : Quaternion.identity;
        }

        defaultPoseCached = true;
    }

    private void SetCameraCoverVisualOpen(bool open)
    {
        Transform cover = ResolveCameraCoverTransform();
        if (cover == null)
        {
            return;
        }

        Vector3 targetLocalPosition = open
            ? ResolveCameraCoverOpenTargetLocalPosition(cover)
            : defaultCameraCoverLocalPosition;
        if (cameraCoverAnimation != null)
        {
            StopCoroutine(cameraCoverAnimation);
            cameraCoverAnimation = null;
        }

        if (!isActiveAndEnabled)
        {
            cover.localPosition = targetLocalPosition;
            return;
        }

        cameraCoverAnimation = StartCoroutine(AnimateCameraCoverLocalPosition(cover, targetLocalPosition));
    }

    private IEnumerator RunCameraCoverVisualRoutine(bool open)
    {
        SetCameraCoverVisualOpen(open);
        Coroutine activeAnimation = cameraCoverAnimation;
        if (activeAnimation != null)
        {
            yield return activeAnimation;
        }
    }

    private IEnumerator AnimateCameraCoverLocalPosition(Transform cover, Vector3 targetLocalPosition)
    {
        if (cover == null)
        {
            cameraCoverAnimation = null;
            yield break;
        }

        Vector3 startLocalPosition = cover.localPosition;
        float duration = Mathf.Max(0.01f, cameraCoverMoveDurationSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return ExecutionPauseController.WaitWhilePaused();

            if (cover == null)
            {
                cameraCoverAnimation = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = t * t * (3f - 2f * t);
            cover.localPosition = Vector3.LerpUnclamped(startLocalPosition, targetLocalPosition, easedT);
            yield return null;
        }

        if (cover != null)
        {
            cover.localPosition = targetLocalPosition;
        }

        cameraCoverAnimation = null;
    }

    private void RestoreCameraCoverLocalPosition()
    {
        Transform cover = ResolveCameraCoverTransform();
        if (cover == null)
        {
            return;
        }

        if (cameraCoverAnimation != null)
        {
            StopCoroutine(cameraCoverAnimation);
            cameraCoverAnimation = null;
        }

        cover.localPosition = defaultCameraCoverLocalPosition;
    }

    private Vector3 ResolveCameraCoverOpenTargetLocalPosition(Transform cover)
    {
        Vector3 targetWorldPosition = transform.TransformPoint(cameraCoverOpenLocalPosition);
        return cover.parent != null
            ? cover.parent.InverseTransformPoint(targetWorldPosition)
            : targetWorldPosition;
    }

    private Transform ResolveCameraCoverTransform()
    {
        if (cameraCoverTransform != null)
        {
            CacheCameraCoverDefaultPosition();
            return cameraCoverTransform;
        }

        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < childTransforms.Length; i++)
        {
            Transform candidate = childTransforms[i];
            if (candidate == null || candidate == transform)
            {
                continue;
            }

            if (candidate.name == cameraCoverObjectName)
            {
                cameraCoverTransform = candidate;
                CacheCameraCoverDefaultPosition();
                return cameraCoverTransform;
            }
        }

        for (int i = 0; i < childTransforms.Length; i++)
        {
            Transform candidate = childTransforms[i];
            if (candidate == null || candidate == transform)
            {
                continue;
            }

            if (candidate.name.Contains("OpenCloseCamera"))
            {
                cameraCoverTransform = candidate;
                CacheCameraCoverDefaultPosition();
                return cameraCoverTransform;
            }
        }

        return null;
    }

    private void CacheCameraCoverDefaultPosition()
    {
        if (cameraCoverDefaultPositionCached || cameraCoverTransform == null)
        {
            return;
        }

        defaultCameraCoverLocalPosition = cameraCoverTransform.localPosition;
        cameraCoverDefaultPositionCached = true;
    }

    private bool IsOrbitDrivenExternally()
    {
        return orbitCenter != null && orbitCenter.GetComponent<OrbitEarth>() != null;
    }

    private void DetachFromOrbitCenterParent()
    {
        if (orbitCenter == null)
        {
            return;
        }

        if (transform.parent != orbitCenter)
        {
            return;
        }

        transform.SetParent(null, true);
    }

    private void SyncOrbitFromCurrentPosition()
    {
        if (orbitCenter == null)
        {
            return;
        }

        Vector3 direction = transform.position - orbitCenter.position;
        if (direction.sqrMagnitude <= DirectionEpsilon)
        {
            return;
        }

        orbitAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        orbitRadius = direction.magnitude;
    }

    private void AdvanceOrbit(float deltaTime, bool useRigidbodyMotion)
    {
        if (orbitCenter == null || Mathf.Approximately(deltaTime, 0f))
        {
            return;
        }

        orbitAngle += orbitSpeed * deltaTime;
        Vector3 nextPosition = CalculateOrbitPosition();

        if (useRigidbodyMotion && attachedRigidbody != null)
        {
            attachedRigidbody.MovePosition(nextPosition);
            return;
        }

        transform.position = nextPosition;
    }

    private Vector3 CalculateOrbitPosition()
    {
        float x = Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        float z = Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        Vector3 orbitPosition = new Vector3(x, 0f, z);
        Quaternion inclinationRotation = Quaternion.Euler(orbitInclination, 0f, 0f);
        orbitPosition = inclinationRotation * orbitPosition;
        return orbitCenter.position + orbitPosition;
    }

    private IEnumerator RotateTransformTowardDirection(
        Transform targetTransform,
        System.Func<Vector3> directionProvider,
        Vector3 localAimAxis,
        float rotationSpeedDegreesPerSecond,
        float trackingDurationSeconds = 0f)
    {
        if (targetTransform == null || directionProvider == null)
        {
            yield break;
        }

        if (targetTransform == transform)
        {
            StopActiveMotion();
        }

        float resolvedRotationSpeed = ResolveRotationSpeed(rotationSpeedDegreesPerSecond);
        Vector3 resolvedLocalAimAxis = NormalizeAxis(localAimAxis, Vector3.up);
        float trackingDuration = Mathf.Max(0f, trackingDurationSeconds);
        float elapsed = 0f;

        while (true)
        {
            yield return WaitForCommandPhysicsStep();
            float stepDeltaTime = GetCommandFixedDeltaTime();

            Vector3 desiredDirection = directionProvider();
            if (desiredDirection.sqrMagnitude <= DirectionEpsilon)
            {
                yield break;
            }

            if (TryRotateTowardDirectionStep(
                targetTransform,
                desiredDirection,
                resolvedLocalAimAxis,
                resolvedRotationSpeed,
                stepDeltaTime,
                out bool completed) &&
                trackingDuration <= 0f &&
                completed)
            {
                yield break;
            }

            if (trackingDuration > 0f)
            {
                elapsed += stepDeltaTime;
                if (elapsed >= trackingDuration)
                {
                    yield break;
                }
            }
        }
    }

    private bool TryRotateTowardDirectionStep(
        Transform targetTransform,
        Vector3 targetDirection,
        Vector3 localAimAxis,
        float rotationSpeedDegreesPerSecond,
        float deltaTime,
        out bool completed)
    {
        completed = true;
        if (targetTransform == null || targetDirection.sqrMagnitude <= DirectionEpsilon)
        {
            return false;
        }

        Quaternion desiredRotation = BuildRotationForLocalAxis(
            targetTransform,
            NormalizeAxis(localAimAxis, Vector3.up),
            targetDirection.normalized);
        float remainingAngle = Quaternion.Angle(targetTransform.rotation, desiredRotation);
        if (remainingAngle <= rotationCompletionAngleDegrees)
        {
            ApplyRotation(targetTransform, desiredRotation);
            return true;
        }

        Quaternion nextRotation = Quaternion.RotateTowards(
            targetTransform.rotation,
            desiredRotation,
            rotationSpeedDegreesPerSecond * Mathf.Max(0f, deltaTime));
        ApplyRotation(targetTransform, nextRotation);

        completed = Quaternion.Angle(nextRotation, desiredRotation) <= rotationCompletionAngleDegrees;
        if (completed)
        {
            ApplyRotation(targetTransform, desiredRotation);
        }

        return true;
    }

    private Quaternion BuildRotationForLocalAxis(
        Transform targetTransform,
        Vector3 localAimAxis,
        Vector3 normalizedTargetDirection)
    {
        if (targetTransform == null || normalizedTargetDirection.sqrMagnitude <= DirectionEpsilon)
        {
            return targetTransform != null ? targetTransform.rotation : Quaternion.identity;
        }

        Vector3 localReferenceAxis = SelectReferenceLocalAxis(localAimAxis);
        if (!TryBuildBasis(localAimAxis, localReferenceAxis, out Vector3 localUp, out Vector3 localForward))
        {
            Vector3 currentAimDirection = targetTransform.rotation * localAimAxis;
            return Quaternion.FromToRotation(currentAimDirection, normalizedTargetDirection) * targetTransform.rotation;
        }

        if (!TryBuildWorldBasis(targetTransform, normalizedTargetDirection, localReferenceAxis, out Vector3 worldUp, out Vector3 worldForward))
        {
            Vector3 currentAimDirection = targetTransform.rotation * localAimAxis;
            return Quaternion.FromToRotation(currentAimDirection, normalizedTargetDirection) * targetTransform.rotation;
        }

        Quaternion localBasis = Quaternion.LookRotation(localForward, localUp);
        Quaternion worldBasis = Quaternion.LookRotation(worldForward, worldUp);
        return worldBasis * Quaternion.Inverse(localBasis);
    }

    private bool TryBuildWorldBasis(
        Transform targetTransform,
        Vector3 normalizedTargetDirection,
        Vector3 localReferenceAxis,
        out Vector3 worldUp,
        out Vector3 worldForward)
    {
        Quaternion currentRotation = targetTransform.rotation;
        if (TryBuildBasis(normalizedTargetDirection, currentRotation * localReferenceAxis, out worldUp, out worldForward))
        {
            return true;
        }

        if (TryBuildBasis(normalizedTargetDirection, currentRotation * Vector3.forward, out worldUp, out worldForward))
        {
            return true;
        }

        if (TryBuildBasis(normalizedTargetDirection, currentRotation * Vector3.right, out worldUp, out worldForward))
        {
            return true;
        }

        if (TryBuildBasis(normalizedTargetDirection, currentRotation * Vector3.up, out worldUp, out worldForward))
        {
            return true;
        }

        if (TryBuildBasis(normalizedTargetDirection, Vector3.forward, out worldUp, out worldForward))
        {
            return true;
        }

        if (TryBuildBasis(normalizedTargetDirection, Vector3.right, out worldUp, out worldForward))
        {
            return true;
        }

        return TryBuildBasis(normalizedTargetDirection, Vector3.up, out worldUp, out worldForward);
    }

    private static bool TryBuildBasis(
        Vector3 primaryAxis,
        Vector3 referenceAxis,
        out Vector3 upAxis,
        out Vector3 forwardAxis)
    {
        upAxis = NormalizeAxis(primaryAxis, Vector3.up);
        Vector3 projectedReference = Vector3.ProjectOnPlane(referenceAxis, upAxis);
        if (projectedReference.sqrMagnitude <= AxisEpsilon)
        {
            forwardAxis = Vector3.zero;
            return false;
        }

        forwardAxis = projectedReference.normalized;
        return true;
    }

    private static Vector3 SelectReferenceLocalAxis(Vector3 localAimAxis)
    {
        float upDot = Mathf.Abs(Vector3.Dot(localAimAxis, Vector3.up));
        float rightDot = Mathf.Abs(Vector3.Dot(localAimAxis, Vector3.right));
        float forwardDot = Mathf.Abs(Vector3.Dot(localAimAxis, Vector3.forward));

        if (forwardDot <= upDot && forwardDot <= rightDot)
        {
            return Vector3.forward;
        }

        if (rightDot <= upDot && rightDot <= forwardDot)
        {
            return Vector3.right;
        }

        return Vector3.up;
    }

    private float ResolveRotationSpeed(float requestedRotationSpeed)
    {
        if (requestedRotationSpeed > 0f)
        {
            return requestedRotationSpeed;
        }

        return Mathf.Max(1f, defaultRotationSpeedDegreesPerSecond);
    }

    private static IEnumerator WaitForCommandPhysicsStep()
    {
        yield return ExecutionPauseController.WaitWhilePaused();
        yield return WaitForFixedStep;
    }

    private static float GetCommandFixedDeltaTime()
    {
        return Time.fixedUnscaledDeltaTime > 0f
            ? Time.fixedUnscaledDeltaTime
            : Time.fixedDeltaTime;
    }

    private Vector3 ResolveEarthFacingAxis(EarthFacingSide facingSide)
    {
        return facingSide == EarthFacingSide.Antenna
            ? NormalizeAxis(earthAntennaAimLocalAxis, new Vector3(0f, -1f, 0f))
            : NormalizeAxis(earthAimLocalAxis, Vector3.up);
    }

    private bool TryBuildEarthRelativeFrame(out Quaternion frame)
    {
        frame = Quaternion.identity;

        Vector3 directionToEarth = GetEarthDirection(transform);
        if (directionToEarth.sqrMagnitude <= DirectionEpsilon)
        {
            return false;
        }

        Vector3 radialAwayFromEarth = -directionToEarth.normalized;
        Vector3 frameForward = Vector3.ProjectOnPlane(Vector3.up, radialAwayFromEarth);
        if (frameForward.sqrMagnitude <= AxisEpsilon)
        {
            frameForward = Vector3.ProjectOnPlane(Vector3.forward, radialAwayFromEarth);
        }

        if (frameForward.sqrMagnitude <= AxisEpsilon)
        {
            frameForward = Vector3.ProjectOnPlane(Vector3.right, radialAwayFromEarth);
        }

        if (frameForward.sqrMagnitude <= AxisEpsilon)
        {
            return false;
        }

        frame = Quaternion.LookRotation(frameForward.normalized, radialAwayFromEarth);
        return true;
    }

    private void ApplyRotation(Transform targetTransform, Quaternion rotation)
    {
        if (targetTransform == transform && attachedRigidbody != null)
        {
            if (!attachedRigidbody.isKinematic)
            {
                attachedRigidbody.angularVelocity = Vector3.zero;
            }

            attachedRigidbody.MoveRotation(rotation);
            targetTransform.rotation = rotation;
            return;
        }

        targetTransform.rotation = rotation;
    }

    private Vector3 GetEarthDirection(Transform sourceTransform)
    {
        if (sourceTransform == null)
        {
            return Vector3.zero;
        }

        Transform resolvedEarthTarget = ResolveEarthTarget();
        if (resolvedEarthTarget != null)
        {
            return resolvedEarthTarget.position - sourceTransform.position;
        }

        return Vector3.zero - sourceTransform.position;
    }

    private Vector3 GetSunDirection()
    {
        Transform resolvedSunTarget = ResolveSunTarget();
        if (resolvedSunTarget != null)
        {
            return resolvedSunTarget.position - transform.position;
        }

        Light sceneSun = RenderSettings.sun;
        if (sceneSun != null)
        {
            if (sceneSun.type == LightType.Directional)
            {
                return -sceneSun.transform.forward;
            }

            return sceneSun.transform.position - transform.position;
        }

        return Vector3.zero;
    }

    private Transform ResolveSunTarget()
    {
        if (sunTarget != null)
        {
            return sunTarget;
        }

        if (sunSensor != null && sunSensor.SunTransform != null)
        {
            sunTarget = sunSensor.SunTransform;
            return sunTarget;
        }

        Sun sunComponent = FindFirstObjectByType<Sun>(FindObjectsInactive.Include);
        if (sunComponent != null)
        {
            sunTarget = sunComponent.transform;
            return sunTarget;
        }

        GameObject sunObject = GameObject.Find("Sun");
        if (sunObject != null)
        {
            sunTarget = sunObject.transform;
        }

        return sunTarget;
    }

    private SatellitePhotoPreviewController ResolvePhotoPreviewController()
    {
        if (photoPreviewController != null)
        {
            return photoPreviewController;
        }

        photoPreviewController = SatellitePhotoPreviewController.FindOrCreate();
        return photoPreviewController;
    }

    private SatellitePhotoPreviewController FindExistingPhotoPreviewController()
    {
        if (photoPreviewController != null)
        {
            return photoPreviewController;
        }

        SatellitePhotoPreviewController[] existingControllers = FindObjectsByType<SatellitePhotoPreviewController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < existingControllers.Length; i++)
        {
            SatellitePhotoPreviewController controller = existingControllers[i];
            if (controller == null)
            {
                continue;
            }

            photoPreviewController = controller;
            return photoPreviewController;
        }

        return null;
    }

    private Transform ResolveEarthTarget()
    {
        if (earthTarget != null)
        {
            return earthTarget;
        }

        GameObject explicitEarthTarget = GameObject.Find("EarthTarget");
        if (explicitEarthTarget != null)
        {
            earthTarget = explicitEarthTarget.transform;
            return earthTarget;
        }

        GameObject earthObject = GameObject.Find("Earth");
        if (earthObject != null)
        {
            earthTarget = earthObject.transform;
            return earthTarget;
        }

        if (orbitCenter != null)
        {
            earthTarget = orbitCenter;
        }

        return earthTarget;
    }

    private Transform ResolveAntennaTransform()
    {
        if (antennaTransform != null)
        {
            return antennaTransform;
        }

        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.name == "dish_etc" || candidate.name.Contains("dish"))
            {
                antennaTransform = candidate;
                return antennaTransform;
            }
        }

        return null;
    }

    private Transform ResolveTransmissionRayOrigin()
    {
        if (transmissionRayOrigin != null)
        {
            return transmissionRayOrigin;
        }

        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.name == "Ray")
            {
                transmissionRayOrigin = candidate;
                transmissionRayOriginResolvedAutomatically = true;
                return transmissionRayOrigin;
            }
        }

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.name.ToLowerInvariant().Contains("ray"))
            {
                transmissionRayOrigin = candidate;
                transmissionRayOriginResolvedAutomatically = true;
                return transmissionRayOrigin;
            }
        }

        return null;
    }

    private Camera ResolvePhotoCamera()
    {
        if (satelliteCamera != null)
        {
            ConfigurePhotoCamera(satelliteCamera);
            return satelliteCamera;
        }

        Transform existingCameraTransform = transform.Find("SatelliteCamera");
        if (existingCameraTransform != null)
        {
            satelliteCamera = existingCameraTransform.GetComponent<Camera>();
        }

        if (satelliteCamera == null)
        {
            GameObject cameraObject = new GameObject("SatelliteCamera");
            cameraObject.transform.SetParent(transform, false);
            satelliteCamera = cameraObject.AddComponent<Camera>();
        }

        ConfigurePhotoCamera(satelliteCamera);
        SyncPhotoCameraPose(satelliteCamera);
        return satelliteCamera;
    }

    private void ConfigurePhotoCamera(Camera photoCamera)
    {
        if (photoCamera == null)
        {
            return;
        }

        photoCamera.enabled = false;
        photoCamera.clearFlags = RenderSettings.skybox != null
            ? CameraClearFlags.Skybox
            : CameraClearFlags.SolidColor;
        photoCamera.backgroundColor = PhotoSpaceBackgroundColor;
        photoCamera.cullingMask = ~0;
        photoCamera.orthographic = false;
        photoCamera.fieldOfView = photoCameraFieldOfView;
        photoCamera.nearClipPlane = photoCameraNearClipPlane;
        photoCamera.farClipPlane = photoCameraFarClipPlane;
        photoCamera.depth = -100f;
        photoCamera.useOcclusionCulling = false;
        photoCamera.allowMSAA = false;
        photoCamera.targetTexture = photoRenderTexture;
    }

    private void SyncPhotoCameraPose(Camera photoCamera)
    {
        if (photoCamera == null)
        {
            return;
        }

        Transform cameraTransform = photoCamera.transform;
        if (cameraTransform.parent != transform)
        {
            cameraTransform.SetParent(transform, false);
        }

        cameraTransform.position = transform.position + transform.up * photoCameraForwardOffset;
        cameraTransform.rotation = Quaternion.LookRotation(transform.up, transform.forward);
    }

    private bool IsEarthVisibleInPhotoFrame(Camera photoCamera, Transform resolvedEarthTarget)
    {
        if (photoCamera == null || resolvedEarthTarget == null)
        {
            return false;
        }

        if (TryGetTargetBounds(resolvedEarthTarget, out Bounds targetBounds))
        {
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(photoCamera);
            return GeometryUtility.TestPlanesAABB(frustumPlanes, targetBounds);
        }

        Vector3 viewportPoint = photoCamera.WorldToViewportPoint(resolvedEarthTarget.position);
        return viewportPoint.z > photoCamera.nearClipPlane &&
               viewportPoint.x >= 0f &&
               viewportPoint.x <= 1f &&
               viewportPoint.y >= 0f &&
               viewportPoint.y <= 1f;
    }

    private static bool TryGetTargetBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        if (target == null)
        {
            return false;
        }

        bool hasBounds = false;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        if (hasBounds)
        {
            return true;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(collider.bounds);
        }

        return hasBounds;
    }

    private void EnsurePhotoRenderTexture()
    {
        int resolution = Mathf.Max(128, photoResolution);
        bool needsRecreate =
            photoRenderTexture == null ||
            photoRenderTexture.width != resolution ||
            photoRenderTexture.height != resolution;

        if (!needsRecreate)
        {
            if (!photoRenderTexture.IsCreated())
            {
                photoRenderTexture.Create();
            }

            return;
        }

        if (photoRenderTexture != null)
        {
            photoRenderTexture.Release();
            Destroy(photoRenderTexture);
        }

        photoRenderTexture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32)
        {
            name = "SatellitePhotoRenderTexture",
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        photoRenderTexture.Create();

        if (satelliteCamera != null)
        {
            satelliteCamera.targetTexture = photoRenderTexture;
        }
    }

    private void ClearPhotoRenderTexture(Color color)
    {
        if (photoRenderTexture == null)
        {
            return;
        }

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = photoRenderTexture;
        GL.Clear(true, true, color);
        RenderTexture.active = previousActive;
    }

    private IEnumerator TransmitMessage(float transmissionDurationSeconds)
    {
        float elapsed = 0f;
        float resolvedDuration = transmissionDurationSeconds > 0f
            ? transmissionDurationSeconds
            : defaultMessageTransmissionDurationSeconds;
        LineRenderer beam = ResolveTransmissionBeam();

        while (elapsed < resolvedDuration)
        {
            yield return ExecutionPauseController.WaitWhilePaused();

            Vector3 transmissionDirection = GetTransmissionDirection();
            if (transmissionDirection.sqrMagnitude <= DirectionEpsilon)
            {
                break;
            }

            UpdateTransmissionBeamVisual(beam, transmissionDirection, elapsed);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        HideTransmissionBeam();
    }

    private LineRenderer ResolveTransmissionBeam()
    {
        if (transmissionBeam == null)
        {
            Transform existingBeamTransform = transform.Find("TransmissionBeam");
            if (existingBeamTransform != null)
            {
                transmissionBeam = existingBeamTransform.GetComponent<LineRenderer>();
            }
        }

        if (transmissionBeam == null)
        {
            GameObject beamObject = new GameObject("TransmissionBeam");
            beamObject.transform.SetParent(transform, false);
            transmissionBeam = beamObject.AddComponent<LineRenderer>();
        }

        ConfigureTransmissionBeam(transmissionBeam);
        return transmissionBeam;
    }

    private void ConfigureTransmissionBeam(LineRenderer beam)
    {
        if (beam == null)
        {
            return;
        }

        if (transmissionBeamMaterial == null)
        {
            Shader beamShader = Shader.Find("Sprites/Default");
            if (beamShader == null)
            {
                beamShader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (beamShader != null)
            {
                transmissionBeamMaterial = new Material(beamShader)
                {
                    name = "TransmissionBeamMaterial"
                };

                if (transmissionBeamMaterial.HasProperty("_Color"))
                {
                    transmissionBeamMaterial.SetColor("_Color", transmissionBeamStartColor);
                }

                if (transmissionBeamMaterial.HasProperty("_BaseColor"))
                {
                    transmissionBeamMaterial.SetColor("_BaseColor", transmissionBeamStartColor);
                }
            }
        }

        beam.enabled = false;
        beam.positionCount = 2;
        beam.useWorldSpace = true;
        beam.alignment = LineAlignment.View;
        beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beam.receiveShadows = false;
        beam.textureMode = LineTextureMode.Stretch;
        beam.numCapVertices = 0;
        beam.numCornerVertices = 0;
        beam.startWidth = transmissionBeamWidth;
        beam.endWidth = transmissionBeamWidth * 0.6f;
        beam.startColor = transmissionBeamStartColor;
        beam.endColor = transmissionBeamEndColor;
        beam.widthMultiplier = 1f;

        if (transmissionBeamMaterial != null)
        {
            beam.sharedMaterial = transmissionBeamMaterial;
        }
    }

    private void UpdateTransmissionBeamVisual(LineRenderer beam, Vector3 transmissionDirection, float elapsedTime)
    {
        if (beam == null)
        {
            return;
        }

        Vector3 normalizedDirection = transmissionDirection.normalized;
        Vector3 sourcePosition = GetTransmissionOriginWorldPosition(normalizedDirection);
        Vector3 targetPosition = sourcePosition + normalizedDirection * transmissionBeamLength;
        float pulse = 1f + Mathf.Sin(elapsedTime * transmissionBeamPulseSpeed) * transmissionBeamPulseAmplitude;

        beam.enabled = true;
        beam.widthMultiplier = Mathf.Max(0.01f, pulse);
        beam.SetPosition(0, sourcePosition);
        beam.SetPosition(1, targetPosition);
    }

    private Vector3 GetTransmissionOriginWorldPosition(Vector3 normalizedTransmissionDirection)
    {
        Transform rayOrigin = ResolveExplicitTransmissionRayOrigin();
        if (rayOrigin != null)
        {
            return rayOrigin.position;
        }

        Transform antenna = ResolveAntennaTransform();
        Vector3 worldAimDirection = normalizedTransmissionDirection.sqrMagnitude > DirectionEpsilon
            ? normalizedTransmissionDirection.normalized
            : GetTransmissionDirection();
        if (antenna != null)
        {
            return antenna.position + worldAimDirection * transmissionBeamSourceOffset;
        }

        rayOrigin = ResolveTransmissionRayOrigin();
        if (rayOrigin != null)
        {
            return rayOrigin.position;
        }

        return transform.position + worldAimDirection * transmissionBeamSourceOffset;
    }

    private Vector3 GetTransmissionDirection()
    {
        Transform rayOrigin = ResolveExplicitTransmissionRayOrigin();
        if (rayOrigin != null)
        {
            Vector3 rayForward = rayOrigin.forward;
            if (rayForward.sqrMagnitude > DirectionEpsilon)
            {
                return rayForward.normalized;
            }
        }

        Transform antenna = ResolveAntennaTransform();
        if (antenna != null)
        {
            Vector3 antennaDirection = antenna.TransformDirection(NormalizeAxis(antennaAimLocalAxis, Vector3.back));
            if (antennaDirection.sqrMagnitude > DirectionEpsilon)
            {
                return antennaDirection.normalized;
            }
        }

        rayOrigin = ResolveTransmissionRayOrigin();
        if (rayOrigin != null)
        {
            Vector3 rayForward = rayOrigin.forward;
            if (rayForward.sqrMagnitude > DirectionEpsilon)
            {
                return rayForward.normalized;
            }
        }

        return transform.TransformDirection(NormalizeAxis(transmissionFallbackLocalAxis, Vector3.forward));
    }

    private Transform ResolveExplicitTransmissionRayOrigin()
    {
        if (transmissionRayOrigin == null || transmissionRayOriginResolvedAutomatically)
        {
            return null;
        }

        return transmissionRayOrigin;
    }

    private void HideTransmissionBeam()
    {
        if (transmissionBeam != null)
        {
            transmissionBeam.enabled = false;
        }
    }

    private Transform[] ResolveSolarPanelTransforms()
    {
        if (solarPanelTransforms != null && solarPanelTransforms.Length > 0)
        {
            return solarPanelTransforms;
        }

        List<Transform> resolvedPanels = new List<Transform>();
        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < childTransforms.Length; i++)
        {
            Transform candidate = childTransforms[i];
            if (!IsNamedSolarPanelCandidate(candidate) || resolvedPanels.Contains(candidate))
            {
                continue;
            }

            resolvedPanels.Add(candidate);
        }

        if (resolvedPanels.Count == 0)
        {
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                Transform candidate = meshFilter != null ? meshFilter.transform : null;
                if (!IsMeshSolarPanelCandidate(candidate, meshFilter) || resolvedPanels.Contains(candidate))
                {
                    continue;
                }

                resolvedPanels.Add(candidate);
            }
        }

        solarPanelTransforms = resolvedPanels.ToArray();
        return solarPanelTransforms;
    }

    private bool IsNamedSolarPanelCandidate(Transform candidate)
    {
        if (candidate == null || candidate == transform)
        {
            return false;
        }

        string candidateName = candidate.name.ToLowerInvariant();
        if (candidateName.Contains("sensor") || candidateName.Contains("camera") || candidateName.Contains("dish"))
        {
            return false;
        }

        return candidateName.Contains("solar")
            || candidateName.Contains("panel")
            || candidateName.Contains("blade")
            || candidateName.Contains("wing");
    }

    private bool IsMeshSolarPanelCandidate(Transform candidate, MeshFilter meshFilter)
    {
        if (candidate == null || candidate == transform || meshFilter == null || meshFilter.sharedMesh == null)
        {
            return false;
        }

        string candidateName = candidate.name.ToLowerInvariant();
        if (candidateName.Contains("sensor")
            || candidateName.Contains("camera")
            || candidateName.Contains("dish")
            || candidateName.Contains("satellite"))
        {
            return false;
        }

        Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
        float smallest = Mathf.Min(meshSize.x, Mathf.Min(meshSize.y, meshSize.z));
        float largest = Mathf.Max(meshSize.x, Mathf.Max(meshSize.y, meshSize.z));
        float middle = meshSize.x + meshSize.y + meshSize.z - smallest - largest;

        if (largest <= 0.01f || middle <= 0.01f)
        {
            return false;
        }

        return smallest <= 0.0001f || (smallest * 6f < middle && smallest * 6f < largest);
    }

    private IEnumerator RotateSolarPanelsToSun(float rotationSpeedDegreesPerSecond)
    {
        Transform[] solarPanels = ResolveSolarPanelTransforms();
        if (solarPanels.Length == 0)
        {
            yield break;
        }

        float resolvedRotationSpeed = ResolveRotationSpeed(rotationSpeedDegreesPerSecond);
        while (true)
        {
            yield return WaitForCommandPhysicsStep();
            float stepDeltaTime = GetCommandFixedDeltaTime();

            Vector3 directionToSun = GetSunDirection();
            if (directionToSun.sqrMagnitude <= DirectionEpsilon)
            {
                yield break;
            }

            bool anyPanelStillRotating = false;
            for (int i = 0; i < solarPanels.Length; i++)
            {
                Transform solarPanel = solarPanels[i];
                if (solarPanel == null)
                {
                    continue;
                }

                if (!TryRotateSolarPanelToSunStep(
                    solarPanel,
                    directionToSun,
                    resolvedRotationSpeed,
                    stepDeltaTime,
                    out bool completed))
                {
                    continue;
                }

                if (!completed)
                {
                    anyPanelStillRotating = true;
                }
            }

            if (!anyPanelStillRotating)
            {
                yield break;
            }
        }
    }

    private IEnumerator TrackSunForDuration(float rotationSpeedDegreesPerSecond, float trackingDurationSeconds)
    {
        float resolvedRotationSpeed = ResolveRotationSpeed(rotationSpeedDegreesPerSecond);
        Vector3 resolvedBodyAimAxis = NormalizeAxis(sunAimLocalAxis, Vector3.forward);
        Transform[] solarPanels = ResolveSolarPanelTransforms();
        float elapsed = 0f;
        float trackingDuration = Mathf.Max(0f, trackingDurationSeconds);

        StopActiveMotion();
        while (elapsed < trackingDuration)
        {
            yield return WaitForCommandPhysicsStep();
            float stepDeltaTime = GetCommandFixedDeltaTime();

            Vector3 directionToSun = GetSunDirection();
            if (directionToSun.sqrMagnitude <= DirectionEpsilon)
            {
                yield break;
            }

            TryRotateTowardDirectionStep(
                transform,
                directionToSun,
                resolvedBodyAimAxis,
                resolvedRotationSpeed,
                stepDeltaTime,
                out _);

            for (int i = 0; i < solarPanels.Length; i++)
            {
                Transform solarPanel = solarPanels[i];
                if (solarPanel == null)
                {
                    continue;
                }

                TryRotateSolarPanelToSunStep(
                    solarPanel,
                    directionToSun,
                    resolvedRotationSpeed,
                    stepDeltaTime,
                    out _);
            }

            elapsed += stepDeltaTime;
        }
    }

    private bool TryRotateSolarPanelToSunStep(
        Transform solarPanel,
        Vector3 directionToSun,
        float resolvedRotationSpeed,
        float deltaTime,
        out bool completed)
    {
        completed = true;
        if (solarPanel == null || directionToSun.sqrMagnitude <= DirectionEpsilon)
        {
            return false;
        }

        Vector3 panelAimAxis = ResolveSolarPanelAimAxis(solarPanel, directionToSun);
        return TryRotateTowardDirectionStep(
            solarPanel,
            directionToSun,
            panelAimAxis,
            resolvedRotationSpeed,
            deltaTime,
            out completed);
    }

    private Vector3 ResolveSolarPanelAimAxis(Transform solarPanel, Vector3 sunDirection)
    {
        Vector3 defaultAxis = GetSolarPanelLocalNormalAxis(solarPanel);
        if (solarPanel == null || sunDirection.sqrMagnitude <= DirectionEpsilon)
        {
            return defaultAxis;
        }

        Vector3 normalizedSunDirection = sunDirection.normalized;
        Vector3 positiveNormal = solarPanel.TransformDirection(defaultAxis);
        Vector3 negativeNormal = solarPanel.TransformDirection(-defaultAxis);
        return Vector3.Dot(positiveNormal, normalizedSunDirection)
            >= Vector3.Dot(negativeNormal, normalizedSunDirection)
            ? defaultAxis
            : -defaultAxis;
    }

    private Vector3 GetSolarPanelLocalNormalAxis(Transform solarPanel)
    {
        MeshFilter meshFilter = solarPanel != null ? solarPanel.GetComponent<MeshFilter>() : null;
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return Vector3.up;
        }

        Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
        if (meshSize.x <= meshSize.y && meshSize.x <= meshSize.z)
        {
            return Vector3.right;
        }

        if (meshSize.y <= meshSize.x && meshSize.y <= meshSize.z)
        {
            return Vector3.up;
        }

        return Vector3.forward;
    }

    private void RestoreSolarPanelLocalRotations()
    {
        Transform[] solarPanels = ResolveSolarPanelTransforms();
        int count = Mathf.Min(solarPanels.Length, defaultSolarPanelLocalRotations.Length);
        for (int i = 0; i < count; i++)
        {
            if (solarPanels[i] == null)
            {
                continue;
            }

            solarPanels[i].localRotation = defaultSolarPanelLocalRotations[i];
        }
    }

    private static Vector3 NormalizeAxis(Vector3 axis, Vector3 fallbackAxis)
    {
        return axis.sqrMagnitude > AxisEpsilon
            ? axis.normalized
            : fallbackAxis.normalized;
    }
}
