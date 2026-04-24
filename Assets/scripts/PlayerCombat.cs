using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [System.Serializable]
    public class HitBoxData
    {
        public Vector2 size = new Vector2(1f, 0.8f);
        public Vector2 offset = new Vector2(0.8f, 0f);
        public int damage = 1;
        public float drawDuration = 0.1f;
    }

    [System.Serializable]
    public class AttackStep
    {
        public string name = "Attack";
        public HitBoxData hitBox = new HitBoxData();
        public float attackDuration = 0.35f;
        public float hitDelay = 0.1f;
    }

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference attackAction;
    public InputActionReference skillAction;

    [Header("References")]
    public Transform attackPoint;
    public LayerMask enemyLayer;
    public Animator animator;

    [Header("Combo Attack")]
    public AttackStep[] comboAttacks = new AttackStep[4];
    public float comboResetTime = 0.8f;

    [Header("Directional Attack")]
    public AttackStep upAttack = new AttackStep();
    public AttackStep downAttack = new AttackStep();
    public float directionThreshold = 0.5f;

    [Header("Skill")]
    public HitBoxData skillHitBox = new HitBoxData
    {
        size = new Vector2(1.5f, 1.2f),
        offset = new Vector2(1f, 0f),
        damage = 3,
        drawDuration = 0.15f
    };

    public float skillCooldown = 2f;
    public float skillDuration = 0.5f;
    public float skillHitDelay = 0.15f;

    private int comboIndex = 0;
    private bool isAttacking = false;
    private float lastAttackTime = -999f;
    private float skillTimer = 0f;

    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (animator == null)
            animator = GetComponent<Animator>();

        InitDefaultCombo();
        InitDirectionalAttacks();
    }

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Enable();

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
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Disable();

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
        if (skillTimer > 0f)
            skillTimer -= Time.deltaTime;

        if (!isAttacking && Time.time - lastAttackTime > comboResetTime)
            comboIndex = 0;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (isAttacking)
            return;

        Vector2 moveInput = Vector2.zero;

        if (moveAction != null && moveAction.action != null)
            moveInput = moveAction.action.ReadValue<Vector2>();

        if (moveInput.y > directionThreshold)
        {
            Debug.Log("upAtk");
            StartCoroutine(AttackRoutine(upAttack, 10));
        }
        else if (moveInput.y < -directionThreshold)
        {
            Debug.Log("DownAtk");
            StartCoroutine(AttackRoutine(downAttack, 11));
        }
        else
        {
            Debug.Log("forwardAtk");
            StartComboAttack();
        }
    }

    private void OnSkill(InputAction.CallbackContext context)
    {
        if (isAttacking || skillTimer > 0f)
            return;

        StartCoroutine(SkillRoutine());
    }

    private void StartComboAttack()
    {
        if (comboAttacks == null || comboAttacks.Length == 0)
            return;

        if (Time.time - lastAttackTime > comboResetTime)
            comboIndex = 0;

        StartCoroutine(AttackRoutine(comboAttacks[comboIndex], comboIndex + 1));

        comboIndex++;

        if (comboIndex >= comboAttacks.Length)
            comboIndex = 0;

        lastAttackTime = Time.time;
    }

    private IEnumerator AttackRoutine(AttackStep step, int attackIndex)
    {
        isAttacking = true;

        if (animator != null)
        {
            animator.SetInteger("AttackIndex", attackIndex);
            animator.SetTrigger("attack");
        }

        yield return new WaitForSeconds(step.hitDelay);

        DoHit(step.hitBox);

        float remainTime = step.attackDuration - step.hitDelay;

        if (remainTime > 0f)
            yield return new WaitForSeconds(remainTime);

        isAttacking = false;
    }

    private IEnumerator SkillRoutine()
    {
        isAttacking = true;
        skillTimer = skillCooldown;

        if (animator != null)
            animator.SetTrigger("skill");

        yield return new WaitForSeconds(skillHitDelay);

        DoHit(skillHitBox);

        float remainTime = skillDuration - skillHitDelay;

        if (remainTime > 0f)
            yield return new WaitForSeconds(remainTime);

        isAttacking = false;
    }

    private void DoHit(HitBoxData hitBox)
    {
        int dir = 1;

        if (playerController != null)
            dir = playerController.FacingDirection;

        Vector2 origin = attackPoint != null
            ? (Vector2)attackPoint.position
            : (Vector2)transform.position;

        Vector2 finalOffset = new Vector2(
            hitBox.offset.x * dir,
            hitBox.offset.y
        );

        Vector2 center = origin + finalOffset;

        DrawBox(center, hitBox.size, Color.red, hitBox.drawDuration);

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            center,
            hitBox.size,
            0f,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(hitBox.damage);
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

    private void InitDirectionalAttacks()
    {
        upAttack.name = "Up Attack";
        upAttack.hitBox.size = new Vector2(0.9f, 1.4f);
        upAttack.hitBox.offset = new Vector2(0f, 0.9f);
        upAttack.hitBox.damage = 1;
        upAttack.attackDuration = 0.35f;
        upAttack.hitDelay = 0.1f;

        downAttack.name = "Down Attack";
        downAttack.hitBox.size = new Vector2(0.9f, 1.2f);
        downAttack.hitBox.offset = new Vector2(0f, -0.8f);
        downAttack.hitBox.damage = 1;
        downAttack.attackDuration = 0.35f;
        downAttack.hitDelay = 0.1f;
    }

    private void InitDefaultCombo()
    {
        if (comboAttacks == null || comboAttacks.Length != 4)
            comboAttacks = new AttackStep[4];

        for (int i = 0; i < comboAttacks.Length; i++)
        {
            if (comboAttacks[i] == null)
                comboAttacks[i] = new AttackStep();

            if (comboAttacks[i].hitBox == null)
                comboAttacks[i].hitBox = new HitBoxData();
        }

        comboAttacks[0].name = "Attack 1";
        comboAttacks[0].hitBox.size = new Vector2(1f, 0.7f);
        comboAttacks[0].hitBox.offset = new Vector2(0.7f, 0f);
        comboAttacks[0].hitBox.damage = 1;
        comboAttacks[0].attackDuration = 0.3f;
        comboAttacks[0].hitDelay = 0.08f;

        comboAttacks[1].name = "Attack 2";
        comboAttacks[1].hitBox.size = new Vector2(1.2f, 0.8f);
        comboAttacks[1].hitBox.offset = new Vector2(0.8f, 0f);
        comboAttacks[1].hitBox.damage = 1;
        comboAttacks[1].attackDuration = 0.35f;
        comboAttacks[1].hitDelay = 0.1f;

        comboAttacks[2].name = "Attack 3";
        comboAttacks[2].hitBox.size = new Vector2(1.4f, 0.9f);
        comboAttacks[2].hitBox.offset = new Vector2(0.9f, 0f);
        comboAttacks[2].hitBox.damage = 1;
        comboAttacks[2].attackDuration = 0.4f;
        comboAttacks[2].hitDelay = 0.12f;

        comboAttacks[3].name = "Attack 4";
        comboAttacks[3].hitBox.size = new Vector2(1.6f, 1f);
        comboAttacks[3].hitBox.offset = new Vector2(1f, 0f);
        comboAttacks[3].hitBox.damage = 2;
        comboAttacks[3].attackDuration = 0.5f;
        comboAttacks[3].hitDelay = 0.15f;
    }
}