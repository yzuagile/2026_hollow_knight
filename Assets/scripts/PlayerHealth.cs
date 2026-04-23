using System.Drawing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class PlayerHealth : MonoBehaviour
{
    public int currentHealth = 100;
    public Slider healthSlider; // 在 Inspector 把 Slider 拖進來
    public float hitAnimeTime = 0.2f;
    SpriteRenderer sr;
    private PlayerController playerController; // 引用移動腳本來檢查格擋狀態

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
    }

    IEnumerator FlashRed(float duration)
    {
        sr.color = UnityEngine.Color.red;
        yield return new WaitForSeconds(duration);
        sr.color = UnityEngine.Color.white;
    }

    // 當兩個有 Collider 的物體「撞在一起」時會觸發
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 檢查撞到的物件標籤是不是 "Enemy"
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(10, collision.transform.position); // 扣 10 滴血
        }
    }

    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        if (playerController != null && playerController.IsBlocking)
        {
            // 計算敵人相對於玩家的方向
            int directionToAttacker = (attackerPosition.x > transform.position.x) ? 1 : -1;

            // 如果玩家朝向等於敵人方位，格擋成功
            if (playerController.FacingDirection == directionToAttacker)
            {
                Debug.Log("完美格擋！不扣血");
                // 這裡可以播放一個「鏘」的音效或藍色閃光
                return;
            }
            else {
                Debug.Log("格擋方向錯了，被背刺！");
            }
        }
        currentHealth -= damage;
        healthSlider.value = currentHealth; // 更新 UI
        Debug.Log("撞到怪了，剩餘血量：" + currentHealth);
        StartCoroutine(FlashRed(0.2f));
    }
}