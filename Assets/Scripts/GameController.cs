using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    public ShapesManager ShapeController;
    public AttackController AttackController;
    public Player1Controller Player1;
    public Player2Controller Player2;
    public GameObject TurnArrow;

    public PlayerControllerBase CurrentPlayer { get; private set; }
    public PlayerControllerBase OpponentPlayer { get; private set; }
    public int TurnNumber { get; private set; } = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    private void Start()
    {
        SetCurrentPlayer(Player1, Player2);
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

        CurrentPlayer.EndTurn();
        SetCurrentPlayer(OpponentPlayer, CurrentPlayer);
        TurnNumber++;
    }

    private void SetCurrentPlayer(PlayerControllerBase currentPlayer, PlayerControllerBase opponentPlayer)
    {
        CurrentPlayer = currentPlayer;
        OpponentPlayer = opponentPlayer;

        if (CurrentPlayer != null)
            CurrentPlayer.StartTurn();

        UpdateTurnArrow();
    }

    private void UpdateTurnArrow()
    {
        if (TurnArrow == null || CurrentPlayer == null)
            return;

        TurnArrow.transform.position = CurrentPlayer.GetTurnArrowPoint().position;
    }

    private void ResolveReferences()
    {
        if (ShapeController == null)
            ShapeController = FindObjectOfType<ShapesManager>();

        if (AttackController == null)
            AttackController = FindObjectOfType<AttackController>();

        if (Player1 == null)
            Player1 = FindObjectOfType<Player1Controller>();

        if (Player2 == null)
            Player2 = FindObjectOfType<Player2Controller>();
    }
}
