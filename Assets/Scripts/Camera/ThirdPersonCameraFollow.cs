using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 5.5f;
    [SerializeField] private float height = 2.8f;
    [SerializeField] private float lookAtHeight = 1.5f;
    [SerializeField] private float smoothTime = 0.08f;
    [SerializeField] private float fieldOfView = 60f;

    private Camera followCamera;
    private Vector3 currentVelocity;

    private void Awake()
    {
        followCamera = GetComponent<Camera>();
        EnsureTarget();
        ApplyCameraSettings();
        SnapToTarget();
    }

    private void OnValidate()
    {
        distance = Mathf.Max(0.1f, distance);
        height = Mathf.Max(0.1f, height);
        smoothTime = Mathf.Max(0.01f, smoothTime);
        fieldOfView = Mathf.Clamp(fieldOfView, 35f, 90f);
    }

    private void LateUpdate()
    {
        EnsureTarget();
        if (target == null)
        {
            return;
        }

        ApplyCameraSettings();

        Transform followRoot = target.parent != null ? target.parent : target;
        Vector3 targetPos = followRoot.position + Vector3.up * lookAtHeight;
        Vector3 desiredPos = followRoot.position - followRoot.forward * distance + Vector3.up * height;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, smoothTime);
        transform.LookAt(targetPos);
    }

    private void EnsureTarget()
    {
        if (target != null)
        {
            return;
        }

        AC_PlayerController player = Object.FindFirstObjectByType<AC_PlayerController>();
        if (player == null)
        {
            return;
        }

        target = GetOrCreateCameraTarget(player.transform);
    }

    private Transform GetOrCreateCameraTarget(Transform player)
    {
        Transform cameraTarget = player.Find("CameraTarget");
        if (cameraTarget != null)
        {
            cameraTarget.localPosition = new Vector3(0f, lookAtHeight, 0f);
            cameraTarget.localRotation = Quaternion.identity;
            return cameraTarget;
        }

        GameObject targetObject = new GameObject("CameraTarget");
        targetObject.transform.SetParent(player, false);
        targetObject.transform.localPosition = new Vector3(0f, lookAtHeight, 0f);
        targetObject.transform.localRotation = Quaternion.identity;
        return targetObject.transform;
    }

    private void ApplyCameraSettings()
    {
        if (followCamera == null)
        {
            followCamera = GetComponent<Camera>();
        }

        if (followCamera == null)
        {
            return;
        }

        followCamera.enabled = true;
        followCamera.orthographic = false;
        followCamera.fieldOfView = fieldOfView;
        followCamera.rect = new Rect(0f, 0f, 1f, 1f);
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Transform followRoot = target.parent != null ? target.parent : target;
        Vector3 targetPos = followRoot.position + Vector3.up * lookAtHeight;
        transform.position = followRoot.position - followRoot.forward * distance + Vector3.up * height;
        transform.LookAt(targetPos);
        currentVelocity = Vector3.zero;
    }
}
