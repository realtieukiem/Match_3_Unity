using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapMenuController : MonoBehaviour
{
    public string FallbackGameplaySceneName = "GamePlay";
    public int MaxSelectedCards = 3;
    public List<CardDefinition> SelectedCards = new List<CardDefinition>();

    private void OnValidate()
    {
        MaxSelectedCards = Mathf.Max(1, MaxSelectedCards);
    }

    public void PlayLevel(LevelConfig levelConfig)
    {
        if (levelConfig == null)
        {
            Debug.LogError($"{nameof(MapMenuController)} requires a level config.", this);
            return;
        }

        SelectedCardLoadout.Set(SelectedCards, MaxSelectedCards);
        SelectedLevel.Set(levelConfig);
        string sceneName = string.IsNullOrEmpty(levelConfig.GameplaySceneName)
            ? FallbackGameplaySceneName
            : levelConfig.GameplaySceneName;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"{nameof(MapMenuController)} requires a gameplay scene name.", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public bool TrySelectCard(CardDefinition card)
    {
        if (card == null || SelectedCards.Contains(card) || SelectedCards.Count >= MaxSelectedCards)
            return false;

        SelectedCards.Add(card);
        return true;
    }

    public void SelectCard(CardDefinition card)
    {
        TrySelectCard(card);
    }

    public void DeselectCard(CardDefinition card)
    {
        if (card != null)
            SelectedCards.Remove(card);
    }

    public void ToggleCard(CardDefinition card)
    {
        if (card == null)
            return;

        if (SelectedCards.Contains(card))
            SelectedCards.Remove(card);
        else
            TrySelectCard(card);
    }

    public void ClearSelectedCards()
    {
        SelectedCards.Clear();
    }
}
