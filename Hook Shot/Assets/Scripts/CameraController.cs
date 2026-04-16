using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target; // The ball/player to follow
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f); // Position offset from target
    [SerializeField] private float smoothFollowSpeed = 5f; // Smooth follow speed
    [SerializeField] private bool useFixedUpdate = true;

    [Header("Zoom Settings")]
    [SerializeField] private float minZoom = 5f; // Minimum camera distance
    [SerializeField] private float maxZoom = 20f; // Maximum camera distance
    [SerializeField] private float currentZoom = 10f; // Current zoom level
    [SerializeField] private float zoomSmoothness = 5f; // How smooth the zoom is

    [Header("Zoom Input")]
    [SerializeField] private KeyCode zoomInKey = KeyCode.E; // Zoom in key
    [SerializeField] private KeyCode zoomOutKey = KeyCode.Q; // Zoom out key
    [SerializeField] private float zoomInputAmount = 1f; // How much to zoom per input

    [Header("Look Ahead")]
    [SerializeField] private bool useLookAhead = true;
    [SerializeField] private float lookAheadDistance = 2f;

    private Camera mainCamera;
    private Vector3 currentVelocity = Vector3.zero;
    private float targetZoom;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Find the target if not assigned (assuming it's the ball)
        if (target == null)
        {
            Rigidbody ballRb = FindObjectOfType<BallController>()?.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                target = ballRb.transform;
            }
        }

        targetZoom = currentZoom;
    }

    private void Update()
    {
        if (!useFixedUpdate)
        {
            HandleZoomInput();
            UpdateCamera();
        }
    }

    private void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            HandleZoomInput();
            UpdateCamera();
        }
    }

    private void UpdateCamera()
    {
        if (target == null) return;

        // Calculate target position with look ahead
        Vector3 targetPosition = target.position + offset;
        
        if (useLookAhead && target.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            targetPosition += rb.linearVelocity.normalized * lookAheadDistance;
        }

        // Smoothly move camera to target position
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            1f / smoothFollowSpeed
        );

        // Look at the target
        transform.LookAt(target);

        // Update zoom
        UpdateZoom();
    }

    private void HandleZoomInput()
    {
        if (Input.GetKey(zoomInKey))
        {
            targetZoom -= zoomInputAmount * Time.deltaTime;
        }
        
        if (Input.GetKey(zoomOutKey))
        {
            targetZoom += zoomInputAmount * Time.deltaTime;
        }

        // Clamp zoom value
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    private void UpdateZoom()
    {
        // Smoothly interpolate zoom
        currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomSmoothness);
        
        // Update camera FOV based on zoom
        if (mainCamera != null)
        {
            mainCamera.fieldOfView = currentZoom;
        }

        // Also adjust offset distance based on zoom
        float zoomFactor = currentZoom / 10f; // Base zoom is 10
        Vector3 adjustedOffset = offset * zoomFactor;
    }

    /// <summary>
    /// Manually zoom in
    /// </summary>
    public void ZoomIn(float amount = 1f)
    {
        targetZoom -= amount;
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    /// <summary>
    /// Manually zoom out
    /// </summary>
    public void ZoomOut(float amount = 1f)
    {
        targetZoom += amount;
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    /// <summary>
    /// Reset zoom to default
    /// </summary>
    public void ResetZoom()
    {
        targetZoom = currentZoom;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public float GetCurrentZoom()
    {
        return currentZoom;
    }
}
