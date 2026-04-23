using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private InputSystem_Actions controls;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 moveInput;
    private bool jumpPressed;
    public bool IsBlocking { get; private set; }

    private Color originalColor;

    // 1 = 朝右, -1 = 朝左
    public int FacingDirection { get; private set; } = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        controls = new InputSystem_Actions();

        // 移動（Vector2）
        controls.Player.Move.performed += ctx =>
        {
            moveInput = ctx.ReadValue<Vector2>();
        };

        controls.Player.Move.canceled += ctx =>
        {
            moveInput = Vector2.zero;
        };

        // 跳躍（Button）
        controls.Player.Jump.performed += ctx =>
        {
            jumpPressed = true;
        };
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Update()
    {
        // 動畫速度參數：左右移動時切換 Idle / Run
        if (animator != null)
            animator.SetFloat("Speed", Mathf.Abs(moveInput.x));

        // 左右翻面 + 更新朝向
        if (moveInput.x > 0.01f)
        {
            spriteRenderer.flipX = false;
            FacingDirection = 1;
        }
        else if (moveInput.x < -0.01f)
        {
            spriteRenderer.flipX = true;
            FacingDirection = -1;
        }
        // 新版 Input System 的快速偵測鍵盤方式
        if (Keyboard.current.gKey.isPressed)
        {
            IsBlocking = true;
            spriteRenderer.color = Color.cyan;
        }
        else
        {
            IsBlocking = false;
            spriteRenderer.color = originalColor;
        }
    }

    private void FixedUpdate()
    {
        // 左右移動
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        // 跳躍
        if (jumpPressed && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        jumpPressed = false;
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
}