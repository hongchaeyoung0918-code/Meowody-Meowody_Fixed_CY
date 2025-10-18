using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ChartEditor : MonoBehaviour
{
    [Header("Component References")]    
    public AudioSource audioSource;  // 오디오 소스 컴포넌트

    [Header("Editor Settings")]
    public string savePath = "Assets/Charts/chart.json";  // 차트 저장 경로
    private ChartData currentChart;

    [Header("Lane Control")]
    [Range(0, 2)]
    public int currentLane = 0;  // 현재 선택된 레인 (0, 1, 2)

    private enum Type
    {
        NormalJump1,
        NormalJump2,
        DoubleJump,
        Trampoline,
        Slide,
        Attack
            //추가해야 하는 것: NormalJump2, 가시..?
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentChart = new ChartData();
        Debug.Log("Chart Editor Initialized. P to Play/Pause, S to Save");
    }

    // Update is called once per frame
    void Update()
    {
        if(audioSource == null)
        {
            Debug.LogWarning("AudioSource is not assigned.");
            return;
        }

        // Save chart   
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SaveChart();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePlayback();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            // 레인을 1 증가시키지만 최대 레인을 넘지 않도록
            currentLane = Mathf.Min(currentLane + 1, 3);
            Debug.Log($"Lane changed to: {currentLane}");
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            // 레인을 1 감소시키지만 최소 레인(0)보다 작아지지 않도록
            currentLane = Mathf.Max(currentLane - 1, -1);
            Debug.Log($"Lane changed to: {currentLane}");
        }

        if (!audioSource.isPlaying)
           return;

        // Record notes based on key inputs

        // Normal Jump 1
        if (Input.GetKeyDown(KeyCode.W))
        {
            RecordNote(Type.NormalJump1.ToString(), currentLane);
        }

        // Normal Jump 2
        if (Input.GetKeyDown(KeyCode.Q))
        {
            RecordNote(Type.NormalJump2.ToString(), currentLane);
        }

        // Double Jump
        if (Input.GetKeyDown(KeyCode.E))
        {
            RecordNote(Type.DoubleJump.ToString(), currentLane);
        }

        // Trampoline
        if (Input.GetKeyDown(KeyCode.A))
        {
            RecordNote(Type.Trampoline.ToString(), currentLane);
        }

        // Slide
        if (Input.GetKeyDown(KeyCode.S))
        {
            RecordNote(Type.Slide.ToString(), currentLane);
        }

        // Attack
        if (Input.GetKeyDown(KeyCode.D))
        {
            RecordNote(Type.Attack.ToString(), currentLane);
        }
    }

    private void TogglePlayback()
    {
        if(audioSource.isPlaying)
        {
            audioSource.Pause();
            Debug.Log($"Paused at {audioSource.time:F2}s");
        }
        else
        {
            audioSource.Play();
            Debug.Log($"Playing from {audioSource.time:F2}s");
        }
    }

    private void RecordNote(string gimmick, int lane)
    {
        float currentTime = audioSource.time;

        NoteInfo newNote = new NoteInfo
        {
            time = currentTime,
            type = gimmick,
            laneIndex = lane
        };

        currentChart.notes.Add(newNote);

        Debug.Log($"Note Recorded: Time={currentTime:F3}s, Type={gimmick}, Lane={lane}");
    }

    [ContextMenu("Save Chart")]
    private void SaveChart()
    {
        string json = JsonUtility.ToJson(currentChart, true);

        File.WriteAllText(savePath, json);

        Debug.Log($"Chart saved to {savePath}");
    }
}
