using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Maneja toda la presentación de texto: HUD, mensajes de ronda, marcador,
/// carteles de la arena, y visibilidad del menú principal.
/// Extraído del AC_GameManager.
/// </summary>
public class AC_MatchHUD : MonoBehaviour
{
    [Header("Referencias UI")]
    public Text statusText;
    public Text scoreText;

    private AC_MainMenuUI mainMenuUI;
    private AC_GameManager gm;

    // Estado interno
    private string mensajeActual = string.Empty;
    private string etiquetaEvento = string.Empty;
    private string ultimoEstado = string.Empty;

    public string EtiquetaEvento
    {
        get => etiquetaEvento;
        set => etiquetaEvento = value ?? string.Empty;
    }

    public void Configurar(AC_GameManager gameManager, AC_MainMenuUI menu, Text status, Text score)
    {
        gm = gameManager;
        mainMenuUI = menu;
        statusText = status;
        scoreText = score;
    }

    // ─── Métodos públicos usados por el GameManager ───

    public void MostrarEstado(string texto, bool registrarLog)
    {
        if (statusText != null)
            statusText.text = texto;

        if (registrarLog && texto != ultimoEstado)
        {
            Debug.Log(texto);
            ultimoEstado = texto;
        }
    }

    public void MostrarMensajeRonda(string texto)
    {
        mensajeActual = texto;
        MostrarEstado(texto, true);
    }

    public void ActualizarHUDEnVivo(int numeroRonda)
    {
        string vivo = "Ronda " + numeroRonda + " | " + LeyATexto(gm.CurrentLaw) + " | "
            + Mathf.CeilToInt(gm.controlState.TiempoRestante) + "s";

        if (!string.IsNullOrEmpty(etiquetaEvento))
            vivo += " | " + etiquetaEvento;

        MostrarEstado(vivo, false);
    }

    public void ActualizarHUDEnPausa()
    {
        ActualizarMarcador();
        MostrarEstado("Menu listo | Esperando iniciar", false);
    }

    public void ActualizarMarcador()
    {
        if (scoreText == null) return;

        string nombreP1 = gm.player1 != null ? gm.player1.displayName : "P1";
        string nombreP2 = gm.player2 != null ? gm.player2.displayName : "P2";

        int p1 = gm.controlState.GetScore(1);
        int p2 = gm.controlState.GetScore(2);

        scoreText.text = nombreP1 + ": " + p1 + " | " + nombreP2 + ": " + p2;
    }

    public string ResultadoFinal()
    {
        int p1 = gm.controlState.GetScore(1);
        int p2 = gm.controlState.GetScore(2);

        string ganador;
        if (p1 == p2)
            ganador = "Empate";
        else
            ganador = p1 > p2 ? gm.player1.displayName + " gana" : gm.player2.displayName + " gana";

        return "Fin: " + ganador + " | " + p1 + " - " + p2;
    }

    public void MostrarMenu(bool visible, string mensajeFooter)
    {
        if (mainMenuUI == null) return;
        mainMenuUI.ShowMenu(visible, mensajeFooter);
    }

    // ─── Métodos públicos usados por AC_SceneDecorator ───

    public string TextoCartelArena()
    {
        if (gm.CurrentMatchState == AC_MatchState.Pregame)
            return "ABRAZO\nCOMPETITIVO";

        if (!string.IsNullOrEmpty(etiquetaEvento))
            return etiquetaEvento.ToUpperInvariant();

        if (gm.CurrentMatchState == AC_MatchState.Playing || gm.CurrentMatchState == AC_MatchState.Countdown)
            return LeyATexto(gm.CurrentLaw).ToUpperInvariant();

        return mensajeActual.ToUpperInvariant();
    }

    public string TextoCartelControles(int numeroJugador)
    {
        if (numeroJugador == 1)
            return "P1\nWASD mover\nQ saltar\nSpace abrazo\nShift dash\nCtrl bloqueo";

        return "P2\nFlechas mover\nKeypad0 saltar\nEnter abrazo\nRShift dash\nRCtrl bloqueo";
    }

    // ─── Utilidad estática ───

    public static string LeyATexto(AC_HugLaw ley)
    {
        switch (ley)
        {
            case AC_HugLaw.AbrazoPorLaEspalda: return "Abrazo por la espalda";
            case AC_HugLaw.AbrazoMutuo: return "Abrazo mutuo";
            case AC_HugLaw.AbrazoCargado: return "Abrazo cargado";
            case AC_HugLaw.ToqueMaldito: return "Toque maldito";
            case AC_HugLaw.SoloUnoVale: return "Solo uno vale";
            default: return "Sin ley";
        }
    }
}
