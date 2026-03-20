using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NoteManager : MonoBehaviour
{
    private string chartFilePath = "Charts/chart1-1";
    private ChartData currentChart;

    [Header("Game Setup")]
    public AudioSource audioSource;
    public Transform spawnPoint;
    public float noteSpeed = 7f;
    public float preSpawnTime = 2f;
    private bool isGameActive = false;

    [Header("Object Pooling/Prefabs")]
    public GameObject[] notePrefabs;
    private Dictionary<string, GameObject> gimmickToPrefabMap;

    [Header("Note Offsets")]
    public float gimmickYOffset = 1.3f;
    public float slideYOffset = 2.5f;

    [Header("Lane Setup")]
    public float[] laneYPositions = new float[] { -5f, -2.5f, 0f, 2.5f };

    [Header("Tutorial Status (Stage 1 Only)")]
    private bool hasShownJump = false;
    private bool hasShownSlide = false;
    private bool hasShownJump2 = false;

    private int nextNoteIndex = 0;
    private bool isSongFinished = false;
    private float sideNoteXOffset = 1.5f;

    void Start()
    {
        InitializeGimmickMap();
        int currentStage = GameSettings.SelectedStage;
        string dynamicChartPath = $"Charts/chart{currentStage}";
        TextAsset jsonText = Resources.Load<TextAsset>(dynamicChartPath);

        if (jsonText == null) { enabled = false; return; }
        currentChart = ChartLoader.LoadChart(jsonText.text);
        if (currentChart == null) { enabled = false; return; }
    }

    void Update()
    {
        // 튜토리얼 일시정지 중이면 노트 생성 로직 중단
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsPaused()) return;

        if (!isGameActive || currentChart == null || isSongFinished) return;

        float currentTime = audioSource.time;

        if (audioSource.clip != null && !audioSource.isPlaying && currentTime > 0)
        {
            isSongFinished = true;
            MainUIManager mainUIManager = FindFirstObjectByType<MainUIManager>();
            if (mainUIManager != null) mainUIManager.ShowGameClear();
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

    // [중요] 노트가 일정 거리에 도달했을 때 NoteMovement에서 이 함수를 호출합니다.
    public void TriggerTutorialIfFirstTime(string type)
    {
        if (GameSettings.SelectedStage != 1) return;

        if ((type == "SmallThorn" || type == "BigThorn") && !hasShownJump)
        {
            hasShownJump = true;
            TutorialManager.Instance.ShowTutorial(type, audioSource);
        }
        else if (type == "Slide" && !hasShownSlide)
        {
            hasShownSlide = true;
            TutorialManager.Instance.ShowTutorial(type, audioSource);
        }
        else if (type == "Jump2" && !hasShownJump2)
        {
            hasShownJump2 = true;
            TutorialManager.Instance.ShowTutorial(type, audioSource);
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

        if (needsSupport)
        {
            NoteInfo supportNote = new NoteInfo { time = noteData.time, type = "NormalJump2", laneIndex = noteData.laneIndex };
            PerformSpawn(supportNote, 0f);

            if (type == "Slide" || type == "SmallThorn" || type == "BigThorn")
            {
                PerformSpawn(supportNote, 0f, -sideNoteXOffset);
                PerformSpawn(supportNote, 0f, sideNoteXOffset);
            }

            float finalOffset = (type == "Slide") ? slideYOffset : (type == "Attack" ? gimmickYOffset + 0.4f : gimmickYOffset);
            PerformSpawn(noteData, finalOffset);
        }
        else
        {
            PerformSpawn(noteData, 0f);
        }
    }

    private void PerformSpawn(NoteInfo noteData, float offsetValue, float xOffset = 0f)
    {
        if (!gimmickToPrefabMap.TryGetValue(noteData.type, out GameObject prefab)) return;

        float spawnY = (noteData.laneIndex >= 0 && noteData.laneIndex < laneYPositions.Length) ? laneYPositions[noteData.laneIndex] : 0f;
        spawnY += offsetValue;
        float finalX = spawnPoint.position.x + xOffset;

        GameObject newNote = Instantiate(prefab, new Vector3(finalX, spawnY, spawnPoint.position.z), Quaternion.identity);

        NoteMovement noteMovement = newNote.GetComponent<NoteMovement>();
        if (noteMovement != null)
        {
            // Initialize 시 타입 정보도 함께 전달
            noteMovement.Initialize(noteSpeed, noteData.type);
        }
    }

    public void SetGameActive(bool isActive)
    {
        this.isGameActive = isActive;
        if (isActive && audioSource != null && !audioSource.isPlaying) audioSource.Play();
        else if (!isActive && audioSource != null && audioSource.isPlaying) audioSource.Pause();
    }

    public void StopGame()
    {
        this.isGameActive = false;
        if (audioSource != null) audioSource.Stop();
        foreach (NoteMovement note in FindObjectsByType<NoteMovement>(FindObjectsSortMode.None)) Destroy(note.gameObject);
    }
}