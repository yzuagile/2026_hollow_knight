using UnityEngine;

public class EnemyAwake : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float detectDistance = 5f;
    [SerializeField] private float moveSpeed = 2f; // ���ʳt��
    [SerializeField] private GameObject Enemy;

    private Transform player;
    private Animator animator;

    private void Awake()
    {
        animator = Enemy.GetComponentInChildren<Animator>();

        // ۰ʴMҬ "Player"
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < detectDistance)
        {
            // 1. ����ʵe
            animator.SetBool("awake", true);

            // 2. ���沾��
            MoveTowardsPlayer();

            // 3. (��t) ���ĤH��V���a
            FlipSprite();
        }
        else
        {
            animator.SetBool("awake", false);
        }
    }

    private void MoveTowardsPlayer()
    {
        // �p��s��m�G�q���e��m�����a��m����
        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            moveSpeed * Time.deltaTime
        );
    }

    private void FlipSprite()
    {
        // �ھڪ��a�b�����٬O�k��A½��Ϥ�
        if (player.position.x > Enemy.transform.position.x)
            Enemy.transform.localScale = new Vector3(1, 1, 1); // Vk
        else
            Enemy.transform.localScale = new Vector3(-1, 1, 1); // V
    }
}