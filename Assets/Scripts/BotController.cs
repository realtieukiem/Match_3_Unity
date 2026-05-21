using System.Collections;
using UnityEngine;

public class BotController : PlayerControllerBase
{
    public float ThinkDelay = 0.6f;

    private Coroutine turnCoroutine;

    public override bool AllowsBoardInput
    {
        get { return false; }
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        ThinkDelay = Mathf.Max(0f, ThinkDelay);
    }

    public override void Configure(PlayerConfig config)
    {
        base.Configure(config);

        if (config != null)
            ThinkDelay = config.BotThinkDelay;
    }

    public override void StartTurn()
    {
        base.StartTurn();

        if (turnCoroutine != null)
            StopCoroutine(turnCoroutine);

        turnCoroutine = StartCoroutine(PlayTurn());
    }

    public override void EndTurn()
    {
        if (turnCoroutine != null)
        {
            StopCoroutine(turnCoroutine);
            turnCoroutine = null;
        }

        base.EndTurn();
    }

    private IEnumerator PlayTurn()
    {
        if (ThinkDelay > 0f)
            yield return new WaitForSeconds(ThinkDelay);

        turnCoroutine = null;

        if (GameController.Instance == null)
            yield break;

        if (GameController.Instance.CurrentPlayer == this && !GameController.Instance.TryStartBotTurnAction(this))
            GameController.Instance.EndCurrentTurn();
    }
}
