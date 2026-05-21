using UnityEngine;

[CreateAssetMenu(menuName = "Match3/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("Prefab")]
    public PlayerControllerBase PlayerPrefab;

    [Header("Visual")]
    public Sprite CharacterSprite;

    [Header("Stats")]
    public int MaxHealth = 200;
    public int MaxRage = 100;
    public int MaxMana = 100;
    public float MaxRageAttackMultiplier = 2f;

    [Header("Bot")]
    public float BotThinkDelay = 0.6f;

    private void OnValidate()
    {
        MaxHealth = Mathf.Max(1, MaxHealth);
        MaxRage = Mathf.Max(1, MaxRage);
        MaxMana = Mathf.Max(1, MaxMana);
        MaxRageAttackMultiplier = Mathf.Max(1f, MaxRageAttackMultiplier);
        BotThinkDelay = Mathf.Max(0f, BotThinkDelay);
    }
}
