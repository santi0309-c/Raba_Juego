using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AC_HighScoreEntry
{
    public string nombreJugador;
    public int puntaje;
    public string fecha;
}

public class AC_ScorePersistence : MonoBehaviour
{
    private string CLAVE = "Raba_HighScores";
    private int MAX_PUNTAJES = 10;

    public void GuardarPuntaje(string nombre, int puntaje)
    {
        List<AC_HighScoreEntry> ranking = CargarTop();

        AC_HighScoreEntry nueva = new AC_HighScoreEntry();
        nueva.nombreJugador = nombre;
        nueva.puntaje = puntaje;
        nueva.fecha = DateTime.Now.ToString("dd/MM/yyyy");

        ranking.Add(nueva);
        OrdenarRanking(ranking);

        if (ranking.Count > MAX_PUNTAJES)
        {
            ranking.RemoveRange(MAX_PUNTAJES, ranking.Count - MAX_PUNTAJES);
        }

        AC_HighScoreListWrapper wrapper = new AC_HighScoreListWrapper();
        wrapper.puntajes = ranking;
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(CLAVE, json);
        PlayerPrefs.Save();

        Debug.Log("[AC_ScorePersistence] Puntaje guardado: " + nombre + " - " + puntaje);
    }

    private void OrdenarRanking(List<AC_HighScoreEntry> ranking)
    {
        for (int i = 0; i < ranking.Count - 1; i++)
        {
            for (int j = i + 1; j < ranking.Count; j++)
            {
                if (ranking[j].puntaje > ranking[i].puntaje)
                {
                    AC_HighScoreEntry temp = ranking[i];
                    ranking[i] = ranking[j];
                    ranking[j] = temp;
                }
            }
        }
    }

    public List<AC_HighScoreEntry> CargarTop()
    {
        if (!PlayerPrefs.HasKey(CLAVE))
        {
            return new List<AC_HighScoreEntry>();
        }

        string json = PlayerPrefs.GetString(CLAVE);
        AC_HighScoreListWrapper wrapper = JsonUtility.FromJson<AC_HighScoreListWrapper>(json);

        if (wrapper != null && wrapper.puntajes != null)
        {
            return wrapper.puntajes;
        }

        return new List<AC_HighScoreEntry>();
    }

    public void BorrarTodo()
    {
        PlayerPrefs.DeleteKey(CLAVE);
        PlayerPrefs.Save();
        Debug.Log("[AC_ScorePersistence] Ranking borrado.");
    }

    public string RankingATexto()
    {
        List<AC_HighScoreEntry> ranking = CargarTop();
        if (ranking.Count == 0)
        {
            return "Sin puntajes todavia.";
        }

        string texto = "MEJORES PUNTAJES\n\n";
        for (int i = 0; i < ranking.Count; i++)
        {
            texto += (i + 1) + ". " + ranking[i].nombreJugador
                  + " - " + ranking[i].puntaje
                  + " puntos (" + ranking[i].fecha + ")\n";
        }

        return texto;
    }

    [Serializable]
    private class AC_HighScoreListWrapper
    {
        public List<AC_HighScoreEntry> puntajes;
    }
}
