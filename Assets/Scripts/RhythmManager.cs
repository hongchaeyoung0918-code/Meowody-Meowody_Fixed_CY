using UnityEngine;

public class RhythmManager : MonoBehaviour
{
    [Header("Rhythm Settings")]
    public float bpm = 120f; // 음악의 BPM
    public float songStartTime = 0f; // 음악 재생 시작 시간 (Unity Time.time)

    [Header("Game Time")]
    public float secPerBeat;    // 1박자 길이 (초)
    public float songPosition;  // 노래가 시작된 후 경과된 시간
    public float currentBeat;   // 현재 박자 번호 (1.0, 2.0, 3.0...)

    // 생성 알고리즘이 플레이어에게 인지 시간을 주기 위한 지연 시간
    // 이 시간만큼 미리 생성 위치를 계산합니다.
    public float timeToReact = 3f;

    private AudioSource audioSource;
    public LevelGenerator generator; // LevelGenerator 참조

    void Awake()
    {
        secPerBeat = 60f / bpm;

        // 씬에서 AudioSource와 LevelGenerator를 찾습니다.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) Debug.LogError("AudioSource 컴포넌트가 필요합니다!");

        generator = FindObjectOfType<LevelGenerator>();
        if (generator == null) Debug.LogError("LevelGenerator가 씬에 없습니다!");
    }

    void Start()
    {
        // 음악 시작
        songStartTime = Time.time;
        if (audioSource != null)
        {
            audioSource.Play(); // 실제 음악 재생 코드는 여기에 위치
        }

        // 맵 데이터 생성 요청 (노드를 미리 배열합니다)
        if (generator != null)
        {
            generator.GenerateMapData();
        }
    }

    void Update()
    {
        // 현재 노래 경과 시간 계산
        if (audioSource != null)
        {
            songPosition = audioSource.time;
        }
        else
        {
            songPosition = Time.time - songStartTime;
        }

        // 현재 박자 번호 계산
        currentBeat = songPosition / secPerBeat;

        // 생성 요청: 플레이어에게 인지 시간(timeToReact)을 보정하여 노드를 생성합니다.
        // 예를 들어 timeToReact가 3초라면, 현재 시간 + 3초에 도달할 노드를 생성합니다.
        if (generator != null)
        {
            float targetTime = songPosition + timeToReact;
            float targetBeat = targetTime / secPerBeat;

            generator.CheckAndSpawn(targetBeat);
        }
    }
}
