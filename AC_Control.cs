using UnityEngine;

public class AC_Control : MonoBehaviour
{
    [Header("Puntaje")]
    public int puntajeJugador1;
    public int puntajeJugador2;

    [Header("Vidas")]
    public int vidasJugador1 = 1;
    public int vidasJugador2 = 1;

    [Header("Tiempo de ronda")]
    public float limiteDeTiempo = 60f;

    private float tiempo;
    private bool trackRoundTime;

    public float TiempoActual => tiempo;
    public float TiempoRestante => Mathf.Max(0f, limiteDeTiempo - tiempo);
    public bool IsActive => trackRoundTime;
    public bool CanScore => IsActive && tiempo < limiteDeTiempo;

    public void StartRound(float maxSeconds)
    {
        limiteDeTiempo = Mathf.Max(0.1f, maxSeconds);
        tiempo = 0f;
        trackRoundTime = true;
    }

    public void EndRound()
    {
        trackRoundTime = false;
    }

    public void ResetScoresAndLives(int vidasIniciales = 1)
    {
        puntajeJugador1 = 0;
        puntajeJugador2 = 0;
        vidasJugador1 = vidasIniciales;
        vidasJugador2 = vidasIniciales;
        tiempo = 0f;
    }

    public void AddScore(int playerId, int amount)
    {
        if (amount == 0 || !CanScore) return;

        if (playerId == 1)
        {
            puntajeJugador1 += amount;
        }
        else if (playerId == 2)
        {
            puntajeJugador2 += amount;
        }
    }

    public int GetScore(int playerId)
    {
        return playerId == 1 ? puntajeJugador1 : puntajeJugador2;
    }

    public void AddLife(int playerId, int amount)
    {
        if (playerId == 1)
        {
            vidasJugador1 += amount;
        }
        else if (playerId == 2)
        {
            vidasJugador2 += amount;
        }
    }

    public void RemoveLife(int playerId, int amount = 1)
    {
        AddLife(playerId, -Mathf.Abs(amount));
    }

    private void Update()
    {
        if (!trackRoundTime) return;

        tiempo += Time.deltaTime;
    }
}
