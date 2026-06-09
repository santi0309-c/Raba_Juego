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

    [Header("Piso inclinado")]
    public float tiltAngle = 20f;
    public float tiltSmoothSpeed = 2f;

    [Header("Niebla baja")]
    public float fogDensity = 0.15f;
    public Color fogColor = new Color(0.7f, 0.7f, 0.75f, 1f);

    private int chosenRound;
    private bool eventUsed;
    private Vector3 originalArenaScale;
    private Quaternion originalArenaRotation;

    // Estado original de niebla
    private bool originalFogEnabled;
    private float originalFogDensity;
    private Color originalFogColor;
    private FogMode originalFogMode;

    public void BeginMatch()
    {
        chosenRound = Random.Range(minRound, maxRound + 1);
        eventUsed = false;
        if (arenaVisual != null)
        {
            originalArenaScale = arenaVisual.localScale;
            originalArenaRotation = arenaVisual.localRotation;
        }

        // Guardar estado original de niebla
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

    // FIX #5: Piso inclinado — rota 20° la arena durante el evento
    private IEnumerator TiltEvent(float duration)
    {
        if (arenaVisual == null) yield break;

        Debug.Log("EVENTO: El piso se inclina");

        // Elegir una dirección aleatoria para la inclinación
        Vector3 tiltAxis = Random.value < 0.5f ? Vector3.forward : Vector3.right;
        Quaternion targetRotation = originalArenaRotation * Quaternion.AngleAxis(tiltAngle, tiltAxis);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Entrada suave: lerp hacia la inclinación
            if (arenaVisual != null)
            {
                float t = Mathf.Clamp01(elapsed / 2f); // 2 segundos para entrar
                arenaVisual.localRotation = Quaternion.Slerp(arenaVisual.localRotation, targetRotation, Time.deltaTime * tiltSmoothSpeed);
            }

            yield return null;
        }

        // Restaurar rotación original suavemente
        float restoreTimer = 0f;
        while (restoreTimer < 2f && arenaVisual != null)
        {
            restoreTimer += Time.deltaTime;
            arenaVisual.localRotation = Quaternion.Slerp(arenaVisual.localRotation, originalArenaRotation, Time.deltaTime * tiltSmoothSpeed);
            yield return null;
        }

        if (arenaVisual != null) arenaVisual.localRotation = originalArenaRotation;
        Debug.Log("FIN EVENTO: El piso se inclina");
    }

    // FIX #5: Niebla baja — reduce visibilidad drásticamente durante el evento
    private IEnumerator FogEvent(float duration)
    {
        Debug.Log("EVENTO: Niebla baja");

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogColor = fogColor;

        yield return new WaitForSeconds(duration);

        // Restaurar estado original de niebla
        RenderSettings.fog = originalFogEnabled;
        RenderSettings.fogDensity = originalFogDensity;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogMode = originalFogMode;

        Debug.Log("FIN EVENTO: Niebla baja");
    }
}
