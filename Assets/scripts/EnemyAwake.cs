using UnityEngine;

public class EnemyAwake : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float detectDistance = 5f;
    [SerializeField] private float moveSpeed = 2f; // 移動速度

    private Transform player;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        // 自動尋找標籤為 "Player" 的物件
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < detectDistance)
        {
            // 1. 播放動畫
            animator.SetBool("awake", true);

            // 2. 執行移動
            MoveTowardsPlayer();

            // 3. (選配) 讓敵人轉向玩家
            FlipSprite();
        }
        else
        {
            animator.SetBool("awake", false);
        }
    }

    private void MoveTowardsPlayer()
    {
        // 計算新位置：從當前位置往玩家位置移動
        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            moveSpeed * Time.deltaTime
        );
    }

    private void FlipSprite()
    {
        // 根據玩家在左邊還是右邊，翻轉圖片
        if (player.position.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1); // 面向右
        else
            transform.localScale = new Vector3(-1, 1, 1); // 面向左
    }
}