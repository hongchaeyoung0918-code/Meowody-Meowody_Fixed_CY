using UnityEngine;

public class ParallaxMover : MonoBehaviour
{
    // 배경이 카메라보다 느리게 움직일 비율 (0~1)
    public float parallaxSpeed = 0.5f;
    public float mapBaseSpeed = 5f; // Inspector에서 설정

    private Transform mainCamera;

    private float objectWidth;

    void Start()
    {
        mainCamera = Camera.main.transform;

        //
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = GetComponentInChildren<SpriteRenderer>();
        }

        if (sr != null)
        {
            // 배경의 전체 월드 너비를 저장합니다.
            objectWidth = sr.bounds.size.x;
        }
        else
        {
            Debug.LogWarning("ParallaxMover: SpriteRenderer를 찾을 수 없습니다. 너비 계산이 부정확할 수 있습니다.");
            objectWidth = 20f; // 기본값
        }
    }

    void Update()
    {
        if (!GameSettings.IsGameplayActive)
        {
            return;
        }

        // 1. 패럴렉스 이동량 계산
        // 맵이 왼쪽으로 이동(-X)하므로, 속도에 마이너스를 곱합니다.
        float parallaxMovement = -mapBaseSpeed * parallaxSpeed * Time.deltaTime;

        // 2. 오브젝트 위치 업데이트
        transform.Translate(parallaxMovement, 0, 0);

        // 3. 파괴 조건 (화면 밖으로 완전히 벗어났을 때)
        /*float cameraLeftEdgeX = Camera.main.ViewportToWorldPoint(Vector3.zero).x;
        float destroyBuffer = 5f; // 파괴 경계 80 -> 5
        float destroyBoundary = cameraLeftEdgeX - destroyBuffer;

        if (transform.position.x < destroyBoundary)
        {
            Destroy(gameObject);
        }*/

        float cameraLeftEdgeX = Camera.main.ViewportToWorldPoint(Vector3.zero).x;

        // 안전 버퍼 (파괴 시 화면에 비치는 것을 막기 위한 작은 여유)
        float destroyBuffer = 0.5f;

        float objectRightEdgeX = transform.position.x + (objectWidth / 2);

        // 오브젝트의 오른쪽 끝이 카메라 왼쪽 경계 - 버퍼보다 작아졌을 때 파괴
        if (objectRightEdgeX < cameraLeftEdgeX - destroyBuffer)
        {
            Destroy(gameObject);
        }
    }
}