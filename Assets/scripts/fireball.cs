using UnityEngine;

public class fireball : MonoBehaviour
{
    [System.Serializable]
    public class FireballData
    {
        public float speed = 8f;
        public Vector2 direction = Vector2.right;
        public Transform trackingTarget;
        public LayerMask enemyLayer;
        public LayerMask obstacleLayer;
        public int damage = 1;
        public float lifeTime = 5f;
    }

    public FireballData data = new FireballData();

    private float aliveTime;

    private void Start()
    {
        data.direction = GetSafeDirection(data.direction);
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

        if (aliveTime >= data.lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        UpdateDirection();
        transform.position += (Vector3)(data.direction * data.speed * Time.deltaTime);
    }

    public void Init(
        float speed,
        Vector2 direction,
        Transform trackingTarget,
        LayerMask enemyLayer,
        LayerMask obstacleLayer,
        int damage = 1,
        float lifeTime = 5f)
    {
        data.speed = speed;
        data.direction = GetSafeDirection(direction);
        data.trackingTarget = trackingTarget;
        data.enemyLayer = enemyLayer;
        data.obstacleLayer = obstacleLayer;
        data.damage = damage;
        data.lifeTime = lifeTime;
        aliveTime = 0f;
    }

    public void Init(FireballData fireballData)
    {
        data.speed = fireballData.speed;
        data.direction = GetSafeDirection(fireballData.direction);
        data.trackingTarget = fireballData.trackingTarget;
        data.enemyLayer = fireballData.enemyLayer;
        data.obstacleLayer = fireballData.obstacleLayer;
        data.damage = fireballData.damage;
        data.lifeTime = fireballData.lifeTime;
        aliveTime = 0f;
    }

    private void UpdateDirection()
    {
        if (data.trackingTarget != null)
            data.direction = GetSafeDirection(data.trackingTarget.position - transform.position);

        FaceDirection(data.direction);
    }

    private void FaceDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Vector2 GetSafeDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0f)
            return Vector2.right;

        return direction.normalized;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider2D other)
    {
        if (IsInLayerMask(other.gameObject.layer, data.enemyLayer))
        {
            EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
                enemyHealth.TakeDamage(data.damage);

            Destroy(gameObject);
            return;
        }

        if (IsInLayerMask(other.gameObject.layer, data.obstacleLayer))
            Destroy(gameObject);
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}
