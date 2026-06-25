using System.Collections;
using UnityEngine;

/// <summary>
/// Simula el parpadeo de un farol japonés con variación orgánica.
/// Agregar al GameObject del farol (que tenga componente Light).
/// </summary>
public class AC_LanternFlicker : MonoBehaviour
{
    [Header("Intensidad")]
    public float intensidadBase = 0.6f;
    public float variacionMaxima = 0.25f;

    [Header("Velocidad")]
    public float velocidadCambio = 0.12f;
    public float suavizado = 0.3f;

    private Light luz;
    private float intensidadObjetivo;
    private float intensidadActual;

    private void Awake()
    {
        luz = GetComponent<Light>();
        if (luz == null)
        {
            enabled = false;
            return;
        }

        intensidadActual = luz.intensity;
        intensidadObjetivo = intensidadBase;
    }

    private void Start()
    {
        StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        while (enabled && luz != null)
        {
            // Elegir nueva intensidad objetivo con variación aleatoria
            float variacion = Random.Range(-variacionMaxima, variacionMaxima);
            intensidadObjetivo = Mathf.Max(0.15f, intensidadBase + variacion);

            // Suavizar hacia el objetivo
            while (Mathf.Abs(intensidadActual - intensidadObjetivo) > 0.01f && enabled)
            {
                intensidadActual = Mathf.Lerp(intensidadActual, intensidadObjetivo, suavizado);
                luz.intensity = intensidadActual;
                yield return null;
            }

            yield return new WaitForSeconds(velocidadCambio);
        }
    }

    private void OnDisable()
    {
        if (luz != null)
            luz.intensity = intensidadBase;
    }
}
