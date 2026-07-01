using System.Collections;
using UnityEngine;

public class AC_EnvironmentEvents : MonoBehaviour
{
    public enum AC_EventType
    {
        VientoLateral,
        ArenaSeAchica,
        PisoInclinado,
        NieblaBaja
    }

    public int minRound = 2;
    public int maxRound = 4;
    public float minDelayInsideRound = 8f;
    public float maxDelayInsideRound = 25f;
    public float minDuration = 15f;
    public float maxDuration = 25f;

    public float windStrength = 3f;

    public Transform arenaVisual;
    public float shrinkFactor = 0.5f;

    public float tiltAngle = 20f;
    public float tiltSmoothSpeed = 2f;

    public float fogDensity = 0.15f;
    public Color fogColor = new Color(0.7f, 0.7f, 0.75f, 1f);

    private int chosenRound;
    private bool eventUsed;
    private Vector3 originalArenaScale;
    private Quaternion originalArenaRotation;

    private bool originalFogEnabled;
    private float originalFogDensity;
    private Color originalFogColor;
    private FogMode originalFogMode;

    public void BeginMatch()
    {
        int min = minRound;
        int max = maxRound;
        if (min > max)
        {
            int temp = min;
            min = max;
            max = temp;
        }
        chosenRound = Random.Range(min, max + 1);
        eventUsed = false;

        if (arenaVisual != null)
        {
            originalArenaScale = arenaVisual.localScale;
            originalArenaRotation = arenaVisual.localRotation;
        }

        originalFogEnabled = RenderSettings.fog;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogColor = RenderSettings.fogColor;
        originalFogMode = RenderSettings.fogMode;
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

        AC_EventType type = (AC_EventType)Random.Range(0, 4);
        float duration = Random.Range(minDuration, maxDuration);

        switch (type)
        {
            case AC_EventType.VientoLateral:
                yield return StartCoroutine(WindEvent(duration));
                break;
            case AC_EventType.ArenaSeAchica:
                yield return StartCoroutine(ShrinkEvent(duration));
                break;
            case AC_EventType.PisoInclinado:
                yield return StartCoroutine(TiltEvent(duration));
                break;
            case AC_EventType.NieblaBaja:
                yield return StartCoroutine(FogEvent(duration));
                break;
        }
    }

    private IEnumerator WindEvent(float duration)
    {
        if (AC_GameManager.Instance == null) yield break;

        Vector3 dir;
        if (Random.value < 0.5f)
        {
            dir = Vector3.right;
        }
        else
        {
            dir = Vector3.left;
        }

        AC_GameManager.Instance.CurrentWindVelocity = dir * windStrength;
        AC_GameManager.Instance.SetEventLabel("Evento: Viento lateral");
        Debug.Log("EVENTO: Viento lateral");

        yield return new WaitForSeconds(duration);

        AC_GameManager.Instance.CurrentWindVelocity = Vector3.zero;
        AC_GameManager.Instance.SetEventLabel(string.Empty);
        Debug.Log("FIN EVENTO: Viento lateral");
    }

    private IEnumerator ShrinkEvent(float duration)
    {
        if (AC_GameManager.Instance == null) yield break;

        Debug.Log("EVENTO: La arena se achica");
        AC_GameManager.Instance.SetEventLabel("Evento: Arena chica");
        float originalRadius = AC_GameManager.Instance.BaseArenaRadius;
        AC_GameManager.Instance.SetArenaRadius(originalRadius * shrinkFactor);

        if (arenaVisual != null)
        {
            arenaVisual.localScale = new Vector3(
                originalArenaScale.x * shrinkFactor,
                originalArenaScale.y,
                originalArenaScale.z * shrinkFactor
            );
        }

        yield return new WaitForSeconds(duration);

        AC_GameManager.Instance.ResetArenaRadius();
        AC_GameManager.Instance.SetEventLabel(string.Empty);
        if (arenaVisual != null)
        {
            arenaVisual.localScale = originalArenaScale;
        }
        Debug.Log("FIN EVENTO: La arena se achica");
    }

    private IEnumerator TiltEvent(float duration)
    {
        if (arenaVisual == null) yield break;

        Debug.Log("EVENTO: El piso se inclina");
        if (AC_GameManager.Instance != null)
        {
            AC_GameManager.Instance.SetEventLabel("Evento: Piso inclinado");
        }

        Vector3 tiltAxis;
        if (Random.value < 0.5f)
        {
            tiltAxis = Vector3.forward;
        }
        else
        {
            tiltAxis = Vector3.right;
        }
        Quaternion targetRotation = originalArenaRotation * Quaternion.AngleAxis(tiltAngle, tiltAxis);

        float elapsed = 0f;
        float enterDuration = 2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (arenaVisual != null)
            {
                float t = elapsed / enterDuration;
                if (t > 1f) t = 1f;
                arenaVisual.localRotation = Quaternion.Slerp(originalArenaRotation, targetRotation, t);
            }

            yield return null;
        }

        float restoreTimer = 0f;
        while (restoreTimer < 2f && arenaVisual != null)
        {
            restoreTimer += Time.deltaTime;
            arenaVisual.localRotation = Quaternion.Slerp(arenaVisual.localRotation, originalArenaRotation, Time.deltaTime * tiltSmoothSpeed);
            yield return null;
        }

        if (arenaVisual != null)
        {
            arenaVisual.localRotation = originalArenaRotation;
        }
        if (AC_GameManager.Instance != null)
        {
            AC_GameManager.Instance.SetEventLabel(string.Empty);
        }
        Debug.Log("FIN EVENTO: El piso se inclina");
    }

    private IEnumerator FogEvent(float duration)
    {
        Debug.Log("EVENTO: Niebla baja");
        if (AC_GameManager.Instance != null)
        {
            AC_GameManager.Instance.SetEventLabel("Evento: Niebla baja");
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogColor = fogColor;

        yield return new WaitForSeconds(duration);

        RenderSettings.fog = originalFogEnabled;
        RenderSettings.fogDensity = originalFogDensity;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogMode = originalFogMode;
        if (AC_GameManager.Instance != null)
        {
            AC_GameManager.Instance.SetEventLabel(string.Empty);
        }

        Debug.Log("FIN EVENTO: Niebla baja");
    }
}
