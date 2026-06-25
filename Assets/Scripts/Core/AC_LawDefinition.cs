using UnityEngine;

/// <summary>
/// Define el comportamiento de una ley de abrazo.
/// Crear desde: Assets > Create > Raba > Hug Law
/// </summary>
[CreateAssetMenu(fileName = "Law_Normal", menuName = "Raba/Hug Law")]
public class AC_LawDefinition : ScriptableObject
{
    [Header("Identidad")]
    public string nombreMostrado = "Normal";
    [TextArea(2, 4)]
    public string descripcion = "Abrazo común: +1 punto.";

    [Header("Puntaje")]
    public int puntosPorAbrazo = 1;
    public int puntosPorMutuo = 1;

    [Header("Reglas especiales")]
    public bool requierePorLaEspalda = false;
    public bool inviertePuntos = false;       // Toque maldito
    public bool bonusPrimerAbrazo = false;     // Solo uno vale
    public int bonusPrimerAbrazoPuntos = 3;
    public bool esCargado = false;             // Abrazo cargado
    public float duracionMaximaCargado = 3f;
}
