using UnityEngine;
using UnityEngine.InputSystem;

public class BallController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;           // Ball travel speed in units/sec
    [SerializeField] private float roundRadius = 25f;        // Radius of the round play area centered at world origin
    [SerializeField] private float outsideRoundFailPercent = 1f; // Allowed overflow beyond the round before failing
    [SerializeField] private float indicatorLength = 1.5f;   // Length of the direction indicator
    [SerializeField] private float indicatorWidth = 0.15f;   // Width of the direction indicator

    private float currentAngle;          // Current rotation angle in degrees
    private bool isMoving;
    private Vector3 moveDirection;
    private Vector3 launchPosition;
    private GameObject directionIndicator;
    private Rigidbody rb;

    // Expose for TrajectoryLine and HookLine
    public Vector3 CurrentDirection => moveDirection.normalized;
    public bool IsMoving => isMoving;
    public Vector3 LaunchPosition => launchPosition;
    public float CurrentAngle => currentAngle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        CreateDirectionIndicator();
    }

    private void Start()
    {
        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart += OnGameStart;
            GameManager.Instance.OnBallStopped += OnBallStopped;
        }

        // Hide indicator until game starts
        if (directionIndicator != null)
            directionIndicator.SetActive(false);

        isMoving = false;
        currentAngle = 0f;
        UpdateMoveDirection();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart -= OnGameStart;
            GameManager.Instance.OnBallStopped -= OnBallStopped;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        GameState state = GameManager.Instance.CurrentState;

        // Rotate indicator while Playing (stationary)
        if (state == GameState.Playing && !isMoving)
        {
            float degPerSec = SpeedController.Instance != null
                ? SpeedController.Instance.CurrentDegreesPerSecond
                : 90f;

            currentAngle += degPerSec * Time.deltaTime;
            if (currentAngle >= 360f) currentAngle -= 360f;

            UpdateMoveDirection();
            UpdateIndicatorVisual();

            // Check for tap input
            if (DetectTap())
            {
                LaunchBall();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isMoving) return;

        // Move ball via transform
        transform.position += moveDirection * moveSpeed * Time.fixedDeltaTime;

        // Out-of-bounds check (ball fell off ground or moved beyond the round boundary)
        Vector3 pos = transform.position;
        float failRadius = roundRadius * (1f + outsideRoundFailPercent / 100f);
        Vector2 horizontalPosition = new Vector2(pos.x, pos.z);

        if (pos.y < -1f || horizontalPosition.sqrMagnitude > failRadius * failRadius)
        {
            FailAndRestartLevel();
        }
    }

    private void FailAndRestartLevel()
    {
        if (!isMoving)
        {
            return;
        }

        isMoving = false;
        if (directionIndicator != null)
            directionIndicator.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.NotifyGameFailed();
        }
    }

    /// <summary>
    /// Detects tap input — touch on mobile, mouse click on PC.
    /// </summary>
    private bool DetectTap()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            return !IsPointerOverUI(touchId);
        }

        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame &&
               !IsPointerOverUI();
    }

    /// <summary>
    /// Prevents taps on UI buttons from also launching the ball.
    /// </summary>
    private bool IsPointerOverUI(int? pointerId = null)
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            return false;
        }

        return pointerId.HasValue
            ? UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(pointerId.Value)
            : UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    private void LaunchBall()
    {
        isMoving = true;
        launchPosition = transform.position;

        // Hide direction indicator
        if (directionIndicator != null)
            directionIndicator.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyBallMoved();
    }

    /// <summary>
    /// Called when ball hits a Block — uses OnCollisionEnter since blocks have
    /// regular (non-trigger) colliders and the ball has a kinematic Rigidbody.
    /// Kinematic Rigidbody + non-kinematic colliders will generate collision events
    /// as long as the blocks also have colliders.
    /// 
    /// Design choice: We use OnTriggerEnter with trigger colliders on Blocks and Goal
    /// because kinematic Rigidbodies generate trigger events reliably, and it avoids
    /// needing Rigidbodies on every block.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!isMoving) return;

        if (other.CompareTag("Block"))
        {
            isMoving = false;

            // Tell the block to destroy itself with animation
            BlockController block = other.GetComponent<BlockController>();
            if (block != null)
            {
                block.DestroyBlock();
            }

            if (GameManager.Instance != null)
                GameManager.Instance.NotifyBallStopped();
        }
        else if (other.CompareTag("Goal"))
        {
            isMoving = false;

            // Spawn celebration particles
            SpawnGoalParticles();

            if (GameManager.Instance != null)
                GameManager.Instance.NotifyLevelComplete();
        }
    }

    private void OnGameStart()
    {
        // Show direction indicator and begin spinning
        if (directionIndicator != null)
            directionIndicator.SetActive(true);
    }

    private void OnBallStopped()
    {
        // Resume indicator spinning
        if (directionIndicator != null)
            directionIndicator.SetActive(true);
    }

    private void UpdateMoveDirection()
    {
        float rad = currentAngle * Mathf.Deg2Rad;
        moveDirection = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)).normalized;
    }

    /// <summary>
    /// Creates the direction indicator as a thin elongated cube child of the Ball.
    /// </summary>
    private void CreateDirectionIndicator()
    {
        directionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        directionIndicator.name = "DirectionIndicator";
        directionIndicator.transform.SetParent(transform);

        // Position it so its back edge starts at ball center
        directionIndicator.transform.localPosition = new Vector3(0f, 0f, indicatorLength * 0.5f);
        directionIndicator.transform.localScale = new Vector3(indicatorWidth, indicatorWidth, indicatorLength);

        // Remove collider so it doesn't interfere with gameplay
        Collider col = directionIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Set a bright yellow color
        Renderer rend = directionIndicator.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            rend.material.color = Color.yellow;
        }
    }

    private void UpdateIndicatorVisual()
    {
        // Rotate the ball's local Y-axis so the indicator child follows
        transform.rotation = Quaternion.Euler(0f, currentAngle, 0f);
    }

    /// <summary>
    /// Spawns a simple particle burst at the ball position when reaching the goal.
    /// </summary>
    private void SpawnGoalParticles()
    {
        GameObject particleObj = new GameObject("GoalParticles");
        particleObj.transform.position = transform.position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        // Stop the auto-play so we can configure first
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = 0.8f;
        main.startSpeed = 5f;
        main.startSize = 0.3f;
        main.startColor = new Color(1f, 0.85f, 0f, 1f); // Gold
        main.maxParticles = 50;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 40)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        // Use default particle material
        ParticleSystemRenderer psRenderer = particleObj.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            psRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            psRenderer.material.color = new Color(1f, 0.85f, 0f, 1f);
        }

        ps.Play();

        // Auto-destroy after particle lifetime
        Destroy(particleObj, 2f);
    }
}
