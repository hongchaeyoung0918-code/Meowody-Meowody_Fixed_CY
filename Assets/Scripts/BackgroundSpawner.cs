using System;
using UnityEngine;

public class BackgroundSpawner : MonoBehaviour
{
    // === 언덕 등의 연속 배경 설정 ===
    public GameObject[] repeatingBackgrounds; // 언덕 등 연속 배경 프리팹 배열
    public Transform backgroundSpawnPoint;   // 배경이 생성될 기준 위치
    private float nextSpawnX = 0f;          // 다음 배경이 생성되어야 할 X 좌표

    [Header("배경 너비 설정")]
    public float backgroundWidth = 20f;
    
    public float spawnBuffer = 3f;         // 카메라 시야 + 추가 버퍼 (이 거리 이내에 생성)
    private int repeatingIndex = 0;

    public float mapBaseSpeed = 5f; // 맵의 기본 이동 속도

    // === 장식 오브젝트 설정 ===
    public GameObject[] decorationPrefabs;   // 풍차/울타리 등 장식 오브젝트 프리팹 배열
    public float minDecorationInterval = 5f;
    public float maxDecorationInterval = 15f;
    private float nextDecorationX;

    [Header("장식 오브젝트 Y축 보정")]
    // 이 값을 음수로 설정하여 장식 오브젝트의 높이를 낮춥니다. (Inspector에서 설정)
    public float decorationYOffset = -2f;

    private bool isGameActive = false;

    private float psdBackgroundYOffset = -5.0f;

    void Start()
    {
        /*        if (repeatingBackgrounds.Length > 0)
                {
                    SpriteRenderer sr = repeatingBackgrounds[0].GetComponentInChildren<SpriteRenderer>();
                    if (sr != null)
                    {
                        backgroundWidth = sr.bounds.size.x;
                    }
                }*/

        if (repeatingBackgrounds.Length > 0)
        {
            // 1. 모든 자식 SpriteRenderer 중 가장 큰 너비를 가진 것을 찾습니다.
            SpriteRenderer[] srs = repeatingBackgrounds[0].GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer fullWidthSr = null;
            float maxBoundsX = 0f;

            foreach (SpriteRenderer sr in srs)
            {
                // SpriteRenderer의 월드 공간 바운드 너비를 확인
                if (sr.bounds.size.x > maxBoundsX)
                {
                    maxBoundsX = sr.bounds.size.x;
                    fullWidthSr = sr;
                }
            }

            if (fullWidthSr != null)
            {
                // 가장 넓은 SpriteRenderer의 너비로 backgroundWidth 설정
                backgroundWidth = fullWidthSr.bounds.size.x;
                Debug.Log($"Background Width Calculated: {backgroundWidth}");
            }
            else
            {
                // SpriteRenderer를 찾지 못한 경우
                Debug.LogWarning("Could not find any SpriteRenderer in background prefab. Using default backgroundWidth.");
            }
        }

        nextSpawnX = Camera.main.transform.position.x;
        nextDecorationX = nextSpawnX;

        float initialFillBoundary = Camera.main.transform.position.x + (backgroundWidth * 3f);

        // 배경 초기 생성
        while (initialFillBoundary > nextSpawnX)
        {
            SpawnRepeatingBackground();
        }

        // 장식 초기 생성 (화면이 채워질 때까지)
        float decorationFillBoundary = Camera.main.transform.position.x + spawnBuffer;
        while (decorationFillBoundary > nextDecorationX)
        {
            SpawnRandomDecoration();
        }
    }

    void Update()
    {
        if (!isGameActive) return;

        // 맵 이동에 따라 다음 생성 지점을 왼쪽으로 이동
        float movement = mapBaseSpeed * Time.deltaTime;
        nextSpawnX -= movement;
        nextDecorationX -= movement;

        //float spawnBoundary = backgroundSpawnPoint.position.x; // 일반적으로 0f
        float cameraRightEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;
        float spawnBoundary = cameraRightEdgeX + spawnBuffer;

/*        if (nextSpawnX <= spawnBoundary)
        {
            SpawnRepeatingBackground();
        }*/

        // 3. 랜덤 장식 오브젝트 생성 확인 (핵심 수정: while 대신 if 사용)
        if (nextDecorationX <= spawnBoundary)
        {
            SpawnRandomDecoration();
        }
    }

    void SpawnRepeatingBackground()
    {
        if (repeatingBackgrounds.Length == 0) return;

        GameObject bgPrefab = repeatingBackgrounds[repeatingIndex];
        float spawnY = backgroundSpawnPoint.position.y;

        if (bgPrefab.GetComponent<PSDTag>() != null)
        {
            // PSD 배경일 경우에만 오프셋을 적용합니다.
            spawnY += psdBackgroundYOffset;
        }

        repeatingIndex++;
        if (repeatingIndex >= repeatingBackgrounds.Length)
        {
            repeatingIndex = 0;
        }

        //Vector3 spawnPos = new Vector3(nextSpawnX, backgroundSpawnPoint.position.y, backgroundSpawnPoint.position.z);
        //Instantiate(bgPrefab, spawnPos, Quaternion.identity, transform);
        Vector3 spawnPos = new Vector3(nextSpawnX, spawnY, backgroundSpawnPoint.position.z);
        Instantiate(bgPrefab, spawnPos, Quaternion.identity, transform);


        // 다음 생성 지점을 현재 배경의 오른쪽 끝으로 업데이트합니다.
        nextSpawnX += backgroundWidth;
    }

    void SpawnRandomDecoration()
    {
        if (decorationPrefabs.Length == 0) return;

        GameObject decPrefab = decorationPrefabs[UnityEngine.Random.Range(0, decorationPrefabs.Length)];

        float spawnY = backgroundSpawnPoint.position.y + decorationYOffset;

        Vector3 spawnPos = new Vector3(nextDecorationX, spawnY, backgroundSpawnPoint.position.z - 1f);
        Instantiate(decPrefab, spawnPos, Quaternion.identity, transform);

        SetNextDecorationX(nextDecorationX);
    }

    void SetNextDecorationX(float currentX)
    {
        nextDecorationX = currentX + UnityEngine.Random.Range(minDecorationInterval, maxDecorationInterval);
    }

    public void SetGameActive(bool isActive)
    {
        this.isGameActive = isActive;

        // 비활성화 시 Debug 로그 출력
        if (!isActive)
        {
            Debug.Log("BackgroundSpawner: Movement Halted (Dialogue active).");
        }
        else
        {
            Debug.Log("BackgroundSpawner: Movement Resumed.");
        }
    }
}