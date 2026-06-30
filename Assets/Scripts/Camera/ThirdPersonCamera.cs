using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform del personaje a seguir")]
    public Transform target;

    [Header("Offset")]
    [Tooltip("Offset local respecto a la rotacion del personaje (ej: (0, 2.5, -3.5))")]
    public Vector3 offsetLocal = new Vector3(0f, 2.5f, -3.5f);

    [Header("Smoothing")]
    [Tooltip("Tiempo de suavizado para la posicion. Valores mas bajos = mas rapido")]
    public float positionSmoothTime = 0.12f;

    [Tooltip("Tiempo de suavizado para la rotacion. Normalmente mas rapido que la posicion")]
    public float rotationSmoothTime = 0.08f;

    [Header("Collision")]
    [Tooltip("Si es true, la camara har un raycast desde el target hacia la posicion objetivo y se ajustara si hay obstaculos")]
    public bool enableCollision = true;

    [Tooltip("Radio de la esfera para el SphereCast de colision")]
    public float sphereRadius = 0.25f;

    [Tooltip("Distancia minima a la que puede quedar la camara si hay una pared")]
    public float minDistance = 1f;

    [Tooltip("Margen de seguridad contra la pared")]
    public float collisionOffset = 0.1f;

    [Tooltip("Capas con las que la camara puede colisionar")]
    public LayerMask collisionMask = ~0;

    private Camera cam;
    private Vector3 positionVelocity;
    private float currentDistanceFactor = 1f;
    private float distanceVelocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position;
        Quaternion targetRotation = target.rotation;

        Vector3 rawOffset = targetRotation * offsetLocal;

        float desiredDistanceFactor = 1f;
        if (enableCollision)
        {
            float rawDistance = rawOffset.magnitude;
            if (rawDistance > 0.001f && Physics.SphereCast(targetPosition, sphereRadius, rawOffset.normalized, out RaycastHit hit, rawDistance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float safeDistance = hit.distance - collisionOffset - sphereRadius;
                safeDistance = Mathf.Clamp(safeDistance, minDistance, rawDistance);
                desiredDistanceFactor = safeDistance / rawDistance;
            }
        }

        currentDistanceFactor = Mathf.SmoothDamp(currentDistanceFactor, desiredDistanceFactor, ref distanceVelocity, positionSmoothTime);

        Vector3 finalDesiredPosition = targetPosition + rawOffset * currentDistanceFactor;
        transform.position = Vector3.SmoothDamp(transform.position, finalDesiredPosition, ref positionVelocity, positionSmoothTime);

        Vector3 lookDirection = targetPosition - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.unscaledDeltaTime / Mathf.Max(rotationSmoothTime, 0.001f));
        }
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 rawOffset = target.rotation * offsetLocal;
        transform.position = target.position + rawOffset;

        Vector3 lookDirection = target.position - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        positionVelocity = Vector3.zero;
        distanceVelocity = 0f;
        currentDistanceFactor = 1f;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
    }

    private void OnDrawGizmos()
    {
        if (target == null)
        {
            return;
        }

        Vector3 rawOffset = target.rotation * offsetLocal;
        Vector3 desiredPosition = target.position + rawOffset;
        Vector3 origin = target.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(desiredPosition, 0.15f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, desiredPosition);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}
