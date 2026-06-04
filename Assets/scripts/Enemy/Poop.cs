using UnityEngine;

public class Poop : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool landed = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (landed) return;

        bool hitGround = ((1 << collision.gameObject.layer) & groundLayer) != 0;

        if (hitGround)
        {
            landed = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }
    }
}