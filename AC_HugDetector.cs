using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AC_PlayerController))]
public class AC_HugDetector : MonoBehaviour
{
    [Header("Volumen de abrazo")]
    public Vector3 localBoxCenter = new Vector3(0f, 1f, 0.85f);
    public Vector3 halfExtents = new Vector3(0.65f, 0.65f, 0.55f);
    public float activeTime = 0.22f;
    public LayerMask playerMask;

    [Header("Raycast de depuración / bloqueo")]
    public float chestHeight = 1f;
    public LayerMask blockersMask;
    public bool useLineOfSightRaycast = true;

    [Header("Visual opcional")]
    public GameObject volumeVisual;

    private AC_PlayerController owner;
    private Coroutine hugRoutine;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        owner = GetComponent<AC_PlayerController>();
        if (volumeVisual != null) volumeVisual.SetActive(false);
    }

    public void StartHug()
    {
        if (hugRoutine != null) StopCoroutine(hugRoutine);
        hugRoutine = StartCoroutine(HugRoutine());
    }

    private IEnumerator HugRoutine()
    {
        IsActive = true;
        if (volumeVisual != null) volumeVisual.SetActive(true);

        float timer = 0f;
        bool alreadyHit = false;

        while (timer < activeTime)
        {
            if (!alreadyHit)
            {
                AC_PlayerController target = FindTargetInHugVolume();
                if (target != null && AC_GameManager.Instance != null)
                {
                    alreadyHit = true;
                    AC_GameManager.Instance.RegisterHugAttempt(owner, target, Time.time);
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        IsActive = false;
        if (volumeVisual != null) volumeVisual.SetActive(false);
        hugRoutine = null;
    }

    public AC_PlayerController FindTargetInHugVolume()
    {
        Vector3 center = transform.TransformPoint(localBoxCenter);
        Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation, playerMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits.Length; i++)
        {
            AC_PlayerController candidate = hits[i].GetComponentInParent<AC_PlayerController>();
            // FIX: verificar por referencia de objeto, no solo por componente
            if (candidate == null || candidate == owner) continue;
            // FIX: verificar también por playerId como doble seguro
            if (candidate.playerId == owner.playerId) continue;
            if (!HasLineOfSight(candidate)) continue;
            return candidate;
        }

        return null;
    }

    public bool IsTargetCloseForChargedHug(AC_PlayerController target, float maxDistance)
    {
        if (target == null) return false;
        Vector3 a = new Vector3(owner.transform.position.x, 0f, owner.transform.position.z);
        Vector3 b = new Vector3(target.transform.position.x, 0f, target.transform.position.z);
        return Vector3.Distance(a, b) <= maxDistance;
    }

    private bool HasLineOfSight(AC_PlayerController target)
    {
        if (!useLineOfSightRaycast || blockersMask.value == 0) return true;

        Vector3 origin = transform.position + Vector3.up * chestHeight;
        Vector3 targetPoint = target.transform.position + Vector3.up * chestHeight;
        Vector3 direction = targetPoint - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f) return true;
        direction /= distance;

        RaycastHit hit;
        bool blocked = Physics.Raycast(origin, direction, out hit, distance, blockersMask, QueryTriggerInteraction.Ignore);
        // Debug.DrawRay: referencia directa al PDF de Raycast del profe
        Debug.DrawRay(origin, direction * distance, blocked ? Color.red : Color.green, 0.25f);
        return !blocked;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.TransformPoint(localBoxCenter);
        Gizmos.color = Color.magenta;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
        Gizmos.matrix = oldMatrix;
    }
}
