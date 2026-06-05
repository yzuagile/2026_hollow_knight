using UnityEngine;

public class fireballHitable : MonoBehaviour
{
    public bool canBeTargeted = true;
    public Transform targetPoint;

    public Transform TargetTransform
    {
        get
        {
            if (targetPoint != null)
                return targetPoint;

            return transform;
        }
    }

    public Vector2 TargetCenter
    {
        get
        {
            Collider2D targetCollider = GetComponent<Collider2D>();

            if (targetCollider != null)
                return targetCollider.bounds.center;

            return TargetTransform.position;
        }
    }
}
