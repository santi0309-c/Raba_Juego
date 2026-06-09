using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum AC_HugLaw
{
    Normal,
    AbrazoPorLaEspalda,
    AbrazoMutuo,
    AbrazoCargado,
    ToqueMaldito,
    SoloUnoVale
}

public class AC_GameManager : MonoBehaviour
{
    public static AC_GameManager Instance { get; private set; }

    [Header("Jugadores")]
    public AC_PlayerController player1;
    public AC_PlayerController player2;
    public Transform spawn1;
    public Transform spawn2;

    [Header("Arena")]
    public Transform arenaCenter;
    public float baseArenaRadius = 7.5f;
    public float CurrentArenaRadius { get; private set; }
    public Vector3 CurrentWindVelocity { get; set; }

    [Header("Rondas")]
    public int roundsToPlay = 5;
    public float lawReadSeconds = 5f;
    public float countdownSeconds = 3f;
    public float roundSeconds = 60f;
    public float mutualWindow = 0.12f;

    [Header("Abrazo cargado")]
    public float chargedMaxSeconds = 3f;
    public float chargedBreakDistance = 1.6f;

    [Header("Feedback opcional")]
    public Text statusText;
    public Text scoreText;

    [Header("Eventos de entorno")]
    public AC_EnvironmentEvents environmentEvents;

    private int scoreP1;
    private int scoreP2;
    private int currentRoundIndex;
    private AC_HugLaw currentLaw;
    private bool firstHugAlreadyScored;
    private bool resolvingPair;

    private List<AC_HugLaw> lawOrder = new List<AC_HugLaw>();

    public bool IsRoundActive { get; private set; }
    public AC_HugLaw CurrentLaw { get { return currentLaw; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CurrentArenaRadius = baseArenaRadius;
    }

    private void Start()
    {
        BuildLawOrder();
        if (environmentEvents != null) environmentEvents.BeginMatch();
        StartCoroutine(MatchLoop());
    }

    private void BuildLawOrder()
    {
        lawOrder.Clear();
        lawOrder.Add(AC_HugLaw.AbrazoPorLaEspalda);
        lawOrder.Add(AC_HugLaw.AbrazoMutuo);
        lawOrder.Add(AC_HugLaw.AbrazoCargado);
        lawOrder.Add(AC_HugLaw.ToqueMaldito);
        lawOrder.Add(AC_HugLaw.SoloUnoVale);

        for (int i = 0; i < lawOrder.Count; i++)
        {
            int randomIndex = Random.Range(i, lawOrder.Count);
            AC_HugLaw temp = lawOrder[i];
            lawOrder[i] = lawOrder[randomIndex];
            lawOrder[randomIndex] = temp;
        }
    }

    private IEnumerator MatchLoop()
    {
        UpdateScoreUI();

        for (currentRoundIndex = 0; currentRoundIndex < roundsToPlay; currentRoundIndex++)
        {
            currentLaw = lawOrder[currentRoundIndex % lawOrder.Count];
            yield return StartCoroutine(PlayRound(currentRoundIndex + 1, currentLaw));
        }

        if (scoreP1 == scoreP2)
        {
            currentLaw = AC_HugLaw.Normal;
            yield return StartCoroutine(PlayRound(roundsToPlay + 1, currentLaw));
        }

        IsRoundActive = false;
        string winner = scoreP1 == scoreP2 ? "Empate final" : (scoreP1 > scoreP2 ? player1.displayName + " gana" : player2.displayName + " gana");
        SetStatus("FIN: " + winner + " | " + scoreP1 + " - " + scoreP2);
    }

    private IEnumerator PlayRound(int roundNumber, AC_HugLaw law)
    {
        ResetBothPlayers();
        firstHugAlreadyScored = false;
        // FIX: resetear resolvingPair al inicio de cada ronda
        resolvingPair = false;
        CurrentWindVelocity = Vector3.zero;

        SetStatus("RONDA " + roundNumber + " | LEY: " + GetLawText(law));
        yield return new WaitForSeconds(lawReadSeconds);

        for (int i = Mathf.CeilToInt(countdownSeconds); i > 0; i--)
        {
            SetStatus(i.ToString());
            yield return new WaitForSeconds(1f);
        }

        IsRoundActive = true;
        SetStatus("¡ABRÁCENSE!");
        if (environmentEvents != null) environmentEvents.NotifyRoundStarted(roundNumber);

        float timer = roundSeconds;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            SetStatus("Ronda " + roundNumber + " | " + GetLawText(law) + " | " + Mathf.CeilToInt(timer) + "s");
            yield return null;
        }

        // FIX: limpiar viento al terminar ronda también
        CurrentWindVelocity = Vector3.zero;
        IsRoundActive = false;
        SetStatus("Resultado parcial: " + scoreP1 + " - " + scoreP2);
        yield return new WaitForSeconds(2f);
    }

    public void RegisterHugAttempt(AC_PlayerController attacker, AC_PlayerController target, float attemptTime)
    {
        if (!IsRoundActive || attacker == null || target == null) return;
        StartCoroutine(ResolveHugAfterMutualWindow(attacker, target));
    }

    private IEnumerator ResolveHugAfterMutualWindow(AC_PlayerController attacker, AC_PlayerController target)
    {
        yield return new WaitForSeconds(mutualWindow);

        if (!IsRoundActive || resolvingPair) yield break;

        resolvingPair = true;

        bool mutual = Mathf.Abs(attacker.LastHugPressTime - target.LastHugPressTime) <= mutualWindow;
        if (mutual)
        {
            ResolveMutual(attacker, target);
            resolvingPair = false;
            yield break;
        }

        if (currentLaw == AC_HugLaw.AbrazoPorLaEspalda && !IsBehindTarget(attacker, target))
        {
            SetStatus("No contó: tenía que ser por la espalda.");
            resolvingPair = false;
            yield break;
        }

        if (currentLaw == AC_HugLaw.AbrazoCargado)
        {
            yield return StartCoroutine(ResolveChargedHug(attacker, target));
            resolvingPair = false;
            yield break;
        }

        if (currentLaw == AC_HugLaw.ToqueMaldito)
        {
            AddScore(target, 1);
            SetStatus("Toque maldito: punto para " + target.displayName);
            resolvingPair = false;
            yield break;
        }

        int points = 1;
        if (currentLaw == AC_HugLaw.SoloUnoVale && !firstHugAlreadyScored)
        {
            points = 3;
            firstHugAlreadyScored = true;
        }

        AddScore(attacker, points);
        SetStatus(attacker.displayName + " abrazó: +" + points);
        PushApart(attacker, target);
        resolvingPair = false;
    }

    private void ResolveMutual(AC_PlayerController a, AC_PlayerController b)
    {
        if (currentLaw == AC_HugLaw.AbrazoMutuo)
        {
            AddScore(a, 2);
            AddScore(b, 2);
            SetStatus("Abrazo mutuo: +2 para ambos");
        }
        // FIX #4: ToqueMaldito con abrazo mutuo — ambos fueron abrazados, ambos suman +1
        else if (currentLaw == AC_HugLaw.ToqueMaldito)
        {
            AddScore(a, 1);
            AddScore(b, 1);
            SetStatus("Toque maldito mutuo: +1 para ambos (los dos fueron abrazados)");
        }
        else
        {
            SetStatus("Empate de abrazo: ambos rebotan");
        }

        PushApart(a, b);
    }

    private IEnumerator ResolveChargedHug(AC_PlayerController attacker, AC_PlayerController target)
    {
        SetStatus("Abrazo cargado: mantené cerca al rival...");
        float held = 0f;

        while (held < chargedMaxSeconds && IsRoundActive)
        {
            if (attacker == null || target == null) break;
            if (attacker.HugDetector == null) break;
            if (!attacker.HugDetector.IsTargetCloseForChargedHug(target, chargedBreakDistance)) break;
            if (Time.time - target.LastDashTime < 0.25f) break;

            held += Time.deltaTime;
            yield return null;
        }

        int points = 1 + Mathf.FloorToInt(held);
        AddScore(attacker, points);
        SetStatus("Abrazo cargado: +" + points + " para " + attacker.displayName);
        PushApart(attacker, target);
    }

    private bool IsBehindTarget(AC_PlayerController attacker, AC_PlayerController target)
    {
        Vector3 fromTargetToAttacker = attacker.transform.position - target.transform.position;
        fromTargetToAttacker.y = 0f;
        if (fromTargetToAttacker.sqrMagnitude < 0.01f) return false;
        fromTargetToAttacker.Normalize();

        Vector3 targetForward = target.transform.forward;
        targetForward.y = 0f;
        targetForward.Normalize();

        Vector3 attackerToTarget = target.transform.position - attacker.transform.position;
        attackerToTarget.y = 0f;
        attackerToTarget.Normalize();

        bool attackerIsBehind = Vector3.Dot(targetForward, fromTargetToAttacker) < -0.45f;
        bool attackerFacesTarget = Vector3.Dot(attacker.transform.forward, attackerToTarget) > 0.25f;
        return attackerIsBehind && attackerFacesTarget;
    }

    private void PushApart(AC_PlayerController a, AC_PlayerController b)
    {
        Vector3 dir = a.transform.position - b.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = a.transform.forward;
        dir.Normalize();
        a.AddImpulse(dir * 7f, 0.12f);
        b.AddImpulse(-dir * 7f, 0.12f);
    }

    public void PlayerFell(AC_PlayerController player)
    {
        if (!IsRoundActive || player == null) return;
        AddScore(player, -1);
        SetStatus(player.displayName + " cayó: -1");

        if (player == player1 && spawn1 != null)
        {
            player.Respawn(spawn1.position, spawn1.rotation);
        }
        else if (player == player2 && spawn2 != null)
        {
            player.Respawn(spawn2.position, spawn2.rotation);
        }
    }

    private void ResetBothPlayers()
    {
        if (player1 != null && spawn1 != null) player1.Respawn(spawn1.position, spawn1.rotation);
        if (player2 != null && spawn2 != null) player2.Respawn(spawn2.position, spawn2.rotation);
    }

    private void AddScore(AC_PlayerController player, int amount)
    {
        if (player == player1) scoreP1 += amount;
        else if (player == player2) scoreP2 += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            string p1Name = player1 != null ? player1.displayName : "P1";
            string p2Name = player2 != null ? player2.displayName : "P2";
            scoreText.text = p1Name + ": " + scoreP1 + " | " + p2Name + ": " + scoreP2;
        }
    }

    private void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
        Debug.Log(text);
    }

    public void SetArenaRadius(float radius)
    {
        CurrentArenaRadius = Mathf.Max(0.5f, radius);
    }

    public void ResetArenaRadius()
    {
        CurrentArenaRadius = baseArenaRadius;
    }

    private string GetLawText(AC_HugLaw law)
    {
        switch (law)
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
