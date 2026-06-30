using UnityEngine;

public class AC_ArenaManager : MonoBehaviour
{
    public float radioBase = 7.5f;

    private float radioActual;

    public float RadioActual;
    public Vector3 VientoActual;

    public void Configurar()
    {
        radioActual = radioBase;
        RadioActual = radioActual;
        VientoActual = Vector3.zero;
    }

    private void Update()
    {
        if (radioActual < 0.5f)
        {
            radioActual = 0.5f;
        }
        RadioActual = radioActual;
    }

    public void CambiarRadio(float radio)
    {
        radioActual = radio;
        if (radioActual < 0.5f)
        {
            radioActual = 0.5f;
        }
        RadioActual = radioActual;
    }

    public void ReiniciarRadio()
    {
        radioActual = radioBase;
        if (radioActual < 0.5f)
        {
            radioActual = 0.5f;
        }
        RadioActual = radioActual;
    }

    public void ValidarReferenciaArena(AC_EnvironmentEvents environmentEvents, ref Transform centroArena)
    {
        if (centroArena == null)
        {
            GameObject cilindro = GameObject.Find("ArenaCylinder");
            if (cilindro != null)
            {
                centroArena = cilindro.transform;
            }
        }
    }
}
