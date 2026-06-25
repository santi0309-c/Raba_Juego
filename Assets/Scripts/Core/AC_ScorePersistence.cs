using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entrada individual del ranking de puntajes.
/// </summary>
[System.Serializable]
public class AC_HighScoreEntry
{
    public string nombreJugador;
    public int puntaje;
    public string fecha;
}

/// <summary>
/// Guarda y carga el top 10 de puntajes usando PlayerPrefs + JSON.
/// Vive en el mismo GameObject que el GameManager.
/// </summary>
public class AC_ScorePersistence : MonoBehaviour
{
    private const string CLAVE = "Raba_HighScores";
    private const int MAX_PUNTAJES = 10;

    /// <summary>
    /// Guarda un puntaje en el ranking. Solo persiste si entra en el top 10.
    /// </summary>
    public void GuardarPuntaje(string nombre, int puntaje)
    {
        List<AC_HighScoreEntry> ranking = CargarTop();

        AC_HighScoreEntry nueva = new AC_HighScoreEntry
        {
            nombreJugador = nombre,
            puntaje = puntaje,
            fecha = DateTime.Now.ToString("dd/MM/yyyy")
        };

        ranking.Add(nueva);
        ranking.Sort((a, b) => b.puntaje.CompareTo(a.puntaje)); // Mayor a menor

        if (ranking.Count > MAX_PUNTAJES)
            ranking.RemoveRange(MAX_PUNTAJES, ranking.Count - MAX_PUNTAJES);

        string json = JsonUtility.ToJson(new AC_HighScoreListWrapper { puntajes = ranking });
        PlayerPrefs.SetString(CLAVE, json);
        PlayerPrefs.Save();

        Debug.Log("[AC_ScorePersistence] Puntaje guardado: " + nombre + " - " + puntaje);
    }

    /// <summary>
    /// Carga el top 10 de puntajes. Retorna lista vacía si no hay datos.
    /// </summary>
    public List<AC_HighScoreEntry> CargarTop()
    {
        if (!PlayerPrefs.HasKey(CLAVE))
            return new List<AC_HighScoreEntry>();

        string json = PlayerPrefs.GetString(CLAVE);
        AC_HighScoreListWrapper wrapper = JsonUtility.FromJson<AC_HighScoreListWrapper>(json);

        return wrapper != null && wrapper.puntajes != null
            ? wrapper.puntajes
            : new List<AC_HighScoreEntry>();
    }

    /// <summary>
    /// Borra todos los puntajes guardados.
    /// </summary>
    public void BorrarTodo()
    {
        PlayerPrefs.DeleteKey(CLAVE);
        PlayerPrefs.Save();
        Debug.Log("[AC_ScorePersistence] Ranking borrado.");
    }

    /// <summary>
    /// Convierte el ranking a texto para mostrar en pantalla.
    /// </summary>
    public string RankingATexto()
    {
        List<AC_HighScoreEntry> ranking = CargarTop();
        if (ranking.Count == 0)
            return "Sin puntajes todavía.";

        string texto = "🏆 MEJORES PUNTAJES 🏆\n\n";
        for (int i = 0; i < ranking.Count; i++)
        {
            texto += (i + 1) + ". " + ranking[i].nombreJugador
                  + " — " + ranking[i].puntaje
                  + " puntos (" + ranking[i].fecha + ")\n";
        }

        return texto;
    }

    // Wrapper porque JsonUtility no serializa List<T> directamente
    [System.Serializable]
    private class AC_HighScoreListWrapper
    {
        public List<AC_HighScoreEntry> puntajes;
    }
}
