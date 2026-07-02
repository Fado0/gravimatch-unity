using System.Collections;
using UnityEngine;

/// <summary>
/// A screen/board shaking component to give visual impact and feedback during major events.
/// Supports lazy-initialization to automatically find and hook onto the Main Camera at runtime.
/// </summary>
public class BoardShake : MonoBehaviour
{
    private static BoardShake instance;
    public static BoardShake Instance
    {
        get
        {
            if (instance == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    instance = mainCam.gameObject.AddComponent<BoardShake>();
                }
                else
                {
                    GameObject go = new GameObject("BoardShake");
                    instance = go.AddComponent<BoardShake>();
                }
            }
            return instance;
        }
    }

    private Vector3 originalLocalPos;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            originalLocalPos = transform.localPosition;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
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
