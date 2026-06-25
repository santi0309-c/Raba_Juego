using UnityEngine;

/// <summary>
/// Configuración de jugador como ScriptableObject.
/// Permite crear perfiles intercambiables ("rápido", "tanque", etc.)
/// sin tocar el prefab ni la escena.
/// Crear desde: Assets > Create > Raba > Player Config
/// </summary>
[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Raba/Player Config")]
public class AC_PlayerConfig : ScriptableObject
{
    [Header("Identidad")]
    public string nombreMostrado = "Jugador";

    [Header("Teclas")]
    public KeyCode teclaArriba = KeyCode.W;
    public KeyCode teclaAbajo = KeyCode.S;
    public KeyCode teclaIzquierda = KeyCode.A;
    public KeyCode teclaDerecha = KeyCode.D;
    public KeyCode teclaAbrazo = KeyCode.Space;
    public KeyCode teclaDash = KeyCode.LeftShift;
    public KeyCode teclaAguantar = KeyCode.LeftControl;
    public KeyCode teclaSaltar = KeyCode.None;

    [Header("Movimiento")]
    public float velocidadMovimiento = 5.5f;
    public float velocidadRotacion = 12f;
    public float gravedad = -25f;
    public float caidaY = -4f;

    [Header("Salto")]
    public float fuerzaSalto = 8f;
    public float distanciaCheckSuelo = 0.15f;
    public float radioCheckSuelo = 0.25f;

    [Header("Dash")]
    public float distanciaDash = 3f;
    public float duracionDash = 0.14f;
    public float enfriamientoDash = 4f;

    [Header("Aguantar")]
    public float radioAguantar = 0.18f;
    public float multiplicadorAlturaAguantar = 0.75f;
}
