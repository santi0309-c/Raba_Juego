using UnityEngine;

/// <summary>
/// Maneja todo lo relacionado con la arena: radio, viento, validación de referencias.
/// Extraído del AC_GameManager.
/// </summary>
public class AC_ArenaManager : MonoBehaviour
{
    [Header("Arena")]
    public float radioBase = 7.5f;

    private float radioActual;
    private Vector3 vientoActual;

    public float RadioActual => Mathf.Max(0.5f, radioActual);
    public Vector3 VientoActual
    {
        get => vientoActual;
        set => vientoActual = value;
    }

    public void Configurar()
    {
        radioActual = radioBase;
        vientoActual = Vector3.zero;
    }

    public void CambiarRadio(float radio)
    {
        radioActual = Mathf.Max(0.5f, radio);
    }

    public void ReiniciarRadio()
    {
        radioActual = Mathf.Max(0.5f, radioBase);
    }

    public void ReiniciarViento()
    {
        vientoActual = Vector3.zero;
    }

    /// <summary>
    /// Valida que haya una referencia de arena válida, buscando por nombre si hace falta.
    /// </summary>
    public void ValidarReferenciaArena(AC_EnvironmentEvents eventosEntorno, ref Transform centroArena)
    {
        if (centroArena == null)
        {
            if (eventosEntorno != null && eventosEntorno.arenaVisual != null)
            {
                centroArena = eventosEntorno.arenaVisual;
            }
            else
            {
                GameObject cilindro = GameObject.Find("ArenaCylinder");
                if (cilindro != null)
                    centroArena = cilindro.transform;
            }
        }
    }
}
