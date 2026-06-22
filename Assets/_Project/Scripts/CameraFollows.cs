using UnityEngine;

public class CameraFollows : MonoBehaviour
{
    private const float LookDirectionEpsilon = 0.0001f;

    [SerializeField] private Transform _targetLookAt;
    [SerializeField] private Transform _targetPosition;

    private void Awake()
    {
        DetachFromTargetHierarchy();
    }

    private void Start()
    {
        DetachFromTargetHierarchy();
    }

    private void LateUpdate()
    {
        if (_targetPosition == null || _targetLookAt == null)
        {
            return;
        }

        DetachFromTargetHierarchy();

        transform.position = _targetPosition.position;

        Vector3 lookDirection = _targetLookAt.position - transform.position;
        if (lookDirection.sqrMagnitude <= LookDirectionEpsilon)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private void DetachFromTargetHierarchy()
    {
        if (_targetPosition == null || !transform.IsChildOf(_targetPosition))
        {
            return;
        }

        transform.SetParent(null, true);
    }
}
