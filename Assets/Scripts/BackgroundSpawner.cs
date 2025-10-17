using UnityEngine;

public class BackgroundSpawner : MonoBehaviour
{
    // === 언덕 등의 연속 배경 설정 ===
    public GameObject[] repeatingBackgrounds; // 언덕 등 연속 배경 프리팹 배열
    public Transform backgroundSpawnPoint;   // 배경이 생성될 기준 위치
    private float nextSpawnX = 0f;          // 다음 배경이 생성되어야 할 X 좌표

    [Header("배경 너비 설정")]
    public float backgroundWidth = 20f;
    
    public float spawnBuffer = 15f;         // 카메라 시야 + 추가 버퍼 (이 거리 이내에 생성)
    private int repeatingIndex = 0;

    // === 장식 오브젝트 설정 ===
    public GameObject[] decorationPrefabs;   // 풍차/울타리 등 장식 오브젝트 프리팹 배열
    public float minDecorationInterval = 5f;
    public float maxDecorationInterval = 15f;
    private float nextDecorationX;

    [Header("장식 오브젝트 Y축 보정")]
    // 이 값을 음수로 설정하여 장식 오브젝트의 높이를 낮춥니다. (Inspector에서 설정)
    public float decorationYOffset = -2f;

    void Start()
    {

        if (repeatingBackgrounds.Length > 0)
        {
            SpriteRenderer sr = repeatingBackgrounds[0].GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                // 월드 좌표계에서의 너비를 사용합니다.
                backgroundWidth = sr.bounds.size.x;
            }
            else
            {
                Debug.LogError("Repeating Background 프리팹에 SpriteRenderer 컴포넌트가 없습니다!");
            }
        }

        // 초기 시작 위치 설정 (첫 배경은 0에서 시작)
        nextSpawnX = backgroundSpawnPoint.position.x;
        SetNextDecorationX(nextSpawnX);

        float initialFillBoundary = Camera.main.transform.position.x + (backgroundWidth * 3f);
        
        // 만약 다음 생성 위치(nextSpawnX)가 아직 화면 경계(initialFillBoundary)를 넘지 않았다면 계속 생성
        while (initialFillBoundary > nextSpawnX)
        {
            SpawnRepeatingBackground();
        }

        Debug.Log("Calculated Background Width: " + backgroundWidth);
    }

    void Update()
    {
        float cameraRightEdge = Camera.main.transform.position.x + spawnBuffer;

        while (cameraRightEdge > nextSpawnX)
        {
            SpawnRepeatingBackground();
        }

        // 2. 랜덤 장식 오브젝트 생성 확인
        if (cameraRightEdge > nextDecorationX)
        {
            SpawnRandomDecoration();
        }
    }

    void SpawnRepeatingBackground()
    {
        if (repeatingBackgrounds.Length == 0) return;

        // 수정: 인덱스를 사용하여 배열의 프리팹을 순차적으로 선택합니다.
        GameObject bgPrefab = repeatingBackgrounds[repeatingIndex];

        // 인덱스를 증가시키고 배열의 끝에 도달하면 0으로 리셋하여 순환합니다.
        repeatingIndex++;
        if (repeatingIndex >= repeatingBackgrounds.Length)
        {
            repeatingIndex = 0;
        }

        Vector3 spawnPos = new Vector3(nextSpawnX, backgroundSpawnPoint.position.y, backgroundSpawnPoint.position.z);
        Instantiate(bgPrefab, spawnPos, Quaternion.identity, transform);

        // 다음 생성 지점 업데이트
        nextSpawnX += backgroundWidth;
    }

    void SpawnRandomDecoration()
    {
        if (decorationPrefabs.Length == 0) return;

        GameObject decPrefab = decorationPrefabs[Random.Range(0, decorationPrefabs.Length)];

        // Y축에 decorationYOffset을 적용하여 높이를 조절합니다.
        float spawnY = backgroundSpawnPoint.position.y + decorationYOffset;
        Vector3 spawnPos = new Vector3(nextDecorationX, spawnY, backgroundSpawnPoint.position.z - 1f);
        Instantiate(decPrefab, spawnPos, Quaternion.identity, transform);

        // 다음 장식 생성 지점 업데이트
        SetNextDecorationX(nextDecorationX);
    }

    void SetNextDecorationX(float currentX)
    {
        // 현재 위치에서 최소/최대 간격 사이의 랜덤 값만큼 떨어진 곳에 다음 생성 지점 설정
        nextDecorationX = currentX + Random.Range(minDecorationInterval, maxDecorationInterval);
    }
}