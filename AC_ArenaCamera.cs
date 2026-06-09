using UnityEngine;

/// <summary>
/// Cámara de arena para prototipo MVP.
/// Modo 1 (default): cámara fija elevada mirando el centro — sin setup.
/// Modo 2: cámara que hace zoom out cuando los jugadores se alejan.
/// Agregar este script a la Main Camera. No requiere referencias obligatorias.
/// </summary>
public class AC_ArenaCamera : MonoBehaviour
{
    [Header("Modo")]
    public bool dynamicZoom = true;

    [Header("Posición fija (si dynamicZoom = false)")]
    public float fixedHeight = 14f;
    public float fixedDistance = 0f; // 0 = directamente arriba del centro

    [Header("Zoom dinámico")]
    public float minHeight = 10f;
    public float maxHeight = 22f;
    public float zoomMargin = 3f;    // espacio extra alrededor de los jugadores
    public float zoomSmoothSpeed = 3f;

    [Header("Referencias (opcionales — se buscan automáticamente)")]
    public Transform arenaCenter;
    public Transform player1;
    public Transform player2;

    private Vector3 targetPosition;
    private float currentHeight;

    private void Start()
    {
        // Buscar referencias automáticamente si no se asignaron
        if (arenaCenter == null && AC_GameManager.Instance != null)
            arenaCenter = AC_GameManager.Instance.arenaCenter;

        if (player1 == null && AC_GameManager.Instance != null && AC_GameManager.Instance.player1 != null)
            player1 = AC_GameManager.Instance.player1.transform;

        if (player2 == null && AC_GameManager.Instance != null && AC_GameManager.Instance.player2 != null)
            player2 = AC_GameManager.Instance.player2.transform;

        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        currentHeight = dynamicZoom ? minHeight : fixedHeight;
        transform.position = center + Vector3.up * currentHeight;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void LateUpdate()
    {
        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;

        if (!dynamicZoom || player1 == null || player2 == null)
        {
            // Cámara fija directamente arriba del centro
            transform.position = center + Vector3.up * fixedHeight;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            return;
        }

        // Calcular distancia máxima de cualquier jugador al centro
        float dist1 = Vector3.Distance(
            new Vector3(player1.position.x, 0f, player1.position.z),
            new Vector3(center.x, 0f, center.z));
        float dist2 = Vector3.Distance(
            new Vector3(player2.position.x, 0f, player2.position.z),
            new Vector3(center.x, 0f, center.z));

        float maxDist = Mathf.Max(dist1, dist2) + zoomMargin;

        // Mapear distancia a altura: más lejos → más alto
        float targetHeight = Mathf.Lerp(minHeight, maxHeight,
            Mathf.InverseLerp(0f, AC_GameManager.Instance != null ? AC_GameManager.Instance.baseArenaRadius : 7.5f, maxDist));

        targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * zoomSmoothSpeed);

        transform.position = Vector3.Lerp(transform.position, center + Vector3.up * currentHeight, Time.deltaTime * zoomSmoothSpeed);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
