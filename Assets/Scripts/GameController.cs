using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    public ShapesManager ShapeController;
    public AttackController AttackController;
    public PlayerControllerBase Player1;
    public PlayerControllerBase Player2;
    public TurnTimer TurnTimer;

    public PlayerControllerBase CurrentPlayer { get; private set; }
    public PlayerControllerBase OpponentPlayer { get; private set; }
    public int TurnNumber { get; private set; } = 1;

    private bool isResolvingTurnAction;
    private bool endTurnWhenActionFinishes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
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
            CurrentPlayer.StartTurn();

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

        ResolvePlayers();
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
}
