using System.Collections;
using UnityEngine;

public class AC_HugDetector : MonoBehaviour
{
    public Vector3 localBoxCenter = new Vector3(0f, 1f, 0.85f);
    public Vector3 halfExtents = new Vector3(0.65f, 0.65f, 0.55f);
    public float activeTime = 0.22f;
    public float hitWindowStart = 0.65f;
    public float hitWindowEnd = 0.95f;
    public LayerMask playerMask;

    public float chestHeight = 1f;
    public LayerMask blockersMask;
    public bool useLineOfSightRaycast = true;

    public GameObject volumeVisual;

    private AC_PlayerController owner;
    private Coroutine hugRoutine;

    public bool IsActive;

    private void Awake()
    {
        owner = GetComponent<AC_PlayerController>();
        ValidatePlayerMask();
        EnsureVolumeVisual();
        ConfigureVolumeVisualColliders();
    }

    private void ValidatePlayerMask()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            playerMask = 1 << playerLayer;
            return;
        }

        int ownerLayer = owner != null ? owner.gameObject.layer : -1;
        if (ownerLayer >= 0)
        {
            playerMask = 1 << ownerLayer;
            return;
        }

        playerMask = ~0;
    }

    private void EnsureVolumeVisual()
    {
        if (volumeVisual != null)
        {
            volumeVisual.SetActive(false);
            return;
        }

        Transform preferred = transform.Find("HugVolumeVisual");
        if (preferred == null)
        {
            preferred = transform.Find("HugVolumeVIsual");
        }

        if (preferred != null)
        {
            volumeVisual = preferred.gameObject;
            volumeVisual.SetActive(false);
            return;
        }

        foreach (Transform child in transform)
        {
            if (child.name.Contains("HugVolume"))
            {
                volumeVisual = child.gameObject;
                volumeVisual.SetActive(false);
                return;
            }
        }
    }

    private void ConfigureVolumeVisualColliders()
    {
        if (volumeVisual == null)
        {
            return;
        }

        Collider[] colliders = volumeVisual.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    public void StartHug()
    {
        if (owner != null && owner.IsBlocking)
        {
            return;
        }

        if (hugRoutine != null)
        {
            StopCoroutine(hugRoutine);
        }

        hugRoutine = StartCoroutine(HugRoutine());
    }

    public void CancelHug()
    {
        if (hugRoutine != null)
        {
            StopCoroutine(hugRoutine);
            hugRoutine = null;
        }

        IsActive = false;
        if (volumeVisual != null)
        {
            volumeVisual.SetActive(false);
        }
    }

    private IEnumerator HugRoutine()
    {
        IsActive = true;
        if (volumeVisual != null)
        {
            volumeVisual.SetActive(true);
        }

        float timer = 0f;
        bool alreadyHit = false;

        while (timer < activeTime)
        {
            float normalized;
            if (activeTime > 0f)
            {
                normalized = timer / activeTime;
            }
            else
            {
                normalized = 1f;
            }

            if (!alreadyHit && normalized >= hitWindowStart && normalized <= hitWindowEnd)
            {
                AC_PlayerController target = FindTargetInHugVolume();
                if (target != null && AC_GameManager.Instance != null)
                {
                    alreadyHit = true;
                    AC_GameManager.Instance.RegisterHugAttempt(owner, target, owner.LastHugPressTime);
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        IsActive = false;
        if (volumeVisual != null)
        {
            volumeVisual.SetActive(false);
        }

        hugRoutine = null;
    }

    public AC_PlayerController FindTargetInHugVolume()
    {
        Vector3 center = transform.TransformPoint(localBoxCenter);
        Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation, playerMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits.Length; i++)
        {
            AC_PlayerController candidate = hits[i].GetComponentInParent<AC_PlayerController>();
            if (candidate == null || candidate == owner)
            {
                continue;
            }

            if (candidate.playerId == owner.playerId)
            {
                continue;
            }

            if (candidate.IsBlocking)
            {
                continue;
            }

            if (!HasLineOfSight(candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    public bool IsTargetCloseForChargedHug(AC_PlayerController target, float maxDistance)
    {
        if (target == null || owner == null)
        {
            return false;
        }

        Vector3 a = new Vector3(owner.transform.position.x, 0f, owner.transform.position.z);
        Vector3 b = new Vector3(target.transform.position.x, 0f, target.transform.position.z);
        return Vector3.Distance(a, b) <= maxDistance;
    }

    private bool HasLineOfSight(AC_PlayerController target)
    {
        if (!useLineOfSightRaycast || blockersMask.value == 0)
        {
            return true;
        }

        Vector3 origin = transform.position + Vector3.up * chestHeight;
        Vector3 targetPoint = target.transform.position + Vector3.up * chestHeight;
        Vector3 direction = targetPoint - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
        {
            return true;
        }

        direction = direction / distance;
        bool blocked = Physics.Raycast(origin, direction, out RaycastHit hit, distance, blockersMask, QueryTriggerInteraction.Ignore);
        Debug.DrawRay(origin, direction * distance, blocked ? Color.red : Color.green, 0.25f);
        return !blocked;
    }
}
