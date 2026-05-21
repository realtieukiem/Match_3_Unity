using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardController : MonoBehaviour
{
    [Header("Card UI")]
    public Transform CardButtonRoot;
    public CardButtonView CardButtonPrefab;
    public bool HideCardsForAutomaticPlayers;

    [Header("Player 1 Cards")]
    public List<CardDefinition> DefaultPlayer1Cards = new List<CardDefinition>();
    public List<CardDefinition> Player1Cards = new List<CardDefinition>();

    [Header("Player 2 Cards")]
    public List<CardDefinition> DefaultPlayer2Cards = new List<CardDefinition>();
    public List<CardDefinition> Player2RandomCardPool = new List<CardDefinition>();
    public List<CardDefinition> Player2Cards = new List<CardDefinition>();
    public bool RandomizePlayer2CardsWhenBot = true;
    public int MaxPlayer2RandomCards = 3;

    [Header("Auto Use")]
    [Range(0f, 1f)]
    public float AutoUseCardChance = 0.5f;
    public int MaxAutoCardsPerTurn = 99;

    [Header("Turn Rules")]
    public int MaxCardsPerTurn = 1;

    private readonly Dictionary<PlayerControllerBase, int> usedCardsByPlayer = new Dictionary<PlayerControllerBase, int>();
    private readonly List<CardButtonView> spawnedCardButtons = new List<CardButtonView>();
    private LevelConfig levelConfig;

    private void Awake()
    {
        BuildRuntimeCards();
    }

    private void OnValidate()
    {
        MaxCardsPerTurn = Mathf.Max(1, MaxCardsPerTurn);
        MaxAutoCardsPerTurn = Mathf.Max(0, MaxAutoCardsPerTurn);
        MaxPlayer2RandomCards = Mathf.Max(1, MaxPlayer2RandomCards);
    }

    public IReadOnlyList<CardDefinition> GetCards(PlayerControllerBase player)
    {
        GameController gameController = GameController.Instance;
        if (gameController == null || player == null)
            return Player1Cards;

        return player == gameController.Player2 ? Player2Cards : Player1Cards;
    }

    public void StartTurn(PlayerControllerBase player)
    {
        if (player == null)
            return;

        usedCardsByPlayer[player] = 0;
        RefreshCardButtons(player);
    }

    public bool CanUseCard(CardDefinition card, PlayerControllerBase owner)
    {
        GameController gameController = GameController.Instance;
        if (gameController == null || card == null || owner == null)
            return false;

        if (gameController.CurrentPlayer != owner)
            return false;

        PlayerControllerBase opponent = gameController.OpponentPlayer;
        return GetUsedCardCount(owner) < MaxCardsPerTurn
            && card.CanUse(owner, opponent);
    }

    public bool TryUseCard(CardDefinition card)
    {
        GameController gameController = GameController.Instance;
        if (gameController == null)
            return false;

        return TryUseCard(card, gameController.CurrentPlayer);
    }

    public void UseCard(CardDefinition card)
    {
        TryUseCard(card);
    }

    public void UsePlayerCardAtIndex(int cardIndex)
    {
        GameController gameController = GameController.Instance;
        if (gameController == null)
            return;

        UseCardAtIndex(gameController.CurrentPlayer, cardIndex);
    }

    public void UseCardAtIndex(PlayerControllerBase player, int cardIndex)
    {
        IReadOnlyList<CardDefinition> cards = GetCards(player);
        if (cards == null || cardIndex < 0 || cardIndex >= cards.Count)
            return;

        TryUseCard(cards[cardIndex], player);
    }

    public bool TryUseCard(CardDefinition card, PlayerControllerBase owner)
    {
        if (!CanUseCard(card, owner))
            return false;

        PlayerControllerBase opponent = GameController.Instance.OpponentPlayer;
        card.ConsumeCost(owner);
        card.Apply(owner, opponent);
        usedCardsByPlayer[owner] = GetUsedCardCount(owner) + 1;
        RemoveCard(owner, card);
        RefreshCardButtons(owner);
        return true;
    }

    public int UseRandomCards(PlayerControllerBase player)
    {
        IReadOnlyList<CardDefinition> cards = GetCards(player);
        if (player == null || cards == null || cards.Count == 0 || MaxAutoCardsPerTurn <= 0)
            return 0;

        int usedCount = 0;
        int attempts = 0;
        while (usedCount < MaxAutoCardsPerTurn && attempts < cards.Count)
        {
            attempts++;

            if (Random.value > AutoUseCardChance)
                break;

            CardDefinition card = cards[Random.Range(0, cards.Count)];
            if (TryUseCard(card, player))
                usedCount++;
            else if (GetUsedCardCount(player) >= MaxCardsPerTurn)
                break;
        }

        return usedCount;
    }

    public void Configure(LevelConfig config)
    {
        levelConfig = config;
        BuildRuntimeCards();
        RefreshCurrentPlayerCards();
    }

    public void BuildRuntimeCards()
    {
        Player1Cards.Clear();
        Player2Cards.Clear();

        if (levelConfig != null && levelConfig.Player1Cards.Count > 0)
            AddCards(Player1Cards, levelConfig.Player1Cards, levelConfig.MaxPlayer1Cards);
        else
            BuildPlayer1CardsFromFallbacks();

        if (ShouldRandomizePlayer2Cards())
            AddRandomCards(Player2Cards, GetPlayer2RandomCardPool(), GetMaxPlayer2RandomCards());

        if (Player2Cards.Count == 0)
            AddCards(Player2Cards, GetDefaultPlayer2Cards(), int.MaxValue);
    }

    public void RefreshCurrentPlayerCards()
    {
        GameController gameController = GameController.Instance;
        if (gameController == null)
            return;

        RefreshCardButtons(gameController.CurrentPlayer);
    }

    private int GetUsedCardCount(PlayerControllerBase player)
    {
        int usedCount;
        return player != null && usedCardsByPlayer.TryGetValue(player, out usedCount)
            ? usedCount
            : 0;
    }

    private void RemoveCard(PlayerControllerBase owner, CardDefinition card)
    {
        if (owner == null || card == null)
            return;

        if (GameController.Instance != null && owner == GameController.Instance.Player2)
            Player2Cards.Remove(card);
        else
            Player1Cards.Remove(card);
    }

    private bool ShouldRandomizePlayer2Cards()
    {
        return GetRandomizePlayer2CardsWhenBot()
            && GameController.Instance != null
            && GameController.Instance.Player2 is BotController
            && GetPlayer2RandomCardPool().Count > 0;
    }

    private void RefreshCardButtons(PlayerControllerBase player)
    {
        ClearCardButtons();

        if (CardButtonRoot == null || player == null)
            return;

        IReadOnlyList<CardDefinition> cards = GetCards(player);
        bool shouldShow = cards != null
            && cards.Count > 0
            && (!HideCardsForAutomaticPlayers || player.AllowsBoardInput);
        CardButtonRoot.gameObject.SetActive(shouldShow);

        if (!shouldShow)
            return;

        for (int i = 0; i < cards.Count; i++)
        {
            CardDefinition card = cards[i];
            if (card == null)
                continue;

            CardButtonView buttonView = CreateCardButton();
            bool interactable = player.AllowsBoardInput && CanUseCard(card, player);
            buttonView.Bind(this, player, card, interactable);
            spawnedCardButtons.Add(buttonView);
        }
    }

    private CardButtonView CreateCardButton()
    {
        if (CardButtonPrefab != null)
            return Instantiate(CardButtonPrefab, CardButtonRoot);

        GameObject buttonGo = new GameObject("CardButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CardButtonView));
        buttonGo.transform.SetParent(CardButtonRoot, false);

        RectTransform rectTransform = buttonGo.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(160f, 64f);

        Image background = buttonGo.GetComponent<Image>();
        background.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        CardButtonView buttonView = buttonGo.GetComponent<CardButtonView>();
        buttonView.IconImage = CreateButtonIcon(buttonGo.transform);
        buttonView.NameText = CreateButtonText(buttonGo.transform, "NameText", 24, TextAlignmentOptions.Left);
        buttonView.ManaCostText = CreateManaCostText(buttonGo.transform);
        return buttonView;
    }

    private Image CreateButtonIcon(Transform parent)
    {
        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(parent, false);

        RectTransform rectTransform = iconGo.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 0.5f);
        rectTransform.anchorMax = new Vector2(0f, 0.5f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(8f, 0f);
        rectTransform.sizeDelta = new Vector2(48f, 48f);

        Image image = iconGo.GetComponent<Image>();
        image.preserveAspect = true;
        return image;
    }

    private TextMeshProUGUI CreateButtonText(Transform parent, string objectName, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textGo = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);

        RectTransform rectTransform = textGo.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(64f, 4f);
        rectTransform.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.enableWordWrapping = true;
        return text;
    }

    private TextMeshProUGUI CreateManaCostText(Transform parent)
    {
        GameObject textGo = new GameObject("ManaCostText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);

        RectTransform rectTransform = textGo.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-6f, -4f);
        rectTransform.sizeDelta = new Vector2(40f, 24f);

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Right;
        text.fontSize = 18;
        text.color = new Color(0.45f, 0.85f, 1f, 1f);
        text.enableWordWrapping = false;
        return text;
    }

    private void ClearCardButtons()
    {
        for (int i = 0; i < spawnedCardButtons.Count; i++)
        {
            if (spawnedCardButtons[i] != null)
                Destroy(spawnedCardButtons[i].gameObject);
        }

        spawnedCardButtons.Clear();
    }

    private void BuildPlayer1CardsFromFallbacks()
    {
        IReadOnlyList<CardDefinition> selectedCards = SelectedCardLoadout.Cards;
        if (selectedCards != null && selectedCards.Count > 0)
        {
            AddCards(Player1Cards, selectedCards, SelectedCardLoadout.MaxSelectedCards);
            return;
        }

        AddCards(Player1Cards, DefaultPlayer1Cards, SelectedCardLoadout.MaxSelectedCards);
    }

    private IReadOnlyList<CardDefinition> GetDefaultPlayer2Cards()
    {
        if (levelConfig != null && levelConfig.Player2Cards.Count > 0)
            return levelConfig.Player2Cards;

        return DefaultPlayer2Cards;
    }

    private List<CardDefinition> GetPlayer2RandomCardPool()
    {
        if (levelConfig != null && levelConfig.Player2RandomCardPool.Count > 0)
            return levelConfig.Player2RandomCardPool;

        return Player2RandomCardPool;
    }

    private bool GetRandomizePlayer2CardsWhenBot()
    {
        if (levelConfig != null)
            return levelConfig.RandomizePlayer2CardsWhenBot;

        return RandomizePlayer2CardsWhenBot;
    }

    private int GetMaxPlayer2RandomCards()
    {
        if (levelConfig != null)
            return levelConfig.MaxPlayer2RandomCards;

        return MaxPlayer2RandomCards;
    }

    private void AddCards(List<CardDefinition> target, IEnumerable<CardDefinition> source, int maxCount)
    {
        if (target == null || source == null)
            return;

        int limit = maxCount > 0 ? maxCount : int.MaxValue;
        foreach (CardDefinition card in source)
        {
            if (card == null || target.Contains(card))
                continue;

            if (target.Count >= limit)
                break;

            target.Add(card);
        }
    }

    private void AddRandomCards(List<CardDefinition> target, List<CardDefinition> source, int maxCount)
    {
        if (target == null || source == null || source.Count == 0)
            return;

        int limit = Mathf.Min(Mathf.Max(1, maxCount), source.Count);
        int attempts = 0;
        while (target.Count < limit && attempts < source.Count * 2)
        {
            attempts++;

            CardDefinition card = source[Random.Range(0, source.Count)];
            if (card != null && !target.Contains(card))
                target.Add(card);
        }
    }
}
