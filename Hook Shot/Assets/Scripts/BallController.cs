using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BallController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float launchForce = 10f;
    [SerializeField] private float maxLaunchDistance = 15f;
    [SerializeField] private float minLaunchDistance = 0.35f;
    [SerializeField] private float stopVelocityThreshold = 0.1f;
    [SerializeField] private float stopCheckDuration = 0.2f;
    [SerializeField] private float minimumInputDelay = 0.1f;

    [Header("Physics Resistance")]
    [SerializeField] private float airDrag = 0.8f;
    [SerializeField] private float angularDrag = 0.5f;
    [SerializeField] private float groundedDrag = 1.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Hook Settings")]
    [SerializeField] private LayerMask hookableLayer;
    [SerializeField] private float hookRange = 20f;
    [SerializeField] private float hookPullForce = 15f;
    [SerializeField] private float detachDistance = 0.5f;
    [SerializeField] private float maxHookDuration = 4f;

    [Header("Swing Physics (SpringJoint)")]
    [SerializeField] private float springForce = 50f;
    [SerializeField] private float springDamper = 5f;
    [SerializeField] private float ropeLength = 5f;

    [Header("Play Area")]
    [SerializeField] private bool usePlayAreaBounds = false;
    [SerializeField] private Bounds playAreaBounds = new Bounds(Vector3.zero, new Vector3(100f, 100f, 100f));
    [SerializeField] private float failYThreshold = -25f;

    [Header("References")]
    [SerializeField] private LineRenderer hookLineRenderer;
    [SerializeField] private LineRenderer trajectoryRenderer;
    [SerializeField] private Camera mainCamera;

    private Rigidbody rb;
    private bool isMoving = false;
    private bool isHooked = false;
    private Vector3 hookPoint;
    private SpringJoint hookJoint;
    private float stoppedTimer = 0f;
    private GameObject hookAnchor;
    private Transform hookTarget;
    private float hookTimer = 0f;
    private float lastInputTime = -999f;

    public event Action OnLaunched;
    public event Action OnStopped;
    public event Action OnHooked;
    public event Action OnDetached;
    public event Action OnFellOff;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("BallController requires a Rigidbody component.", this);
            enabled = false;
            return;
        }

        rb.linearDamping = airDrag;
        rb.angularDamping = angularDrag;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (hookLineRenderer != null)
        {
            hookLineRenderer.enabled = false;
            hookLineRenderer.positionCount = 2;
        }

        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.enabled = true;
        }
    }

    private void Update()
    {
        if (!enabled)
        {
            return;
        }

        HandleInput();
        UpdateHookLine();
        CheckDetachConditions();
        CheckFailState();
    }

    private void FixedUpdate()
    {
        if (!enabled)
        {
            return;
        }

        CheckIfStopped();

        if (isHooked)
        {
            // Extra pull improves responsiveness in addition to SpringJoint behavior.
            Vector3 toHook = (hookPoint - transform.position).normalized;
            rb.AddForce(toHook * hookPullForce, ForceMode.Acceleration);
        }
    }

    private void HandleInput()
    {
        if (mainCamera == null || Time.time - lastInputTime < minimumInputDelay)
        {
            return;
        }

        bool pressed;
        Vector2 screenPosition;
        int pointerId;
        if (!TryGetPrimaryPress(out pressed, out screenPosition, out pointerId) || !pressed)
        {
            return;
        }

        if (IsPointerOverUI(pointerId))
        {
            return;
        }

        Vector3 worldPoint = ScreenToWorldOnBallPlane(screenPosition);
        if (!IsInputInsidePlayArea(worldPoint))
        {
            return;
        }

        if (!isMoving)
        {
            Launch(worldPoint);
            lastInputTime = Time.time;
            return;
        }

        // When moving, use tap/click as hook command instead of re-launching.
        if (!isHooked)
        {
            TryHookToPoint(worldPoint);
        }
        else
        {
            DetachHook();
        }

        lastInputTime = Time.time;
    }

    private bool TryGetPrimaryPress(out bool pressed, out Vector2 screenPosition, out int pointerId)
    {
        pressed = false;
        screenPosition = default;
        pointerId = -1;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0); // single-touch only
            if (touch.phase != TouchPhase.Began)
            {
                return true;
            }

            pressed = true;
            screenPosition = touch.position;
            pointerId = touch.fingerId;
            return true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            pressed = true;
            screenPosition = Input.mousePosition;
            pointerId = -1;
            return true;
        }

        return true;
    }

    private bool IsPointerOverUI(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (pointerId >= 0)
        {
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private Vector3 ScreenToWorldOnBallPlane(Vector2 screenPos)
    {
        float depth = Vector3.Dot(transform.position - mainCamera.transform.position, mainCamera.transform.forward);
        depth = Mathf.Max(depth, 0.01f);

        Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        return world;
    }

    private void Launch(Vector3 targetPosition)
    {
        Vector3 delta = targetPosition - transform.position;
        delta.y = 0f; // Keep launch mostly horizontal for predictable control.

        float distance = delta.magnitude;
        if (distance < minLaunchDistance)
        {
            return;
        }

        float clampedDistance = Mathf.Min(distance, maxLaunchDistance);
        Vector3 direction = delta.normalized;
        Vector3 impulse = direction * (launchForce * (clampedDistance / maxLaunchDistance));

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(impulse, ForceMode.Impulse);

        isMoving = true;
        stoppedTimer = 0f;

        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.enabled = false;
        }

        OnLaunched?.Invoke();
    }

    private void CheckIfStopped()
    {
        if (!isMoving)
        {
            return;
        }

        if (rb.linearVelocity.magnitude < stopVelocityThreshold)
        {
            stoppedTimer += Time.fixedDeltaTime;
            if (stoppedTimer >= stopCheckDuration)
            {
                isMoving = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Snap to avoid tiny floating point drift when the ball settles.
                Vector3 p = transform.position;
                transform.position = new Vector3(
                    Mathf.Round(p.x * 1000f) / 1000f,
                    Mathf.Round(p.y * 1000f) / 1000f,
                    Mathf.Round(p.z * 1000f) / 1000f);

                if (trajectoryRenderer != null)
                {
                    trajectoryRenderer.enabled = true;
                }

                OnStopped?.Invoke();
            }
        }
        else
        {
            stoppedTimer = 0f;
        }
    }

    private void TryHookToPoint(Vector3 tapPosition)
    {
        Vector3 direction = tapPosition - transform.position;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
        {
            return;
        }

        if (distance > hookRange)
        {
            direction = direction.normalized * hookRange;
        }

        if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, hookRange, hookableLayer, QueryTriggerInteraction.Ignore))
        {
            hookPoint = hit.point;
            hookTarget = hit.transform;

            if (hookAnchor == null)
            {
                hookAnchor = new GameObject("HookAnchor");
            }

            hookAnchor.transform.position = hookPoint;

            if (hookJoint != null)
            {
                Destroy(hookJoint);
            }

            hookJoint = gameObject.AddComponent<SpringJoint>();
            hookJoint.autoConfigureConnectedAnchor = false;
            hookJoint.connectedAnchor = hookPoint;
            hookJoint.spring = springForce;
            hookJoint.damper = springDamper;
            hookJoint.maxDistance = ropeLength;
            hookJoint.minDistance = 0f;
            hookJoint.enableCollision = true;

            isHooked = true;
            hookTimer = 0f;

            if (hookLineRenderer != null)
            {
                hookLineRenderer.enabled = true;
            }

            OnHooked?.Invoke();
        }
    }

    private void UpdateHookLine()
    {
        if (!isHooked || hookLineRenderer == null)
        {
            return;
        }

        hookLineRenderer.SetPosition(0, transform.position);
        hookLineRenderer.SetPosition(1, hookPoint);
    }

    private void CheckDetachConditions()
    {
        if (!isHooked)
        {
            return;
        }

        hookTimer += Time.deltaTime;

        if (hookTarget == null)
        {
            DetachHook();
            return;
        }

        // Keep the anchor aligned if the hooked object is moving.
        hookPoint = hookTarget.position;
        if (hookAnchor != null)
        {
            hookAnchor.transform.position = hookPoint;
        }

        float distance = Vector3.Distance(transform.position, hookPoint);
        if (distance <= detachDistance || hookTimer >= maxHookDuration)
        {
            DetachHook();
        }
    }

    private void DetachHook()
    {
        if (hookJoint != null)
        {
            Destroy(hookJoint);
            hookJoint = null;
        }

        if (hookLineRenderer != null)
        {
            hookLineRenderer.enabled = false;
        }

        hookTarget = null;
        hookTimer = 0f;
        isHooked = false;

        OnDetached?.Invoke();
    }

    private bool IsInputInsidePlayArea(Vector3 worldPoint)
    {
        if (!usePlayAreaBounds)
        {
            return true;
        }

        return playAreaBounds.Contains(worldPoint);
    }

    private void CheckFailState()
    {
        if (transform.position.y < failYThreshold)
        {
            OnFellOff?.Invoke();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            rb.linearDamping = groundedDrag;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            rb.linearDamping = airDrag;
        }
    }

    public void ResetBall(Vector3 position)
    {
        DetachHook();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = position;

        stoppedTimer = 0f;
        isMoving = false;

        if (trajectoryRenderer != null)
        {
            trajectoryRenderer.enabled = true;
        }
    }

    public bool IsMoving() => isMoving;
    public bool IsHooked() => isHooked;
}
