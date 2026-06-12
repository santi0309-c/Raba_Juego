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
    [SerializeField] private float currentArenaRadius;
    [SerializeField] private Vector3 currentWindVelocity;
    public float CurrentArenaRadius { get { return currentArenaRadius; } }
    public Vector3 CurrentWindVelocity { get { return currentWindVelocity; } set { currentWindVelocity = value; } }

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
    [Tooltip("Control de puntaje/vida/temporizador inspirado en control.cs del profe")]
    public AC_Control controlState;

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

        if (controlState == null)
        {
            controlState = GetComponent<AC_Control>();
        }
        if (controlState == null)
        {
            controlState = gameObject.AddComponent<AC_Control>();
        }
        controlState.ResetScoresAndLives(1);

        currentArenaRadius = baseArenaRadius;
        currentWindVelocity = Vector3.zero;
    }

    private void Start()
    {
        ValidateArenaReferences();

        if (spawn1 == null || spawn2 == null)
        {
            Debug.LogWarning("[AC_GameManager] No hay spawn1/spawn2 configurados. Los respawns pueden fallar.");
        }

        ConfigurePlayerRespawns();
        SynchronizePlayerControllers();
        BuildLawOrder();

        if (environmentEvents != null)
        {
            if (environmentEvents.arenaVisual == null && arenaCenter != null)
            {
                environmentEvents.arenaVisual = arenaCenter;
                Debug.Log("[AC_GameManager] arenaVisual faltante en AC_EnvironmentEvents: se usa arenaCenter.");
            }

            environmentEvents.EnsureArenaVisualAligned();
            RecenterArenaCylinderIfNeeded();
            environmentEvents.BeginMatch();
        }
        StartCoroutine(MatchLoop());
    }

    private void RecenterArenaCylinderIfNeeded()
    {
        if (environmentEvents == null || environmentEvents.arenaVisual == null) return;
        if (arenaCenter == null) return;

        if (environmentEvents.arenaVisual.name != "ArenaCylinder")
        {
            return;
        }

        Vector3 target = arenaCenter.position;
        Vector3 current = environmentEvents.arenaVisual.position;
        float horizontalDeltaSqr = new Vector3(current.x - target.x, 0f, current.z - target.z).sqrMagnitude;
        if (horizontalDeltaSqr > 0.0001f)
        {
            environmentEvents.arenaVisual.position = new Vector3(target.x, current.y, target.z);
            Debug.Log("[AC_GameManager] ArenaCylinder recenterizado sobre arenaCenter.");
        }
    }

    private void ValidateArenaReferences()
    {
        if (arenaCenter == null)
        {
            if (environmentEvents != null && environmentEvents.arenaVisual != null)
            {
                arenaCenter = environmentEvents.arenaVisual;
                Debug.LogWarning("[AC_GameManager] arenaCenter faltante. Se usó AC_EnvironmentEvents.arenaVisual.");
            }
            else
            {
                Debug.LogWarning("[AC_GameManager] arenaCenter y environmentEvents.arenaVisual no están asignados. Se pueden desalinear límites/recorte.");
            }
        }

        if (arenaCenter != null && arenaCenter.name == "ArenaCenter")
        {
            GameObject arenaCylinder = GameObject.Find("ArenaCylinder");
            if (arenaCylinder != null)
            {
                arenaCenter = arenaCylinder.transform;
                Debug.Log("[AC_GameManager] arenaCenter corregido a ArenaCylinder por nombre.");
            }
        }
    }

    private void SynchronizePlayerControllers()
    {
        if (player1 == null || player2 == null) return;
        if (player1 == player2) return;

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
        if (controlState != null)
        {
            controlState.EndRound();
        }

        string winner = scoreP1 == scoreP2 ? "Empate final" : (scoreP1 > scoreP2 ? player1.displayName + " gana" : player2.displayName + " gana");
        SetStatus("FIN: " + winner + " | " + scoreP1 + " - " + scoreP2);
    }

    private IEnumerator PlayRound(int roundNumber, AC_HugLaw law)
    {
        ResetBothPlayers();
        firstHugAlreadyScored = false;
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
        if (controlState != null)
        {
            controlState.StartRound(roundSeconds);
        }
        SetStatus("¡Abrácense!");

        if (environmentEvents != null)
        {
            environmentEvents.NotifyRoundStarted(roundNumber);
        }

        if (controlState != null)
        {
            while (controlState.IsActive && controlState.TiempoRestante > 0f)
            {
                SetStatus("Ronda " + roundNumber + " | " + GetLawText(law) + " | " + Mathf.CeilToInt(controlState.TiempoRestante) + "s");
                yield return null;
            }
        }
        else
        {
            float timer = roundSeconds;
            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                SetStatus("Ronda " + roundNumber + " | " + GetLawText(law) + " | " + Mathf.CeilToInt(timer) + "s");
                yield return null;
            }
        }

        if (controlState != null)
        {
            controlState.EndRound();
        }

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

        if (controlState != null)
        {
            if (player == player1) controlState.RemoveLife(1);
            else if (player == player2) controlState.RemoveLife(2);
        }

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
        if (player == null || amount == 0) return;
        int playerId = player == player1 ? 1 : (player == player2 ? 2 : 0);
        if (playerId == 0) return;

        if (controlState != null)
        {
            controlState.AddScore(playerId, amount);
            SyncScoresFromControl();
        }
        else
        {
            if (playerId == 1) scoreP1 += amount;
            else scoreP2 += amount;
        }

        UpdateScoreUI();
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

    private void SyncScoresFromControl()
    {
        if (controlState == null) return;
        scoreP1 = controlState.GetScore(1);
        scoreP2 = controlState.GetScore(2);
    }

    private void UpdateScoreUI()
    {
        SyncScoresFromControl();
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
        currentArenaRadius = Mathf.Max(0.5f, radius);
    }

    public void ResetArenaRadius()
    {
        currentArenaRadius = baseArenaRadius;
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
