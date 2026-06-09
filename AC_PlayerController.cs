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
    public KeyCode dashKey = KeyCode.LeftShift;
    public KeyCode holdKey = KeyCode.LeftControl;

    [Header("Movimiento")]
    public float moveSpeed = 5.5f;
    public float rotationSpeed = 12f;
    public float gravity = -25f;
    public float fallY = -4f;

    [Header("Dash")]
    public float dashDistance = 3f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 4f;

    [Header("Aguantar")]
    public float holdRadius = 0.18f;
    public float holdHeightMultiplier = 0.75f;

    private CharacterController controller;
    private AC_HugDetector hugDetector;

    private float normalRadius;
    private float normalHeight;
    private Vector3 normalCenter;
    private Vector3 verticalVelocity;

    private Vector3 dashVelocity;
    private float dashTimer;
    private float dashCooldownTimer;

    private Vector3 impulseVelocity;
    private float impulseTimer;

    public bool IsHolding { get; private set; }
    public bool IsDashing { get { return dashTimer > 0f; } }
    public float LastDashTime { get; private set; } = -999f;
    public float LastHugPressTime { get; private set; } = -999f;
    public AC_HugDetector HugDetector { get { return hugDetector; } }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        hugDetector = GetComponent<AC_HugDetector>();
        normalRadius = controller.radius;
        normalHeight = controller.height;
        normalCenter = controller.center;
    }

    private void Update()
    {
        if (AC_GameManager.Instance != null && !AC_GameManager.Instance.IsRoundActive)
        {
            RestoreNormalCollider();
            return;
        }

        Vector3 input = ReadInputDirection();
        bool wantsHold = Input.GetKey(holdKey) && input.sqrMagnitude < 0.01f && !IsDashing;
        SetHolding(wantsHold);

        if (Input.GetKeyDown(hugKey) && hugDetector != null && !IsHolding)
        {
            LastHugPressTime = Time.time;
            hugDetector.StartHug();
        }

        dashCooldownTimer -= Time.deltaTime;
        if (Input.GetKeyDown(dashKey) && dashCooldownTimer <= 0f && !IsHolding)
        {
            StartDash(input);
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

        Vector3 dir = new Vector3(x, 0f, z);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        return dir;
    }

    private void MovePlayer(Vector3 input)
    {
        Vector3 horizontal = Vector3.zero;

        if (!IsHolding)
        {
            horizontal = input * moveSpeed;
        }

        if (input.sqrMagnitude > 0.01f && !IsHolding)
        {
            Quaternion targetRot = Quaternion.LookRotation(input, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        if (dashTimer > 0f)
        {
            horizontal += dashVelocity;
            dashTimer -= Time.deltaTime;
        }

        if (impulseTimer > 0f)
        {
            horizontal += impulseVelocity;
            impulseTimer -= Time.deltaTime;
        }

        if (AC_GameManager.Instance != null)
        {
            horizontal += AC_GameManager.Instance.CurrentWindVelocity;
        }

        if (controller.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }
        verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 motion = (horizontal + verticalVelocity) * Time.deltaTime;
        controller.Move(motion);
    }

    private void StartDash(Vector3 input)
    {
        Vector3 dashDir = input.sqrMagnitude > 0.01f ? input.normalized : transform.forward;
        dashVelocity = dashDir * (dashDistance / Mathf.Max(0.01f, dashDuration));
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

    private void CheckFall()
    {
        if (AC_GameManager.Instance == null) return;

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

    public void Respawn(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        controller.enabled = true;

        verticalVelocity = Vector3.zero;
        dashTimer = 0f;
        impulseTimer = 0f;
        RestoreNormalCollider();
    }
}
