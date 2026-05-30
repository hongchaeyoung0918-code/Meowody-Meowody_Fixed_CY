using System.Collections;
using UnityEngine;

public class RotateY : MonoBehaviour
{
    public float swingAngle = 15f;
    public float swingSpeed = 2f;
    public float bounceHeight = 0.15f;
    public float bounceSpeed = 2f;

    [Header("Scale Punch")]
    public ColorKeeper colorKeeper;
    public float punchScale = 0.12f;
    public float punchDuration = 0.15f;

    private Vector3 startPos;
    private Vector3 startRot;
    private Vector3 originalScale;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.localEulerAngles;
        originalScale = transform.localScale;

        if (colorKeeper != null)
            colorKeeper.OnColorized += OnColorChanged;
    }

    void OnDestroy()
    {
        if (colorKeeper != null)
            colorKeeper.OnColorized -= OnColorChanged;
    }

    void Update()
    {
        float z = Mathf.Sin(Time.time * swingSpeed) * swingAngle;
        transform.localEulerAngles = new Vector3(startRot.x, startRot.y, z);

        float y = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
        transform.position = startPos + new Vector3(0f, y, 0f);
    }

    private void OnColorChanged()
    {
        StartCoroutine(ScalePunch());
    }

    private IEnumerator ScalePunch()
    {
        transform.localScale = new Vector3(punchScale, punchScale, originalScale.z);

        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            transform.localScale = Vector3.Lerp(
                new Vector3(punchScale, punchScale, originalScale.z),
                originalScale,
                t
            );
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
