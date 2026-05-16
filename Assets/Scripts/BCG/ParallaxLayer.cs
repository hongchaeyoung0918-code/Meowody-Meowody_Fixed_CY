using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public Transform cameraTransform;
    [Tooltip("1이면 카메라와 완벽히 동기화(멈춘 것처럼 보임), 0이면 고정(가장 빠르게 지나감)")]
    [Range(0f, 1f)] public float scrollFactorX;

    private float length;
    private float startPosX;

    void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        startPosX = transform.position.x;

        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) length = spriteRenderer.bounds.size.x;
    }

    void LateUpdate()
    {
        float distance = cameraTransform.position.x * scrollFactorX;
        transform.position = new Vector3(startPosX + distance, transform.position.y, transform.position.z);

        float temp = cameraTransform.position.x * (1 - scrollFactorX);
        if (temp > startPosX + length) startPosX += length;
        else if (temp < startPosX - length) startPosX -= length;
    }
}