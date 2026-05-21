using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CardButtonView : MonoBehaviour
{
    public Image IconImage;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI DescriptionText;
    public TextMeshProUGUI ManaCostText;
    public float DisabledAlpha = 0.45f;

    private Button button;
    private CanvasGroup canvasGroup;
    private CardController cardController;
    private PlayerControllerBase owner;
    private CardDefinition card;

    private void Awake()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(CardController controller, PlayerControllerBase cardOwner, CardDefinition definition, bool interactable)
    {
        cardController = controller;
        owner = cardOwner;
        card = definition;

        if (button == null)
            button = GetComponent<Button>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        button.interactable = interactable;
        canvasGroup.alpha = interactable ? 1f : DisabledAlpha;

        if (IconImage != null)
        {
            IconImage.sprite = card != null ? card.Icon : null;
            IconImage.enabled = IconImage.sprite != null;
        }

        if (NameText != null)
            NameText.text = card != null ? card.DisplayName : string.Empty;

        if (DescriptionText != null)
            DescriptionText.text = card != null ? card.Description : string.Empty;

        if (ManaCostText != null)
            ManaCostText.text = card != null && card.ManaCost > 0 ? card.ManaCost.ToString() : string.Empty;
    }

    private void HandleClick()
    {
        if (cardController == null || owner == null || card == null)
            return;

        cardController.TryUseCard(card, owner);
    }
}
