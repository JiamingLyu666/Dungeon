using UnityEngine;

public class EnemyChasePlayer : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";

    [Header("Touch Settings")]
    public float touchDistance = 1.1f;
    public bool destroyOnTouch = true;

    [Header("Optional Movement")]
    public float moveSpeed = 0f;
    public float rotateSpeed = 10f;
    public float stopDistance = 0.75f;
    public bool keepCurrentHeight = true;

    private Transform playerTarget;
    private Collider playerCollider;
    private Collider selfCollider;
    private bool wasTouching;
    private bool consumed;

    void Awake()
    {
        selfCollider = GetComponent<Collider>();
    }

    void Start()
    {
        FindPlayerIfNeeded();
    }

    void Update()
    {
        if (consumed) return;

        FindPlayerIfNeeded();
        if (playerTarget == null) return;

        if (moveSpeed > 0f)
        {
            UpdateMovement();
        }

        bool touchingNow = IsTouchingPlayer();
        if (touchingNow && !wasTouching)
        {
            OnTouchPlayer();
        }

        wasTouching = touchingNow;
    }

    void OnTriggerEnter(Collider other)
    {
        if (consumed) return;
        if (!other.CompareTag(playerTag)) return;

        playerCollider = other;
        OnTouchPlayer();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (consumed) return;
        if (!collision.collider.CompareTag(playerTag)) return;

        playerCollider = collision.collider;
        OnTouchPlayer();
    }

    private void FindPlayerIfNeeded()
    {
        if (playerTarget != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null) return;

        playerTarget = playerObject.transform;
        playerCollider = playerObject.GetComponent<Collider>();
    }

    private void UpdateMovement()
    {
        Vector3 targetPosition = playerTarget.position;
        Vector3 currentPosition = transform.position;

        if (keepCurrentHeight)
        {
            targetPosition.y = currentPosition.y;
        }

        Vector3 toTarget = targetPosition - currentPosition;
        float distanceToTarget = toTarget.magnitude;

        if (distanceToTarget > stopDistance)
        {
            Vector3 moveDirection = toTarget.normalized;
            transform.position += moveDirection * moveSpeed * Time.deltaTime;

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime);
            }
        }
    }

    private bool IsTouchingPlayer()
    {
        if (playerTarget == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool isCloseEnough = distanceToPlayer <= touchDistance;

        if (selfCollider != null && playerCollider != null)
        {
            bool intersects = selfCollider.bounds.Intersects(playerCollider.bounds);
            return intersects || isCloseEnough;
        }

        return isCloseEnough;
    }

    private void OnTouchPlayer()
    {
        if (consumed) return;
        consumed = true;

        if (destroyOnTouch)
        {
            if (selfCollider != null)
            {
                selfCollider.enabled = false;
            }

            Destroy(gameObject);
        }
    }
}