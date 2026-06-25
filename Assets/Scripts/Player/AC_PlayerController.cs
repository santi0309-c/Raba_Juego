using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class AC_PlayerController : MonoBehaviour
{
    [Header("Identidad")]
    public int playerId = 1;
    public string displayName = "Jugador 1";

    [Header("Configuración (opcional — pisa los defaults)")]
    public AC_PlayerConfig config;

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
    public KeyCode jumpKey = KeyCode.Q;

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

    [Header("Bloqueo")]
    public float holdRadius = 0.18f;
    public float holdHeightMultiplier = 0.75f;
    public Renderer[] bodyRenderers;
    public Color blockingColor = new Color(1f, 0.84f, 0.35f, 1f);

    private CharacterController controller;
    private AC_HugDetector hugDetector;
    private Rigidbody rigidBody;
    private Collider fallbackCollider;
    private AC_Spawner spawnerHelper;
    private Animator animator;
    private Transform modelTransform;
    private Coroutine hugRoutine;

    [Header("Animación")]
    public bool enableAnimator = true;
    public float hugSquashIntensity = 0.3f;
    public float hugSquashDuration = 0.4f;

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
    private Color[] originalColors;

    public bool IsBlocking { get; private set; }
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
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        modelTransform = transform.Find("Model");

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
        AplicarConfigSiExiste();
        NormalizePhysicsComponents();
        CacheBodyRenderers();
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
            if (jumpKey == KeyCode.Q || jumpKey == KeyCode.None) jumpKey = KeyCode.Keypad0;
            displayName = "Jugador 2";
        }
        else
        {
            if (jumpKey == KeyCode.None)
            {
                jumpKey = KeyCode.Q;
            }
        }
    }

    private void AplicarConfigSiExiste()
    {
        if (config == null) return;

        displayName = config.nombreMostrado;

        // Teclas
        upKey = config.teclaArriba;
        downKey = config.teclaAbajo;
        leftKey = config.teclaIzquierda;
        rightKey = config.teclaDerecha;
        hugKey = config.teclaAbrazo;
        dashKey = config.teclaDash;
        holdKey = config.teclaAguantar;
        jumpKey = config.teclaSaltar;

        // Movimiento
        moveSpeed = config.velocidadMovimiento;
        rotationSpeed = config.velocidadRotacion;
        gravity = config.gravedad;
        fallY = config.caidaY;

        // Salto
        jumpForce = config.fuerzaSalto;
        groundCheckDistance = config.distanciaCheckSuelo;
        groundCheckRadius = config.radioCheckSuelo;

        // Dash
        dashDistance = config.distanciaDash;
        dashDuration = config.duracionDash;
        dashCooldown = config.enfriamientoDash;

        // Aguantar
        holdRadius = config.radioAguantar;
        holdHeightMultiplier = config.multiplicadorAlturaAguantar;

        Debug.Log("[AC_PlayerController] Config aplicada: " + displayName);
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

    private void CacheBodyRenderers()
    {
        if (bodyRenderers == null || bodyRenderers.Length == 0)
        {
            bodyRenderers = GetComponentsInChildren<Renderer>();
        }

        originalColors = new Color[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] != null && bodyRenderers[i].material != null)
            {
                originalColors[i] = bodyRenderers[i].material.color;
            }
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
        bool wantsBlock = IsActionHeld(holdKey, alternateHoldKey) && !IsDashing && !HasMovementInput();
        SetBlocking(wantsBlock);

        if (CanTryHug() && IsActionPressed(hugKey, alternateHugKey))
        {
            LastHugPressTime = Time.time;
            hugDetector.StartHug();
            if (enableAnimator && modelTransform != null)
            {
                if (hugRoutine != null) StopCoroutine(hugRoutine);
                hugRoutine = StartCoroutine(HugSquashEffect());
            }
        }

        dashCooldownTimer -= Time.deltaTime;
        if (CanTryDash() && IsActionPressed(dashKey, alternateDashKey) && dashCooldownTimer <= 0f)
        {
            StartDash(input);
        }

        if (CanTryJump() && Input.GetKeyDown(jumpKey) && IsGrounded())
        {
            verticalVelocity.y = jumpForce;
        }

        MovePlayer(input);
        UpdateAnimatorParams();
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
        return mainCamera != null ? mainCamera.transform : null;
    }

    private void MovePlayer(Vector3 input)
    {
        Vector3 movementInput = input;
        Vector3 horizontal = Vector3.zero;

        if (tankControls)
        {
            if (Mathf.Abs(turnInput) > 0.01f && !IsBlocking)
            {
                transform.Rotate(Vector3.up, turnInput * turnSpeedDegrees * Time.deltaTime, Space.World);
            }

            movementInput = Mathf.Abs(forwardInput) > 0.01f ? transform.forward * forwardInput : Vector3.zero;
            if (movementInput.sqrMagnitude > 0.01f)
            {
                lastNonZeroInput = movementInput.normalized;
            }
        }

        if (!IsBlocking)
        {
            horizontal = movementInput * moveSpeed;
        }

        if (!tankControls && movementInput.sqrMagnitude > 0.01f && !IsBlocking)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementInput, Vector3.up);
            float maxDegreesDelta = Mathf.Max(90f, rotationSpeed * 90f) * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxDegreesDelta);
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
        controller.Move(motion);
    }

    private void StartDash(Vector3 input)
    {
        Vector3 dashDirection = input.sqrMagnitude > 0.01f ? input.normalized : lastNonZeroInput;
        if (dashDirection.sqrMagnitude < 0.01f)
        {
            dashDirection = transform.forward;
        }

        float dashSpeed = dashDistance / Mathf.Max(0.01f, dashDuration);
        dashVelocity = dashDirection * dashSpeed;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        LastDashTime = Time.time;
    }

    public void AddImpulse(Vector3 velocity, float duration)
    {
        impulseVelocity = velocity;
        impulseTimer = duration;
    }

    private void SetBlocking(bool value)
    {
        IsBlocking = value;
        if (value)
        {
            controller.radius = holdRadius;
            controller.height = normalHeight * holdHeightMultiplier;
            controller.center = new Vector3(normalCenter.x, controller.height * 0.5f, normalCenter.z);
            SetRendererColors(blockingColor);
        }
        else
        {
            RestoreNormalCollider();
        }
    }

    private void RestoreNormalCollider()
    {
        IsBlocking = false;
        controller.radius = normalRadius;
        controller.height = normalHeight;
        controller.center = normalCenter;
        RestoreRendererColors();
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
        Vector3 centerPosition = center != null ? center.position : Vector3.zero;
        Vector3 flatPlayer = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatCenter = new Vector3(centerPosition.x, 0f, centerPosition.z);
        float distance = Vector3.Distance(flatPlayer, flatCenter);

        bool fellByHeight = transform.position.y < fallY;
        // 0.05f de margen mínimo — el jugador cae apenas sale del borde visual
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
        hugDetector?.CancelHug();
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
            float maxRadius = Mathf.Max(0.5f, AC_GameManager.Instance.CurrentArenaRadius - 0.75f);

            if (Vector3.Distance(flatPosition, flatCenter) > maxRadius)
            {
                Vector3 direction = (flatPosition - flatCenter).normalized;
                flatPosition = flatCenter + direction * maxRadius;
                position.x = flatPosition.x;
                position.z = flatPosition.z;
            }
        }

        position.y = Mathf.Max(position.y, fallY + 1f);
    }

    private bool HasMovementInput()
    {
        return Mathf.Abs(forwardInput) > 0.01f || Mathf.Abs(turnInput) > 0.01f;
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
        return jumpKey != KeyCode.None && !IsBlocking;
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

    private void SetRendererColors(Color targetColor)
    {
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] != null && bodyRenderers[i].material != null)
            {
                bodyRenderers[i].material.color = targetColor;
            }
        }
    }

    private void RestoreRendererColors()
    {
        if (originalColors == null)
        {
            return;
        }

        for (int i = 0; i < bodyRenderers.Length && i < originalColors.Length; i++)
        {
            if (bodyRenderers[i] != null && bodyRenderers[i].material != null)
            {
                bodyRenderers[i].material.color = originalColors[i];
            }
        }
    }

    private void UpdateAnimatorParams()
    {
        if (!enableAnimator || animator == null) return;

        float speed = controller.velocity.magnitude / moveSpeed;
        speed = Mathf.Clamp01(speed);
        animator.SetFloat("Speed", speed);

        bool isJumping = !IsGrounded() && verticalVelocity.y > 0f;
        animator.SetFloat("IsJumping", isJumping ? 1f : 0f);

        animator.SetFloat("IsGrounded", IsGrounded() ? 1f : 0f);
    }

    private System.Collections.IEnumerator HugSquashEffect()
    {
        Vector3 originalScale = modelTransform.localScale;
        float halfDuration = hugSquashDuration * 0.5f;
        float elapsed = 0f;

        // Squash down (scale Y down, X/Z up)
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            float sx = Mathf.Lerp(1f, 1f + hugSquashIntensity, t);
            float sy = Mathf.Lerp(1f, 1f - hugSquashIntensity, t);
            float sz = Mathf.Lerp(1f, 1f + hugSquashIntensity, t);
            modelTransform.localScale = new Vector3(originalScale.x * sx, originalScale.y * sy, originalScale.z * sz);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Stretch back
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            float sx = Mathf.Lerp(1f + hugSquashIntensity, 1f, t);
            float sy = Mathf.Lerp(1f - hugSquashIntensity, 1f, t);
            float sz = Mathf.Lerp(1f + hugSquashIntensity, 1f, t);
            modelTransform.localScale = new Vector3(originalScale.x * sx, originalScale.y * sy, originalScale.z * sz);
            elapsed += Time.deltaTime;
            yield return null;
        }

        modelTransform.localScale = originalScale;
        hugRoutine = null;
    }
}
