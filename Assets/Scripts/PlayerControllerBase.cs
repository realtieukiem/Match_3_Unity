using System;
using UnityEngine;

public abstract class PlayerControllerBase : MonoBehaviour
{
    public Transform EffectTarget;
    public GameObject TurnArrow;

    public int MaxHealth = 200;
    public int MaxRage = 100;
    public int MaxMana = 100;
    public float MaxRageAttackMultiplier = 2f;

    public int Health { get; private set; }
    public int Rage { get; private set; }
    public int Armor { get; private set; }
    public int Mana { get; private set; }
    public bool IsDead { get; private set; }
    public PlayerConfig Config { get; private set; }
    public event Action StatsChanged;

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

    public virtual void Configure(PlayerConfig config)
    {
        Config = config;
        if (config != null)
        {
            MaxHealth = config.MaxHealth;
            MaxRage = config.MaxRage;
            MaxMana = config.MaxMana;
            MaxRageAttackMultiplier = config.MaxRageAttackMultiplier;
        }

        ResetStats();
    }

    protected virtual void Awake()
    {
        ResetStats();
        SetTurnIndicatorActive(false);
    }

    public virtual void StartTurn()
    {
        Armor = 0;
        SetTurnIndicatorActive(true);
        NotifyStatsChanged();
    }

    public virtual void EndTurn()
    {
        SetTurnIndicatorActive(false);
        NotifyStatsChanged();
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
        {
            Die();
            return;
        }

        NotifyStatsChanged();
    }

    public virtual void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        Health = Mathf.Min(MaxHealth, Health + amount);
        NotifyStatsChanged();
    }

    public virtual void AddRage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        Rage = Mathf.Min(MaxRage, Rage + amount);
        NotifyStatsChanged();
    }

    public virtual int GetAttackDamageWithRageBonus(int baseDamage)
    {
        if (baseDamage <= 0)
            return 0;

        if (Rage < MaxRage)
            return baseDamage;

        Rage = 0;
        NotifyStatsChanged();
        return Mathf.RoundToInt(baseDamage * MaxRageAttackMultiplier);
    }

    public virtual int DrainRage(int amount)
    {
        if (amount <= 0)
            return 0;

        int drained = Mathf.Min(Rage, amount);
        Rage -= drained;
        NotifyStatsChanged();
        return drained;
    }

    public virtual void AddMana(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        Mana = Mathf.Min(MaxMana, Mana + amount);
        NotifyStatsChanged();
    }

    public virtual int DrainMana(int amount)
    {
        if (amount <= 0)
            return 0;

        int drained = Mathf.Min(Mana, amount);
        Mana -= drained;
        NotifyStatsChanged();
        return drained;
    }

    public virtual void AddArmor(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        Armor += amount;
        NotifyStatsChanged();
    }

    public virtual void Die()
    {
        IsDead = true;
        Health = 0;
        NotifyStatsChanged();
    }

    protected void NotifyStatsChanged()
    {
        if (StatsChanged != null)
            StatsChanged.Invoke();
    }

    protected void ResetStats()
    {
        Health = Mathf.CeilToInt(MaxHealth * 0.8f);
        Rage = 0;
        Armor = 0;
        Mana = 0;
        IsDead = false;
        NotifyStatsChanged();
    }
}
