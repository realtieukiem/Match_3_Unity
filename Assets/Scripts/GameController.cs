using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Systems")]
    public ShapesManager ShapeController;
    public AttackController AttackController;
    public TurnTimer TurnTimer;
    public CardController CardController;

    [Header("Player Setup")]
    public LevelConfig DefaultLevelConfig;
    public PlayerConfig Player1Config;
    public PlayerConfig Player2Config;
    public Transform Player1SpawnRoot;
    public Transform Player2SpawnRoot;

    [Header("Runtime Players")]
    public PlayerControllerBase Player1;
    public PlayerControllerBase Player2;

    public PlayerControllerBase CurrentPlayer { get; private set; }
    public PlayerControllerBase OpponentPlayer { get; private set; }
    public int TurnNumber { get; private set; } = 1;

    private bool isResolvingTurnAction;
    private bool endTurnWhenActionFinishes;
    private LevelConfig activeLevelConfig;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyLevelConfig();
        ResolveReferences();
        SetupPlayers();
        if (CardController != null)
            CardController.Configure(activeLevelConfig);
        RegisterTimerEvents();
    }

    private void Start()
    {
        SetCurrentPlayer(Player1, Player2);
    }

    private void OnDestroy()
    {
        if (TurnTimer != null)
            TurnTimer.TimeExpired.RemoveListener(HandleTurnTimeExpired);
    }

    public IEnumerator PlayMatchedItems(IEnumerable<ShapeMatchData> matchedItems)
    {
        if (AttackController == null || CurrentPlayer == null || OpponentPlayer == null)
            yield break;

        yield return AttackController.PlayMatchedItems(matchedItems, CurrentPlayer, OpponentPlayer);
    }

    public void EndCurrentTurn()
    {
        if (CurrentPlayer == null || OpponentPlayer == null)
            return;

        if (isResolvingTurnAction)
        {
            endTurnWhenActionFinishes = true;
            return;
        }

        if (ShapeController != null)
            ShapeController.CancelCurrentSelection();

        if (TurnTimer != null)
            TurnTimer.Pause();

        CurrentPlayer.EndTurn();
        SetCurrentPlayer(OpponentPlayer, CurrentPlayer);
        TurnNumber++;
    }

    public bool CanCurrentPlayerAct()
    {
        return CurrentPlayer != null
            && OpponentPlayer != null
            && !isResolvingTurnAction
            && CurrentPlayer.AllowsBoardInput;
    }

    public bool TryStartBotTurnAction(BotController bot)
    {
        if (bot == null || CurrentPlayer != bot || isResolvingTurnAction || ShapeController == null)
            return false;

        if (CardController != null)
            CardController.UseRandomCards(bot);

        BeginCurrentTurnAction();
        if (ShapeController.TryPlayAutomaticMove())
            return true;

        isResolvingTurnAction = false;
        if (TurnTimer != null)
            TurnTimer.Pause();

        return false;
    }

    public void BeginCurrentTurnAction()
    {
        isResolvingTurnAction = true;

        if (TurnTimer != null)
            TurnTimer.Pause();
    }

    public void CompleteCurrentTurnAction(bool shouldEndTurn)
    {
        isResolvingTurnAction = false;

        if (shouldEndTurn || endTurnWhenActionFinishes)
        {
            endTurnWhenActionFinishes = false;
            EndCurrentTurn();
            return;
        }

        if (TurnTimer != null)
            TurnTimer.Resume();
    }

    private void SetCurrentPlayer(PlayerControllerBase currentPlayer, PlayerControllerBase opponentPlayer)
    {
        CurrentPlayer = currentPlayer;
        OpponentPlayer = opponentPlayer;

        if (OpponentPlayer != null)
            OpponentPlayer.SetTurnIndicatorActive(false);

        if (CurrentPlayer != null)
        {
            CurrentPlayer.StartTurn();
            if (CardController != null)
                CardController.StartTurn(CurrentPlayer);
        }

        if (TurnTimer != null)
            TurnTimer.Restart();
    }

    private void RegisterTimerEvents()
    {
        if (TurnTimer == null)
            return;

        TurnTimer.TimeExpired.RemoveListener(HandleTurnTimeExpired);
        TurnTimer.TimeExpired.AddListener(HandleTurnTimeExpired);
    }

    private void HandleTurnTimeExpired()
    {
        EndCurrentTurn();
    }

    private void ResolveReferences()
    {
        if (ShapeController == null)
            ShapeController = FindObjectOfType<ShapesManager>();

        if (AttackController == null)
            AttackController = FindObjectOfType<AttackController>();

        if (TurnTimer == null)
            TurnTimer = GetComponent<TurnTimer>();

        if (TurnTimer == null)
            TurnTimer = FindObjectOfType<TurnTimer>();

        if (TurnTimer == null)
            TurnTimer = gameObject.AddComponent<TurnTimer>();

        if (CardController == null)
            CardController = GetComponent<CardController>();

        if (CardController == null)
            CardController = FindObjectOfType<CardController>();

        if (CardController == null)
            CardController = gameObject.AddComponent<CardController>();

        bool needsPlayer1Fallback = Player1 == null && (Player1Config == null || Player1Config.PlayerPrefab == null);
        bool needsPlayer2Fallback = Player2 == null && (Player2Config == null || Player2Config.PlayerPrefab == null);
        if (needsPlayer1Fallback || needsPlayer2Fallback)
            ResolvePlayers();
    }

    private void ApplyLevelConfig()
    {
        activeLevelConfig = SelectedLevel.Current != null ? SelectedLevel.Current : DefaultLevelConfig;
        if (activeLevelConfig == null)
            return;

        if (activeLevelConfig.Player1Config != null)
            Player1Config = activeLevelConfig.Player1Config;

        if (activeLevelConfig.BotConfig != null)
            Player2Config = activeLevelConfig.BotConfig;
    }

    private void SetupPlayers()
    {
        Player1 = SetupPlayer(Player1Config, Player1SpawnRoot, Player1);
        Player2 = SetupPlayer(Player2Config, Player2SpawnRoot, Player2);
    }

    private void ResolvePlayers()
    {
        PlayerControllerBase[] players = FindObjectsOfType<PlayerControllerBase>();
        foreach (PlayerControllerBase player in players)
        {
            if (Player1 == null)
            {
                Player1 = player;
                continue;
            }

            if (Player2 == null && player != Player1)
            {
                Player2 = player;
                return;
            }
        }
    }

    private PlayerControllerBase SetupPlayer(PlayerConfig config, Transform spawnRoot, PlayerControllerBase fallbackPlayer)
    {
        if (config == null)
            return fallbackPlayer;

        PlayerControllerBase player = fallbackPlayer;
        if (config.PlayerPrefab != null)
        {
            Transform parent = spawnRoot != null ? spawnRoot : transform;
            player = Instantiate(config.PlayerPrefab, parent);
            ResetSpawnedPlayerTransform(player.transform);
        }

        if (player == null)
        {
            Debug.LogError($"{nameof(GameController)} requires a player prefab or fallback player for {config.name}.", this);
            return null;
        }

        player.Configure(config);
        return player;
    }

    private void ResetSpawnedPlayerTransform(Transform playerTransform)
    {
        RectTransform rectTransform = playerTransform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            return;
        }

        playerTransform.localPosition = Vector3.zero;
        playerTransform.localRotation = Quaternion.identity;
        playerTransform.localScale = Vector3.one;
    }
}
