using UnityEngine;

public class OrbitEarth : MonoBehaviour
{
    [Header("Orbit Target")]
    [SerializeField] private Transform satelliteTransform;
    [SerializeField] private Rigidbody satelliteRigidbody;
    [SerializeField] private bool useSatelliteControllerSettings = true;

    [Header("Orbit Settings")]
    [SerializeField] private Vector3 orbitCenter = Vector3.zero;
    [SerializeField] private float orbitSpeed = 5f;
    [SerializeField] private float orbitRadius = 220f;
    [SerializeField] private float orbitInclination = 10f;

    private Rigidbody centerRigidbody;
    private SatelliteController satelliteController;
    private float orbitAngle;

    private void Awake()
    {
        centerRigidbody = GetComponent<Rigidbody>();
        if (centerRigidbody != null)
        {
            centerRigidbody.isKinematic = true;
            centerRigidbody.useGravity = false;
        }

        transform.position = orbitCenter;
        transform.rotation = Quaternion.identity;

        ResolveSatelliteReferences();
        SyncOrbitFromCurrentPosition();
    }

    private void Start()
    {
        ResolveSatelliteReferences();
        SyncOrbitFromCurrentPosition();
    }

    private void FixedUpdate()
    {
        if (ExecutionPauseController.IsPaused)
        {
            return;
        }

        ResolveSatelliteReferences();
        if (satelliteTransform == null)
        {
            return;
        }

        transform.position = orbitCenter;
        transform.rotation = Quaternion.identity;

        orbitAngle += orbitSpeed * Time.fixedDeltaTime;
        Vector3 nextPosition = CalculateOrbitPosition();

        if (satelliteRigidbody != null)
        {
            satelliteRigidbody.MovePosition(nextPosition);
            return;
        }

        satelliteTransform.position = nextPosition;
    }

    private void ResolveSatelliteReferences()
    {
        if (satelliteTransform == null)
        {
            SatelliteController resolvedController = GetComponentInChildren<SatelliteController>(true);
            if (resolvedController == null)
            {
                GameObject namedSatellite = GameObject.Find("Satellite");
                if (namedSatellite != null && namedSatellite.TryGetComponent(out SatelliteController namedController))
                {
                    resolvedController = namedController;
                }
            }

            if (resolvedController != null)
            {
                satelliteController = resolvedController;
                satelliteTransform = resolvedController.transform;
            }
            else if (transform.childCount > 0)
            {
                satelliteTransform = transform.GetChild(0);
            }
        }
        else if (satelliteController == null)
        {
            satelliteController = satelliteTransform.GetComponent<SatelliteController>();
        }

        if (satelliteRigidbody == null && satelliteTransform != null)
        {
            satelliteRigidbody = satelliteTransform.GetComponent<Rigidbody>();
        }

        ConfigureDrivenSatelliteRigidbody();

        if (!useSatelliteControllerSettings || satelliteController == null)
        {
            return;
        }

        orbitSpeed = satelliteController.OrbitSpeed;
        orbitRadius = satelliteController.OrbitRadius;
        orbitInclination = satelliteController.OrbitInclination;
        orbitCenter = Vector3.zero;
    }

    private void ConfigureDrivenSatelliteRigidbody()
    {
        if (satelliteRigidbody == null)
        {
            return;
        }

        satelliteRigidbody.useGravity = false;
        satelliteRigidbody.isKinematic = true;
        satelliteRigidbody.linearVelocity = Vector3.zero;
        satelliteRigidbody.angularVelocity = Vector3.zero;
    }

    private void SyncOrbitFromCurrentPosition()
    {
        if (satelliteTransform == null)
        {
            return;
        }

        Vector3 direction = satelliteTransform.position - orbitCenter;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion inverseInclination = Quaternion.Inverse(Quaternion.Euler(orbitInclination, 0f, 0f));
        Vector3 planarDirection = inverseInclination * direction;
        orbitRadius = new Vector2(planarDirection.x, planarDirection.z).magnitude;
        orbitAngle = Mathf.Atan2(planarDirection.x, planarDirection.z) * Mathf.Rad2Deg;
    }

    private Vector3 CalculateOrbitPosition()
    {
        float x = Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        float z = Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        Vector3 orbitOffset = new Vector3(x, 0f, z);
        Quaternion inclinationRotation = Quaternion.Euler(orbitInclination, 0f, 0f);
        return orbitCenter + inclinationRotation * orbitOffset;
    }
}
