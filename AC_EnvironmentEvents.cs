using System.Collections;
using UnityEngine;

public class AC_EnvironmentEvents : MonoBehaviour
{
    public enum AC_EventType
    {
        VientoLateral,
        ArenaSeAchica
    }

    [Header("Evento único por partida")]
    public int minRound = 2;
    public int maxRound = 4;
    public float minDelayInsideRound = 8f;
    public float maxDelayInsideRound = 25f;
    public float minDuration = 15f;
    public float maxDuration = 25f;

    [Header("Viento")]
    public float windStrength = 3f;

    [Header("Arena se achica")]
    public Transform arenaVisual;
    public float shrinkFactor = 0.5f;

    private int chosenRound;
    private bool eventUsed;
    private Vector3 originalArenaScale;

    public void BeginMatch()
    {
        chosenRound = Random.Range(minRound, maxRound + 1);
        eventUsed = false;
        if (arenaVisual != null) originalArenaScale = arenaVisual.localScale;
    }

    public void NotifyRoundStarted(int roundNumber)
    {
        if (eventUsed || roundNumber != chosenRound) return;
        StartCoroutine(TriggerEventAfterDelay());
    }

    private IEnumerator TriggerEventAfterDelay()
    {
        eventUsed = true;
        float delay = Random.Range(minDelayInsideRound, maxDelayInsideRound);
        yield return new WaitForSeconds(delay);

        AC_EventType type = (Random.value < 0.5f) ? AC_EventType.VientoLateral : AC_EventType.ArenaSeAchica;
        float duration = Random.Range(minDuration, maxDuration);

        if (type == AC_EventType.VientoLateral)
        {
            yield return StartCoroutine(WindEvent(duration));
        }
        else
        {
            yield return StartCoroutine(ShrinkEvent(duration));
        }
    }

    private IEnumerator WindEvent(float duration)
    {
        if (AC_GameManager.Instance == null) yield break;

        Vector3 dir = Random.value < 0.5f ? Vector3.right : Vector3.left;
        AC_GameManager.Instance.CurrentWindVelocity = dir * windStrength;
        Debug.Log("EVENTO: Viento lateral");

        yield return new WaitForSeconds(duration);

        AC_GameManager.Instance.CurrentWindVelocity = Vector3.zero;
        Debug.Log("FIN EVENTO: Viento lateral");
    }

    private IEnumerator ShrinkEvent(float duration)
    {
        if (AC_GameManager.Instance == null) yield break;

        Debug.Log("EVENTO: La arena se achica");
        float originalRadius = AC_GameManager.Instance.baseArenaRadius;
        AC_GameManager.Instance.SetArenaRadius(originalRadius * shrinkFactor);

        if (arenaVisual != null)
        {
            arenaVisual.localScale = new Vector3(originalArenaScale.x * shrinkFactor, originalArenaScale.y, originalArenaScale.z * shrinkFactor);
        }

        yield return new WaitForSeconds(duration);

        AC_GameManager.Instance.ResetArenaRadius();
        if (arenaVisual != null) arenaVisual.localScale = originalArenaScale;
        Debug.Log("FIN EVENTO: La arena se achica");
    }
}
