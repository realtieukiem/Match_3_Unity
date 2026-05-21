using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUi : MonoBehaviour
{
    public enum PlayerSlot
    {
        Player1,
        Player2
    }

    [Header("Source")]
    public GameController GameControllerReference;
    public PlayerSlot Slot;

    [Header("Texts")]
    public TextMeshProUGUI HealthText;
    public TextMeshProUGUI RageText;
    public TextMeshProUGUI ArmorText;
    public TextMeshProUGUI ManaText;

    [Header("Visual")]
    public Image CharacterImage;

    [Header("Fill Images")]
    public Image HealthFill;
    public Image RageFill;
    public Image ManaFill;

    [Header("Fill Tween")]
    public float FillTweenDuration = 0.25f;
    public Ease FillTweenEase = Ease.OutQuad;

    private PlayerControllerBase player;
    private bool hasStarted;
    private Tween healthFillTween;
    private Tween rageFillTween;
    private Tween manaFillTween;

    private void OnEnable()
    {
        if (hasStarted)
        {
            BindPlayer();
            Refresh(false);
        }
    }

    private void Start()
    {
        hasStarted = true;
        BindPlayer();
        Refresh(false);
    }

    private void OnDisable()
    {
        KillFillTweens();
        UnbindPlayer();
    }

    private void BindPlayer()
    {
        UnbindPlayer();

        if (GameControllerReference == null)
            GameControllerReference = GameController.Instance;

        if (GameControllerReference == null)
        {
            Debug.LogError($"{nameof(PlayerStatusUi)} requires a {nameof(GameController)} reference.", this);
            enabled = false;
            return;
        }

        player = Slot == PlayerSlot.Player1 ? GameControllerReference.Player1 : GameControllerReference.Player2;
        if (player == null)
        {
            Debug.LogError($"{nameof(PlayerStatusUi)} could not resolve {Slot}.", this);
            enabled = false;
            return;
        }

        player.StatsChanged += HandleStatsChanged;
    }

    private void UnbindPlayer()
    {
        if (player == null)
            return;

        player.StatsChanged -= HandleStatsChanged;
        player = null;
    }

    private void HandleStatsChanged()
    {
        Refresh(true);
    }

    private void Refresh(bool animateFill)
    {
        if (player == null)
            return;

        if (HealthText != null)
            HealthText.text = player.Health + "/" + player.MaxHealth;

        if (RageText != null)
            RageText.text = player.Rage + "/" + player.MaxRage;

        if (ArmorText != null)
            ArmorText.text = player.Armor.ToString();

        if (ManaText != null)
            ManaText.text = player.Mana + "/" + player.MaxMana;

        if (CharacterImage != null && player.Config != null)
        {
            CharacterImage.sprite = player.Config.CharacterSprite;
            CharacterImage.enabled = CharacterImage.sprite != null;
        }

        if (HealthFill != null)
            SetFillAmount(HealthFill, ref healthFillTween, GetFillAmount(player.Health, player.MaxHealth), animateFill);

        if (RageFill != null)
            SetFillAmount(RageFill, ref rageFillTween, GetFillAmount(player.Rage, player.MaxRage), animateFill);

        if (ManaFill != null)
            SetFillAmount(ManaFill, ref manaFillTween, GetFillAmount(player.Mana, player.MaxMana), animateFill);
    }

    private void SetFillAmount(Image fillImage, ref Tween fillTween, float targetFillAmount, bool animateFill)
    {
        fillTween?.Kill();

        if (!animateFill || FillTweenDuration <= 0f || !isActiveAndEnabled)
        {
            fillImage.fillAmount = targetFillAmount;
            fillTween = null;
            return;
        }

        fillTween = fillImage
            .DOFillAmount(targetFillAmount, FillTweenDuration)
            .SetEase(FillTweenEase)
            .SetLink(fillImage.gameObject);
    }

    private void KillFillTweens()
    {
        healthFillTween?.Kill();
        rageFillTween?.Kill();
        manaFillTween?.Kill();
        healthFillTween = null;
        rageFillTween = null;
        manaFillTween = null;
    }

    private float GetFillAmount(int currentValue, int maxValue)
    {
        if (maxValue <= 0)
            return 0f;

        return Mathf.Clamp01((float)currentValue / maxValue);
    }
}
