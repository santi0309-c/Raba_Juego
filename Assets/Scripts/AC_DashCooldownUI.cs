using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra el cooldown del dash en pantalla para cada jugador.
/// Agregar a un Empty en la escena. Asignar los dos jugadores y los dos textos de UI.
/// Ejemplo de texto: "DASH P1: LISTO" o "DASH P1: 2.4s"
/// </summary>
public class AC_DashCooldownUI : MonoBehaviour
{
    [Header("Referencias")]
    public AC_PlayerController player1;
    public AC_PlayerController player2;

    [Header("Textos de UI (Text legacy)")]
    public Text dashTextP1;
    public Text dashTextP2;

    [Header("Colores")]
    public Color readyColor = Color.green;
    public Color cooldownColor = Color.yellow;

    // Acceso a cooldown a través de reflejo de tiempo
    // dashCooldown es public en AC_PlayerController, así que lo leemos directamente
    // Para el tiempo restante calculamos: dashCooldown - (Time.time - LastDashTime)

    private void Update()
    {
        UpdateDashText(player1, dashTextP1);
        UpdateDashText(player2, dashTextP2);
    }

    private void UpdateDashText(AC_PlayerController player, Text label)
    {
        if (player == null || label == null) return;

        float remaining = player.DashCooldownRemaining;

        if (remaining <= 0f)
        {
            label.text = "DASH: LISTO";
            label.color = readyColor;
        }
        else
        {
            label.text = "DASH: " + remaining.ToString("F1") + "s";
            label.color = cooldownColor;
        }
    }
}
