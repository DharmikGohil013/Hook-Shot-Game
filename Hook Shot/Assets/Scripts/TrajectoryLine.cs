using UnityEngine;

public class TrajectoryLine : MonoBehaviour
{
    [SerializeField] private float maxRange = 15f;           // Maximum trajectory preview distance
    [SerializeField] private float dashLength = 0.3f;        // Length of each visible dash segment
    [SerializeField] private float gapLength = 0.2f;         // Length of each gap between dashes
    [SerializeField] private LayerMask raycastMask = ~0;     // Layers to raycast against

    private LineRenderer lineRenderer;
    private BallController ballController;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Default line settings
        lineRenderer.startWidth = 0.08f;
        lineRenderer.endWidth = 0.08f;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
    }

    private void Start()
    {
        ballController = GetComponentInParent<BallController>();
        lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (ballController == null || GameManager.Instance == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        GameState state = GameManager.Instance.CurrentState;

        // Only show trajectory while ball is stationary and game is in Playing state
        if (state == GameState.Playing && !ballController.IsMoving)
        {
            lineRenderer.enabled = true;
            DrawDashedLine();
        }
        else
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }
    }

    private void DrawDashedLine()
    {
        Vector3 origin = transform.parent.position; // Ball position
        Vector3 direction = ballController.CurrentDirection;

        // Raycast to find hit distance
        float hitDistance = maxRange;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, raycastMask))
        {
            hitDistance = hit.distance;
        }

        // Build dashed line points
        // Each dash uses 2 points (start, end), gaps are simply skipped
        float segmentLength = dashLength + gapLength;
        int dashCount = Mathf.CeilToInt(hitDistance / segmentLength);

        // Allocate points: each dash = 2 points, plus we need line breaks
        // We'll use a continuous line but place points at dash boundaries
        // with zero-length gaps (overlapping points) to create visual dashes
        var points = new System.Collections.Generic.List<Vector3>();

        float traveled = 0f;
        for (int i = 0; i < dashCount && traveled < hitDistance; i++)
        {
            float dashStart = traveled;
            float dashEnd = Mathf.Min(traveled + dashLength, hitDistance);

            // Add dash segment start and end
            points.Add(origin + direction * dashStart);
            points.Add(origin + direction * dashEnd);

            // Move past the gap
            traveled = dashEnd + gapLength;

            // If there's more to draw, add a zero-length connector at gap end
            // to create a visual break (put two points at same position)
            if (traveled < hitDistance && i < dashCount - 1)
            {
                Vector3 gapEnd = origin + direction * traveled;
                points.Add(origin + direction * dashEnd); // duplicate at dash end
                points.Add(gapEnd);                       // jump to gap end
            }
        }

        if (points.Count < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }
}
