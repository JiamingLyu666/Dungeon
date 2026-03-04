using UnityEngine;

public class PlayerAnimDriver : MonoBehaviour
{
    public Animator animator;
    public float moveThreshold = 0.01f;

    private Vector3 lastPosition;
    private bool hasIsMovingParam;

    void Start()
    {
        if (animator == null)
        {
            enabled = false;
            return;
        }

        AnimatorControllerParameter[] ps = animator.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].type == AnimatorControllerParameterType.Bool && ps[i].name == "isMoving")
            {
                hasIsMovingParam = true;
                break;
            }
        }

        if (!hasIsMovingParam)
        {
            enabled = false;
            return;
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        if (animator == null || !hasIsMovingParam) return;

        float distance = Vector3.Distance(transform.position, lastPosition);
        bool isMoving = distance > moveThreshold;

        animator.SetBool("isMoving", isMoving);

        lastPosition = transform.position;
    }
}
