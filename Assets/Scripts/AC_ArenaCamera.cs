using UnityEngine;

public class AC_ArenaCamera : MonoBehaviour
{
    [Header("Modo")]
    public bool splitScreen = true;
    public bool dynamicZoom = true;
    public bool forceOrthographic;

    [Header("Posicion")]
    public float fixedHeight = 12f;
    public float followLerp = 6f;
    public float playerBias = 0.7f;
    public float thirdPersonHeight = 5.5f;
    public float thirdPersonDistance = 7.5f;
    public float lookAhead = 2.5f;
    public float sideOffset = 2.0f;
    public float cameraHeightLerp = 4f;
    public float minPitch = 14f;
    public float maxPitch = 24f;

    [Header("Zoom")]
    public float minHeight = 10f;
    public float maxHeight = 22f;
    public float zoomMargin = 2.5f;
    public float zoomSmoothSpeed = 4f;

    [Header("Referencias")]
    public Transform arenaCenter;
    public Transform player1;
    public Transform player2;

    private Camera primaryCamera;
    private Camera secondaryCamera;
    private AudioListener primaryListener;
    private float primaryHeight;
    private float secondaryHeight;

    private void Awake()
    {
        primaryCamera = GetComponent<Camera>();
        primaryListener = GetComponent<AudioListener>();
    }

    private void Start()
    {
        ResolveReferences();
        EnsureCameraSetup();
        SnapToCurrentLayout();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        EnsureCameraSetup();

        if (splitScreen && player1 != null && player2 != null)
        {
            UpdateSplitScreenCamera(primaryCamera, player1, ref primaryHeight);
            UpdateSplitScreenCamera(secondaryCamera, player2, ref secondaryHeight);
            return;
        }

        UpdateSharedCamera();
    }

    private void EnsureCameraSetup()
    {
        if (primaryCamera == null)
        {
            primaryCamera = GetComponent<Camera>();
        }

        if (primaryCamera == null)
        {
            return;
        }

        primaryCamera.enabled = true;
        primaryCamera.orthographic = forceOrthographic;
        primaryCamera.rect = splitScreen ? new Rect(0f, 0f, 0.5f, 1f) : new Rect(0f, 0f, 1f, 1f);
        primaryCamera.depth = 0f;

        if (primaryListener != null)
        {
            primaryListener.enabled = true;
        }

        if (splitScreen)
        {
            EnsureSecondaryCamera();
        }
        else if (secondaryCamera != null)
        {
            secondaryCamera.gameObject.SetActive(false);
        }
    }

    private void EnsureSecondaryCamera()
    {
        if (secondaryCamera == null)
        {
            Transform existing = transform.parent != null ? transform.parent.Find("P2 Camera") : null;
            if (existing != null)
            {
                secondaryCamera = existing.GetComponent<Camera>();
            }
        }

        if (secondaryCamera == null)
        {
            GameObject cameraObject = new GameObject("P2 Camera");
            if (transform.parent != null)
            {
                cameraObject.transform.SetParent(transform.parent);
            }

            secondaryCamera = cameraObject.AddComponent<Camera>();
        }

        secondaryCamera.gameObject.SetActive(true);
        secondaryCamera.enabled = true;
        secondaryCamera.orthographic = forceOrthographic;
        secondaryCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        secondaryCamera.depth = 0f;
        secondaryCamera.clearFlags = primaryCamera.clearFlags;
        secondaryCamera.backgroundColor = primaryCamera.backgroundColor;
        secondaryCamera.nearClipPlane = primaryCamera.nearClipPlane;
        secondaryCamera.farClipPlane = primaryCamera.farClipPlane;
        secondaryCamera.cullingMask = primaryCamera.cullingMask;
        secondaryCamera.allowHDR = primaryCamera.allowHDR;
        secondaryCamera.allowMSAA = primaryCamera.allowMSAA;
    }

    private void UpdateSharedCamera()
    {
        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        float arenaRadius = AC_GameManager.Instance != null ? AC_GameManager.Instance.CurrentArenaRadius : 7.5f;
        float targetRadius = arenaRadius + zoomMargin;

        if (dynamicZoom && player1 != null && player2 != null)
        {
            float dist1 = FlatDistance(player1.position, center);
            float dist2 = FlatDistance(player2.position, center);
            targetRadius = Mathf.Max(Mathf.Max(dist1, dist2), arenaRadius) + zoomMargin;
        }

        primaryHeight = Mathf.Lerp(primaryHeight <= 0f ? fixedHeight : primaryHeight, Mathf.Clamp(targetRadius + 5f, minHeight, maxHeight), Time.deltaTime * zoomSmoothSpeed);
        ApplySharedCamera(primaryCamera, center, ref primaryHeight, targetRadius);
    }

    private void UpdateSplitScreenCamera(Camera targetCamera, Transform focusPlayer, ref float currentHeight)
    {
        if (targetCamera == null || focusPlayer == null)
        {
            return;
        }

        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        float arenaRadius = AC_GameManager.Instance != null ? AC_GameManager.Instance.CurrentArenaRadius : 7.5f;
        Vector3 desiredCenter = Vector3.Lerp(center, focusPlayer.position, playerBias);
        desiredCenter.y = center.y;

        float targetRadius = zoomMargin + Mathf.Max(4f, arenaRadius * 0.55f);
        if (dynamicZoom && targetCamera.orthographic)
        {
            float focusDistance = FlatDistance(focusPlayer.position, center);
            targetRadius = Mathf.Clamp(focusDistance + zoomMargin + 2f, 5f, arenaRadius + zoomMargin);
        }

        if (targetCamera.orthographic)
        {
            currentHeight = Mathf.Lerp(currentHeight <= 0f ? fixedHeight : currentHeight, Mathf.Clamp(targetRadius + 5f, minHeight, maxHeight), Time.deltaTime * zoomSmoothSpeed);
            ApplySharedCamera(targetCamera, desiredCenter, ref currentHeight, targetRadius);
            return;
        }

        ApplyStableSplitCamera(targetCamera, focusPlayer, center, targetRadius);
    }

    private void ApplySharedCamera(Camera targetCamera, Vector3 targetCenter, ref float currentHeight, float targetRadius)
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 targetPosition = targetCenter + Vector3.up * currentHeight;
        targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, targetPosition, Time.deltaTime * followLerp);
        targetCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, Mathf.Clamp(targetRadius, 5f, maxHeight), Time.deltaTime * zoomSmoothSpeed);
        }
    }

    private void ApplyStableSplitCamera(Camera targetCamera, Transform focusPlayer, Vector3 arenaWorldCenter, float targetRadius)
    {
        Vector3 fromCenterToPlayer = focusPlayer.position - arenaWorldCenter;
        fromCenterToPlayer.y = 0f;
        if (fromCenterToPlayer.sqrMagnitude < 0.001f)
        {
            fromCenterToPlayer = focusPlayer.position.x < 0f ? Vector3.left : Vector3.right;
        }

        fromCenterToPlayer.Normalize();
        Vector3 viewDirection = -fromCenterToPlayer;
        Vector3 side = Vector3.Cross(Vector3.up, viewDirection).normalized;

        Vector3 lookTarget = focusPlayer.position + viewDirection * lookAhead;
        lookTarget.y = focusPlayer.position.y + 1.5f;

        float distance = dynamicZoom ? Mathf.Lerp(thirdPersonDistance, thirdPersonDistance + 2f, Mathf.InverseLerp(4f, maxHeight, targetRadius)) : thirdPersonDistance;
        float height = Mathf.Lerp(thirdPersonHeight, thirdPersonHeight + 1f, Mathf.InverseLerp(4f, maxHeight, targetRadius));
        Vector3 desiredPosition = focusPlayer.position - viewDirection * distance + side * sideOffset + Vector3.up * height;
        desiredPosition = ClampThirdPersonPosition(desiredPosition, arenaWorldCenter);

        targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, desiredPosition, Time.deltaTime * followLerp);

        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - targetCamera.transform.position, Vector3.up);
        Vector3 euler = targetRotation.eulerAngles;
        if (euler.x > 180f)
        {
            euler.x -= 360f;
        }
        euler.x = Mathf.Clamp(euler.x, minPitch, maxPitch);
        targetRotation = Quaternion.Euler(euler.x, targetRotation.eulerAngles.y, 0f);
        targetCamera.transform.rotation = Quaternion.Slerp(targetCamera.transform.rotation, targetRotation, Time.deltaTime * followLerp);

        if (!targetCamera.orthographic)
        {
            float targetFov = Mathf.Lerp(58f, 68f, Mathf.InverseLerp(5f, maxHeight, targetRadius));
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFov, Time.deltaTime * zoomSmoothSpeed);
        }
    }

    private Vector3 ClampThirdPersonPosition(Vector3 desiredPosition, Vector3 arenaWorldCenter)
    {
        if (AC_GameManager.Instance == null)
        {
            return desiredPosition;
        }

        Vector3 flatCenter = new Vector3(arenaWorldCenter.x, 0f, arenaWorldCenter.z);
        Vector3 flatDesired = new Vector3(desiredPosition.x, 0f, desiredPosition.z);
        Vector3 delta = flatDesired - flatCenter;
        float maxOffset = Mathf.Max(1.5f, AC_GameManager.Instance.CurrentArenaRadius * 0.8f);

        if (delta.sqrMagnitude > maxOffset * maxOffset)
        {
            delta = delta.normalized * maxOffset;
            desiredPosition.x = flatCenter.x + delta.x;
            desiredPosition.z = flatCenter.z + delta.z;
        }

        return desiredPosition;
    }

    private void SnapToCurrentLayout()
    {
        primaryHeight = fixedHeight;
        secondaryHeight = fixedHeight;

        if (splitScreen && player1 != null && player2 != null)
        {
            UpdateSplitScreenCamera(primaryCamera, player1, ref primaryHeight);
            UpdateSplitScreenCamera(secondaryCamera, player2, ref secondaryHeight);
        }
        else
        {
            UpdateSharedCamera();
        }
    }

    private void ResolveReferences()
    {
        if (AC_GameManager.Instance != null)
        {
            if (arenaCenter == null)
            {
                arenaCenter = AC_GameManager.Instance.arenaCenter;
            }

            if (player1 == null && AC_GameManager.Instance.player1 != null)
            {
                player1 = AC_GameManager.Instance.player1.transform;
            }

            if (player2 == null && AC_GameManager.Instance.player2 != null)
            {
                player2 = AC_GameManager.Instance.player2.transform;
            }
        }

        if (arenaCenter != null && arenaCenter.name == "ArenaCenter")
        {
            GameObject arenaCylinder = GameObject.Find("ArenaCylinder");
            if (arenaCylinder != null)
            {
                arenaCenter = arenaCylinder.transform;
            }
        }
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector3 aa = new Vector3(a.x, 0f, a.z);
        Vector3 bb = new Vector3(b.x, 0f, b.z);
        return Vector3.Distance(aa, bb);
    }
}
