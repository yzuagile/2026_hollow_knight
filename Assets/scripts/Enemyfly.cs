using UnityEngine;

public class FlyingEnemyController : MonoBehaviour
{
    [Header("飛行設定")]
    public float moveSpeed = 2f;
    public Transform pointA;
    public Transform pointB;

    private bool movingToB;
    private float fixedY;
    private SpriteRenderer sr;

    private void Start()
    {
        fixedY = transform.position.y;
        sr = GetComponentInChildren<SpriteRenderer>();

        // 初始方向：朝向 X 軸離得較遠的點
        float distA = Mathf.Abs(transform.position.x - pointA.position.x);
        float distB = Mathf.Abs(transform.position.x - pointB.position.x);
        movingToB = distB > distA;
        UpdateSpriteDirection();
    }

    private void Update()
    {
        if (pointA == null || pointB == null) return;

        Vector3 target = movingToB ? pointB.position : pointA.position;
        target.y = fixedY; // 固定水平巡邏

        // 移動
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, step);

        // 到達目標點就切換方向
        if (Mathf.Abs(transform.position.x - target.x) < 0.05f)
        {
            movingToB = !movingToB;
            UpdateSpriteDirection();
        }
    }

    private void UpdateSpriteDirection()
    {
        if (sr == null) return;
        sr.flipX = !movingToB;
    }
}