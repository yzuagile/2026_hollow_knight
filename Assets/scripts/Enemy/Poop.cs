using UnityEngine;

public class Poop : MonoBehaviour, IDamageable
{
    public static int activeUnflattenedPoopCount = 0;

    [Header("Ground Settings")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Flatten Settings")]
    [SerializeField] private float disappearAfterFlatten = 3f;
    [SerializeField] private Vector3 flattenedScale = new Vector3(1.4f, 0.35f, 1f);

    [Header("Stomp Settings")]
    [SerializeField] private float topCheckTolerance = 0.15f;

    private Rigidbody2D rb;
    private Collider2D poopCollider;

    private bool landed = false;
    private bool flattened = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        poopCollider = GetComponent<Collider2D>();

        activeUnflattenedPoopCount++;
    }

    private void OnDestroy()
    {
        if (!flattened)
        {
            activeUnflattenedPoopCount--;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (flattened) return;

        bool hitGround = ((1 << collision.gameObject.layer) & groundLayer) != 0;

        if (hitGround && !landed)
        {
            landed = true;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;

            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            TryFlattenByPlayer(collision);
        }
    }

    private void TryFlattenByPlayer(Collision2D collision)
    {
        Collider2D playerCollider = collision.collider;
        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

        if (playerCollider == null || poopCollider == null) return;

        // 玩家腳底是否在大便上方
        bool playerAbove =
            playerCollider.bounds.min.y >= poopCollider.bounds.max.y - topCheckTolerance;

        // 玩家是否正在往下掉或站在上面
        bool playerFalling =
            playerRb == null || playerRb.linearVelocity.y <= 0.1f;

        if (playerAbove && playerFalling)
        {
            Flatten();
        }
    }

    private void Flatten()
    {
        if (flattened) return;

        flattened = true;
        activeUnflattenedPoopCount--;

        // 踩扁後不要再當敵人傷害玩家
        gameObject.tag = "Untagged";
        gameObject.layer = LayerMask.NameToLayer("Default");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        transform.localScale = flattenedScale;

        Destroy(gameObject, disappearAfterFlatten);
    }

    public void TakeDamage(int damage)
    {
        Destroy(gameObject);
    }
}