using UnityEngine;

public class EnemyAwake : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;   // 主角

    [Header("Detect Settings")]
    [SerializeField] private float detectDistance = 5f;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            Debug.LogError("沒有指定 Player，請在 Inspector 拖入主角！");
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < detectDistance)
        {
            animator.SetBool("awake", true);
        }
        else
        {
            animator.SetBool("awake", false);
        }
    }
}