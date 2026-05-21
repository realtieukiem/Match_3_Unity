using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MapLevelButton : MonoBehaviour
{
    public MapMenuController MenuController;
    public LevelConfig LevelConfig;
    public Image IconImage;
    public TextMeshProUGUI NameText;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        Refresh();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(HandleClick);
        Refresh();
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    private void OnValidate()
    {
        Refresh();
    }

    private void HandleClick()
    {
        if (MenuController == null)
            MenuController = GetComponentInParent<MapMenuController>();

        if (MenuController == null)
        {
            Debug.LogError($"{nameof(MapLevelButton)} requires a {nameof(MapMenuController)}.", this);
            return;
        }

        MenuController.PlayLevel(LevelConfig);
    }

    private void Refresh()
    {
        if (LevelConfig == null)
            return;

        if (IconImage != null)
        {
            IconImage.sprite = LevelConfig.MapIcon;
            IconImage.enabled = IconImage.sprite != null;
        }

        if (NameText != null)
            NameText.text = LevelConfig.DisplayName;
    }
}
