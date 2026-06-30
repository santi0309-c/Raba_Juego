using System.Collections;
using UnityEngine;

public class AC_LanternFlicker : MonoBehaviour
{
    public float intensidadBase = 0.6f;
    public float variacionMaxima = 0.25f;

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
            float variacion = Random.Range(-variacionMaxima, variacionMaxima);
            intensidadObjetivo = intensidadBase + variacion;
            if (intensidadObjetivo < 0.15f)
            {
                intensidadObjetivo = 0.15f;
            }

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
        {
            luz.intensity = intensidadBase;
        }
    }
}
