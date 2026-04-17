using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference attackAction;
    public InputActionReference skillAction;

    [Header("References")]
    public Transform attackPoint;
    public LayerMask enemyLayer;
    public Animator animator;

    [Header("Normal Attack")]
    public int attackDamage = 1;
    public Vector2 attackSize = new Vector2(1f, 0.8f);
    public float attackCooldown = 0.3f;
    public float attackOffsetX = 0.8f;

    [Header("Skill")]
    public int skillDamage = 3;
    public Vector2 skillSize = new Vector2(1.5f, 1.2f);
    public float skillCooldown = 2f;
    public float skillOffsetX = 1f;

    private float attackTimer;
    private float skillTimer;
    private bool isAttacking;

    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (attackAction != null && attackAction.action != null)
        {
            attackAction.action.performed += OnAttack;
            attackAction.action.Enable();
        }

        if (skillAction != null && skillAction.action != null)
        {
            skillAction.action.performed += OnSkill;
            skillAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (attackAction != null && attackAction.action != null)
        {
            attackAction.action.performed -= OnAttack;
            attackAction.action.Disable();
        }

        if (skillAction != null && skillAction.action != null)
        {
            skillAction.action.performed -= OnSkill;
            skillAction.action.Disable();
        }
    }

    private void Update()
    {
        attackTimer -= Time.deltaTime;
        skillTimer -= Time.deltaTime;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (attackTimer > 0f || isAttacking)
            return;

        StartAttack();
    }

    private void OnSkill(InputAction.CallbackContext context)
    {
        if (skillTimer > 0f || isAttacking)
            return;

        StartSkill();
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        if (animator != null)
            animator.SetTrigger("attack");

        // 目前先直接判定
        DoHit(attackSize, attackDamage, attackOffsetX);

        EndAttack();
    }

    private void StartSkill()
    {
        isAttacking = true;
        skillTimer = skillCooldown;

        if (animator != null)
            animator.SetTrigger("skill");

        // 目前先直接判定
        DoHit(skillSize, skillDamage, skillOffsetX);

        EndAttack();
    }

    private void DoHit(Vector2 size, int damage, float offsetX)
    {
        int dir = 1;

        if (playerController != null)
            dir = playerController.FacingDirection;

        Vector2 origin = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
        Vector2 center = origin + new Vector2(dir * offsetX, 0f);

        DrawBox(center, size, Color.red, 0.1f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            center,
            size,
            0f,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                Debug.Log("Hit: " + hit.name);
            }
        }
    }

    private void DrawBox(Vector2 center, Vector2 size, Color color, float duration)
    {
        Vector2 half = size / 2f;

        Vector2 topLeft = center + new Vector2(-half.x, half.y);
        Vector2 topRight = center + new Vector2(half.x, half.y);
        Vector2 bottomLeft = center + new Vector2(-half.x, -half.y);
        Vector2 bottomRight = center + new Vector2(half.x, -half.y);

        Debug.DrawLine(topLeft, topRight, color, duration);
        Debug.DrawLine(topRight, bottomRight, color, duration);
        Debug.DrawLine(bottomRight, bottomLeft, color, duration);
        Debug.DrawLine(bottomLeft, topLeft, color, duration);
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = attackPoint != null ? attackPoint.position : transform.position;

        int dir = 1;

        if (Application.isPlaying && playerController != null)
            dir = playerController.FacingDirection;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(origin + new Vector3(dir * attackOffsetX, 0f, 0f), attackSize);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(origin + new Vector3(dir * skillOffsetX, 0f, 0f), skillSize);
    }
}