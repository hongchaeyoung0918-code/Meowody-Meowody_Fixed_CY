using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject slideObstaclePrefab;
    public GameObject citizenPrefab;
    public GameObject jumpOrbPrefab;
    public GameObject trampoPrefab;

    [Header("References")]
    public Transform playerTransform;
    public Camera mainCamera;

    [Header("Tilemap References")]
    public Tilemap groundTilemap;    // 지형을 배치할 Tilemap 컴포넌트
    public TileBase platformTile;  //junmp

    [Header("Generation Settings")]
    public float playerBaseY = 0f;     // 플레이어가 서 있는 기준 Y 높이
    public float spawnMargin = 15f;    // 카메라 오른쪽 경계에서 생성될 거리
    public int totalBeatsToGenerate = 200; // 생성할 총 박자 수
    public float creationProbability = 0.3f; // 각 박자에 오브젝트를 생성할 확률

    // 맵 데이터 리스트 (노드 배열)
    private List<Node> mapNodes = new List<Node>();
    private float lastSpawnBeat = 0;

    private const float TILE_HEIGHT = 1.0f;


    void Start()
    {
        if (playerTransform == null) playerTransform = FindObjectOfType<PlayerController>()?.transform;
        if (mainCamera == null) mainCamera = Camera.main;

        // 플레이어의 초기 Y 위치를 기준 높이로 설정 (맵의 평지 높이)
        if (playerTransform != null)
        {
            playerBaseY = playerTransform.position.y;
        }
    }

    // 맵 데이터 생성 (전처리 단계)
    public void GenerateMapData()
    {
        mapNodes.Clear();
        System.Random rand = new System.Random();

        for (int i = 1; i <= totalBeatsToGenerate; i++)
        {
            float currentBeat = (float)i;

            // 1. 박자에 오브젝트를 생성할지 확률적으로 결정
            if (rand.NextDouble() < creationProbability)
            {
                // 2. 랜덤 타입 결정
                NodeType type = GetRandomNodeType(rand);

                // 3. 타입에 따른 Y 위치 설정 (플레이어 땅 높이 기준)
                float relativeY = 0f;

                switch (type)
                {
                    case NodeType.SLIDE_OBSTACLE:
                        // 땅 높이(0.0)에서 생성. (슬라이딩 오브젝트는 땅 위를 지나가야 함)
                        // relativeY = 0.0f;로 설정하면 플레이어 발에 맞춰 생성될 수 있으므로, 
                        // 플레이어의 서있는 높이를 기준으로 장애물의 중심을 잡는 것이 좋습니다.
                        relativeY = 0.5f;
                        break;

                    case NodeType.CITIZEN:
                        // 땅 높이(0.0f)에 생성. (시민은 땅 위에 서 있어야 함)
                        relativeY = 0.0f;
                        break;

                    // 새로운 플랫폼 높이 설정:
                    case NodeType.JUMP_PLATFORM_1:
                        // 땅보다 TILE_HEIGHT * 1 만큼 아래에 생성하여 밟을 수 있는 공간 확보
                        relativeY = -TILE_HEIGHT * 1;
                        break;
                    case NodeType.JUMP_PLATFORM_2:
                        relativeY = -TILE_HEIGHT * 2;
                        break;
                    case NodeType.JUMP_PLATFORM_3:
                        relativeY = -TILE_HEIGHT * 3;
                        break;

                    case NodeType.TRAMPOLINE:
                        // 트램폴린도 땅 높이에 생성
                        relativeY = 0.0f;
                        break;

                    case NodeType.JUMP_ORB:
                        // 조정: 점프 오브를 2칸 정도 높은 위치에 생성
                        relativeY = 2.0f;
                        break;
                }

                // 4. 노드 리스트에 추가
                mapNodes.Add(new Node(type, currentBeat, relativeY));
            }
        }
        Debug.Log($"맵 데이터 생성 완료. 총 노드: {mapNodes.Count}");
    }

    // 생성된 맵 데이터를 확인하고 스폰해야 할 노드가 있는지 체크
    public void CheckAndSpawn(float targetBeat)
    {
        if (mainCamera == null || playerTransform == null) return;

        // X축 생성 위치 계산 (카메라 오른쪽 경계 + 마진)
        Vector3 viewportRightEdge = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, mainCamera.nearClipPlane));
        float spawnX = viewportRightEdge.x + spawnMargin;

        // 이미 생성되지 않았고, 생성해야 할 박자 번호가 targetBeat보다 작거나 같으며, 
        // 마지막으로 스폰된 박자보다 큰 노드를 찾습니다.
        List<Node> nodesToSpawn = mapNodes
            .Where(n => !n.isGenerated && n.beatNumber <= targetBeat && n.beatNumber > lastSpawnBeat)
            .OrderBy(n => n.beatNumber)
            .ToList();

        if (nodesToSpawn.Count > 0)
        {
            foreach (var node in nodesToSpawn)
            {
                SpawnNode(node, spawnX);
                node.isGenerated = true;
                lastSpawnBeat = node.beatNumber;
            }
        }
    }

    // 실제 오브젝트 생성
    private void SpawnNode(Node node, float spawnX)
    {
        // Y축 위치 계산: 맵 기준 높이 + 노드의 상대적 Y
        float spawnY = playerBaseY + node.relativeY;

        // 수정된 JUMP_PLATFORM (타일) 처리: 1칸, 2칸, 3칸 높이 모두 포함
        if (node.type == NodeType.JUMP_PLATFORM_1 ||
            node.type == NodeType.JUMP_PLATFORM_2 ||
            node.type == NodeType.JUMP_PLATFORM_3)
        {
            if (groundTilemap != null && platformTile != null)
            {
                // 월드 좌표를 타일맵 좌표(정수)로 변환
                Vector3Int tilePosition = groundTilemap.WorldToCell(new Vector3(spawnX, spawnY, 0));

                // Tilemap에 타일 배치
                groundTilemap.SetTile(tilePosition, platformTile);

                // 플레이어에게 도달할 때까지 사라지지 않도록, 카메라 왼쪽 밖으로 나간 후 타일을 제거하는
                // 추가적인 'Tilemap Cleaner' 로직이 필요합니다. (여기서는 생략)

                Debug.Log($"노드 생성: {node.type} (타일) at Beat {node.beatNumber}");
            }
            else
            {
                Debug.LogError("점프 플랫폼 생성을 위해 groundTilemap 또는 platformTile이 설정되지 않았습니다.");
            }
            return; // 타일은 프리팹 인스턴스화가 필요 없으므로 함수 종료
        }

        // 나머지 상호작용 오브젝트 (프리팹) 처리 (CITIZEN, SLIDE_OBSTACLE, TRAMPOLINE, JUMP_ORB)

        GameObject prefab = GetPrefabByType(node.type);

        if (prefab == null)
        {
            Debug.LogError($"노드 타입 {node.type}에 대한 프리팹이 설정되지 않았습니다!");
            return;
        }

        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0);

        // 프리팹 인스턴스화
        Instantiate(prefab, spawnPosition, Quaternion.identity);
        Debug.Log($"노드 생성: {node.type} (프리팹) at Beat {node.beatNumber}");
    }

    // 노드 타입에 맞는 프리팹 반환
    private GameObject GetPrefabByType(NodeType type)
    {
        switch (type)
        {
            case NodeType.SLIDE_OBSTACLE: return slideObstaclePrefab;
            case NodeType.TRAMPOLINE: return trampoPrefab;
            case NodeType.JUMP_ORB: return jumpOrbPrefab;
            case NodeType.CITIZEN: return citizenPrefab;

            // 모든 플랫폼 타입은 타일맵을 사용하므로 null 반환 (SpawnNode에서 처리)
            case NodeType.JUMP_PLATFORM_1:
            case NodeType.JUMP_PLATFORM_2:
            case NodeType.JUMP_PLATFORM_3:
                return null;
            default: return null;
        }
    }

    private NodeType GetRandomNodeType(System.Random rand)
    {
        // 기존의 JUMP_PLATFORM 대신 새로운 3가지 타입을 포함한 배열을 만듭니다.
        NodeType[] availableTypes = new NodeType[]
        {
        NodeType.SLIDE_OBSTACLE,
        NodeType.CITIZEN,
        NodeType.JUMP_PLATFORM_1,
        NodeType.JUMP_PLATFORM_2,
        NodeType.JUMP_PLATFORM_3,
        NodeType.TRAMPOLINE,
        NodeType.JUMP_ORB
        };

        return availableTypes[rand.Next(availableTypes.Length)];
    }
}