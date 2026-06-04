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

    [Header("Fireball Skill 1")]
    public GameObject fireballPrefab;
    public Transform fireballSpawnPoint;
    public float fireballSpeed = 8f;
    public int fireballDamage = 1;
    public float fireballLifeTime = 5f;
    public float fireballCooldown = 1f;
    public float fireballTargetRadius = 6f;
    public LayerMask fireballObstacleLayer;

    // ─────────────────────────────────────────
    // [新增] Dash 設定
    // dashSpeed      : 位移速度，數字越大衝越快
    // dashDuration   : 最長衝刺時間（秒），時間到自動停
    // dashCooldown   : 冷卻時間（秒）
    // dashBodySize   : BoxCast 用的角色碰撞箱大小，依實際角色大小調整
    // dashObstacleLayer : 勾選「會擋住 Dash 的 Layer」，Enemy 不勾
    // ─────────────────────────────────────────
    [Header("Dash (C 鍵)")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public Vector2 dashBodySize = new Vector2(0.4f, 0.8f);

    [Tooltip("勾選會擋住 Dash 的 Layer。Enemy Layer 不要勾，其他地形、石頭都勾。")]
    public LayerMask dashObstacleLayer;

    // Dash 冷卻計時器（倒數到 0 才能再次 Dash）
    private float dashCooldownTimer = 0f;

    // ─────────────────────────────────────────
    // [新增] 引用 PlayerHealth 以控制無敵狀態
    // ─────────────────────────────────────────
    private PlayerHealth playerHealth;

    // ─────────────────────────────────────────
    // [新增] 用來監聽 C 鍵（Crouch Action）的 Input
    // ─────────────────────────────────────────
    private InputSystem_Actions dashInput;

    private int comboIndex = 0;
    private bool isAttacking = false;
    private float lastAttackTime = -999f;
    private float fireballCooldownTimer = 0f;

    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>(); // [新增] 取得 PlayerHealth

        if (animator == null)
            animator = GetComponent<Animator>();

        // [新增] 初始化 Dash 用的 Input（監聽 C 鍵 Crouch Action）
        dashInput = new InputSystem_Actions();

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

        // [新增] 啟用 C 鍵 Dash 監聽
        dashInput.Player.Crouch.performed += OnDash;
        dashInput.Player.Skill1.performed += OnSkill1;
        dashInput.Enable();
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

        // [新增] 停用 C 鍵 Dash 監聽
        dashInput.Player.Crouch.performed -= OnDash;
        dashInput.Player.Skill1.performed -= OnSkill1;
        dashInput.Disable();
    }

    private void Update()
    {
        if (fireballCooldownTimer > 0f)
            fireballCooldownTimer = Mathf.Max(0f, fireballCooldownTimer - Time.deltaTime);

        // [新增] Dash 冷卻倒數
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;
        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.deltaTime);

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

    private void OnSkill1(InputAction.CallbackContext context)
    {
        if (fireballCooldownTimer > 0f)
            return;

        SpawnFireball();
    }

    // ─────────────────────────────────────────
    // [新增] C 鍵觸發 Dash
    // isAttacking 或冷卻中都不能觸發
    // ─────────────────────────────────────────
    private void OnDash(InputAction.CallbackContext context)
    {
        Debug.Log("C鍵，isAttacking=" + isAttacking + " cooldown=" + dashCooldownTimer);
        
        if (isAttacking || dashCooldownTimer > 0f)
            return;

        StartCoroutine(DashRoutine());
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

    private void SpawnFireball()
    {
        if (fireballPrefab == null)
        {
            Debug.LogWarning("Fireball prefab is not assigned.");
            return;
        }

        fireballCooldownTimer = fireballCooldown;

        int dir = GetFacingDirection();
        Vector2 direction = new Vector2(dir, 0f);
        Vector2 playerCenter = GetPlayerCenter();
        Vector2 spawnPosition = fireballSpawnPoint != null
            ? (Vector2)fireballSpawnPoint.position
            : playerCenter;

        Transform target = FindNearestEnemyTarget(playerCenter);
        GameObject fireballObject = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);
        fireball fireballScript = fireballObject.GetComponent<fireball>();

        if (fireballScript == null)
        {
            Debug.LogWarning("Fireball prefab does not have a fireball component.");
            return;
        }

        fireballScript.Init(
            fireballSpeed,
            direction,
            target,
            enemyLayer,
            fireballObstacleLayer,
            fireballDamage,
            fireballLifeTime
        );
    }

    private Transform FindNearestEnemyTarget(Vector2 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, fireballTargetRadius, enemyLayer);
        EnemyHealth nearestEnemy = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null)
                continue;

            Vector2 enemyCenter = hit.bounds.center;
            float distanceSqr = (enemyCenter - center).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemyHealth;
            }
        }

        return nearestEnemy != null ? nearestEnemy.transform : null;
    }

    private Vector2 GetPlayerCenter()
    {
        Collider2D playerCollider = GetComponent<Collider2D>();

        if (playerCollider != null)
            return playerCollider.bounds.center;

        return transform.position;
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

    // ─────────────────────────────────────────
    // [新增] Dash 主邏輯
    //
    // 流程：
    //   1. 鎖定 isAttacking，防止 Dash 中再次觸發攻擊或 Dash
    //   2. 開啟無敵（PlayerHealth.isInvincible = true）
    //   3. 每個 FixedUpdate 用 BoxCast 往前偵測
    //      - 碰到 dashObstacleLayer 的物體 → 立即停止
    //      - 沒碰到 → 繼續移動
    //   4. 超過 dashDuration 時間 → 自動停止
    //   5. 關閉無敵，解鎖 isAttacking，重置冷卻計時器
    // ─────────────────────────────────────────
    private IEnumerator DashRoutine()
    {
        isAttacking = true;
        dashCooldownTimer = dashCooldown;

        if (playerHealth != null)
            playerHealth.isInvincible = true;

        // [新增] Dash 期間關掉玩家跟 Enemy layer 的碰撞
        int playerLayer = gameObject.layer;
        int enemyLayer_index = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer_index, true);

        int dir = GetFacingDirection();
        float elapsed = 0f;
        float stepDistance = dashSpeed * Time.fixedDeltaTime;

        while (elapsed < dashDuration)
        {
            // 把 0.1f 改成 dashBodySize.y * 0.5f，剛好偏移到角色中心
            RaycastHit2D hit = Physics2D.BoxCast(
                transform.position + new Vector3(0f, dashBodySize.y * 0.5f, 0f),
                new Vector2(dashBodySize.x, dashBodySize.y * 0.5f), // 高度縮小一半，只掃身體中段
                0f,
                new Vector2(dir, 0f),
                stepDistance,
                dashObstacleLayer
            );

            if (hit.collider != null)
            {
                Debug.Log("Dash 撞到：" + hit.collider.name);
                break;
            }

            transform.position += new Vector3(dir * stepDistance, 0f, 0f);
            elapsed += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        // [新增] Dash 結束，恢復跟 Enemy 的碰撞
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer_index, false);

        if (playerHealth != null)
            playerHealth.isInvincible = false;

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
