using System.Collections;
using UnityEngine;

/// <summary>
/// A screen/board shaking component to give visual impact and feedback during major events.
/// Implemented as a Singleton helper for simplicity.
/// </summary>
public class BoardShake : MonoBehaviour
{
    public static BoardShake Instance { get; private set; }

    private Vector3 originalLocalPos;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        originalLocalPos = transform.localPosition;
    }

    /// <summary>
    /// Trigger a shake action with specified time and strength.
    /// </summary>
    public void Shake(float duration = 0.2f, float magnitude = 0.1f)
    {
        if (gameObject.activeInHierarchy)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
        }
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalLocalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
    }
}
