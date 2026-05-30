using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab; // 위에서 만든 Obstacle 프리팹 연결

    [Header("참조 오브젝트")]
    public Transform player;          // 플레이어 트랜스폼 (높이 기준)
    public Camera mainCamera;         // 메인 카메라 (화면 경계 기준)

    [Header("생성 설정")]
    public float spawnInterval = 3f;  // 장애물 생성 간격
    public float margin = 2f;         // 카메라 오른쪽 경계에서 추가로 떨어진 거리

    private float timer;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("Player Transform이 설정되지 않았습니다!");
            enabled = false;
            return;
        }
        if (mainCamera == null)
        {
            // 메인 카메라를 자동으로 찾습니다.
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera를 씬에서 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }

        timer = spawnInterval;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            SpawnObstacle();
            timer = spawnInterval;
        }
    }

    void SpawnObstacle()
    {
        if (obstaclePrefab == null) return;

        // 1. X축 위치 계산: 카메라 뷰포트의 오른쪽 끝 X 좌표
        // ViewportToWorldPoint(1, y, z)는 카메라 뷰의 오른쪽 끝을 의미합니다.
        Vector3 viewportRightEdge = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, mainCamera.nearClipPlane));

        // 카메라 오른쪽 경계보다 margin 만큼 더 오른쪽에 생성
        float spawnXStart = viewportRightEdge.x + margin;

        // 2. Y축 위치 계산: 플레이어의 현재 Y축 위치 (땅에 서 있는 높이)에 상체 높이만큼 오프셋 추가
        // 0.5f는 플레이어 오브젝트의 중앙에서 상체로 올리는 오프셋 (조정 가능)
        float playerBaseY = player.position.y;
        float spawnYHeight = playerBaseY + 1f;

        Vector3 spawnPosition = new Vector3(spawnXStart, spawnYHeight, 0);

        // 장애물 생성
        Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"장애물 생성: X={spawnXStart:F2}, Y={spawnYHeight:F2}");
    }
}
