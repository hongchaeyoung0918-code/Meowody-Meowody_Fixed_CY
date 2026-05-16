using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public Transform cameraTransform;
    [Tooltip("1이면 카메라와 완벽 동기화, 0이면 고정(가장 빠르게 지나감)")]
    [Range(0f, 1f)] public float scrollFactorX;

    private float length;
    private float startPosX;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (cameraTransform == null) cameraTransform = mainCam.transform;

        startPosX = transform.position.x;

        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            length = spriteRenderer.bounds.size.x;
        }
        else
        {
            // 자식 오브젝트들에 스프라이트가 여러 장 붙어있는 경우 전체 너비 계산
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            foreach (var r in renderers)
            {
                minX = Mathf.Min(minX, r.bounds.min.x);
                maxX = Mathf.Max(maxX, r.bounds.max.x);
            }
            length = maxX - minX;
        }
    }

    void LateUpdate()
    {
        // 1. 패럴렉스 공식에 따른 이동
        float distance = cameraTransform.position.x * scrollFactorX;
        transform.position = new Vector3(startPosX + distance, transform.position.y, transform.position.z);

        // 2. 가상의 '카메라 상대 위치' 계산
        float temp = cameraTransform.position.x * (1 - scrollFactorX);

        // 3. 끊김 방지를 위한 버퍼 추가 (카메라 가로 시야 절반 크기)
        float cameraHalfWidth = mainCam.orthographicSize * mainCam.aspect;

        // 카메라가 현재 배경 영역의 오른쪽 경계를 넘어서려고 하면, 배경을 오른쪽으로 한 칸 이동
        if (temp > startPosX + length - cameraHalfWidth)
        {
            startPosX += length;
        }
        // 반대의 경우 왼쪽으로 이동
        else if (temp < startPosX - length + cameraHalfWidth)
        {
            startPosX -= length;
        }
    }
}