using UnityEngine;

public class ParallaxMover : MonoBehaviour
{
    // 배경이 카메라보다 느리게 움직일 비율 (0~1)
    public float parallaxSpeed = 0.5f;

    // 이전에 카메라의 위치를 저장할 변수
    private float lastCameraX;

    // 메인 카메라를 참조합니다.
    private Transform mainCamera;

    void Start()
    {
        mainCamera = Camera.main.transform;
        lastCameraX = mainCamera.position.x;
    }

    void Update()
    {
        // 1. 카메라 이동량 계산
        float deltaCameraX = mainCamera.position.x - lastCameraX;

        // 2. 패럴렉스 이동 계산
        float parallaxMovement = deltaCameraX * parallaxSpeed;

        // 3. 오브젝트 위치 업데이트
        transform.Translate(-parallaxMovement, 0, 0);

        // 4. 다음 프레임을 위한 카메라 위치 업데이트
        lastCameraX = mainCamera.position.x;

        // 5. 파괴 조건 (화면 밖으로 완전히 벗어났을 때)
        // 오브젝트의 X 위치가 카메라 왼쪽 경계보다 훨씬 왼쪽일 때 파괴
        float destroyBoundary = mainCamera.position.x - 80f; // 넉넉한 파괴 경계
        if (transform.position.x < destroyBoundary)
        {
            Destroy(gameObject);
        }
    }
}