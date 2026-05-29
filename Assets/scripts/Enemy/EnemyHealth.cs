using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    int currentHealth;
    int maxHealth = 4;
    [SerializeField] Slider healthBar;
    public float lastHitTime = 0;
    public float stunDuration = 0.2f;
    float knockback_resistant = 0.2f;
    Rigidbody2D rb;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;
    }

    public void TakeKnockback(float force, Vector2 dir, float duration = 0.2f)
    {
        force -= knockback_resistant;
        if (force <= 0) return;
        Debug.Log("tack knock back");
        Debug.Log(dir);
        Debug.Log(force);
        dir = dir.normalized;
        lastHitTime = Time.time;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dir * force, ForceMode2D.Impulse);

    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        healthBar.value = currentHealth;
        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }
}