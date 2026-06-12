using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator anim;
    private CharacterController controller;
    private bool hasJumped = false;
    float velocityZ;
    float velocityX;
    public float acceleration = 2.0f;
    public float desceleration = 2.0f;
    public float MaxWalkSpeed = 1.0f;
    public float MaxRunSpeed = 2.0f;
    public float speedTransitionRate = 3.0f; //Cuán rápido cambia entre correr y caminar

    private float currentMaxSpeed; // Valor suavizado del límite actual

    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        currentMaxSpeed = MaxWalkSpeed;
    }

    void Update()
    {
        bool w = Input.GetKey(KeyCode.W);
        bool s = Input.GetKey(KeyCode.S);
        bool a = Input.GetKey(KeyCode.A);
        bool d = Input.GetKey(KeyCode.D);
        bool shift = Input.GetKey(KeyCode.LeftShift);

        float targetSpeed = shift ? MaxRunSpeed : MaxWalkSpeed;

        //Suaviza el cambio entre caminar <-> correr
        currentMaxSpeed = Mathf.Lerp(currentMaxSpeed, targetSpeed, Time.deltaTime * speedTransitionRate);

        // Movimiento progresivo adelante/atrás
        if (w && velocityZ < currentMaxSpeed)
            velocityZ += Time.deltaTime * acceleration;
        if (s && velocityZ > -currentMaxSpeed)
            velocityZ -= Time.deltaTime * acceleration;

        // Movimiento progresivo izquierda/derecha
        if (a && velocityX > -currentMaxSpeed)
            velocityX -= Time.deltaTime * acceleration;
        if (d && velocityX < currentMaxSpeed)
            velocityX += Time.deltaTime * acceleration;

        // Desaceleración en Z
        if (!w && !s)
        {
            if (velocityZ > 0f)
                velocityZ -= Time.deltaTime * desceleration;
            else if (velocityZ < 0f)
                velocityZ += Time.deltaTime * desceleration;

            if (Mathf.Abs(velocityZ) < 0.1f)
                velocityZ = 0f;
        }

        // Desaceleración en X
        if (!a && !d)
        {
            if (velocityX > 0f)
                velocityX -= Time.deltaTime * desceleration;
            else if (velocityX < 0f)
                velocityX += Time.deltaTime * desceleration;

            if (Mathf.Abs(velocityX) < 0.1f)
                velocityX = 0f;
        }

        //Límite progresivo usando el valor suavizado
        velocityZ = Mathf.Clamp(velocityZ, -currentMaxSpeed, currentMaxSpeed);
        velocityX = Mathf.Clamp(velocityX, -currentMaxSpeed, currentMaxSpeed);


        bool isGrounded = IsGrounded();
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !hasJumped)
        {
            anim.SetTrigger("IsGrounded");
            hasJumped = true;
        }

        // Reset cuando el jugador toca el suelo
        if (isGrounded)
        {
            hasJumped = false;
        }
        anim.SetFloat("YSpeed", velocityZ);
        anim.SetFloat("XSpeed", velocityX);
    }



    private bool IsGrounded()
    {
        float radius = controller.radius * 0.9f;

        return Physics.SphereCast(
            transform.position + controller.center,   // centro
            radius,                                   // radio
            Vector3.down,                             // dirección
            out RaycastHit hit,                       // hit
            (controller.height / 2f) + groundCheckDistance, // distancia
            groundLayer                                // layer del piso
        );
    }

}