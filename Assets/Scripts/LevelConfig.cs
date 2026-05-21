using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Match3/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Menu")]
    public string DisplayName = "Level";
    public Sprite MapIcon;

    [Header("Scene")]
    public string GameplaySceneName = "GamePlay";

    [Header("Players")]
    public PlayerConfig Player1Config;
    public PlayerConfig BotConfig;

    [Header("Cards")]
    public int MaxPlayer1Cards = 3;
    public List<CardDefinition> Player1Cards = new List<CardDefinition>();
    public List<CardDefinition> Player2Cards = new List<CardDefinition>();
    public List<CardDefinition> Player2RandomCardPool = new List<CardDefinition>();
    public bool RandomizePlayer2CardsWhenBot = true;
    public int MaxPlayer2RandomCards = 3;

    private void OnValidate()
    {
        MaxPlayer1Cards = Mathf.Max(1, MaxPlayer1Cards);
        MaxPlayer2RandomCards = Mathf.Max(1, MaxPlayer2RandomCards);
    }
}
