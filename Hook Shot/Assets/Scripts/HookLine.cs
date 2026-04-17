using System.Collections;
using UnityEngine;

public class HookLine : MonoBehaviour
{
    [SerializeField] private float fadeDelay = 1.0f;   // Seconds to keep line visible after stop
    [SerializeField] private float fadeDuration = 0.3f; // Seconds to fade alpha to 0

    private LineRenderer lineRenderer;
    private BallController ballController;
    private Color lineColor;
    private bool isFading;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;

        // Store the base color
        lineColor = lineRenderer.startColor;
        if (lineColor.a < 0.01f)
        {
            lineColor = Color.cyan;
        }
    }

    private void Start()
    {
        ballController = GetComponentInParent<BallController>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBallMoved += OnBallMoved;
            GameManager.Instance.OnBallStopped += OnBallStopped;
            GameManager.Instance.OnLevelComplete += OnBallStoppedGoal;
            GameManager.Instance.OnGameFailed += OnGameFailed;
        }

        lineRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBallMoved -= OnBallMoved;
            GameManager.Instance.OnBallStopped -= OnBallStopped;
            GameManager.Instance.OnLevelComplete -= OnBallStoppedGoal;
            GameManager.Instance.OnGameFailed -= OnGameFailed;
        }
    }

    private void Update()
    {
        if (ballController == null) return;

        // While ball is moving, update the hook line from launch pos to current pos
        if (ballController.IsMoving)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, ballController.LaunchPosition);
            lineRenderer.SetPosition(1, transform.parent.position);

            // Ensure full alpha while drawing
            SetLineAlpha(1f);
        }
    }

    private void OnBallMoved()
    {
        // Stop any ongoing fade
        StopAllCoroutines();
        isFading = false;

        // Reset alpha and show line
        SetLineAlpha(1f);
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, ballController.LaunchPosition);
        lineRenderer.SetPosition(1, transform.parent.position);
    }

    private void OnBallStopped()
    {
        // Ball hit a block — clear the hook line immediately
        StartCoroutine(FadeAndHideCoroutine());
    }

    private void OnBallStoppedGoal()
    {
        // Ball reached goal — fade out the hook line
        StartCoroutine(FadeAndHideCoroutine());
    }

    private void OnGameFailed()
    {
        // Ball went out of bounds — hide immediately
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    private IEnumerator FadeAndHideCoroutine()
    {
        if (isFading) yield break;
        isFading = true;

        // Keep visible briefly
        yield return new WaitForSeconds(fadeDelay);

        // Fade alpha from 1 to 0
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            SetLineAlpha(alpha);
            yield return null;
        }

        SetLineAlpha(0f);
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
        isFading = false;
    }

    private void SetLineAlpha(float alpha)
    {
        Color c = lineColor;
        c.a = alpha;
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
    }
}
