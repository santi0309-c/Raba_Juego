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
    public static AC_GameManager Instance;

    public AC_PlayerController player1;
    public AC_PlayerController player2;
    public Transform spawn1;
    public Transform spawn2;

    public Transform arenaCenter;

    public float roundSeconds = 60f;
    public float mutualWindow = 0.12f;
    public bool enableSpecialLaws;
    public bool enableEnvironmentEvents;

    public int roundsToPlay = 5;
    public float lawReadSeconds = 0f;
    public float countdownSeconds = 3f;

    public float chargedMaxSeconds = 3f;
    public float chargedBreakDistance = 1.6f;

    public Text statusText;
    public Text scoreText;
    public AC_Control controlState;

    public AC_EnvironmentEvents environmentEvents;

    private AC_SceneDecorator sceneDecorator;
    private AC_HugResolver hugResolver;
    private AC_MatchHUD hud;
    private AC_ArenaManager arenaManager;
    private AC_ScorePersistence scorePersistence;
    private AC_HugFeedback hugFeedback;
    private Coroutine matchLoopRoutine;
    private Coroutine startupCountdownRoutine;

    private int scoreP1;
    private int scoreP2;
    private int currentRoundIndex;
    private AC_HugLaw currentLaw = AC_HugLaw.Normal;
    private AC_MatchState currentState = AC_MatchState.Pregame;

    private List<AC_HugLaw> lawOrder = new List<AC_HugLaw>();

    public bool IsRoundActive;
    public AC_HugLaw CurrentLaw;
    public AC_MatchState CurrentMatchState;
    public float BaseArenaRadius;
    public float CurrentArenaRadius;
    public Vector3 CurrentWindVelocity;

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
        hud.ActualizarHUDEnPausa();
        hud.MostrarMenu(true, "Listo para jugar");
    }

    private void Update()
    {
        IsRoundActive = currentState == AC_MatchState.Playing;
        CurrentLaw = currentLaw;
        CurrentMatchState = currentState;
        if (arenaManager != null)
        {
            BaseArenaRadius = arenaManager.radioBase;
            CurrentArenaRadius = arenaManager.RadioActual;
            CurrentWindVelocity = arenaManager.VientoActual;
        }
        else
        {
            BaseArenaRadius = 7.5f;
            CurrentArenaRadius = 7.5f;
            CurrentWindVelocity = Vector3.zero;
        }
    }

    public void BeginMatchFromMenu()
    {
        StopActiveCoroutines();
        ResetMatchState();
        ResetBothPlayers();
        BuildLawOrder();

        hud.MostrarMenu(false, string.Empty);

        AC_MainMenuUI menuUI = GetComponent<AC_MainMenuUI>();
        if (menuUI != null)
        {
            menuUI.HideMenuRoot();
        }

        startupCountdownRoutine = StartCoroutine(StartupCountdownThenBegin());
    }

    private IEnumerator StartupCountdownThenBegin()
    {
        AC_MainMenuUI menuUI = GetComponent<AC_MainMenuUI>();
        if (menuUI != null)
        {
            yield return StartCoroutine(menuUI.RunStartupCountdown());
        }

        matchLoopRoutine = StartCoroutine(RunMatchLoop());
        startupCountdownRoutine = null;
    }

    public void ReturnToMenu()
    {
        StopActiveCoroutines();
        EndCurrentRound();
        ResetMatchState();
        ResetBothPlayers();
        hud.ActualizarHUDEnPausa();
        hud.MostrarMenu(true, "Pulsa Jugar para una nueva ronda");
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
                int lawIndex = currentRoundIndex % lawOrder.Count;
                currentLaw = lawOrder[lawIndex];
                yield return StartCoroutine(PlayRound(currentRoundIndex + 1, currentLaw));
            }

            if (scoreP1 == scoreP2)
            {
                yield return StartCoroutine(PlayRound(roundsToPlay + 1, AC_HugLaw.Normal));
            }
        }

        currentState = AC_MatchState.MatchEnd;
        hud.MostrarMensajeRonda(hud.ResultadoFinal());
        hud.ActualizarMarcador();
        GuardarPuntajeGanador();
        hud.MostrarMenu(true, hud.ResultadoFinal());

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
            hud.MostrarMensajeRonda("Ronda " + roundNumber + " | Ley: " + AC_MatchHUD.LeyATexto(law));
            yield return new WaitForSeconds(lawReadSeconds);
        }

        controlState.StartRound(roundSeconds);
        currentState = AC_MatchState.Playing;
        hud.MostrarMensajeRonda("A jugar");

        if (enableEnvironmentEvents && environmentEvents != null)
        {
            environmentEvents.BeginMatch();
            environmentEvents.NotifyRoundStarted(roundNumber);
        }

        while (currentState == AC_MatchState.Playing && controlState.IsActive && controlState.TiempoRestante > 0f)
        {
            hud.ActualizarHUDEnVivo(roundNumber);
            yield return null;
        }

        EndCurrentRound();
        hud.MostrarMensajeRonda("Resultado parcial: " + scoreP1 + " - " + scoreP2);
        yield return new WaitForSeconds(1.25f);
    }

    public void RegisterHugAttempt(AC_PlayerController attacker, AC_PlayerController target, float attemptTime)
    {
        if (hugResolver != null)
        {
            hugResolver.Resolver(attacker, target, attemptTime);
        }
    }

    public void SumarPuntaje(AC_PlayerController player, int amount)
    {
        AddScore(player, amount);
    }

    public void MostrarMensaje(string texto)
    {
        hud.MostrarMensajeRonda(texto);
    }

    public void FeedbackAbrazo(AC_PlayerController atacante, AC_PlayerController objetivo)
    {
        if (hugFeedback != null)
        {
            hugFeedback.AbrazoExitoso(atacante, objetivo);
        }
    }

    public void FeedbackAbrazoFallido()
    {
        if (hugFeedback != null)
        {
            hugFeedback.AbrazoFallido();
        }
    }

    private void GuardarPuntajeGanador()
    {
        if (scorePersistence == null || controlState == null) return;

        int p1 = controlState.GetScore(1);
        int p2 = controlState.GetScore(2);

        if (player1 != null && p1 >= p2)
        {
            scorePersistence.GuardarPuntaje(player1.displayName, p1);
        }

        if (player2 != null && p2 >= p1)
        {
            scorePersistence.GuardarPuntaje(player2.displayName, p2);
        }
    }

    public void PlayerFell(AC_PlayerController player)
    {
        if (!IsRoundActive || player == null)
        {
            return;
        }

        CancelPlayerInteractions(player);
        AddScore(player, -1);
        hud.MostrarMensajeRonda(player.displayName + " cayo del mapa: -1");

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
        if (arenaManager != null)
        {
            arenaManager.CambiarRadio(radius);
        }
    }

    public void ResetArenaRadius()
    {
        if (arenaManager != null)
        {
            arenaManager.ReiniciarRadio();
        }
    }

    public void SetEventLabel(string label)
    {
        if (hud != null)
        {
            hud.EtiquetaEvento = label;
        }
    }

    public string GetArenaBillboardText()
    {
        if (hud != null)
        {
            return hud.TextoCartelArena();
        }
        return "ABRAZO\nCOMPETITIVO";
    }

    public string GetControlSignText(int playerNumber)
    {
        if (hud != null)
        {
            return hud.TextoCartelControles(playerNumber);
        }
        return "P" + playerNumber;
    }

    private void PrepareMatchPresentation()
    {
        if (hud != null)
        {
            hud.EtiquetaEvento = string.Empty;
        }

        if (arenaManager != null)
        {
            arenaManager.ReiniciarRadio();
            arenaManager.VientoActual = Vector3.zero;
        }

        if (enableEnvironmentEvents && environmentEvents != null)
        {
            environmentEvents.BeginMatch();
        }
    }

    private void CancelPlayerInteractions(AC_PlayerController player)
    {
        if (player == null) return;

        player.CancelAllActions();

        AC_PlayerController otherPlayer = player == player1 ? player2 : player1;
        if (otherPlayer != null)
        {
            otherPlayer.HugDetector.CancelHug();
        }

        if (hugResolver != null)
        {
            hugResolver.Cancelar();
        }
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
        hud.ActualizarMarcador();
    }

    private void ResetRoundState()
    {
        if (hud != null)
        {
            hud.EtiquetaEvento = string.Empty;
        }
        CurrentWindVelocity = Vector3.zero;
        ResetArenaRadius();
        if (player1 != null) player1.CancelAllActions();
        if (player2 != null) player2.CancelAllActions();
        if (hugResolver != null) hugResolver.Resetear();
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
        if (currentState == AC_MatchState.MatchEnd)
        {
            currentState = AC_MatchState.MatchEnd;
        }
        else
        {
            currentState = AC_MatchState.RoundEnd;
        }
        CurrentWindVelocity = Vector3.zero;
        controlState.EndRound();
        if (player1 != null) player1.CancelTransientActions();
        if (player2 != null) player2.CancelTransientActions();
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

    private void SynchronizePlayerControllers()
    {
        ConfigurePlayerRespawns();
    }

    private void AddScore(AC_PlayerController player, int amount)
    {
        if (player == null || controlState == null) return;

        if (player == player1)
        {
            controlState.AddScore(1, amount);
            scoreP1 = controlState.GetScore(1);
        }
        else if (player == player2)
        {
            controlState.AddScore(2, amount);
            scoreP2 = controlState.GetScore(2);
        }

        hud.ActualizarMarcador();
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
        if (arenaManager != null)
        {
            arenaManager.ValidarReferenciaArena(environmentEvents, ref arenaCenter);
        }
    }

    private void StopActiveCoroutines()
    {
        if (matchLoopRoutine != null)
        {
            StopCoroutine(matchLoopRoutine);
            matchLoopRoutine = null;
        }

        if (startupCountdownRoutine != null)
        {
            StopCoroutine(startupCountdownRoutine);
            startupCountdownRoutine = null;
        }

        if (hugResolver != null)
        {
            hugResolver.Cancelar();
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

        if (hugResolver == null)
        {
            hugResolver = GetComponent<AC_HugResolver>();
        }

        if (hugResolver == null)
        {
            hugResolver = gameObject.AddComponent<AC_HugResolver>();
        }

        hugResolver.Configurar(this, mutualWindow, enableSpecialLaws, chargedMaxSeconds, chargedBreakDistance);

        if (arenaManager == null)
        {
            arenaManager = GetComponent<AC_ArenaManager>();
        }

        if (arenaManager == null)
        {
            arenaManager = gameObject.AddComponent<AC_ArenaManager>();
        }

        arenaManager.Configurar();

        if (scorePersistence == null)
        {
            scorePersistence = GetComponent<AC_ScorePersistence>();
        }

        if (scorePersistence == null)
        {
            scorePersistence = gameObject.AddComponent<AC_ScorePersistence>();
        }

        if (hugFeedback == null)
        {
            hugFeedback = GetComponent<AC_HugFeedback>();
        }

        if (hugFeedback == null)
        {
            hugFeedback = gameObject.AddComponent<AC_HugFeedback>();
        }

        if (GetComponent<AC_ScreenshotTool>() == null)
        {
            gameObject.AddComponent<AC_ScreenshotTool>();
        }
    }

    private void EnsurePresentationHelpers()
    {
        if (hud == null)
        {
            hud = GetComponent<AC_MatchHUD>();
        }

        if (hud == null)
        {
            hud = gameObject.AddComponent<AC_MatchHUD>();
        }

        AC_MainMenuUI menu = GetComponent<AC_MainMenuUI>();
        if (menu == null)
        {
            menu = gameObject.AddComponent<AC_MainMenuUI>();
        }
        menu.Configure(this);
        hud.Configurar(this, menu, statusText, scoreText);

        if (sceneDecorator == null)
        {
            sceneDecorator = GetComponent<AC_SceneDecorator>();
        }

        if (sceneDecorator == null)
        {
            sceneDecorator = gameObject.AddComponent<AC_SceneDecorator>();
        }
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
}
