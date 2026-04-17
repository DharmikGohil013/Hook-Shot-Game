using System.Collections;
using UnityEngine;

public class BlockController : MonoBehaviour
{
    [SerializeField] private int fragmentCount = 6;
    [SerializeField] private float explodeForce = 3f;

    /// <summary>
    /// Called by BallController when the ball collides with this block.
    /// Spawns small cube fragments that fly outward, then destroys the block.
    /// </summary>
    public void DestroyBlock()
    {
        // Disable collider immediately so no double-hits
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        StartCoroutine(ExplodeAndDestroy());
    }

    private IEnumerator ExplodeAndDestroy()
    {
        // Cache material before hiding so fragments can copy it
        Material blockMaterial = null;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            blockMaterial = rend.material;
            rend.enabled = false;
        }

        // Spawn fragments
        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject frag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frag.transform.position = transform.position;
            frag.transform.localScale = Vector3.one * 0.2f;

            // Same material/color as the original block
            if (blockMaterial != null)
                frag.GetComponent<Renderer>().material = blockMaterial;

            // Random outward direction
            Vector3 dir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.2f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;

            // Remove collider so fragments don't interfere with gameplay
            Destroy(frag.GetComponent<Collider>());

            StartCoroutine(AnimateFragment(frag, dir));
        }

        yield return new WaitForSeconds(0.4f);
        Destroy(gameObject);
    }

    private IEnumerator AnimateFragment(GameObject frag, Vector3 direction)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 startPos = frag.transform.position;
        Vector3 startScale = frag.transform.localScale;

        while (elapsed < duration)
        {
            if (frag == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Move outward + gravity
            frag.transform.position = startPos
                + direction * explodeForce * t
                + Vector3.down * 2f * t * t;

            // Shrink as it flies
            frag.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // Spin randomly
            frag.transform.Rotate(300f * Time.deltaTime, 200f * Time.deltaTime, 0f);

            yield return null;
        }

        Destroy(frag);
    }
}
