using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AC_Spawner : MonoBehaviour
{
    [Header("Respawn")]
    public Transform respawn;
    public float posicionEjeY = -20f;
    [Header("Opcional: auto-respawn fuera del mapa si se usa directo")]
    public bool autoRespawnOnFall;

    private CharacterController characterController;
    private AC_PlayerController owner;
    private Rigidbody rb;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        owner = GetComponent<AC_PlayerController>();
        rb = GetComponent<Rigidbody>();
    }

    public void SetRespawnPoint(Transform point)
    {
        if (point == null)
        {
            Debug.LogWarning("[AC_Spawner] SetRespawnPoint recibió null.");
        }

        respawn = point;
    }

    public bool NeedsRespawnFromFall()
    {
        return transform.position.y < posicionEjeY;
    }

    public void ResetToFallback()
    {
        if (respawn == null) return;
        ForceRespawn(respawn.position, respawn.rotation);
    }

    public void ForceRespawn(Vector3 position, Quaternion rotation)
    {
        if (characterController == null) return;

        characterController.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        characterController.enabled = true;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (owner != null)
        {
            owner.ClearMovementState();
        }
    }

    private void Update()
    {
        if (!autoRespawnOnFall) return;
        if (NeedsRespawnFromFall())
        {
            ResetToFallback();
        }
    }
}
