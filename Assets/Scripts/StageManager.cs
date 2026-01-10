using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
    public GameObject AlbumPanel;
    private RectTransform albumPanelRect; // Inspector 연결 대신 코드에서 자동 할당
    public Image albumArtImage;
    public TMP_Text songTitleText;
    public TMP_Text artistText;
    public Button playButton;

    [Header("리소스 설정")]
    public string jsonFileName = "SongInformation";

    private List<StageData> stageDataList;
    private int selectedStage = 1;
    private bool isPanelOpen = false;

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }

    void Start()
    {
        // AlbumPanel의 RectTransform 자동 할당
        albumPanelRect = AlbumPanel.GetComponent<RectTransform>();

        LoadStageData();
        InitiallizeButtons();

        selectedStage = currentStage;
        UpdateStageUI();

        AlbumPanel.SetActive(false);
    }

    void InitiallizeButtons()
    {
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stageIndex = i + 1;
            stageButtons[i].onClick.AddListener(() => OnStageButtonClicked(stageIndex));
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
    }

    public void OnStageButtonClicked(int stageNumber)
    {
        selectedStage = stageNumber;
        UpdateStageUI();
        ShowAlbumPanel();
    }

    public void OnPlayButtonClicked()
    {
        GameSettings.SetSelectedStage(selectedStage);
        GameSettings.SetDialogueType(GameSettings.DialogueType.Intro);
        SceneManager.LoadScene($"Stage{selectedStage}");
    }

    // 앨범 패널 열기 (슬라이드 인)
    public void ShowAlbumPanel()
    {
        AlbumPanel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(SlidePanel(albumPanelRect, 1350f, 0f, 0.5f));
        isPanelOpen = true;
    }

    // 앨범 패널 닫기 (슬라이드 아웃)
    public void CloseAlbumPanel()
    {
        if (!isPanelOpen) return;

        StopAllCoroutines();
        StartCoroutine(SlidePanel(albumPanelRect, 0f, 1350f, 0.5f, () => AlbumPanel.SetActive(false)));
        isPanelOpen = false;
    }

    // 슬라이드 애니메이션 코루틴
    IEnumerator SlidePanel(RectTransform rect, float fromX, float toX, float duration, System.Action onComplete = null)
    {
        float elapsed = 0f;
        Vector2 pos = rect.anchoredPosition;
        pos.x = fromX;
        rect.anchoredPosition = pos;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            pos.x = Mathf.Lerp(fromX, toX, t);
            rect.anchoredPosition = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        pos.x = toX;
        rect.anchoredPosition = pos;

        onComplete?.Invoke();
    }

    void UpdateStageUI()
    {
        if (stageDataList == null || stageDataList.Count == 0)
        {
            Debug.LogWarning("Stage data is not loaded or empty.");
            return;
        }

        StageNumber.text = $"Stage {selectedStage}";

        for (int i = 0; i < stageButtons.Length; i++)
        {
            stageButtons[i].interactable = (i + 1 <= currentStage);

            ColorBlock cb = stageButtons[i].colors;
            cb.normalColor = (i + 1 == selectedStage) ? Color.gray : Color.white;
            stageButtons[i].colors = cb;
        }

        StageData data = stageDataList.Find(s => s.stage == selectedStage);

        if (data != null)
        {
            Sprite albumSprite = Resources.Load<Sprite>(data.albumIllustration);
            if (albumSprite != null)
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
            Debug.LogWarning("No stage data found for stage: " + selectedStage);
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