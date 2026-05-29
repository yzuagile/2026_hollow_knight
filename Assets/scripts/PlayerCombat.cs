using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    // 你的攻擊特效圖片比例：1672 x 941
    private const float EffectAspect = 1672f / 941f;

    [System.Serializable]
    public class HitBoxData
    {
        [Header("Hit Box")]
        public Vector2 size = new Vector2(1f, 0.8f);
        public Vector2 offset = new Vector2(0.8f, 0f);
        public Vector2 forceDir = new Vector2(0f, 1f);
        public int damage = 1;
        public float knockbackforce = 5f;
        public float knockbacktime = 0.2f;
        public float drawDuration = 0.1f;

        [Header("Hit Box Aspect")]
        public bool useEffectAspect = true;

        [Tooltip("打勾：用高度推算寬度。取消：用寬度推算高度。")]
        public bool fitWidthByHeight = true;

        [Header("Effect")]
        public bool spawnEffect = true;

        [Tooltip("特效 X 軸旋轉。預設 0。")]
        public float effectRotationX = 0f;

        [Tooltip("特效 Y 軸旋轉。預設 0。Facing 左邊時，程式會自動加 180。")]
        public float effectRotationY = 0f;

        [Tooltip("特效 Z 軸旋轉。橫砍 0，上砍 90，下砍 -90。")]
        public float effectRotationZ = 0f;

        [Tooltip("單招特效大小倍率。覺得某一招太大或太小，就調這裡。")]
        public float effectScaleMultiplier = 1f;

        [Tooltip("特效位置微調。X 會根據角色左右方向翻轉。")]
        public Vector2 effectOffset = Vector2.zero;

        [Tooltip("是否根據角色左右方向翻轉特效。")]
        public bool flipByFacingDirection = true;
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

    [Header("Attack Effect Prefab")]
    public GameObject attackEffectPrefab;

    [Tooltip("是否生成攻擊特效。")]
    public bool spawnEffectOnHitBox = true;

    [Tooltip("全域特效大小倍率。覺得整體太大或太小，優先調這裡。")]
    public float globalEffectScaleMultiplier = 0.4f;

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
        size = new Vector2(0f, 1.2f),
        offset = new Vector2(1f, 0f),
        damage = 3,
        knockbackforce = 10f,
        drawDuration = 0.15f,
        useEffectAspect = true,
        fitWidthByHeight = true,
        spawnEffect = true,

        effectRotationX = 0f,
        effectRotationY = 0f,
        effectRotationZ = 0f,

        effectScaleMultiplier = 1.3f,
        effectOffset = Vector2.zero,
        flipByFacingDirection = true
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
        int dir = GetFacingDirection();

        Vector2 origin = attackPoint != null
            ? (Vector2)attackPoint.position
            : (Vector2)transform.position;

        Vector2 finalOffset = new Vector2(
            hitBox.offset.x * dir,
            hitBox.offset.y
        );

        Vector2 center = origin + finalOffset;

        Vector2 finalSize = GetFinalHitBoxSize(hitBox);

        DrawBox(center, finalSize, Color.red, hitBox.drawDuration);

        SpawnAttackEffect(center, hitBox, dir);

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            center,
            finalSize,
            0f,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            EnemyHealth damageable = hit.GetComponent<EnemyHealth>();

            if (damageable != null)
            {
                damageable.TakeDamage(hitBox.damage);
                damageable.TakeKnockback(hitBox.knockbackforce, new Vector2(hitBox.forceDir.x * dir, hitBox.forceDir.y));
                Debug.Log("Hit: " + hit.name);
            }
        }
    }

    private int GetFacingDirection()
    {
        if (playerController != null)
            return playerController.FacingDirection;

        return transform.localScale.x < 0f ? -1 : 1;
    }

    private Vector2 GetFinalHitBoxSize(HitBoxData hitBox)
    {
        Vector2 finalSize = hitBox.size;

        if (!hitBox.useEffectAspect)
            return finalSize;

        if (hitBox.fitWidthByHeight)
        {
            finalSize.x = finalSize.y * EffectAspect;
        }
        else
        {
            finalSize.y = finalSize.x / EffectAspect;
        }

        return finalSize;
    }

    private void SpawnAttackEffect(Vector2 hitBoxCenter, HitBoxData hitBox, int dir)
    {
        if (!spawnEffectOnHitBox)
            return;

        if (!hitBox.spawnEffect)
            return;

        if (attackEffectPrefab == null)
            return;

        Vector2 effectFinalOffset = new Vector2(
            hitBox.effectOffset.x * dir,
            hitBox.effectOffset.y
        );

        Vector2 spawnPosition = hitBoxCenter + effectFinalOffset;

        float finalRotationX = hitBox.effectRotationX;
        float finalRotationY = hitBox.effectRotationY;
        float finalRotationZ = hitBox.effectRotationZ;

        if (hitBox.flipByFacingDirection && dir < 0)
        {
            finalRotationY += 180f;
        }

        Quaternion rotation = Quaternion.Euler(
            finalRotationX,
            finalRotationY,
            finalRotationZ
        );

        GameObject effect = Instantiate(
            attackEffectPrefab,
            spawnPosition,
            rotation
        );

        float finalScale = globalEffectScaleMultiplier * hitBox.effectScaleMultiplier;

        effect.transform.localScale = new Vector3(
            finalScale,
            finalScale,
            1f
        );
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
        upAttack.hitBox.size = new Vector2(2.25f / EffectAspect, 1.5f * EffectAspect);
        upAttack.hitBox.offset = new Vector2(0.1f, 0.5f);
        upAttack.hitBox.damage = 1;
        upAttack.hitBox.forceDir = new Vector2(0.43f, 0.9f);
        upAttack.hitBox.knockbackforce = 4f;
        upAttack.hitBox.drawDuration = 0.1f;

        upAttack.hitBox.useEffectAspect = false;
        upAttack.hitBox.fitWidthByHeight = true;

        upAttack.hitBox.spawnEffect = true;
        upAttack.hitBox.effectRotationX = 0f;
        upAttack.hitBox.effectRotationY = 0f;
        upAttack.hitBox.effectRotationZ = 90f;

        upAttack.hitBox.effectScaleMultiplier = 0.2f;
        upAttack.hitBox.effectOffset = new Vector2(0.1f, -0.2f);
        upAttack.hitBox.flipByFacingDirection = true;

        upAttack.attackDuration = 0.35f;
        upAttack.hitDelay = 0.1f;


        downAttack.name = "Down Attack";
        downAttack.hitBox.size = new Vector2(2.25f / EffectAspect, 1.5f * EffectAspect);
        downAttack.hitBox.offset = new Vector2(0.1f, 0.2f);
        downAttack.hitBox.damage = 1;
        downAttack.hitBox.forceDir = new Vector2(0f, -1f);
        downAttack.hitBox.drawDuration = 0.1f;

        downAttack.hitBox.useEffectAspect = false;
        downAttack.hitBox.fitWidthByHeight = true;

        downAttack.hitBox.spawnEffect = true;
        downAttack.hitBox.effectRotationX = 0f;
        downAttack.hitBox.effectRotationY = 180f;
        downAttack.hitBox.effectRotationZ = -90f;

        downAttack.hitBox.effectScaleMultiplier = 0.2f;
        downAttack.hitBox.effectOffset = new Vector2(-0.1f, -0.2f);
        downAttack.hitBox.flipByFacingDirection = true;

        downAttack.attackDuration = 0.35f;
        downAttack.hitDelay = 0.1f;
    }

    private void InitDefaultCombo()
    {
        if (comboAttacks == null || comboAttacks.Length != 4)
            comboAttacks = new AttackStep[4];
        comboAttacks = new AttackStep[1];
        for (int i = 0; i < comboAttacks.Length; i++)
        {
            if (comboAttacks[i] == null)
                comboAttacks[i] = new AttackStep();

            if (comboAttacks[i].hitBox == null)
                comboAttacks[i].hitBox = new HitBoxData();
        }

        comboAttacks[0].name = "Attack 1";
        comboAttacks[0].hitBox.size = new Vector2(1.5f * EffectAspect, 2.25f / EffectAspect);
        comboAttacks[0].hitBox.offset = new Vector2(0.5f, -0.2f);
        comboAttacks[0].hitBox.damage = 1;
        comboAttacks[0].hitBox.forceDir = new Vector2(1f, 0f);
        comboAttacks[0].hitBox.drawDuration = 0.1f;
        comboAttacks[0].hitBox.useEffectAspect = false;
        comboAttacks[0].hitBox.fitWidthByHeight = true;
        comboAttacks[0].hitBox.spawnEffect = true;

        comboAttacks[0].hitBox.effectRotationX = 0f;
        comboAttacks[0].hitBox.effectRotationY = 0f;
        comboAttacks[0].hitBox.effectRotationZ = 0f;

        comboAttacks[0].hitBox.effectScaleMultiplier = 0.2f;
        comboAttacks[0].hitBox.effectOffset = new Vector2(-0.1f, 0.0f);
        comboAttacks[0].hitBox.flipByFacingDirection = true;
        comboAttacks[0].attackDuration = 0.3f;
        comboAttacks[0].hitDelay = 0.08f;

    }
}