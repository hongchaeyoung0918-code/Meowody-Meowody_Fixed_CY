using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float xOffset = 3f;
    public float yOffset = 1f;
    public float minY = 0f;
    public float smoothTime = 0.2f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;
        float targetX = target.position.x + xOffset; // X 좌표
        float targetY = target.position.y + yOffset; // Y 좌표

        targetY = Mathf.Max(targetY, minY); 
        // 최소 Y 좌표 제한 -> 낙사 시 카메라가 아래로 내려가지 않게

        Vector3 targetPosition = new Vector3(targetX, targetY, -10f);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}