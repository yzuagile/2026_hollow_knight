using UnityEngine;
using System.Collections;

public class CatAssaultAI : MonoBehaviour, IDamageable
{
    [Header("Movement Settings")]
    [SerializeField] private float detectDistance = 7f; 
    [SerializeField] private float moveSpeed = 1.5f;    
    [SerializeField] private float rushSpeed = 6f;      
    [SerializeField] private float rushDuration = 0.8f; 
    [SerializeField] private float rushCooldown = 3f;   

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 5; // 踩 5 次死掉
    private int currentHealth;

    private Transform player;
    private Animator animator;
    private bool isRushing = false;
    private float lastRushTime;
    private bool isDead = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // 如果死掉了或是正在暴衝，就不執行一般邏輯
        if (player == null || isRushing || isDead) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < detectDistance)
        {
            animator.SetBool("awake", true);
            FlipSprite(); 

            if (Time.time > lastRushTime + rushCooldown && distance < 4f)
            {
                StartCoroutine(RushAttack());
            }
            else
            {
                MoveTowardsPlayer(moveSpeed);
            }
        }
        else
        {
            animator.SetBool("awake", false);
        }
    }

    IEnumerator RushAttack()
    {
        isRushing = true;
        lastRushTime = Time.time;

        FlipSprite(); 
        animator.SetTrigger("attack"); 

        Vector2 targetDir = (player.position - transform.position).normalized;
        float timer = 0f;

        while (timer < rushDuration)
        {
            transform.Translate(targetDir * rushSpeed * Time.deltaTime, Space.World);
            timer += Time.deltaTime;
            yield return null;
        }

        isRushing = false;
    }

    private void MoveTowardsPlayer(float speed)
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            speed * Time.deltaTime
        );
    }

    private void FlipSprite()
    {
        if (player.position.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }

    // 當玩家撞到貓時觸發（踩頭偵測）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // 檢查碰撞法線，判斷玩家是否從上方落下
                if (contact.normal.y < -0.5f) 
                {
                    TakeDamage(1);
                    
                    // 讓玩家彈起來，增加打擊感
                    Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        // 清除舊速度並向上彈起
                        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 8f);
                    }
                    break; 
                }
            }
        }
    }

    // 實作 IDamageable 介面（由玩家攻擊或踩頭呼叫）
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log("貓受傷了！剩餘次數：" + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        isRushing = false;
        
        // 1. 觸發死亡動畫
        animator.SetTrigger("die"); 
        
        // 2. 讓屍體倒在地上，不會再跟玩家碰撞 (避免玩家卡在屍體上)
        // 我們將 Collider 設為 Trigger，這樣玩家可以穿過它，但它依然存在於場景
        GetComponent<Collider2D>().isTrigger = true;

        // 3. 停止物理模擬，防止屍體被推走或滑動
        if (GetComponent<Rigidbody2D>() != null)
        {
            GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            GetComponent<Rigidbody2D>().simulated = false; 
        }

        // 4. 重要：刪除這行 Destroy，屍體就會永遠留著！
        // Destroy(gameObject, 1.0f); 
    }
}