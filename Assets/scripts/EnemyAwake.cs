using UnityEngine;

public class EnemyAwake : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float detectDistance = 5f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] EnemyHealth enemyHealth;

    public Transform EnemyVisual; // 用來翻轉的物件
    private Transform player;
    private Animator animator;
    private Rigidbody2D rb; // 必須使用它來移動

    public float stopDistance = 0.5f; // 距離玩家多近就停止移動

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>(); // 取得 Rigidbody

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null) return;
        if (Time.time - enemyHealth.lastHitTime <= enemyHealth.stunDuration) return;
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < detectDistance)
        {
            animator.SetBool("awake", true);
            MoveTowardsPlayer();
            FlipSprite();
        }
        else
        {
            animator.SetBool("awake", false);
            // 沒看到人時，把速度歸零，否則牠會滑行
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void MoveTowardsPlayer()
    {
        // 決定方向：1 是右，-1 是左
        float direction = (player.position.x > transform.position.x) ? 1f : -1f;
        Collider2D enemyCollider = GetComponent<Collider2D>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        float enemyCenterX = enemyCollider != null ? enemyCollider.bounds.center.x : transform.position.x;
        float playerCenterX = playerCollider != null ? playerCollider.bounds.center.x : player.position.x;

        if (Mathf.Abs(playerCenterX - enemyCenterX) <= stopDistance) direction = 0f;

        // 【關鍵改動】使用物理速度移動，保留原本的 Y 軸速度（受重力影響）
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    private void FlipSprite()
    {
      

        if (player.position.x > transform.position.x)
            EnemyVisual.localScale = new Vector3(3.840232f, EnemyVisual.localScale.y, EnemyVisual.localScale.z);
        else
            EnemyVisual.localScale = new Vector3(-3.840232f, EnemyVisual.localScale.y, EnemyVisual.localScale.z);
    }
}
