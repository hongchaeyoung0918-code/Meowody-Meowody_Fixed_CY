using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class DecorationSpawner : MonoBehaviour
{
    public Transform cameraTransform;
    // Ÿ���� StageData���� StageBCGData�� �����߽��ϴ�!
    public StageBCGData currentStageData;

    private float nextSpawnX;
    private Dictionary<GameObject, IObjectPool<GameObject>> pools = new Dictionary<GameObject, IObjectPool<GameObject>>();
    private List<DecorationData> spawnList = new List<DecorationData>(); // Ȯ�� ���� ����Ʈ

    void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        nextSpawnX = cameraTransform.position.x + 5f;

        if (currentStageData == null) return;

        // 1. �������� ��� ���� ���� �� ����
        for (int i = 0; i < currentStageData.backgroundPrefabs.Length; i++)
        {
            float yPos = (currentStageData.backgroundYPositions != null && i < currentStageData.backgroundYPositions.Length)
                ? currentStageData.backgroundYPositions[i]
                : 0f;
            GameObject bg = Instantiate(currentStageData.backgroundPrefabs[i], new Vector3(0f, yPos, 0f), Quaternion.identity);
            var parallax = bg.GetComponent<ParallaxLayer>();
            if (parallax != null && i < currentStageData.scrollFactors.Length)
            {
                parallax.scrollFactorX = currentStageData.scrollFactors[i];
            }
        }

        // 2. ���ڷ��̼� ����ġ Ȯ�� ����Ʈ �� ������Ʈ Ǯ ����
        foreach (DecorationData deco in currentStageData.decorationDatas)
        {
            if (deco == null || deco.prefab == null) continue;

            // ����ġ��ŭ ����Ʈ�� �ߺ� �߰�
            for (int i = 0; i < deco.spawnWeight; i++)
            {
                spawnList.Add(deco);
            }

            // Ǯ ����
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