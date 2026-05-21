using UnityEngine;

[CreateAssetMenu(menuName = "Match3/Cards/Card Definition")]
public class CardDefinition : ScriptableObject
{
    [Header("Display")]
    public string DisplayName = "Card";
    [TextArea]
    public string Description;
    public Sprite Icon;

    [Header("Effect")]
    public CardEffectType EffectType;
    public int Amount = 10;
    public int ManaCost;
    public bool UseRageAttackMultiplier;

    private void OnValidate()
    {
        Amount = Mathf.Max(0, Amount);
        ManaCost = Mathf.Max(0, ManaCost);
    }

    public bool CanUse(PlayerControllerBase owner, PlayerControllerBase opponent)
    {
        return CanApply(owner, opponent)
            && owner.Mana >= ManaCost;
    }

    public void ConsumeCost(PlayerControllerBase owner)
    {
        if (owner == null || ManaCost <= 0)
            return;

        owner.DrainMana(ManaCost);
    }

    public void Apply(PlayerControllerBase owner, PlayerControllerBase opponent)
    {
        if (!CanApply(owner, opponent))
            return;

        switch (EffectType)
        {
            case CardEffectType.Heal:
                owner.Heal(Amount);
                break;
            case CardEffectType.Rage:
                owner.AddRage(Amount);
                break;
            case CardEffectType.Mana:
                owner.AddMana(Amount);
                break;
            case CardEffectType.Damage:
                ApplyDamage(owner, opponent);
                break;
            case CardEffectType.Armor:
                owner.AddArmor(Amount);
                break;
        }
    }

    private bool CanApply(PlayerControllerBase owner, PlayerControllerBase opponent)
    {
        return owner != null
            && opponent != null
            && !owner.IsDead
            && !opponent.IsDead
            && Amount > 0;
    }

    private void ApplyDamage(PlayerControllerBase owner, PlayerControllerBase opponent)
    {
        int damage = UseRageAttackMultiplier
            ? owner.GetAttackDamageWithRageBonus(Amount)
            : Amount;

        opponent.TakeDamage(damage);
    }
}
