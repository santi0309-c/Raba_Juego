using UnityEngine;

public class AC_PlayerController : MonoBehaviour
{
    public int playerId = 1;
    public string displayName = "Jugador 1";

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
    public KeyCode jumpKey = KeyCode.Q;
    public KeyCode alternateJumpKey = KeyCode.None;

    public float moveSpeed = 5.5f;
    public float rotationSpeed = 12f;
    public float gravity = -25f;
    public float fallY = -4f;

    public bool moveRelativeToCamera = true;
    public Transform movementCamera;

    public float jumpForce = 8f;
    public float groundCheckDistance = 0.15f;
    public float groundCheckRadius = 0.25f;
    public float fallMultiplier = 2.5f;
    public LayerMask groundMask;
    public Transform groundCheck;

    public float dashDistance = 3f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 4f;

    public float holdRadius = 0.18f;
    public float holdHeightMultiplier = 0.75f;
    public Vector3 blockModelOffset = Vector3.zero;

    public bool enableAnimator = true;

    private CharacterController controller;
    private AC_HugDetector hugDetector;
    private AC_Spawner spawnerHelper;
    private Animator animator;
    private Transform modelTransform;

    private float normalRadius;
    private float normalHeight;
    private Vector3 normalCenter;
    private Vector3 normalModelLocalPosition;
    private Vector3 verticalVelocity;
    private Vector3 dashVelocity;
    private float dashTimer;
    private Vector3 impulseVelocity;
    private float impulseTimer;
    private Vector3 lastNonZeroInput = Vector3.forward;
    private float lastRespawnTime = -999f;
    private float respawnProtectionDuration = 0.5f;

    public bool IsBlocking;
    public bool IsDashing;
    public float LastDashTime = -999f;
    public float LastHugPressTime = -999f;
    public float DashCooldownRemaining;
    public bool IsDashReady;

    public CharacterController CharacterController
    {
        get { return controller; }
    }

    public AC_HugDetector HugDetector
    {
        get { return hugDetector; }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        hugDetector = GetComponent<AC_HugDetector>();
        spawnerHelper = GetComponent<AC_Spawner>();
        modelTransform = transform.Find("Model");
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null && modelTransform != null) animator = modelTransform.GetComponent<Animator>();

        if (groundCheck == null)
        {
            groundCheck = transform.Find("GroundCheck");
            if (groundCheck == null)
            {
                GameObject groundCheckGO = new GameObject("GroundCheck");
                groundCheckGO.transform.SetParent(transform, false);
                groundCheckGO.transform.localPosition = new Vector3(0f, -controller.height * 0.5f + controller.center.y, 0f);
                groundCheck = groundCheckGO.transform;
            }
        }

        controller.height = 5f;
        controller.center = new Vector3(0f, -0.14f, 0f);
        controller.radius = 0.45f;
        controller.skinWidth = 0.08f;
        controller.stepOffset = 0.3f;
        controller.minMoveDistance = 0.001f;
        controller.slopeLimit = 45f;

        if (spawnerHelper == null)
        {
            spawnerHelper = gameObject.AddComponent<AC_Spawner>();
        }

        normalRadius = controller.radius;
        normalHeight = controller.height;
        normalCenter = controller.center;
        if (modelTransform != null)
        {
            normalModelLocalPosition = modelTransform.localPosition;
        }

        spawnerHelper.enabled = true;
        spawnerHelper.autoRespawnOnFall = false;
        spawnerHelper.posicionEjeY = fallY;

        ConfigureDefaultBindings();
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
            if (jumpKey == KeyCode.Q || jumpKey == KeyCode.None) jumpKey = KeyCode.End;
            displayName = "Jugador 2";
        }
        else
        {
            if (jumpKey == KeyCode.None)
            {
                jumpKey = KeyCode.Q;
            }
        }

        moveRelativeToCamera = true;
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
        if (groundCheck != null)
        {
            Vector3 localPos = groundCheck.localPosition;
            localPos.y = -controller.height * 0.5f + controller.center.y;
            groundCheck.localPosition = localPos;
        }

        if (AC_GameManager.Instance != null && !AC_GameManager.Instance.IsRoundActive)
        {
            RestoreNormalCollider();
            return;
        }

        Vector3 input = ReadInputDirection();
        bool wantsBlock = IsActionHeld(holdKey, alternateHoldKey) && !IsDashing && !HasMovementInput();
        SetBlocking(wantsBlock);

        if (CanTryHug() && IsActionPressed(hugKey, alternateHugKey))
        {
            LastHugPressTime = Time.time;
            hugDetector.StartHug();
            if (enableAnimator && animator != null)
            {
                animator.SetTrigger("Hug");
            }
        }

        UpdateDashReady();
        if (CanTryDash() && IsActionPressed(dashKey, alternateDashKey) && IsDashReady)
        {
            StartDash(input);
            if (enableAnimator && animator != null)
            {
                animator.SetTrigger("Dash");
            }
        }

        if (CanTryJump() && IsActionPressed(jumpKey, alternateJumpKey) && IsGrounded())
        {
            verticalVelocity.y = jumpForce;
            if (enableAnimator && animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }

        MovePlayer(input);
        UpdateAnimatorParams();
        CheckFall();
    }

    private void UpdateDashReady()
    {
        float timeSinceLastDash = Time.time - LastDashTime;
        if (timeSinceLastDash >= dashCooldown)
        {
            IsDashReady = true;
            DashCooldownRemaining = 0f;
        }
        else
        {
            IsDashReady = false;
            DashCooldownRemaining = dashCooldown - timeSinceLastDash;
        }
    }

    private Vector3 ReadInputDirection()
    {
        float x = 0f;
        float z = 0f;
        if (Input.GetKey(leftKey)) x -= 1f;
        if (Input.GetKey(rightKey)) x += 1f;
        if (Input.GetKey(downKey)) z -= 1f;
        if (Input.GetKey(upKey)) z += 1f;

        Vector3 direction = new Vector3(x, 0f, z);
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        if (moveRelativeToCamera && direction.sqrMagnitude > 0.01f)
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

                direction = cameraRight * direction.x + cameraForward * direction.z;
                if (direction.sqrMagnitude > 1f)
                {
                    direction.Normalize();
                }
            }
        }

        if (direction.sqrMagnitude > 0.01f)
        {
            lastNonZeroInput = direction.normalized;
        }

        return direction;
    }

    private Transform ResolveMovementCamera()
    {
        if (movementCamera != null)
        {
            return movementCamera;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera.transform;
        }

        return null;
    }

    private void MovePlayer(Vector3 input)
    {
        Vector3 movementInput = input;
        Vector3 horizontal = Vector3.zero;

        if (!IsBlocking)
        {
            horizontal = movementInput * moveSpeed;
        }

        if (movementInput.sqrMagnitude > 0.01f && !IsBlocking)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementInput, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (dashTimer > 0f)
        {
            horizontal += dashVelocity;
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                dashTimer = 0f;
                dashVelocity = Vector3.zero;
                IsDashing = false;
            }
        }

        if (impulseTimer > 0f)
        {
            horizontal += impulseVelocity;
            impulseTimer -= Time.deltaTime;
            if (impulseTimer <= 0f)
            {
                impulseTimer = 0f;
                impulseVelocity = Vector3.zero;
            }
        }

        bool grounded = IsGrounded();
        if (grounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        float effectiveGravity = gravity;
        if (!grounded && verticalVelocity.y < 0f)
        {
            effectiveGravity = gravity * fallMultiplier;
        }

        verticalVelocity.y += effectiveGravity * Time.deltaTime;
        Vector3 motion = (horizontal + verticalVelocity) * Time.deltaTime;
        controller.Move(motion);
    }

    private void StartDash(Vector3 input)
    {
        Vector3 dashDirection;
        if (input.sqrMagnitude > 0.01f)
        {
            dashDirection = input.normalized;
        }
        else
        {
            dashDirection = lastNonZeroInput;
        }

        if (dashDirection.sqrMagnitude < 0.01f)
        {
            dashDirection = transform.forward;
        }

        float dashSpeed = dashDistance / dashDuration;
        if (dashDuration < 0.01f)
        {
            dashSpeed = dashDistance / 0.01f;
        }

        dashVelocity = dashDirection * dashSpeed;
        dashTimer = dashDuration;
        IsDashing = true;
        LastDashTime = Time.time;
    }

    public void AddImpulse(Vector3 velocity, float duration)
    {
        impulseVelocity = velocity;
        impulseTimer = duration;
    }

    private void SetBlocking(bool value)
    {
        if (IsBlocking == value) return;
        IsBlocking = value;

        if (value)
        {
            controller.radius = holdRadius;
            float newHeight = normalHeight * holdHeightMultiplier;
            float heightDelta = normalHeight - newHeight;
            controller.height = newHeight;
            controller.center = normalCenter - Vector3.up * (heightDelta * 0.5f);

            ApplyBlockModelOffset(true);
        }
        else
        {
            RestoreNormalCollider();
            ApplyBlockModelOffset(false);
        }
    }

    private void ApplyBlockModelOffset(bool enable)
    {
        if (modelTransform == null) return;

        Vector3 target;
        if (enable)
        {
            target = normalModelLocalPosition + blockModelOffset;
        }
        else
        {
            target = normalModelLocalPosition;
        }

        modelTransform.localPosition = target;
    }

    private void RestoreNormalCollider()
    {
        IsBlocking = false;
        if (controller == null) return;
        controller.radius = normalRadius;
        controller.height = normalHeight;
        controller.center = normalCenter;
    }

    private bool IsGrounded()
    {
        if (controller == null) return false;

        Vector3 origin;
        float radius = controller.radius * groundCheckRadius;
        float distance;

        if (groundCheck != null)
        {
            origin = groundCheck.position;
            distance = groundCheckDistance;
            if (distance < 0.05f)
            {
                distance = 0.05f;
            }
        }
        else
        {
            origin = transform.position + controller.center;
            distance = (controller.height * 0.5f) - controller.radius + groundCheckDistance;
        }

        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit sphereHit, distance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit rayHit, distance + radius, groundMask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return controller.isGrounded;
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

        Vector3 footPosition = groundCheck != null ? groundCheck.position : transform.position;

        Transform center = AC_GameManager.Instance.arenaCenter;
        Vector3 centerPosition = center != null ? center.position : Vector3.zero;
        Vector3 flatPlayer = new Vector3(footPosition.x, 0f, footPosition.z);
        Vector3 flatCenter = new Vector3(centerPosition.x, 0f, centerPosition.z);
        float distance = Vector3.Distance(flatPlayer, flatCenter);

        bool fellByHeight = footPosition.y < fallY;
        bool fellByDistance = distance > AC_GameManager.Instance.CurrentArenaRadius + 0.05f;
        if (fellByHeight || fellByDistance)
        {
            AC_GameManager.Instance.PlayerFell(this);
        }
    }

    public void CancelTransientActions()
    {
        dashTimer = 0f;
        impulseTimer = 0f;
        dashVelocity = Vector3.zero;
        impulseVelocity = Vector3.zero;
        IsDashing = false;
        if (hugDetector != null)
        {
            hugDetector.CancelHug();
        }
    }

    public void CancelAllActions()
    {
        CancelTransientActions();
        verticalVelocity = Vector3.zero;
        RestoreNormalCollider();
    }

    public void ClearMovementState()
    {
        CancelAllActions();
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
            Vector3 flatPosition = new Vector3(position.x, 0f, position.z);
            float maxRadius = AC_GameManager.Instance.CurrentArenaRadius - 0.75f;
            if (maxRadius < 0.5f)
            {
                maxRadius = 0.5f;
            }

            if (Vector3.Distance(flatPosition, flatCenter) > maxRadius)
            {
                Vector3 direction = (flatPosition - flatCenter).normalized;
                flatPosition = flatCenter + direction * maxRadius;
                position.x = flatPosition.x;
                position.z = flatPosition.z;
            }
        }

        if (position.y < fallY + 1f)
        {
            position.y = fallY + 1f;
        }
    }

    private bool HasMovementInput()
    {
        return Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f;
    }

    private bool CanTryHug()
    {
        return hugDetector != null && !IsBlocking;
    }

    private bool CanTryDash()
    {
        return !IsBlocking;
    }

    private bool CanTryJump()
    {
        return (jumpKey != KeyCode.None || alternateJumpKey != KeyCode.None) && !IsBlocking;
    }

    private bool IsActionPressed(KeyCode primary, KeyCode secondary)
    {
        if (primary != KeyCode.None && Input.GetKeyDown(primary))
        {
            return true;
        }
        if (secondary != KeyCode.None && Input.GetKeyDown(secondary))
        {
            return true;
        }
        return false;
    }

    private bool IsActionHeld(KeyCode primary, KeyCode secondary)
    {
        if (primary != KeyCode.None && Input.GetKey(primary))
        {
            return true;
        }
        if (secondary != KeyCode.None && Input.GetKey(secondary))
        {
            return true;
        }
        return false;
    }

    private void ResolveAnimator()
    {
        if (animator != null) return;
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null && modelTransform != null) animator = modelTransform.GetComponent<Animator>();
    }

    private void UpdateAnimatorParams()
    {
        if (!enableAnimator) return;
        ResolveAnimator();
        if (animator == null) return;

        float speed = controller.velocity.magnitude / moveSpeed;
        if (speed > 1f) speed = 1f;
        if (speed < 0f) speed = 0f;
        animator.SetFloat("Speed", speed);

        animator.SetBool("Grounded", IsGrounded());
        animator.SetBool("Blocking", IsBlocking);
    }
}
