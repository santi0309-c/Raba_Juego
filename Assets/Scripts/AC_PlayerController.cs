using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AC_PlayerController : MonoBehaviour
{
    [Header("Identidad")]
    public int playerId = 1;
    public string displayName = "Jugador 1";

    [Header("Teclas")]
    public KeyCode upKey = KeyCode.W;
    public KeyCode downKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode hugKey = KeyCode.Space;
    public KeyCode alternateHugKey = KeyCode.None;
    public KeyCode dashKey = KeyCode.LeftShift;
    public KeyCode alternateDashKey = KeyCode.None;
    public KeyCode holdKey = KeyCode.LeftControl;
    public KeyCode alternateHoldKey = KeyCode.None;
    public KeyCode jumpKey = KeyCode.None;

    [Header("Movimiento")]
    public float moveSpeed = 5.5f;
    public float rotationSpeed = 12f;
    public bool tankControls = true;
    public float turnSpeedDegrees = 260f;
    public float gravity = -25f;
    public float fallY = -4f;

    [Header("Camara")]
    public bool moveRelativeToCamera = true;
    public Transform movementCamera;

    [Header("Salto")]
    public float jumpForce = 8f;
    public float groundCheckDistance = 0.15f;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundMask = ~0;

    [Header("Dash")]
    public float dashDistance = 3f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 4f;

    [Header("Aguantar")]
    public float holdRadius = 0.18f;
    public float holdHeightMultiplier = 0.75f;

    private CharacterController controller;
    private AC_HugDetector hugDetector;
    private Rigidbody rigidBody;
    private Collider fallbackCollider;
    private AC_Spawner spawnerHelper;

    private float normalRadius;
    private float normalHeight;
    private Vector3 normalCenter;
    private Vector3 verticalVelocity;
    private Vector3 dashVelocity;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 impulseVelocity;
    private float impulseTimer;
    private Vector3 lastNonZeroInput = Vector3.forward;
    private float forwardInput;
    private float turnInput;
    private float lastRespawnTime = -999f;
    private readonly float respawnProtectionDuration = 0.5f;

    public bool IsHolding { get; private set; }
    public bool IsDashing => dashTimer > 0f;
    public float LastDashTime { get; private set; } = -999f;
    public float LastHugPressTime { get; private set; } = -999f;
    public AC_HugDetector HugDetector => hugDetector;
    public float DashCooldown => dashCooldown;
    public float DashCooldownRemaining => Mathf.Max(0f, dashCooldown - (Time.time - LastDashTime));
    public CharacterController CharacterController => controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        hugDetector = GetComponent<AC_HugDetector>();
        rigidBody = GetComponent<Rigidbody>();
        fallbackCollider = GetComponent<CapsuleCollider>();
        spawnerHelper = GetComponent<AC_Spawner>();

        if (spawnerHelper == null)
        {
            spawnerHelper = gameObject.AddComponent<AC_Spawner>();
        }

        normalRadius = controller.radius;
        normalHeight = controller.height;
        normalCenter = controller.center;

        spawnerHelper.enabled = true;
        spawnerHelper.autoRespawnOnFall = false;
        spawnerHelper.posicionEjeY = fallY;

        var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }

        ConfigureDefaultBindings();
        NormalizePhysicsComponents();
    }

    private void ConfigureDefaultBindings()
    {
        if (playerId == 2)
        {
            if (upKey == KeyCode.W) upKey = KeyCode.UpArrow;
            if (downKey == KeyCode.S) downKey = KeyCode.DownArrow;
            if (leftKey == KeyCode.A) leftKey = KeyCode.LeftArrow;
            if (rightKey == KeyCode.D) rightKey = KeyCode.RightArrow;
            if (hugKey == KeyCode.Space) hugKey = KeyCode.Return;
            if (alternateHugKey == KeyCode.None) alternateHugKey = KeyCode.KeypadEnter;
            if (dashKey == KeyCode.LeftShift) dashKey = KeyCode.RightShift;
            if (holdKey == KeyCode.LeftControl) holdKey = KeyCode.RightControl;
            displayName = "Jugador 2";
        }
        else
        {
            if (alternateHugKey == KeyCode.None) alternateHugKey = KeyCode.None;
        }
    }

    private void NormalizePhysicsComponents()
    {
        if (rigidBody != null)
        {
            rigidBody.isKinematic = true;
            rigidBody.useGravity = false;
            rigidBody.detectCollisions = false;
        }

        if (fallbackCollider != null)
        {
            fallbackCollider.enabled = false;
        }
    }

    public void SyncControllerSettingsFrom(AC_PlayerController source)
    {
        if (source == null || source.controller == null || controller == null || source == this)
        {
            return;
        }

        controller.height = source.controller.height;
        controller.radius = source.controller.radius;
        controller.center = source.controller.center;
        controller.stepOffset = Mathf.Max(0f, source.controller.stepOffset);
        controller.minMoveDistance = Mathf.Max(0f, source.controller.minMoveDistance);

        normalHeight = controller.height;
        normalRadius = controller.radius;
        normalCenter = controller.center;
    }

    public void SetRespawnPoint(Transform point)
    {
        if (spawnerHelper != null)
        {
            spawnerHelper.SetRespawnPoint(point);
        }
    }

    private void Update()
    {
        if (AC_GameManager.Instance != null && !AC_GameManager.Instance.IsRoundActive)
        {
            RestoreNormalCollider();
            return;
        }

        Vector3 input = ReadInputDirection();
        bool hasControlInput = Mathf.Abs(forwardInput) > 0.01f || Mathf.Abs(turnInput) > 0.01f;
        bool wantsHold = IsActionHeld(holdKey, alternateHoldKey) && !hasControlInput && !IsDashing;
        SetHolding(wantsHold);

        if (IsActionPressed(hugKey, alternateHugKey) && hugDetector != null && !IsHolding)
        {
            LastHugPressTime = Time.time;
            hugDetector.StartHug();
        }

        dashCooldownTimer -= Time.deltaTime;
        if (IsActionPressed(dashKey, alternateDashKey) && dashCooldownTimer <= 0f && !IsHolding)
        {
            StartDash(input);
        }

        if (jumpKey != KeyCode.None && Input.GetKeyDown(jumpKey) && IsGrounded() && !IsHolding)
        {
            verticalVelocity.y = jumpForce;
        }

        MovePlayer(input);
        CheckFall();
    }

    private Vector3 ReadInputDirection()
    {
        float x = 0f;
        float z = 0f;
        if (Input.GetKey(leftKey)) x -= 1f;
        if (Input.GetKey(rightKey)) x += 1f;
        if (Input.GetKey(downKey)) z -= 1f;
        if (Input.GetKey(upKey)) z += 1f;

        turnInput = Mathf.Clamp(x, -1f, 1f);
        forwardInput = Mathf.Clamp(z, -1f, 1f);

        if (tankControls)
        {
            return Mathf.Abs(forwardInput) > 0.01f ? transform.forward * forwardInput : Vector3.zero;
        }

        Vector3 dir = new Vector3(x, 0f, z);
        if (dir.sqrMagnitude > 1f)
        {
            dir.Normalize();
        }

        if (moveRelativeToCamera && dir.sqrMagnitude > 0.01f)
        {
            Transform cameraTransform = ResolveMovementCamera();
            if (cameraTransform != null)
            {
                Vector3 cameraForward = cameraTransform.forward;
                cameraForward.y = 0f;
                cameraForward.Normalize();

                Vector3 cameraRight = cameraTransform.right;
                cameraRight.y = 0f;
                cameraRight.Normalize();

                dir = cameraRight * dir.x + cameraForward * dir.z;
                if (dir.sqrMagnitude > 1f)
                {
                    dir.Normalize();
                }
            }
        }

        if (dir.sqrMagnitude > 0.01f)
        {
            lastNonZeroInput = dir.normalized;
        }

        return dir;
    }

    private Transform ResolveMovementCamera()
    {
        if (movementCamera != null)
        {
            return movementCamera;
        }

        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.transform : null;
    }

    private void MovePlayer(Vector3 input)
    {
        Vector3 movementInput = input;
        Vector3 horizontal = Vector3.zero;

        if (tankControls)
        {
            if (Mathf.Abs(turnInput) > 0.01f && !IsHolding)
            {
                transform.Rotate(Vector3.up, turnInput * turnSpeedDegrees * Time.deltaTime, Space.World);
            }

            movementInput = Mathf.Abs(forwardInput) > 0.01f ? transform.forward * forwardInput : Vector3.zero;
            if (movementInput.sqrMagnitude > 0.01f)
            {
                lastNonZeroInput = movementInput.normalized;
            }
        }

        if (!IsHolding)
        {
            horizontal = movementInput * moveSpeed;
        }

        if (!tankControls && movementInput.sqrMagnitude > 0.01f && !IsHolding)
        {
            Quaternion targetRot = Quaternion.LookRotation(movementInput, Vector3.up);
            float maxDegreesDelta = Mathf.Max(90f, rotationSpeed * 90f) * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, maxDegreesDelta);
        }

        if (dashTimer > 0f)
        {
            horizontal += dashVelocity;
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                dashVelocity = Vector3.zero;
            }
        }

        if (impulseTimer > 0f)
        {
            horizontal += impulseVelocity;
            impulseTimer -= Time.deltaTime;
            if (impulseTimer <= 0f)
            {
                impulseVelocity = Vector3.zero;
            }
        }

        float maxHorizontalSpeed = moveSpeed + dashVelocity.magnitude + impulseVelocity.magnitude;
        horizontal = Vector3.ClampMagnitude(horizontal, Mathf.Max(maxHorizontalSpeed, moveSpeed));

        if (AC_GameManager.Instance != null)
        {
            horizontal += AC_GameManager.Instance.CurrentWindVelocity;
        }

        if (IsGrounded() && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 motion = (horizontal + verticalVelocity) * Time.deltaTime;
        motion = ClampMotionToArena(motion);
        controller.Move(motion);
    }

    private void StartDash(Vector3 input)
    {
        Vector3 dashDir = input.sqrMagnitude > 0.01f ? input.normalized : lastNonZeroInput;
        if (dashDir.sqrMagnitude < 0.01f)
        {
            dashDir = transform.forward;
        }

        float dashSpeed = dashDistance / Mathf.Max(0.01f, dashDuration);
        dashVelocity = dashDir * dashSpeed;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        LastDashTime = Time.time;
    }

    public void AddImpulse(Vector3 velocity, float duration)
    {
        impulseVelocity = velocity;
        impulseTimer = duration;
    }

    private void SetHolding(bool value)
    {
        IsHolding = value;
        if (value)
        {
            controller.radius = holdRadius;
            controller.height = normalHeight * holdHeightMultiplier;
            controller.center = new Vector3(normalCenter.x, controller.height * 0.5f, normalCenter.z);
        }
        else
        {
            RestoreNormalCollider();
        }
    }

    private void RestoreNormalCollider()
    {
        IsHolding = false;
        controller.radius = normalRadius;
        controller.height = normalHeight;
        controller.center = normalCenter;
    }

    private bool IsGrounded()
    {
        Vector3 origin = transform.position + controller.center;
        float radius = controller.radius * groundCheckRadius;
        float distance = (controller.height * 0.5f) - controller.radius + groundCheckDistance;

        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, distance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return controller.isGrounded;
    }

    private Vector3 ClampMotionToArena(Vector3 motion)
    {
        if (AC_GameManager.Instance == null || AC_GameManager.Instance.arenaCenter == null)
        {
            return motion;
        }

        Vector3 center = AC_GameManager.Instance.arenaCenter.position;
        Vector3 currentPos = transform.position;
        Vector3 nextPos = currentPos + motion;
        Vector3 flatCenter = new Vector3(center.x, 0f, center.z);
        Vector3 flatCurrent = new Vector3(currentPos.x, 0f, currentPos.z);
        Vector3 flatNext = new Vector3(nextPos.x, 0f, nextPos.z);
        Vector3 dirCurrent = flatCurrent - flatCenter;
        Vector3 dirFromCenter = flatNext - flatCenter;

        float maxRadius = Mathf.Max(0.5f, AC_GameManager.Instance.CurrentArenaRadius);
        if (dirCurrent.magnitude > maxRadius + 0.05f)
        {
            dirCurrent = dirCurrent.normalized;
            Vector3 safePos = flatCenter + dirCurrent * (maxRadius - 0.1f);
            safePos.y = currentPos.y;
            return safePos - currentPos;
        }

        if (dirFromCenter.magnitude > maxRadius)
        {
            dirFromCenter.Normalize();
            Vector3 clampedNext = flatCenter + dirFromCenter * maxRadius;
            clampedNext.y = nextPos.y;
            motion = clampedNext - currentPos;
        }

        return motion;
    }

    private void CheckFall()
    {
        if (AC_GameManager.Instance == null)
        {
            return;
        }

        if (Time.time - lastRespawnTime < respawnProtectionDuration)
        {
            return;
        }

        Transform center = AC_GameManager.Instance.arenaCenter;
        Vector3 c = center != null ? center.position : Vector3.zero;
        Vector3 flatPlayer = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatCenter = new Vector3(c.x, 0f, c.z);
        float dist = Vector3.Distance(flatPlayer, flatCenter);

        if (transform.position.y < fallY || dist > AC_GameManager.Instance.CurrentArenaRadius + 0.35f)
        {
            AC_GameManager.Instance.PlayerFell(this);
        }
    }

    public void ClearMovementState()
    {
        dashTimer = 0f;
        impulseTimer = 0f;
        verticalVelocity = Vector3.zero;
        RestoreNormalCollider();
    }

    public void Respawn(Vector3 position, Quaternion rotation)
    {
        ClampRespawnPosition(ref position);

        if (spawnerHelper == null)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
        }
        else
        {
            spawnerHelper.ForceRespawn(position, rotation);
        }

        ClearMovementState();
        lastRespawnTime = Time.time;
    }

    private void ClampRespawnPosition(ref Vector3 position)
    {
        if (AC_GameManager.Instance == null)
        {
            return;
        }

        Transform centerTransform = AC_GameManager.Instance.arenaCenter;
        if (centerTransform != null)
        {
            Vector3 center = centerTransform.position;
            Vector3 flatCenter = new Vector3(center.x, 0f, center.z);
            Vector3 flatPos = new Vector3(position.x, 0f, position.z);
            float maxRadius = Mathf.Max(0.5f, AC_GameManager.Instance.CurrentArenaRadius - 0.5f);

            if (Vector3.Distance(flatPos, flatCenter) > maxRadius)
            {
                Vector3 dir = (flatPos - flatCenter).normalized;
                flatPos = flatCenter + dir * maxRadius;
                position.x = flatPos.x;
                position.z = flatPos.z;
            }
        }

        position.y = Mathf.Max(position.y, fallY + 1f);
    }

    private bool IsActionPressed(KeyCode primary, KeyCode secondary)
    {
        return (primary != KeyCode.None && Input.GetKeyDown(primary)) ||
               (secondary != KeyCode.None && Input.GetKeyDown(secondary));
    }

    private bool IsActionHeld(KeyCode primary, KeyCode secondary)
    {
        return (primary != KeyCode.None && Input.GetKey(primary)) ||
               (secondary != KeyCode.None && Input.GetKey(secondary));
    }
}
