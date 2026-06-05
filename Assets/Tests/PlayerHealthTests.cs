using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthTests
{
    private GameObject playerObject;
    private GameObject sliderObject;
    private PlayerHealth playerHealth;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("Test Player");
        playerObject.AddComponent<SpriteRenderer>();
        playerHealth = playerObject.AddComponent<PlayerHealth>();

        sliderObject = new GameObject("Test Health Slider");
        Slider healthSlider = sliderObject.AddComponent<Slider>();
        healthSlider.maxValue = 100;
        healthSlider.value = 100;

        playerHealth.healthSlider = healthSlider;
        playerHealth.currentHealth = 100;
        InvokeAwake(playerHealth);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(playerObject);
        UnityEngine.Object.DestroyImmediate(sliderObject);
    }

    [Test]
    public void TakeDamage_DecreasesHealthAndUpdatesSlider()
    {
        playerHealth.TakeDamage(10, Vector3.right);

        Assert.AreEqual(90, playerHealth.currentHealth);
        Assert.AreEqual(90, playerHealth.healthSlider.value);
    }

    [Test]
    public void TakeDamage_WhenInvincible_DoesNotChangeHealthOrSlider()
    {
        playerHealth.isInvincible = true;

        playerHealth.TakeDamage(10, Vector3.right);

        Assert.AreEqual(100, playerHealth.currentHealth);
        Assert.AreEqual(100, playerHealth.healthSlider.value);
    }

    private static void InvokeAwake(PlayerHealth target)
    {
        typeof(PlayerHealth)
            .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(target, null);
    }
}
