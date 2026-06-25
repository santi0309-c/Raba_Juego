using System.Collections;
using UnityEngine;

/// <summary>
/// Feedback visual para abrazos: shake de cámara, flash de color, partículas.
/// </summary>
public class AC_HugFeedback : MonoBehaviour
{
    [Header("Shake de cámara")]
    public float duracionShake = 0.25f;
    public float fuerzaShake = 0.4f;

    [Header("Flash de color")]
    public float duracionFlash = 0.12f;
    public Color colorExito = Color.green;
    public Color colorFallo = Color.red;

    [Header("Partículas (opcional)")]
    public GameObject prefabParticulas;

    public void AbrazoExitoso(AC_PlayerController atacante, AC_PlayerController objetivo)
    {
        StartCoroutine(ShakeCamara());
        StartCoroutine(FlashRenderers(atacante, colorExito));
        StartCoroutine(FlashRenderers(objetivo, colorFallo));
        SpawnParticulas(atacante, objetivo);
    }

    public void AbrazoFallido()
    {
        StartCoroutine(FlashRenderers(null, colorFallo));
    }

    private IEnumerator ShakeCamara()
    {
        if (Camera.main == null) yield break;

        Vector3 posOriginal = Camera.main.transform.position;
        float timer = 0f;

        while (timer < duracionShake)
        {
            timer += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * fuerzaShake;
            float y = Random.Range(-1f, 1f) * fuerzaShake;
            Camera.main.transform.position = posOriginal + new Vector3(x, y, 0f);
            yield return null;
        }

        Camera.main.transform.position = posOriginal;
    }

    private IEnumerator FlashRenderers(AC_PlayerController jugador, Color color)
    {
        Renderer r = null;
        if (jugador != null)
            r = jugador.GetComponentInChildren<Renderer>();
        else
        {
            // Buscar cualquier jugador en escena
            AC_PlayerController[] jugadores = FindObjectsByType<AC_PlayerController>(FindObjectsSortMode.None);
            if (jugadores.Length > 0)
                r = jugadores[0].GetComponentInChildren<Renderer>();
        }

        if (r == null) yield break;

        Color original = r.material.color;
        r.material.color = color;
        yield return new WaitForSeconds(duracionFlash);
        r.material.color = original;
    }

    private void SpawnParticulas(AC_PlayerController a, AC_PlayerController b)
    {
        if (prefabParticulas == null) return;

        Vector3 puntoMedio = (a.transform.position + b.transform.position) * 0.5f;
        puntoMedio.y += 1f;

        GameObject particulas = Instantiate(prefabParticulas, puntoMedio, Quaternion.identity);
        Destroy(particulas, 2f);
    }
}
