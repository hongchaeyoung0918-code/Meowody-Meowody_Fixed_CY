using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class NoteManager : MonoBehaviour
{
    [Header("Chart Loading")]
    public string chartFilePath = "Assets/Charts/chart.json";
    private ChartData currentChart;

    [Header("Game Setup")]
    public AudioSource audioSource;
    public Transform spawnPoint;
    public float noteSpeed = 5f;
    public float preSpawnTime = 2f; //노드 스폰 시간

    [Header("Object Pooling/Prefabs")]
    public GameObject[] notePrefabs;
    private Dictionary<string, GameObject> gimmickToPrefabMap;

    [Header("Note Offsets")]
    public float gimmickYOffset = 1.3f;
    public float slideYOffset = 2.0f;

    [Header("Lane Setup")]
    public float[] laneYPositions = new float[]
    {
        -5f,
        -2.5f,
        0f,
        2.5f
    };


    private int nextNoteIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeGimmickMap();

        string fullPath = Path.Combine(Application.dataPath, chartFilePath.Replace("Assets/", ""));
        currentChart = ChartLoader.LoadChart(fullPath);

        if (currentChart == null)
        {
            enabled = false;
            return;
        }

        // 3. 게임 시작 (음악 재생)
        audioSource.Play();
        Debug.Log("Music started and Note Spawner active.");
    }

    // Update is called once per frame
    void Update()
    {
        if (currentChart == null || !audioSource.isPlaying) return;

        // 현재 음악 시간
        float currentTime = audioSource.time;

        // 노드 스폰 로직
        while (nextNoteIndex < currentChart.notes.Count)
        {
            NoteInfo nextNote = currentChart.notes[nextNoteIndex];

            if (nextNote.time <= currentTime + preSpawnTime)
            {
                // 노드 생성 및 초기 설정
                //SpawnNote(nextNote);
                SpawnNoteWithSupport(nextNote);
                nextNoteIndex++;
            }
            else
            {
                // 아직 스폰할 시간이 아님. 다음 프레임 대기
                break;
            }
        }
    }

    private void InitializeGimmickMap()
    {
        gimmickToPrefabMap = notePrefabs.ToDictionary(
            prefab => prefab.name.Replace("(Clone)", "").Trim(), // 프리팹 이름이 기믹 타입이 되도록 가정
            prefab => prefab
        );
    }

    private void SpawnNoteWithSupport(NoteInfo noteData)
    {
        string type = noteData.type;
        bool needsSupport = (type == "Slide" || type == "Attack" || type == "Trampoline" || type == "Citizen");

        // 지원 노드(NormalJump2)가 필요한 기믹 목록
        if (needsSupport)
        {
            NoteInfo supportNote = new NoteInfo
            {
                time = noteData.time,
                type = "NormalJump2",
                laneIndex = noteData.laneIndex
            };
            PerformSpawn(supportNote, 0f); // 0f: 오프셋 없음

            // 2. 원래의 기믹 노드를 생성하기 위한 오프셋 값 결정
            float finalOffset;
            if (type == "Slide")
            {
                // 슬라이드만 전용 오프셋 적용
                finalOffset = slideYOffset;
            }
            else
            {
                // 그 외 지원 필요한 기믹은 일반 오프셋 적용
                finalOffset = gimmickYOffset;
            }

            // 3. 원래의 기믹 노드를 생성 (결정된 오프셋 적용)
            PerformSpawn(noteData, finalOffset);
        }
        else
        {
            // 지원 노드가 필요 없는 순수 노드는 오프셋 미적용 (라인 위치에 생성)
            PerformSpawn(noteData, 0f);
        }
    }

    private void PerformSpawn(NoteInfo noteData, float offsetValue)
    {
        // 1. 기믹 타입에 해당하는 프리팹 가져오기
        if (!gimmickToPrefabMap.TryGetValue(noteData.type, out GameObject prefab))
        {
            Debug.LogWarning($"Prefab not found for gimmick type: {noteData.type}. Check assignments and naming.");
            return;
        }

        float spawnY = 0f;
        if (noteData.laneIndex >= 0 && noteData.laneIndex < laneYPositions.Length)
        {
            spawnY = laneYPositions[noteData.laneIndex];
        }
        else
        {
            Debug.LogError($"Invalid laneIndex: {noteData.laneIndex}. Using default (0).");
        }

        // 오프셋 값 적용 (offsetValue가 0이 아니면 적용됨)
        spawnY += offsetValue;

        // 2. 스폰 위치 정의
        Vector3 spawnPosition = new Vector3(spawnPoint.position.x, spawnY, spawnPoint.position.z);

        // 3. 노드 오브젝트 생성
        GameObject newNote = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // 4. NoteMovement 컴포넌트 설정 및 이동 시작
        NoteMovement noteMovement = newNote.GetComponent<NoteMovement>();
        if (noteMovement != null)
        {
            noteMovement.Initialize(noteSpeed);
        }
        else
        {
            Debug.LogError($"Note prefab ({noteData.type}) is missing NoteMovement component.");
        }
    }

    private void SpawnNote(NoteInfo noteData)
    {
        // 1. 기믹 타입에 해당하는 프리팹 가져오기
        if (!gimmickToPrefabMap.TryGetValue(noteData.type, out GameObject prefab))
        {
            Debug.LogWarning($"Prefab not found for gimmick type: {noteData.type}");
            return;
        }

        float spawnY = 0f;
        if (noteData.laneIndex >= 0 && noteData.laneIndex < laneYPositions.Length)
        {
            spawnY = laneYPositions[noteData.laneIndex];
        }
        else
        {
            Debug.LogError($"Invalid laneIndex: {noteData.laneIndex}. Using default (0).");
        }

        // 2. 스폰 위치 정의: (SpawnPoint의 X, 계산된 spawnY, SpawnPoint의 Z)
        Vector3 spawnPosition = new Vector3(spawnPoint.position.x, spawnY, spawnPoint.position.z);

        // 3. 노드 오브젝트 생성
        GameObject newNote = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // 3. NoteMovement 컴포넌트 설정 및 이동 시작
        NoteMovement noteMovement = newNote.GetComponent<NoteMovement>();
        if (noteMovement != null)
        {
            noteMovement.Initialize(noteSpeed);
        }
        else
        {
            Debug.LogError("Note prefab is missing NoteMovement component.");
        }
    }
}
