using UnityEngine;
using UnityEngine.Events;

public class PoolableObject : MonoBehaviour
{
    private Transform cameraTransform;
    private UnityAction<GameObject> returnAction;
    private float leftBoundaryBuffer = 5f; // 카메라 왼쪽으로 얼마나 벗어나면 사라질지 설정

    public void Setup(Transform camera, UnityAction<GameObject> onRelease)
    {
        cameraTransform = camera;
        returnAction = onRelease;
    }

    void Update()
    {
        if (cameraTransform == null) return;

        // 카메라의 왼쪽 끝 시야 좌표 계산
        float cameraLeftEdge = cameraTransform.position.x - (Camera.main.orthographicSize * Camera.main.aspect);

        // 프랍이 카메라 왼쪽 시야 밖으로 완전히 밀려나면 풀로 반환
        if (transform.position.x < cameraLeftEdge - leftBoundaryBuffer)
        {
            returnAction?.Invoke(gameObject);
        }
    }
}