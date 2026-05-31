using System.Collections;
using UnityEngine;

public class RotateY : MonoBehaviour
{
    public float swingAngle = 15f;
    public float swingSpeed = 2f;
    public float bounceHeight = 0.15f;
    public float bounceSpeed = 2f;

    [Header("Proximity Scale")]
    public Transform player;
    public float approachDistance = 4f;
    public float scaleMultiplier = 1.1f;

    [Header("Color Pop")]
    public ColorKeeper colorKeeper;
    public float popScale = 1.3f;
    public float popDuration = 0.2f;

    private Vector3 startPos;
    private Vector3 startRot;
    private Vector3 originalScale;
    private bool isPopping;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.localEulerAngles;
        originalScale = transform.localScale;

        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

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
        // Z축 흔들림
        float z = Mathf.Sin(Time.time * swingSpeed) * swingAngle;
        transform.localEulerAngles = new Vector3(startRot.x, startRot.y, z);

        // 통통 튀기
        float y = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
        transform.position = startPos + new Vector3(0f, y, 0f);

        // 플레이어와 X거리 4 이내면 스케일 1.1배
        if (!isPopping && player != null)
        {
            float distX = Mathf.Abs(player.position.x - startPos.x);
            if (distX <= approachDistance)
                transform.localScale = originalScale * scaleMultiplier;
            else
                transform.localScale = originalScale;
        }
    }

    private void OnColorChanged()
    {
        StartCoroutine(PopAndDisappear());
    }

    private IEnumerator PopAndDisappear()
    {
        isPopping = true;

        // 팝 - 커지기
        Vector3 bigScale = originalScale * popScale;
        transform.localScale = bigScale;

        float half = popDuration * 0.5f;
        float elapsed = 0f;

        // 줄어들면서 사라지기
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            transform.localScale = Vector3.Lerp(bigScale, Vector3.zero, t);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }
}
