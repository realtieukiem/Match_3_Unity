using UnityEngine;
using UnityEngine.UI;

public class PlayerCharacterView : MonoBehaviour
{
    public PlayerControllerBase Player;
    public Image CharacterImage;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (Player != null)
            Player.StatsChanged += HandleStatsChanged;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (Player != null)
            Player.StatsChanged -= HandleStatsChanged;
    }

    private void ResolveReferences()
    {
        if (Player == null)
            Player = GetComponentInParent<PlayerControllerBase>();

        if (CharacterImage == null)
            CharacterImage = GetComponent<Image>();
    }

    private void HandleStatsChanged()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (Player == null || Player.Config == null || CharacterImage == null)
            return;

        CharacterImage.sprite = Player.Config.CharacterSprite;
        CharacterImage.enabled = CharacterImage.sprite != null;
    }
}
