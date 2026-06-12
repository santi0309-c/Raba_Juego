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

public enum AC_MatchState
{
    Pregame,
    Countdown,
    Playing,
    RoundEnd,
    MatchEnd
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
    [SerializeField] private float currentArenaRadius = 7.5f;
    [SerializeField] private Vector3 currentWindVelocity = Vector3.zero;

    [Header("Partida")]
    public float roundSeconds = 60f;
    public float mutualWindow = 0.12f;
    public bool enableSpecialLaws;
    public bool enableEnvironmentEvents;

    [Header("Rondas especiales")]
    public int roundsToPlay = 5;
    public float lawReadSeconds = 3f;
    public float countdownSeconds = 3f;

    [Header("Abrazo cargado")]
    public float chargedMaxSeconds = 3f;
    public float chargedBreakDistance = 1.6f;

    [Header("HUD")]
    public Text statusText;
    public Text scoreText;
    public AC_Control controlState;

    [Header("Eventos de entorno")]
    public AC_EnvironmentEvents environmentEvents;

    private AC_MainMenuUI mainMenuUI;
    private AC_SceneDecorator sceneDecorator;
    private Coroutine matchLoopRoutine;
    private Coroutine pendingResolveRoutine;

    private int scoreP1;
    private int scoreP2;
    private int currentRoundIndex;
    private AC_HugLaw currentLaw = AC_HugLaw.Normal;
    private AC_MatchState currentState = AC_MatchState.Pregame;
    private bool firstHugAlreadyScored;
    private bool resolvingPair;
    private string currentEventLabel = string.Empty;
    private string currentMessage = string.Empty;
    private string lastStatus;

    private readonly List<AC_HugLaw> lawOrder = new List<AC_HugLaw>();

    public bool IsRoundActive => currentState == AC_MatchState.Playing;
    public AC_HugLaw CurrentLaw => currentLaw;
    public AC_MatchState CurrentMatchState => currentState;
    public float CurrentArenaRadius => Mathf.Max(0.5f, currentArenaRadius);
    public Vector3 CurrentWindVelocity
    {
        get => currentWindVelocity;
        set => currentWindVelocity = value;
    }

    private void OnValidate()
    {
        baseArenaRadius = Mathf.Max(0.5f, baseArenaRadius);
        currentArenaRadius = Mathf.Max(baseArenaRadius, 0.5f);
        roundSeconds = Mathf.Max(10f, roundSeconds);
        roundsToPlay = Mathf.Max(1, roundsToPlay);
        countdownSeconds = Mathf.Max(1f, countdownSeconds);
        lawReadSeconds = Mathf.Max(0f, lawReadSeconds);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureCoreReferences();
    }

    private void Start()
    {
        ValidateArenaReferences();
        ConfigurePlayerRespawns();
        SynchronizePlayerControllers();
        EnsurePresentationHelpers();
        ResetMatchState();
        ResetBothPlayers();
        UpdateHudForIdle();
        ShowMainMenu(true, "Listo para jugar");
    }

    public void BeginMatchFromMenu()
    {
        if (matchLoopRoutine != null)
        {
            StopCoroutine(matchLoopRoutine);
        }

        ResetMatchState();
        ResetBothPlayers();
        BuildLawOrder();
        ShowMainMenu(false, string.Empty);
        matchLoopRoutine = StartCoroutine(RunMatchLoop());
    }

    public void ReturnToMenu()
    {
        StopActiveCoroutines();
        EndCurrentRound();
        ResetMatchState();
        ResetBothPlayers();
        UpdateHudForIdle();
        ShowMainMenu(true, "Pulsa Jugar para una nueva ronda");
    }

    private IEnumerator RunMatchLoop()
    {
        PrepareMatchPresentation();

        if (!enableSpecialLaws)
        {
            yield return StartCoroutine(PlayRound(1, AC_HugLaw.Normal));
        }
        else
        {
            for (currentRoundIndex = 0; currentRoundIndex < roundsToPlay; currentRoundIndex++)
            {
                currentLaw = lawOrder[currentRoundIndex % lawOrder.Count];
                yield return StartCoroutine(PlayRound(currentRoundIndex + 1, currentLaw));
            }

            if (scoreP1 == scoreP2)
            {
                yield return StartCoroutine(PlayRound(roundsToPlay + 1, AC_HugLaw.Normal));
            }
        }

        currentState = AC_MatchState.MatchEnd;
        SetRoundMessage(BuildFinalStatus());
        UpdateScoreUI();
        ShowMainMenu(true, BuildFinalStatus());
        matchLoopRoutine = null;
    }

    private IEnumerator PlayRound(int roundNumber, AC_HugLaw law)
    {
        currentState = AC_MatchState.Countdown;
        currentLaw = law;
        ResetRoundState();
        ResetBothPlayers();

        if (lawReadSeconds > 0f)
        {
            SetRoundMessage("Ronda " + roundNumber + " | Ley: " + GetLawText(law));
            yield return new WaitForSeconds(lawReadSeconds);
        }

        for (int i = Mathf.CeilToInt(countdownSeconds); i > 0; i--)
        {
            SetRoundMessage("Empieza en " + i);
            yield return new WaitForSeconds(1f);
        }

        controlState.StartRound(roundSeconds);
        currentState = AC_MatchState.Playing;
        SetRoundMessage("A jugar");

        if (enableEnvironmentEvents && environmentEvents != null)
        {
            environmentEvents.BeginMatch();
            environmentEvents.NotifyRoundStarted(roundNumber);
        }

        while (currentState == AC_MatchState.Playing && controlState.IsActive && controlState.TiempoRestante > 0f)
        {
            UpdateLiveHud(roundNumber);
            yield return null;
        }

        EndCurrentRound();
        SetRoundMessage("Resultado parcial: " + scoreP1 + " - " + scoreP2);
        yield return new WaitForSeconds(1.25f);
    }

    public void RegisterHugAttempt(AC_PlayerController attacker, AC_PlayerController target, float attemptTime)
    {
        if (!IsRoundActive || attacker == null || target == null)
        {
            return;
        }

        if (pendingResolveRoutine != null)
        {
            StopCoroutine(pendingResolveRoutine);
        }

        pendingResolveRoutine = StartCoroutine(ResolveHugAfterMutualWindow(attacker, target, attemptTime));
    }

    private IEnumerator ResolveHugAfterMutualWindow(AC_PlayerController attacker, AC_PlayerController target, float attemptTime)
    {
        yield return new WaitForSeconds(mutualWindow);

        if (!IsRoundActive || resolvingPair || attacker == null || target == null)
        {
            pendingResolveRoutine = null;
            yield break;
        }

        resolvingPair = true;

        bool mutual = Mathf.Abs(attacker.LastHugPressTime - target.LastHugPressTime) <= mutualWindow;
        if (!enableSpecialLaws)
        {
            ResolveSimpleHug(attacker, target, mutual);
            FinishPairResolution();
            yield break;
        }

        if (mutual)
        {
            ResolveMutual(attacker, target);
            FinishPairResolution();
            yield break;
        }

        if (currentLaw == AC_HugLaw.AbrazoPorLaEspalda && !IsBehindTarget(attacker, target))
        {
            SetRoundMessage("No conto: tenia que ser por la espalda");
            FinishPairResolution();
            yield break;
        }

        if (currentLaw == AC_HugLaw.AbrazoCargado)
        {
            yield return StartCoroutine(ResolveChargedHug(attacker, target));
            FinishPairResolution();
            yield break;
        }

        if (currentLaw == AC_HugLaw.ToqueMaldito)
        {
            AddScore(target, 1);
            SetRoundMessage("Toque maldito: punto para " + target.displayName);
            PushApart(attacker, target);
            FinishPairResolution();
            yield break;
        }

        int points = 1;
        if (currentLaw == AC_HugLaw.SoloUnoVale && !firstHugAlreadyScored)
        {
            points = 3;
            firstHugAlreadyScored = true;
        }

        AddScore(attacker, points);
        SetRoundMessage(attacker.displayName + " abrazo: +" + points);
        PushApart(attacker, target);
        FinishPairResolution();
    }

    private void FinishPairResolution()
    {
        resolvingPair = false;
        pendingResolveRoutine = null;
    }

    private void ResolveSimpleHug(AC_PlayerController attacker, AC_PlayerController target, bool mutual)
    {
        if (mutual)
        {
            AddScore(attacker, 1);
            AddScore(target, 1);
            SetRoundMessage("Abrazo mutuo: +1 para ambos");
        }
        else
        {
            AddScore(attacker, 1);
            SetRoundMessage(attacker.displayName + " abrazo: +1");
        }

        PushApart(attacker, target);
    }

    private void ResolveMutual(AC_PlayerController a, AC_PlayerController b)
    {
        if (currentLaw == AC_HugLaw.AbrazoMutuo)
        {
            AddScore(a, 2);
            AddScore(b, 2);
            SetRoundMessage("Abrazo mutuo: +2 para ambos");
        }
        else if (currentLaw == AC_HugLaw.ToqueMaldito)
        {
            AddScore(a, 1);
            AddScore(b, 1);
            SetRoundMessage("Toque maldito mutuo: +1 para ambos");
        }
        else
        {
            SetRoundMessage("Empate de abrazo");
        }

        PushApart(a, b);
    }

    private IEnumerator ResolveChargedHug(AC_PlayerController attacker, AC_PlayerController target)
    {
        float held = 0f;
        SetRoundMessage("Abrazo cargado...");

        while (held < chargedMaxSeconds && IsRoundActive)
        {
            if (attacker == null || target == null || attacker.HugDetector == null)
            {
                break;
            }

            if (!attacker.HugDetector.IsTargetCloseForChargedHug(target, chargedBreakDistance))
            {
                break;
            }

            if (Time.time - target.LastDashTime < 0.25f)
            {
                break;
            }

            held += Time.deltaTime;
            yield return null;
        }

        int points = 1 + Mathf.FloorToInt(held);
        AddScore(attacker, points);
        SetRoundMessage("Abrazo cargado: +" + points + " para " + attacker.displayName);
        PushApart(attacker, target);
    }

    private bool IsBehindTarget(AC_PlayerController attacker, AC_PlayerController target)
    {
        Vector3 fromTargetToAttacker = attacker.transform.position - target.transform.position;
        fromTargetToAttacker.y = 0f;
        if (fromTargetToAttacker.sqrMagnitude < 0.01f)
        {
            return false;
        }

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
        Vector3 direction = a.transform.position - b.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = a.transform.forward;
        }

        direction.Normalize();
        a.AddImpulse(direction * 7f, 0.12f);
        b.AddImpulse(-direction * 7f, 0.12f);
    }

    public void PlayerFell(AC_PlayerController player)
    {
        if (!IsRoundActive || player == null)
        {
            return;
        }

        CancelPlayerInteractions(player);
        AddScore(player, -1);
        SetRoundMessage(player.displayName + " cayo del mapa: -1");

        if (player == player1 && spawn1 != null)
        {
            player.Respawn(spawn1.position, spawn1.rotation);
        }
        else if (player == player2 && spawn2 != null)
        {
            player.Respawn(spawn2.position, spawn2.rotation);
        }
    }

    public void SetArenaRadius(float radius)
    {
        currentArenaRadius = Mathf.Max(0.5f, radius);
    }

    public void ResetArenaRadius()
    {
        currentArenaRadius = Mathf.Max(0.5f, baseArenaRadius);
    }

    public void SetEventLabel(string label)
    {
        currentEventLabel = label ?? string.Empty;
    }

    public string GetArenaBillboardText()
    {
        if (currentState == AC_MatchState.Pregame)
        {
            return "ABRAZO\nCOMPETITIVO";
        }

        if (!string.IsNullOrEmpty(currentEventLabel))
        {
            return currentEventLabel.ToUpperInvariant();
        }

        if (currentState == AC_MatchState.Playing || currentState == AC_MatchState.Countdown)
        {
            return GetLawText(currentLaw).ToUpperInvariant();
        }

        return currentMessage.ToUpperInvariant();
    }

    public string GetControlSignText(int playerNumber)
    {
        if (playerNumber == 1)
        {
            return "P1\nWASD mover\nQ saltar\nSpace abrazo\nShift dash\nCtrl bloqueo";
        }

        return "P2\nFlechas mover\nKeypad0 saltar\nEnter abrazo\nRShift dash\nRCtrl bloqueo";
    }

    private void PrepareMatchPresentation()
    {
        currentEventLabel = string.Empty;
        ResetArenaRadius();
        CurrentWindVelocity = Vector3.zero;
        if (enableEnvironmentEvents && environmentEvents != null)
        {
            environmentEvents.BeginMatch();
        }
    }

    private void CancelPlayerInteractions(AC_PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        player.CancelAllActions();

        AC_PlayerController otherPlayer = player == player1 ? player2 : player1;
        if (otherPlayer != null)
        {
            otherPlayer.HugDetector?.CancelHug();
        }

        if (pendingResolveRoutine != null)
        {
            StopCoroutine(pendingResolveRoutine);
            pendingResolveRoutine = null;
        }

        resolvingPair = false;
    }

    private void ResetMatchState()
    {
        controlState.ResetScoresAndLives(1);
        scoreP1 = 0;
        scoreP2 = 0;
        currentRoundIndex = 0;
        currentLaw = AC_HugLaw.Normal;
        currentState = AC_MatchState.Pregame;
        ResetRoundState();
        UpdateScoreUI();
    }

    private void ResetRoundState()
    {
        firstHugAlreadyScored = false;
        resolvingPair = false;
        currentEventLabel = string.Empty;
        CurrentWindVelocity = Vector3.zero;
        ResetArenaRadius();
        player1?.CancelAllActions();
        player2?.CancelAllActions();
    }

    private void ResetBothPlayers()
    {
        if (player1 != null && spawn1 != null)
        {
            player1.Respawn(spawn1.position, spawn1.rotation);
        }

        if (player2 != null && spawn2 != null)
        {
            player2.Respawn(spawn2.position, spawn2.rotation);
        }
    }

    private void EndCurrentRound()
    {
        currentState = currentState == AC_MatchState.MatchEnd ? AC_MatchState.MatchEnd : AC_MatchState.RoundEnd;
        CurrentWindVelocity = Vector3.zero;
        controlState.EndRound();
        player1?.CancelTransientActions();
        player2?.CancelTransientActions();
    }

    private string BuildFinalStatus()
    {
        string winner;
        if (scoreP1 == scoreP2)
        {
            winner = "Empate";
        }
        else
        {
            winner = scoreP1 > scoreP2 ? player1.displayName + " gana" : player2.displayName + " gana";
        }

        return "Fin: " + winner + " | " + scoreP1 + " - " + scoreP2;
    }

    private void ConfigurePlayerRespawns()
    {
        if (player1 != null && spawn1 != null)
        {
            player1.SetRespawnPoint(spawn1);
        }

        if (player2 != null && spawn2 != null)
        {
            player2.SetRespawnPoint(spawn2);
        }
    }

    private void AddScore(AC_PlayerController player, int amount)
    {
        if (player == null || amount == 0)
        {
            return;
        }

        int playerId = player == player1 ? 1 : (player == player2 ? 2 : 0);
        if (playerId == 0)
        {
            return;
        }

        controlState.AddScore(playerId, amount);
        SyncScoresFromControl();
        UpdateScoreUI();
    }

    private void SynchronizePlayerControllers()
    {
        if (player1 == null || player2 == null || player1 == player2)
        {
            return;
        }

        player1.SyncControllerSettingsFrom(player2);
        player2.SyncControllerSettingsFrom(player1);
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

    private void ValidateArenaReferences()
    {
        if (arenaCenter == null)
        {
            if (environmentEvents != null && environmentEvents.arenaVisual != null)
            {
                arenaCenter = environmentEvents.arenaVisual;
            }
            else
            {
                GameObject arenaCylinder = GameObject.Find("ArenaCylinder");
                if (arenaCylinder != null)
                {
                    arenaCenter = arenaCylinder.transform;
                }
            }
        }
    }

    private void SyncScoresFromControl()
    {
        scoreP1 = controlState.GetScore(1);
        scoreP2 = controlState.GetScore(2);
    }

    private void UpdateScoreUI()
    {
        SyncScoresFromControl();
        if (scoreText == null)
        {
            return;
        }

        string p1Name = player1 != null ? player1.displayName : "P1";
        string p2Name = player2 != null ? player2.displayName : "P2";
        scoreText.text = p1Name + ": " + scoreP1 + " | " + p2Name + ": " + scoreP2;
    }

    private void UpdateLiveHud(int roundNumber)
    {
        string liveText = "Ronda " + roundNumber + " | " + GetLawText(currentLaw) + " | " + Mathf.CeilToInt(controlState.TiempoRestante) + "s";
        if (!string.IsNullOrEmpty(currentEventLabel))
        {
            liveText += " | " + currentEventLabel;
        }

        SetStatus(liveText, false);
    }

    private void UpdateHudForIdle()
    {
        UpdateScoreUI();
        SetStatus("Menu listo | Esperando iniciar", false);
    }

    private void SetRoundMessage(string text)
    {
        currentMessage = text;
        SetStatus(text, true);
    }

    private void SetStatus(string text, bool log)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }

        if (log && text != lastStatus)
        {
            Debug.Log(text);
            lastStatus = text;
        }
    }

    private void ShowMainMenu(bool visible, string footerMessage)
    {
        if (mainMenuUI == null)
        {
            return;
        }

        mainMenuUI.ShowMenu(visible, footerMessage);
    }

    private void StopActiveCoroutines()
    {
        if (matchLoopRoutine != null)
        {
            StopCoroutine(matchLoopRoutine);
            matchLoopRoutine = null;
        }

        if (pendingResolveRoutine != null)
        {
            StopCoroutine(pendingResolveRoutine);
            pendingResolveRoutine = null;
        }
    }

    private void EnsureCoreReferences()
    {
        if (controlState == null)
        {
            controlState = GetComponent<AC_Control>();
        }

        if (controlState == null)
        {
            controlState = gameObject.AddComponent<AC_Control>();
        }
    }

    private void EnsurePresentationHelpers()
    {
        if (mainMenuUI == null)
        {
            mainMenuUI = GetComponent<AC_MainMenuUI>();
        }

        if (mainMenuUI == null)
        {
            mainMenuUI = gameObject.AddComponent<AC_MainMenuUI>();
        }

        if (sceneDecorator == null)
        {
            sceneDecorator = GetComponent<AC_SceneDecorator>();
        }

        if (sceneDecorator == null)
        {
            sceneDecorator = gameObject.AddComponent<AC_SceneDecorator>();
        }

        mainMenuUI.Configure(this);
        sceneDecorator.Configure(this, spawn1, spawn2, arenaCenter);

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            AC_UILayoutManager layoutManager = canvas.GetComponent<AC_UILayoutManager>();
            if (layoutManager == null)
            {
                layoutManager = canvas.gameObject.AddComponent<AC_UILayoutManager>();
            }

            layoutManager.AutoWire(statusText, scoreText);
        }
    }

    private string GetLawText(AC_HugLaw law)
    {
        switch (law)
        {
            case AC_HugLaw.AbrazoPorLaEspalda:
                return "Abrazo por la espalda";
            case AC_HugLaw.AbrazoMutuo:
                return "Abrazo mutuo";
            case AC_HugLaw.AbrazoCargado:
                return "Abrazo cargado";
            case AC_HugLaw.ToqueMaldito:
                return "Toque maldito";
            case AC_HugLaw.SoloUnoVale:
                return "Solo uno vale";
            default:
                return "Sin ley";
        }
    }
}
