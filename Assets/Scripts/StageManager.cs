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
    private RectTransform albumPanelRect;
    public Image albumArtImage;
    public TMP_Text songTitleText;
    public TMP_Text artistText;
    public Button playButton;

    [Header("리소스 설정")]
    public string jsonFileName = "SongInformation";

    [Header("애니메이션 설정")]
    [Range(0.1f, 2.0f)]
    public float animationDuration = 0.4f;
    public float closedX = 1350f;
    public float openedX = 0f;

    private List<StageData> stageDataList;
    private int selectedStage = 1;
    private bool isPanelOpen = false;
    private Coroutine activeSlideCoroutine;

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }

    void Start()
    {
        albumPanelRect = AlbumPanel.GetComponent<RectTransform>();
        Vector2 startPos = albumPanelRect.anchoredPosition;
        startPos.x = closedX;
        albumPanelRect.anchoredPosition = startPos;

        LoadStageData();
        InitiallizeButtons();

        // 해금된 스테이지 반영
        currentStage = GameSettings.CurrentStageUnlocked;
        selectedStage = GameSettings.SelectedStage;

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
        if (playButton != null) playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    public void OnStageButtonClicked(int stageNumber)
    {
        selectedStage = stageNumber;
        GameSettings.SetSelectedStage(stageNumber); //  메서드 사용
        UpdateStageUI();
        ShowAlbumPanel();
    }

    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene($"Stage{selectedStage}");
    }

    public void ShowAlbumPanel()
    {
        AlbumPanel.SetActive(true);
        if (activeSlideCoroutine != null) StopCoroutine(activeSlideCoroutine);
        activeSlideCoroutine = StartCoroutine(SlidePanel(albumPanelRect.anchoredPosition.x, openedX, animationDuration));
        isPanelOpen = true;
    }

    public void CloseAlbumPanel()
    {
        if (!isPanelOpen) return;
        if (activeSlideCoroutine != null) StopCoroutine(activeSlideCoroutine);
        activeSlideCoroutine = StartCoroutine(SlidePanel(albumPanelRect.anchoredPosition.x, closedX, animationDuration, () => AlbumPanel.SetActive(false)));
        isPanelOpen = false;
    }

    IEnumerator SlidePanel(float fromX, float toX, float duration, System.Action onComplete = null)
    {
        float elapsed = 0f;
        Vector2 pos = albumPanelRect.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveT;

            float fastTimeThreshold = 0.25f;
            float distanceThreshold = 0.95f;

            if (t <= fastTimeThreshold)
            {
                float segmentT = t / fastTimeThreshold;
                curveT = Mathf.Lerp(0f, distanceThreshold, segmentT * segmentT);
            }
            else
            {
                float segmentT = (t - fastTimeThreshold) / (1f - fastTimeThreshold);
                float slowCurve = 1f - Mathf.Pow(1f - segmentT, 4f);
                curveT = Mathf.Lerp(distanceThreshold, 1f, slowCurve);
            }

            pos.x = Mathf.LerpUnclamped(fromX, toX, curveT);
            albumPanelRect.anchoredPosition = pos;
            yield return null;
        }

        pos.x = toX;
        albumPanelRect.anchoredPosition = pos;
        onComplete?.Invoke();
        activeSlideCoroutine = null;
    }

    void UpdateStageUI()
    {
        if (stageDataList == null || stageDataList.Count == 0) return;
        StageNumber.text = $"Stage {selectedStage}";
        for (int i = 0; i < stageButtons.Length; i++)
        {
            stageButtons[i].interactable = (i + 1 <= GameSettings.CurrentStageUnlocked);
            ColorBlock cb = stageButtons[i].colors;
            cb.normalColor = (i + 1 == selectedStage) ? Color.gray : Color.white;
            stageButtons[i].colors = cb;
        }

        StageData data = stageDataList.Find(s => s.stage == selectedStage);
        if (data != null)
        {
            Sprite albumSprite = Resources.Load<Sprite>(data.albumIllustration);
            if (albumSprite != null) albumArtImage.sprite = albumSprite;
            songTitleText.text = data.songTitle;
            artistText.text = data.artist;
        }
    }

    void LoadStageData()
    {
        TextAsset jsonText = Resources.Load<TextAsset>(jsonFileName);
        if (jsonText != null) stageDataList = new List<StageData>(FromJson<StageData>(jsonText.text));
    }

    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }
}