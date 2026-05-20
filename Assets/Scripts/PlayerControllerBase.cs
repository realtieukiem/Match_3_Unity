using TMPro;
using UnityEngine;

public abstract class PlayerControllerBase : MonoBehaviour
{
    public Transform EffectTarget;
    public GameObject TurnArrow;

    public TMP_Text HealthText;
    public TMP_Text RageText;
    public TMP_Text ArmorText;
    public TMP_Text ManaText;

    public int MaxHealth = 200;
    public int MaxRage = 100;
    public int MaxMana = 100;
    public float MaxRageAttackMultiplier = 2f;

    public int Health { get; private set; }
    public int Rage { get; private set; }
    public int Armor { get; private set; }
    public int Mana { get; private set; }
    public bool IsDead { get; private set; }
    public virtual bool AllowsBoardInput
    {
        get { return false; }
    }

    protected virtual void OnValidate()
    {
        MaxHealth = Mathf.Max(1, MaxHealth);
        MaxRage = Mathf.Max(1, MaxRage);
        MaxMana = Mathf.Max(1, MaxMana);
        MaxRageAttackMultiplier = Mathf.Max(1f, MaxRageAttackMultiplier);
    }

    public Transform GetEffectTarget()
    {
        return EffectTarget != null ? EffectTarget : transform;
    }

    protected virtual void Awake()
    {
        Health = MaxHealth;
        Rage = 0;
        Armor = 0;
        Mana = 0;
        SetTurnIndicatorActive(false);
        RefreshTexts();
    }

    public virtual void StartTurn()
    {
        Armor = 0;
        SetTurnIndicatorActive(true);
        RefreshTexts();
    }

    public virtual void EndTurn()
    {
        SetTurnIndicatorActive(false);
        RefreshTexts();
    }

    public void SetTurnIndicatorActive(bool active)
    {
        if (TurnArrow != null)
            TurnArrow.SetActive(active);
    }

    public virtual void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        int damageAfterArmor = Mathf.Max(0, amount - Armor);
        Armor = Mathf.Max(0, Armor - amount);
        Health = Mathf.Max(0, Health - damageAfterArmor);

        if (Health <= 0)
            Die();

        RefreshTexts();
    }

    public virtual void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        Health = Mathf.Min(MaxHealth, Health + amount);
        RefreshTexts();
    }

    public virtual void AddRage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        Rage = Mathf.Min(MaxRage, Rage + amount);
        RefreshTexts();
    }

    public virtual int GetAttackDamageWithRageBonus(int baseDamage)
    {
        if (baseDamage <= 0)
            return 0;

        if (Rage < MaxRage)
            return baseDamage;

        Rage = 0;
        RefreshTexts();
        return Mathf.RoundToInt(baseDamage * MaxRageAttackMultiplier);
    }

    public virtual int DrainRage(int amount)
    {
        if (amount <= 0)
            return 0;

        int drained = Mathf.Min(Rage, amount);
        Rage -= drained;
        RefreshTexts();
        return drained;
    }

    public virtual void AddMana(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        Mana = Mathf.Min(MaxMana, Mana + amount);
        RefreshTexts();
    }

    public virtual int DrainMana(int amount)
    {
        if (amount <= 0)
            return 0;

        int drained = Mathf.Min(Mana, amount);
        Mana -= drained;
        RefreshTexts();
        return drained;
    }

    public virtual void AddArmor(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        Armor += amount;
        RefreshTexts();
    }

    public virtual void Die()
    {
        IsDead = true;
        Health = 0;
        RefreshTexts();
    }

    protected virtual void RefreshTexts()
    {
        if (HealthText != null)
            HealthText.text = Health + "/" + MaxHealth;

        if (RageText != null)
            RageText.text = Rage + "/" + MaxRage;

        if (ArmorText != null)
            ArmorText.text = Armor.ToString();

        if (ManaText != null)
            ManaText.text = Mana + "/" + MaxMana;
    }
}
