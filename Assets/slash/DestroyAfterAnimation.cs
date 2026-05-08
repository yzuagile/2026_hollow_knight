using UnityEngine;

public class DestroyAfterAnimation : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (animator == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // normalizedTime >= 1 代表動畫播完一次
        if (stateInfo.normalizedTime >= 1f)
        {
            Destroy(gameObject);
        }
    }
}