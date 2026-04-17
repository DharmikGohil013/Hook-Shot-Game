using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform ball;              // Assign the Ball transform
    [SerializeField] private float followSpeed = 5f;      // Lerp speed for following
    [SerializeField] private float zoomedOutSize = 8f;    // Default orthographic size
    [SerializeField] private float zoomedInSize = 4f;     // Zoomed-in orthographic size
    [SerializeField] private float zoomDuration = 0.4f;   // Duration of zoom transition
    [SerializeField] private float failZoomSize = 2.5f;    // Orthographic size when zooming to failed ball
    [SerializeField] private float failZoomDuration = 0.6f; // Duration of fail-zoom animation
    [SerializeField] private float failPauseDuration = 0.5f; // Pause after zoom before showing restart panel

    private Camera cam;
    private bool isZoomedIn;
    private Coroutine zoomCoroutine;

    public bool IsZoomedIn => isZoomedIn;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = zoomedOutSize;
        }
        isZoomedIn = false;
    }

    private void LateUpdate()
    {
        if (ball == null || cam == null) return;

        // Follow ball's X and Z position, keep camera Y fixed
        Vector3 targetPos = new Vector3(ball.position.x, transform.position.y, ball.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Called by the ZoomButton. Each press toggles between zoomed-in and zoomed-out.
    /// </summary>
    public void ZoomToggle()
    {
        if (cam == null) return;

        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);

        isZoomedIn = !isZoomedIn;
        float targetSize = isZoomedIn ? zoomedInSize : zoomedOutSize;
        zoomCoroutine = StartCoroutine(ZoomCoroutine(targetSize));
    }

    private IEnumerator ZoomCoroutine(float targetSize)
    {
        float startSize = cam.orthographicSize;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomDuration);
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }

        cam.orthographicSize = targetSize;
        zoomCoroutine = null;
    }

    /// <summary>
    /// Smoothly zooms the camera toward the ball when it goes out of bounds.
    /// Calls onComplete after the animation finishes.
    /// </summary>
    public void ZoomToBallOnFail(System.Action onComplete = null)
    {
        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);

        zoomCoroutine = StartCoroutine(ZoomToBallCoroutine(onComplete));
    }

    private IEnumerator ZoomToBallCoroutine(System.Action onComplete)
    {
        float targetSize = failZoomSize;
        float startSize = cam.orthographicSize;
        float elapsed = 0f;
        float duration = failZoomDuration;

        // Smoothly zoom in toward the ball
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }

        cam.orthographicSize = targetSize;
        zoomCoroutine = null;

        // Brief pause so the player can see the ball's position
        yield return new WaitForSeconds(failPauseDuration);

        onComplete?.Invoke();
    }
}
