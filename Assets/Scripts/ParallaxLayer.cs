using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public Transform cameraTransform;
    [Range(0f, 1f)] public float scrollFactorX;

    public float customLength = 0f;

    private float singleImageLength;
    private float startPosX;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (cameraTransform == null) cameraTransform = mainCam.transform;

        startPosX = transform.position.x;

        // 인스펙터에 기획자가 너비를 적었다면 그 값을 최우선으로 사용합니다. (가장 안전)
        if (customLength > 0f)
        {
            singleImageLength = customLength;
        }
        else
        {
            // 적지 않았다면 기존 자동 계산 로직 실행
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) singleImageLength = spriteRenderer.bounds.size.x;
        }

        Debug.Log($"{gameObject.name}의 루프 너비 설정값: {singleImageLength}");
    }

    void LateUpdate()
    {
        float distance = cameraTransform.position.x * scrollFactorX;
        transform.position = new Vector3(startPosX + distance, transform.position.y, transform.position.z);

        float temp = cameraTransform.position.x * (1 - scrollFactorX);

        if (temp > startPosX + singleImageLength)
        {
            startPosX += singleImageLength;
        }
        else if (temp < startPosX - singleImageLength)
        {
            startPosX -= singleImageLength;
        }
    }
}