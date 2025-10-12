using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class StageData
{
    public int stage;
    public string albumIllustration;
    public string songTitle;
    public string artist;
}

public class StageManager : MonoBehaviour
{
    [Header("현재 진행 스테이지 (1~5)")]
    public int currentStage = 1;

    [Header("UI References")]
    public Button[] stageButtons;
    public TMP_Text StageNumber;
    public Image albumArtImage;
    public TMP_Text songTitleText;
    public TMP_Text artistText;
    public Button playButton;

    [Header("리소스 설정")]
    public string jsonFileName = "SongInformation";

    private List<StageData> stageDataList;
    private int selectedStage = 1;  

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadStageData();
        InitiallizeButtons();

        selectedStage = currentStage;
        UpdateStageUI();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void InitiallizeButtons()
    {
        // 1. 스테이지 선택 버튼 리스너 설정 (1부터 시작하므로 인덱스 i에 1을 더합니다)
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stageIndex = i + 1; // 람다/클로저 문제 해결을 위해 로컬 변수에 복사
            stageButtons[i].onClick.AddListener(() => OnStageButtonClicked(stageIndex));
        }

        // 2. Play 버튼 리스너 설정
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
    }

    public void OnStageButtonClicked(int stageNumber)
    {
        selectedStage = stageNumber;
        UpdateStageUI();
    }

    public void OnPlayButtonClicked()
    {
        // 1. 선택된 스테이지 번호를 GameSettings에 저장
        GameSettings.SetSelectedStage(selectedStage);

        // 2. 씬 로드 후 '인트로 대화'부터 시작하도록 플래그 설정
        GameSettings.SetDialogueType(GameSettings.DialogueType.Intro);

        // 3. MainScene으로 이동
        SceneManager.LoadScene("MainScene");

        // SceneManager.LoadScene($"Stage_{selectedStage}"); 
    }

    void UpdateStageUI()
    {
        if(stageDataList == null || stageDataList.Count == 0)
        {
            Debug.LogWarning("Stage data is not loaded or empty.");
            return;
        }

        StageNumber.text = $"Stage {selectedStage}";

        for (int i = 0; i < stageButtons.Length; i++)
        {
            stageButtons[i].interactable = (i + 1 <= currentStage);
            
            ColorBlock cb = stageButtons[i].colors;
            cb.normalColor = (i + 1 == selectedStage) ? Color.yellow : Color.white; // 예시
            stageButtons[i].colors = cb;
        }

        StageData data = stageDataList.Find(s => s.stage == currentStage);
        if(data != null)
        {
            Sprite albumSprite = Resources.Load<Sprite>(data.albumIllustration);
            if(albumSprite != null)
            {
                albumArtImage.sprite = albumSprite;
            }
            else
            {
                Debug.LogWarning("Album illustration not found: " + data.albumIllustration);
            }
            songTitleText.text = data.songTitle;
            artistText.text = data.artist;
        }
        else
        {
            Debug.LogWarning("No stage data found for stage: " + currentStage);
        }
    }

    void LoadStageData()
    {
        TextAsset jsonText = Resources.Load<TextAsset>(jsonFileName);
        if (jsonText != null)
        {
            stageDataList = new List<StageData>(FromJson<StageData>(jsonText.text));
        }
        else
        {
            Debug.LogError("Failed to load stage data from Resources/" + jsonFileName);
        }
    }

    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }
}