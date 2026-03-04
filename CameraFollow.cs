using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public string targetTag = "Player";

    [Header("Position Follow")]
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);
    public float distance = 4f;
    public float minDistance = 2f;
    public float maxDistance = 6f;
    public float positionSmoothTime = 0.06f;

    [Header("Mouse Look")]
    public float mouseSensitivityX = 0.008f;
    public float mouseSensitivityY = 0.006f;
    public float minPitch = -25f;
    public float maxPitch = 60f;

    [Header("Auto Follow Behind Player")]
    public bool followPlayerRotation = true;
    public float alignDelay = 0.8f;
    public float alignSpeed = 6f;

    [Header("Rotation Smooth")]
    public float rotationSmoothSpeed = 12f;

    [Header("Collision")]
    public bool preventClipping = true;
    public float collisionRadius = 0.2f;
    public LayerMask collisionMask = ~0;

    private float yaw;
    private float pitch = 15f;
    private float idleTimer;
    private Vector3 currentVelocity;

    void Start()
    {
        FindTargetIfNeeded();

        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;

        SetCursorLock(true);
    }

    void LateUpdate()
    {
        FindTargetIfNeeded();
        if (target == null) return;

        HandleCursorState();
        HandleMouseLook();
        AutoAlignBehindTarget();
        UpdateCameraPosition();
    }

    private void FindTargetIfNeeded()
    {
        if (target != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(targetTag);
        if (playerObject != null)
        {
            target = playerObject.transform;
        }
    }

    private void HandleCursorState()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLock(false);
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLock(true);
        }
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        bool hasMouseInput =
            Mathf.Abs(mouseDelta.x) > 0.01f ||
            Mathf.Abs(mouseDelta.y) > 0.01f;

        if (hasMouseInput)
        {
            yaw += mouseDelta.x * mouseSensitivityX * 100f;
            pitch -= mouseDelta.y * mouseSensitivityY * 100f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            idleTimer = 0f;
        }
        else
        {
            idleTimer += Time.deltaTime;
        }
    }

    private void AutoAlignBehindTarget()
    {
        if (!followPlayerRotation) return;
        if (target == null) return;
        if (idleTimer < alignDelay) return;

        float targetYaw = target.eulerAngles.y;
        yaw = Mathf.LerpAngle(yaw, targetYaw, alignSpeed * Time.deltaTime);
    }

    private void UpdateCameraPosition()
    {
        Vector3 focusPoint = target.position + targetOffset;

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredDirection = orbitRotation * Vector3.back;
        float finalDistance = distance;

        if (preventClipping)
        {
            if (Physics.SphereCast(
                focusPoint,
                collisionRadius,
                desiredDirection,
                out RaycastHit hit,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
            {
                finalDistance = Mathf.Clamp(hit.distance - 0.1f, minDistance, distance);
            }
        }

        finalDistance = Mathf.Clamp(finalDistance, minDistance, maxDistance);

        Vector3 desiredPosition = focusPoint + desiredDirection * finalDistance;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            positionSmoothTime);

        Quaternion targetRotation = Quaternion.LookRotation(focusPoint - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.deltaTime);
    }

    private void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}