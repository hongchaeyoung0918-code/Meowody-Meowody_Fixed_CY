using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SimpleNoteManager : MonoBehaviour
{
    [Header("Chart Loading")]
    public string chartFilePath = "Assets/Charts/chart.json";
    private ChartData currentChart;

    [Header("Game Setup")]
    public AudioSource audioSource;
    public Transform spawnPoint;
    public float noteSpeed = 6f;
    public float preSpawnTime = 2f;
    public float startDelayTime = 3f;

    [Header("Object Pooling/Prefabs")]
    public GameObject[] notePrefabs;
    private Dictionary<string, GameObject> gimmickToPrefabMap;

    [Header("Note Offsets")]
    public float gimmickYOffset = 1.3f;
    public float slideYOffset = 4.0f;

    [Header("Lane Setup")]
    public float[] laneYPositions = new float[] { -5f, -2.5f, 0f, 2.5f };

    private int nextNoteIndex = 0;
    private bool isSongFinished = false;
    private bool isGameStarted = false;
    private float sideNoteXOffset = 1.5f;

    void Start()
    {
        InitializeGimmickMap();

        string fullPath = Path.Combine(Application.dataPath, chartFilePath.Replace("Assets/", ""));
        Debug.Log($"Attempting to load chart from: {fullPath}");

        currentChart = ChartLoader.LoadChart(fullPath);

        if (currentChart == null)
        {
            Debug.LogError("Failed to load chart. SimpleNoteManager disabled.");
            enabled = false;
            return;
        }

        if (audioSource != null && audioSource.clip != null)
        {
            StartCoroutine(StartGameWithDelay(startDelayTime));
        }
        else
        {
            Debug.LogError("SimpleNoteManager: AudioSource or AudioClip not assigned.");
            enabled = false;
        }
    }

    IEnumerator StartGameWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.Play();
        isGameStarted = true;
    }

    void Update()
    {
        // [추가] 튜토리얼로 인해 게임이 일시정지 상태라면 로직 중단
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsPaused()) return;

        if (currentChart == null || isSongFinished || !isGameStarted) return;

        float currentTime = audioSource.time;

        if (audioSource.clip != null && !audioSource.isPlaying && currentTime > 0)
        {
            isSongFinished = true;
            return;
        }

        while (nextNoteIndex < currentChart.notes.Count)
        {
            NoteInfo nextNote = currentChart.notes[nextNoteIndex];

            if (nextNote.time <= currentTime + preSpawnTime)
            {
                SpawnNoteWithSupport(nextNote);
                nextNoteIndex++;
            }
            else break;
        }
    }

    private void InitializeGimmickMap()
    {
        gimmickToPrefabMap = notePrefabs.ToDictionary(
            prefab => prefab.name.Replace("(Clone)", "").Trim(),
            prefab => prefab
        );
    }

    private void SpawnNoteWithSupport(NoteInfo noteData)
    {
        string type = noteData.type;
        bool needsSupport = (type == "Slide" || type == "Attack" || type == "Trampoline" || type == "Citizen"
            || type == "WideThorn" || type == "SmallThorn" || type == "BigThorn" || type == "Jump2");

        float targetX = spawnPoint.position.x;
        float spawnXDistance = noteSpeed * preSpawnTime;
        float baseSpawnX = targetX + spawnXDistance;

        if (needsSupport)
        {
            NoteInfo supportNote = new NoteInfo { time = noteData.time, type = "NormalJump2", laneIndex = noteData.laneIndex };
            PerformSpawn(supportNote, 0f, baseSpawnX, 0f);

            if (type == "Slide" || type == "SmallThorn" || type == "BigThorn")
            {
                PerformSpawn(supportNote, 0f, baseSpawnX, -sideNoteXOffset);
                PerformSpawn(supportNote, 0f, baseSpawnX, sideNoteXOffset);
            }

            if (type == "WideThorn")
            {
                PerformSpawn(supportNote, 0f, baseSpawnX, -sideNoteXOffset);
                PerformSpawn(supportNote, 0f, baseSpawnX, -sideNoteXOffset * 2);
                PerformSpawn(supportNote, 0f, baseSpawnX, sideNoteXOffset);
                PerformSpawn(supportNote, 0f, baseSpawnX, sideNoteXOffset * 2);
            }

            float finalOffset = (type == "Slide") ? slideYOffset : gimmickYOffset;
            PerformSpawn(noteData, finalOffset, baseSpawnX, 0f);
        }
        else
        {
            PerformSpawn(noteData, 0f, baseSpawnX, 0f);
        }
    }

    private void PerformSpawn(NoteInfo noteData, float offsetValue, float xPosition, float xOffset = 0f)
    {
        if (!gimmickToPrefabMap.TryGetValue(noteData.type, out GameObject prefab)) return;

        float spawnY = (noteData.laneIndex >= 0 && noteData.laneIndex < laneYPositions.Length) ? laneYPositions[noteData.laneIndex] : 0f;
        spawnY += offsetValue;

        float finalX = xPosition + xOffset;
        Vector3 spawnPosition = new Vector3(finalX, spawnY, spawnPoint.position.z);
        GameObject newNote = Instantiate(prefab, spawnPosition, Quaternion.identity);

        NoteMovement noteMovement = newNote.GetComponent<NoteMovement>();
        if (noteMovement != null)
        {
            // [수정된 부분] 이제 인수 2개(speed, type)를 정상적으로 전달합니다.
            noteMovement.Initialize(noteSpeed, noteData.type);
        }
    }
}