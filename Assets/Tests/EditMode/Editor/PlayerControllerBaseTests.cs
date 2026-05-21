using NUnit.Framework;
using UnityEngine;

public class PlayerControllerBaseTests
{
    [Test]
    public void Configure_StartsHealthAtEightyPercentOfMaxHealth()
    {
        GameObject playerObject = new GameObject("Player");
        PlayerController player = playerObject.AddComponent<PlayerController>();
        PlayerConfig config = ScriptableObject.CreateInstance<PlayerConfig>();
        config.MaxHealth = 100;

        try
        {
            player.Configure(config);

            Assert.That(player.Health, Is.EqualTo(80));
            Assert.That(player.MaxHealth, Is.EqualTo(100));
        }
        finally
        {
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(playerObject);
        }
    }
}
