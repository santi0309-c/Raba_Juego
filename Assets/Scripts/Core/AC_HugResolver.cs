using System.Collections;
using UnityEngine;

public class AC_HugResolver : MonoBehaviour
{
    public float ventanaMutua = 0.12f;
    public bool leyesEspecialesActivas;

    public float duracionMaximaCargado = 3f;
    public float distanciaRupturaCargado = 1.6f;

    private bool resolviendoPar;
    private Coroutine rutinaPendiente;
    private bool primerAbrazoYaPuntuo;
    private AC_GameManager gm;

    public bool EstaResolviendo;

    public void Configurar(AC_GameManager gameManager, float ventana, bool leyesEspeciales,
        float duracionCargado, float distanciaCargado)
    {
        gm = gameManager;
        ventanaMutua = ventana;
        leyesEspecialesActivas = leyesEspeciales;
        duracionMaximaCargado = duracionCargado;
        if (distanciaCargado > 0)
        {
            distanciaRupturaCargado = distanciaCargado;
        }
        else
        {
            distanciaRupturaCargado = 1.6f;
        }
    }

    public void Resetear()
    {
        primerAbrazoYaPuntuo = false;
        resolviendoPar = false;
        EstaResolviendo = false;
        if (rutinaPendiente != null)
        {
            StopCoroutine(rutinaPendiente);
            rutinaPendiente = null;
        }
    }

    public void Cancelar()
    {
        if (rutinaPendiente != null)
        {
            StopCoroutine(rutinaPendiente);
            rutinaPendiente = null;
        }
        resolviendoPar = false;
        EstaResolviendo = false;
    }

    public void Resolver(AC_PlayerController atacante, AC_PlayerController objetivo, float tiempoIntento)
    {
        if (gm == null) return;
        if (!gm.IsRoundActive || atacante == null || objetivo == null) return;

        if (rutinaPendiente != null)
        {
            StopCoroutine(rutinaPendiente);
        }

        rutinaPendiente = StartCoroutine(ResolverTrasVentanaMutua(atacante, objetivo));
    }

    private IEnumerator ResolverTrasVentanaMutua(AC_PlayerController atacante, AC_PlayerController objetivo)
    {
        yield return new WaitForSeconds(ventanaMutua);

        if (gm == null || !gm.IsRoundActive || resolviendoPar || atacante == null || objetivo == null)
        {
            rutinaPendiente = null;
            yield break;
        }

        resolviendoPar = true;
        EstaResolviendo = true;

        bool atacanteBloqueando = atacante != null && atacante.IsBlocking;
        bool objetivoBloqueando = objetivo != null && objetivo.IsBlocking;
        if (atacanteBloqueando || objetivoBloqueando)
        {
            gm.MostrarMensaje("Abrazo bloqueado");
            gm.FeedbackAbrazoFallido();
            FinalizarResolucion();
            yield break;
        }

        float diferenciaTiempo = atacante.LastHugPressTime - objetivo.LastHugPressTime;
        if (diferenciaTiempo < 0)
        {
            diferenciaTiempo = -diferenciaTiempo;
        }
        bool mutuo = diferenciaTiempo <= ventanaMutua;

        if (!leyesEspecialesActivas)
        {
            if (mutuo)
            {
                gm.SumarPuntaje(atacante, 1);
                gm.SumarPuntaje(objetivo, 1);
                gm.MostrarMensaje("Abrazo mutuo: +1 para ambos");
            }
            else
            {
                gm.SumarPuntaje(atacante, 1);
                gm.MostrarMensaje(atacante.displayName + " abrazo: +1");
            }

            gm.FeedbackAbrazo(atacante, objetivo);
            SepararJugadores(atacante, objetivo);
            FinalizarResolucion();
            yield break;
        }

        if (mutuo)
        {
            ResolverMutuo(atacante, objetivo);
            FinalizarResolucion();
            yield break;
        }

        AC_HugLaw leyActual = gm.CurrentLaw;

        if (leyActual == AC_HugLaw.AbrazoPorLaEspalda && !EstaDetrasDelObjetivo(atacante, objetivo))
        {
            gm.MostrarMensaje("No conto: tenia que ser por la espalda");
            gm.FeedbackAbrazoFallido();
            FinalizarResolucion();
            yield break;
        }

        if (leyActual == AC_HugLaw.AbrazoCargado)
        {
            yield return StartCoroutine(ResolverCargado(atacante, objetivo));
            FinalizarResolucion();
            yield break;
        }

        if (leyActual == AC_HugLaw.ToqueMaldito)
        {
            gm.SumarPuntaje(objetivo, 1);
            gm.MostrarMensaje("Toque maldito: punto para " + objetivo.displayName);
            SepararJugadores(atacante, objetivo);
            FinalizarResolucion();
            yield break;
        }

        int puntos = 1;
        if (leyActual == AC_HugLaw.SoloUnoVale && !primerAbrazoYaPuntuo)
        {
            puntos = 3;
            primerAbrazoYaPuntuo = true;
        }

        gm.SumarPuntaje(atacante, puntos);
        gm.MostrarMensaje(atacante.displayName + " abrazo: +" + puntos);
        gm.FeedbackAbrazo(atacante, objetivo);
        SepararJugadores(atacante, objetivo);
        FinalizarResolucion();
    }

    private void ResolverMutuo(AC_PlayerController a, AC_PlayerController b)
    {
        AC_HugLaw leyActual = gm != null ? gm.CurrentLaw : AC_HugLaw.Normal;

        if (leyActual == AC_HugLaw.AbrazoMutuo)
        {
            gm.SumarPuntaje(a, 2);
            gm.SumarPuntaje(b, 2);
            gm.MostrarMensaje("Abrazo mutuo: +2 para ambos");
        }
        else if (leyActual == AC_HugLaw.ToqueMaldito)
        {
            gm.SumarPuntaje(a, 1);
            gm.SumarPuntaje(b, 1);
            gm.MostrarMensaje("Toque maldito mutuo: +1 para ambos");
        }
        else
        {
            gm.SumarPuntaje(a, 1);
            gm.SumarPuntaje(b, 1);
            gm.MostrarMensaje("Abrazo mutuo: +1 para ambos");
        }

        gm.FeedbackAbrazo(a, b);
        SepararJugadores(a, b);
    }

    private IEnumerator ResolverCargado(AC_PlayerController atacante, AC_PlayerController objetivo)
    {
        float sostenido = 0f;
        gm.MostrarMensaje("Abrazo cargado...");

        while (sostenido < duracionMaximaCargado && gm.IsRoundActive)
        {
            if (atacante == null || objetivo == null || atacante.HugDetector == null)
            {
                break;
            }

            if (!atacante.HugDetector.IsTargetCloseForChargedHug(objetivo, distanciaRupturaCargado))
            {
                break;
            }

            if (Time.time - objetivo.LastDashTime < 0.25f)
            {
                break;
            }

            if (atacante.IsBlocking || objetivo.IsBlocking)
            {
                break;
            }

            sostenido += Time.deltaTime;
            yield return null;
        }

        int puntos = 1 + Mathf.FloorToInt(sostenido);
        gm.SumarPuntaje(atacante, puntos);
        gm.MostrarMensaje("Abrazo cargado: +" + puntos + " para " + atacante.displayName);
        gm.FeedbackAbrazo(atacante, objetivo);
        SepararJugadores(atacante, objetivo);
    }

    private bool EstaDetrasDelObjetivo(AC_PlayerController atacante, AC_PlayerController objetivo)
    {
        Vector3 deObjetivoAAtacante = atacante.transform.position - objetivo.transform.position;
        deObjetivoAAtacante.y = 0f;
        if (deObjetivoAAtacante.sqrMagnitude < 0.01f) return false;
        deObjetivoAAtacante.Normalize();

        Vector3 adelanteObjetivo = objetivo.transform.forward;
        adelanteObjetivo.y = 0f;
        adelanteObjetivo.Normalize();

        Vector3 atacanteAObjetivo = objetivo.transform.position - atacante.transform.position;
        atacanteAObjetivo.y = 0f;
        atacanteAObjetivo.Normalize();

        bool atacanteDetras = Vector3.Dot(adelanteObjetivo, deObjetivoAAtacante) < -0.45f;
        bool atacanteMiraAlObjetivo = Vector3.Dot(atacante.transform.forward, atacanteAObjetivo) > 0.25f;
        return atacanteDetras && atacanteMiraAlObjetivo;
    }

    private void SepararJugadores(AC_PlayerController a, AC_PlayerController b)
    {
        Vector3 direccion = a.transform.position - b.transform.position;
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.01f)
        {
            direccion = a.transform.forward;
        }

        direccion.Normalize();
        a.AddImpulse(direccion * 7f, 0.12f);
        b.AddImpulse(-direccion * 7f, 0.12f);
    }

    private void FinalizarResolucion()
    {
        resolviendoPar = false;
        EstaResolviendo = false;
        rutinaPendiente = null;
    }
}
