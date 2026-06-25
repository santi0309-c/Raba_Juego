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

    private AC_SceneDecorator sceneDecorator;
    private AC_HugResolver hugResolver;
    private AC_MatchHUD hud;
    private AC_ArenaManager arenaManager;
    private AC_ScorePersistence scorePersistence;
    private AC_HugFeedback hugFeedback;
    private AC_AudioManager audioManager;
    private Coroutine matchLoopRoutine;

    private int scoreP1;
    private int scoreP2;
    private int currentRoundIndex;
    private AC_HugLaw currentLaw = AC_HugLaw.Normal;
    private AC_MatchState currentState = AC_MatchState.Pregame;

    private readonly List<AC_HugLaw> lawOrder = new List<AC_HugLaw>();

    public bool IsRoundActive => currentState == AC_MatchState.Playing;
    public AC_HugLaw CurrentLaw => currentLaw;
    public AC_MatchState CurrentMatchState => currentState;
    public float BaseArenaRadius => arenaManager != null ? arenaManager.radioBase : 7.5f;
    public float CurrentArenaRadius => arenaManager != null ? arenaManager.RadioActual : 7.5f;
    public Vector3 CurrentWindVelocity
    {
        get => arenaManager != null ? arenaManager.VientoActual : Vector3.zero;
        set { if (arenaManager != null) arenaManager.VientoActual = value; }
    }

    private void OnValidate()
    {
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
        hud.ActualizarHUDEnPausa();
        hud.MostrarMenu(true, "Listo para jugar");
        audioManager?.ReproducirMusicaMenu();
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
        hud.MostrarMenu(false, string.Empty);
        audioManager?.ReproducirMusicaPartida();
        matchLoopRoutine = StartCoroutine(RunMatchLoop());
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
                currentLaw = lawOrder[currentRoundIndex % lawOrder.Count];
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

        // Guardar puntaje en el ranking
        GuardarPuntajeGanador();

        hud.MostrarMenu(true, hud.ResultadoFinal());
        audioManager?.ReproducirMusicaMenu();
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

        for (int i = Mathf.CeilToInt(countdownSeconds); i > 0; i--)
        {
            hud.MostrarMensajeRonda("Empieza en " + i);
            yield return new WaitForSeconds(1f);
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

    /// <summary>
    /// Passthrough al AC_HugResolver. Mantenido por compatibilidad con AC_HugDetector.
    /// </summary>
    public void RegisterHugAttempt(AC_PlayerController attacker, AC_PlayerController target, float attemptTime)
    {
        hugResolver?.Resolver(attacker, target, attemptTime);
    }

    /// <summary>
    /// Suma puntaje a un jugador. Lo hizo público el resolver para poder llamarlo.
    /// </summary>
    public void SumarPuntaje(AC_PlayerController player, int amount)
    {
        AddScore(player, amount);
    }

    /// <summary>
    /// Muestra un mensaje en el HUD. Lo hizo público el resolver para poder llamarlo.
    /// </summary>
    public void MostrarMensaje(string texto)
    {
        hud.MostrarMensajeRonda(texto);
    }

    public void FeedbackAbrazo(AC_PlayerController atacante, AC_PlayerController objetivo)
    {
        hugFeedback?.AbrazoExitoso(atacante, objetivo);
    }

    public void FeedbackAbrazoFallido()
    {
        hugFeedback?.AbrazoFallido();
    }

    private void GuardarPuntajeGanador()
    {
        if (scorePersistence == null) return;

        int p1 = controlState.GetScore(1);
        int p2 = controlState.GetScore(2);

        if (p1 >= p2)
            scorePersistence.GuardarPuntaje(player1.displayName, p1);

        if (p2 >= p1)
            scorePersistence.GuardarPuntaje(player2.displayName, p2);
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
        arenaManager?.CambiarRadio(radius);
    }

    public void ResetArenaRadius()
    {
        arenaManager?.ReiniciarRadio();
    }

    public void SetEventLabel(string label)
    {
        if (hud != null) hud.EtiquetaEvento = label;
    }

    public string GetArenaBillboardText()
    {
        return hud != null ? hud.TextoCartelArena() : "ABRAZO\nCOMPETITIVO";
    }

    public string GetControlSignText(int playerNumber)
    {
        return hud != null ? hud.TextoCartelControles(playerNumber) : "P1";
    }

    private void PrepareMatchPresentation()
    {
        if (hud != null) hud.EtiquetaEvento = string.Empty;
        arenaManager?.ReiniciarRadio();
        if (arenaManager != null) arenaManager.VientoActual = Vector3.zero;
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
            otherPlayer.HugDetector?.CancelHug();
        }

        hugResolver?.Cancelar();
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
        if (hud != null) hud.EtiquetaEvento = string.Empty;
        CurrentWindVelocity = Vector3.zero;
        ResetArenaRadius();
        player1?.CancelAllActions();
        player2?.CancelAllActions();
        hugResolver?.Resetear();
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
        hud.ActualizarMarcador();
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
        arenaManager?.ValidarReferenciaArena(environmentEvents, ref arenaCenter);
    }

    private void SyncScoresFromControl()
    {
        scoreP1 = controlState.GetScore(1);
        scoreP2 = controlState.GetScore(2);
    }

    private void StopActiveCoroutines()
    {
        if (matchLoopRoutine != null)
        {
            StopCoroutine(matchLoopRoutine);
            matchLoopRoutine = null;
        }

        hugResolver?.Cancelar();
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

        if (audioManager == null)
        {
            audioManager = GetComponent<AC_AudioManager>();
        }

        if (audioManager == null)
        {
            audioManager = gameObject.AddComponent<AC_AudioManager>();
        }

        // Screenshot tool (F12)
        if (GetComponent<AC_ScreenshotTool>() == null)
            gameObject.AddComponent<AC_ScreenshotTool>();
    }

    private void EnsurePresentationHelpers()
    {
        // Configurar HUD
        if (hud == null)
        {
            hud = GetComponent<AC_MatchHUD>();
        }

        if (hud == null)
        {
            hud = gameObject.AddComponent<AC_MatchHUD>();
        }

        // El HUD necesita el menú principal
        AC_MainMenuUI menu = GetComponent<AC_MainMenuUI>();
        if (menu == null)
        {
            menu = gameObject.AddComponent<AC_MainMenuUI>();
        }
        menu.Configure(this);
        hud.Configurar(this, menu, statusText, scoreText);

        // Decorador de escena
        if (sceneDecorator == null)
        {
            sceneDecorator = GetComponent<AC_SceneDecorator>();
        }

        if (sceneDecorator == null)
        {
            sceneDecorator = gameObject.AddComponent<AC_SceneDecorator>();
        }
        sceneDecorator.Configure(this, spawn1, spawn2, arenaCenter);

        // Layout de UI
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
