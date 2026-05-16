using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class DecorationSpawner : MonoBehaviour
{
    public Transform cameraTransform;
    // 타입을 StageData에서 StageBCGData로 변경했습니다!
    public StageBCGData currentStageData;

    private float nextSpawnX;
    private Dictionary<GameObject, IObjectPool<GameObject>> pools = new Dictionary<GameObject, IObjectPool<GameObject>>();
    private List<DecorationData> spawnList = new List<DecorationData>(); // 확률 계산용 리스트

    void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        nextSpawnX = cameraTransform.position.x + 5f;

        if (currentStageData == null) return;

        // 1. 스테이지 배경 동적 생성 및 세팅
        for (int i = 0; i < currentStageData.backgroundPrefabs.Length; i++)
        {
            GameObject bg = Instantiate(currentStageData.backgroundPrefabs[i], Vector3.zero, Quaternion.identity);
            var parallax = bg.GetComponent<ParallaxLayer>();
            if (parallax != null && i < currentStageData.scrollFactors.Length)
            {
                parallax.scrollFactorX = currentStageData.scrollFactors[i];
            }
        }

        // 2. 데코레이션 가중치 확률 리스트 및 오브젝트 풀 생성
        foreach (DecorationData deco in currentStageData.decorationDatas)
        {
            if (deco == null || deco.prefab == null) continue;

            // 가중치만큼 리스트에 중복 추가
            for (int i = 0; i < deco.spawnWeight; i++)
            {
                spawnList.Add(deco);
            }

            // 풀 생성
            GameObject targetPrefab = deco.prefab;
            if (!pools.ContainsKey(targetPrefab))
            {
                IObjectPool<GameObject> pool = new ObjectPool<GameObject>(
                    createFunc: () => CreateNewFunc(targetPrefab),
                    actionOnGet: (obj) => obj.SetActive(true),
                    actionOnRelease: (obj) => obj.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj)
                );
                pools.Add(targetPrefab, pool);
            }
        }
    }

    void Update()
    {
        if (currentStageData == null || spawnList.Count == 0) return;

        float cameraRightEdge = cameraTransform.position.x + (Camera.main.orthographicSize * Camera.main.aspect);

        if (cameraRightEdge + 5f >= nextSpawnX)
        {
            SpawnRandomDecoration();
            nextSpawnX += Random.Range(currentStageData.minInterval, currentStageData.maxInterval);
        }
    }

    void SpawnRandomDecoration()
    {
        DecorationData selectedData = spawnList[Random.Range(0, spawnList.Count)];

        if (pools.TryGetValue(selectedData.prefab, out var pool))
        {
            GameObject obj = pool.Get();
            obj.transform.position = new Vector3(nextSpawnX, selectedData.yOffset, transform.position.z);
        }
    }

    private GameObject CreateNewFunc(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        var poolable = obj.AddComponent<PoolableObject>();
        poolable.Setup(cameraTransform, (releasedObj) =>
        {
            if (pools.TryGetValue(prefab, out var pool)) pool.Release(releasedObj);
        });
        return obj;
    }
}