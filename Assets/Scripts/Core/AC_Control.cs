using UnityEngine;

public class AC_Control : MonoBehaviour
{
    public int puntajeJugador1;
    public int puntajeJugador2;

    public float limiteDeTiempo = 60f;

    private float tiempo;
    private bool trackRoundTime;

    public float TiempoRestante;
    public bool IsActive;
    public bool CanScore;

    private void Update()
    {
        IsActive = trackRoundTime;
        TiempoRestante = limiteDeTiempo - tiempo;
        if (TiempoRestante < 0f)
        {
            TiempoRestante = 0f;
        }
        CanScore = IsActive && tiempo < limiteDeTiempo;

        if (!trackRoundTime) return;

        tiempo += Time.deltaTime;
    }

    public void StartRound(float maxSeconds)
    {
        if (maxSeconds < 0.1f)
        {
            maxSeconds = 0.1f;
        }
        limiteDeTiempo = maxSeconds;
        tiempo = 0f;
        trackRoundTime = true;
        IsActive = true;
        TiempoRestante = limiteDeTiempo;
        CanScore = true;
    }

    public void EndRound()
    {
        trackRoundTime = false;
        IsActive = false;
        CanScore = false;
    }

    public void ResetScoresAndLives(int vidasIniciales)
    {
        puntajeJugador1 = 0;
        puntajeJugador2 = 0;
        tiempo = 0f;
        IsActive = false;
        TiempoRestante = 0f;
        CanScore = false;
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
        if (playerId == 1)
        {
            return puntajeJugador1;
        }
        return puntajeJugador2;
    }
}
