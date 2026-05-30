using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float xOffset = 3f;
    public float yOffset = 1f;
    public float minY = 0f;

    public float smoothTime = 0.2f;
    private float velocityY = 0f;
    // x, y 축 둘 다 적용에서 Y 축만 부드럽게 이동하도록 변경

    void LateUpdate()
    {
        if (target == null) return;
        float targetX = target.position.x + xOffset; // X 좌표
        float targetY = target.position.y + yOffset; // Y 좌표

        targetY = Mathf.Max(targetY, minY);
        // 최소 Y 좌표 제한 -> 낙사 시 카메라가 아래로 내려가지 않게

        float smoothedY = Mathf.SmoothDamp(transform.position.y, targetY, ref velocityY, smoothTime);
        transform.position = new Vector3(targetX, smoothedY, -10f);
    }
}