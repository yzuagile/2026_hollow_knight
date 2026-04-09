using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int currentHealth = 100;
    public Slider healthSlider; // 在 Inspector 把 Slider 拖進來

    // 當兩個有 Collider 的物體「撞在一起」時會觸發
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 檢查撞到的物件標籤是不是 "Enemy"
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(10); // 扣 10 滴血
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        healthSlider.value = currentHealth; // 更新 UI
        Debug.Log("撞到怪了，剩餘血量：" + currentHealth);
    }
}