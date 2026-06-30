using UnityEngine;

public class AC_CameraManager : MonoBehaviour
{
    [Header("Jugadores")]
    public AC_PlayerController player1;
    public AC_PlayerController player2;

    [Header("Camaras")]
    public Camera primaryCamera;
    public Camera secondaryCamera;

    [Header("Configuracion")]
    [Tooltip("0 = camara compartida, 1 = split vertical (izq/der)")]
    public int verticalSplit = 1;

    [Tooltip("Si es true, en modo split usa ThirdPersonCamera tradicional en vez del seguimiento personalizado")]
    public bool useThirdPersonCamera = true;

    [Header("Modo compartido")]
    public bool dynamicZoom = true;
    public float fixedHeight = 12f;
    public float followLerp = 6f;
    public float zoomMargin = 2.5f;
    public float zoomSmoothSpeed = 4f;
    public float minHeight = 10f;
    public float maxHeight = 22f;

    [Header("Modo split")]
    public Vector3 thirdPersonOffset = new Vector3(0f, 2.5f, -3.5f);
    public float thirdPersonPositionSmoothTime = 0.12f;
    public float thirdPersonRotationSmoothTime = 0.08f;
    public float thirdPersonFieldOfView = 70f;

    [Header("Modo split legacy")]
    public float playerBias = 0.7f;
    public float thirdPersonHeight = 5.5f;
    public float thirdPersonDistance = 7.5f;
    public float lookAhead = 2.5f;
    public float sideOffset = 2.0f;
    public float minPitch = 14f;
    public float maxPitch = 24f;

    public Transform arenaCenter;

    private AudioListener primaryListener;
    private float primaryHeight;
    private float secondaryHeight;

    private void Awake()
    {
        if (primaryCamera == null)
        {
            primaryCamera = GetComponent<Camera>();
        }
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

        bool useSplit = verticalSplit != 0;
        if (useSplit && player1 != null && player2 != null)
        {
            if (!useThirdPersonCamera)
            {
                UpdateSplitScreenCamera(primaryCamera, player1.transform, ref primaryHeight);
                UpdateSplitScreenCamera(secondaryCamera, player2.transform, ref secondaryHeight);
            }
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

        bool useSplit = verticalSplit != 0;
        primaryCamera.enabled = true;
        primaryCamera.rect = useSplit ? new Rect(0f, 0f, 0.5f, 1f) : new Rect(0f, 0f, 1f, 1f);
        primaryCamera.depth = 0f;

        if (primaryListener != null)
        {
            primaryListener.enabled = !useSplit;
        }

        if (useSplit)
        {
            EnsureSecondaryCamera();
            EnsureThirdPersonCameras();
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
        secondaryCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        secondaryCamera.depth = 1f;
        secondaryCamera.clearFlags = CameraClearFlags.Depth;
        secondaryCamera.backgroundColor = primaryCamera.backgroundColor;
        secondaryCamera.nearClipPlane = primaryCamera.nearClipPlane;
        secondaryCamera.farClipPlane = primaryCamera.farClipPlane;
        secondaryCamera.cullingMask = primaryCamera.cullingMask;
        secondaryCamera.allowHDR = primaryCamera.allowHDR;
        secondaryCamera.allowMSAA = primaryCamera.allowMSAA;
    }

    private void EnsureThirdPersonCameras()
    {
        if (!useThirdPersonCamera)
        {
            DisableThirdPersonCamera(primaryCamera);
            DisableThirdPersonCamera(secondaryCamera);
            return;
        }

        ThirdPersonCamera primaryTPC = EnsureThirdPersonCamera(primaryCamera);
        ThirdPersonCamera secondaryTPC = EnsureThirdPersonCamera(secondaryCamera);

        if (primaryTPC != null)
        {
            primaryTPC.enabled = true;
            primaryTPC.positionSmoothTime = thirdPersonPositionSmoothTime;
            primaryTPC.rotationSmoothTime = thirdPersonRotationSmoothTime;
            primaryTPC.offsetLocal = thirdPersonOffset;
            primaryTPC.enableCollision = true;
            primaryTPC.minDistance = 1f;
            primaryTPC.sphereRadius = 0.25f;
            primaryTPC.collisionMask = primaryCamera.cullingMask;
            primaryCamera.fieldOfView = thirdPersonFieldOfView;
            if (primaryTPC.target != player1.transform)
            {
                primaryTPC.SetTarget(player1.transform);
            }
        }

        if (secondaryTPC != null)
        {
            secondaryTPC.enabled = true;
            secondaryTPC.positionSmoothTime = thirdPersonPositionSmoothTime;
            secondaryTPC.rotationSmoothTime = thirdPersonRotationSmoothTime;
            secondaryTPC.offsetLocal = thirdPersonOffset;
            secondaryTPC.enableCollision = true;
            secondaryTPC.minDistance = 1f;
            secondaryTPC.sphereRadius = 0.25f;
            secondaryTPC.collisionMask = secondaryCamera.cullingMask;
            secondaryCamera.fieldOfView = thirdPersonFieldOfView;
            if (secondaryTPC.target != player2.transform)
            {
                secondaryTPC.SetTarget(player2.transform);
            }
        }

        if (player1 != null && primaryCamera != null)
        {
            player1.movementCamera = primaryCamera.transform;
        }

        if (player2 != null && secondaryCamera != null)
        {
            player2.movementCamera = secondaryCamera.transform;
        }
    }

    private ThirdPersonCamera EnsureThirdPersonCamera(Camera targetCamera)
    {
        if (targetCamera == null)
        {
            return null;
        }

        ThirdPersonCamera tpc = targetCamera.GetComponent<ThirdPersonCamera>();
        if (tpc == null)
        {
            tpc = targetCamera.gameObject.AddComponent<ThirdPersonCamera>();
        }
        return tpc;
    }

    private void DisableThirdPersonCamera(Camera targetCamera)
    {
        if (targetCamera == null)
        {
            return;
        }

        ThirdPersonCamera tpc = targetCamera.GetComponent<ThirdPersonCamera>();
        if (tpc != null)
        {
            tpc.enabled = false;
        }
    }

    private void UpdateSharedCamera()
    {
        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        float arenaRadius = AC_GameManager.Instance != null ? AC_GameManager.Instance.CurrentArenaRadius : 7.5f;
        float targetRadius = arenaRadius + zoomMargin;

        if (dynamicZoom && player1 != null && player2 != null)
        {
            float dist1 = FlatDistance(player1.transform.position, center);
            float dist2 = FlatDistance(player2.transform.position, center);
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

    private void ApplyStableSplitCameraInstant(Camera targetCamera, Transform focusPlayer)
    {
        if (targetCamera == null || focusPlayer == null)
        {
            return;
        }

        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        float arenaRadius = AC_GameManager.Instance != null ? AC_GameManager.Instance.CurrentArenaRadius : 7.5f;

        Vector3 fromCenterToPlayer = focusPlayer.position - center;
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

        float focusDistance = FlatDistance(focusPlayer.position, center);
        float targetRadius = Mathf.Clamp(focusDistance + zoomMargin + 2f, 5f, arenaRadius + zoomMargin);
        float distance = dynamicZoom ? Mathf.Lerp(thirdPersonDistance, thirdPersonDistance + 2f, Mathf.InverseLerp(4f, maxHeight, targetRadius)) : thirdPersonDistance;
        float height = Mathf.Lerp(thirdPersonHeight, thirdPersonHeight + 1f, Mathf.InverseLerp(4f, maxHeight, targetRadius));
        Vector3 desiredPosition = focusPlayer.position - viewDirection * distance + side * sideOffset + Vector3.up * height;
        desiredPosition = ClampThirdPersonPosition(desiredPosition, center);

        targetCamera.transform.position = desiredPosition;

        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - targetCamera.transform.position, Vector3.up);
        Vector3 euler = targetRotation.eulerAngles;
        if (euler.x > 180f) euler.x -= 360f;
        euler.x = Mathf.Clamp(euler.x, minPitch, maxPitch);
        targetCamera.transform.rotation = Quaternion.Euler(euler.x, targetRotation.eulerAngles.y, 0f);

        if (!targetCamera.orthographic)
        {
            targetCamera.fieldOfView = Mathf.Lerp(58f, 68f, Mathf.InverseLerp(5f, maxHeight, targetRadius));
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

        bool useSplit = verticalSplit != 0;
        if (useSplit && player1 != null && player2 != null)
        {
            if (useThirdPersonCamera)
            {
                EnsureThirdPersonCameras();
            }
            else
            {
                ApplyStableSplitCameraInstant(primaryCamera, player1.transform);
                ApplyStableSplitCameraInstant(secondaryCamera, player2.transform);
            }
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
                player1 = AC_GameManager.Instance.player1;
            }

            if (player2 == null && AC_GameManager.Instance.player2 != null)
            {
                player2 = AC_GameManager.Instance.player2;
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
