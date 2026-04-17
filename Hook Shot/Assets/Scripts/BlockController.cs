using System.Collections;
using UnityEngine;

public class BlockController : MonoBehaviour
{
    [SerializeField] private float shrinkDuration = 0.2f; // Time to shrink to zero

    /// <summary>
    /// Called by BallController when the ball collides with this block.
    /// Plays a shrink animation then destroys the GameObject.
    /// </summary>
    public void DestroyBlock()
    {
        // Disable collider immediately so no double-hits
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        StartCoroutine(ShrinkAndDestroy());
    }

    private IEnumerator ShrinkAndDestroy()
    {
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        Destroy(gameObject);
    }
}
