using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;

// 에러 방지를 위해 enum을 파일 최상단(클래스 밖)에 정의
public enum GameFlowState
{
    Dialogue,
    Gameplay,
    ClearScreen,
    FailScreen
}

public class MainUIManager : MonoBehaviour
{
    public GameObject InGameUI;
    public GameObject GameClearUI;
    public GameObject GameOverUI;
    public GameObject PauseUI;

    [Header("Game Clear UI Settings")]
    public TMP_Text maxComboText;

    [Header("HP UI Settings")]
    public Image[] hpIcons;
    public Sprite activeHPSprite;
    public Sprite inactiveHPSprite;

    private PlayerStats playerStats;
    private bool isPaused = false;

    [Header("Manager References")]
    public DialogueManager dialogueManager;
    private PlayerController playerController;
    //public NoteManager noteManager;
    public BackgroundSpawner[] backgroundSpawners;

    private GameFlowState currentFlowState = GameFlowState.Dialogue;
    private int currentStage;

    void Start()
    {
        Time.timeScale = 1f;

        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.ResetHP();
            PlayerStats.OnHPChanged += UpdateHPUi;
            UpdateHPUi(playerStats.HP);
        }

        dialogueManager = dialogueManager ?? FindFirstObjectByType<DialogueManager>();
        playerController = playerController ?? FindFirstObjectByType<PlayerController>();
        //noteManager = noteManager ?? FindFirstObjectByType<NoteManager>();
        backgroundSpawners = FindObjectsByType<BackgroundSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (ColorManager.Instance != null)
        {
            if (ColorManager.Instance.volume == null)
            {
                ColorManager.Instance.volume = FindFirstObjectByType<Volume>();
            }
        }

        currentStage = GameSettings.SelectedStage;

        GameClearUI.SetActive(false);
        GameOverUI.SetActive(false);
        InGameUI.SetActive(false);
        if (PauseUI != null) PauseUI.SetActive(false);

        // 초기화 시 게임 요소(ColorManager 포함) 비활성화
        SetGameActive(false);

        if (dialogueManager != null && dialogueManager.dialogueBackgroundImage != null)
        {
            Color color = dialogueManager.dialogueBackgroundImage.color;
            color.a = 0.0f;
            dialogueManager.dialogueBackgroundImage.color = color;
        }

        StartDialogue("Intro", currentStage);
    }

    private void OnDisable()
    {
        if (playerStats != null) PlayerStats.OnHPChanged -= UpdateHPUi;
    }

    private void OnDestroy()
    {
        if (playerStats != null) PlayerStats.OnHPChanged -= UpdateHPUi;
    }

    public void UpdateHPUi(int currentHP)
    {
        if (hpIcons == null || hpIcons.Length == 0) return;
        for (int i = 0; i < hpIcons.Length; i++)
        {
            if (hpIcons[i] == null) continue;
            hpIcons[i].sprite = (i < currentHP) ? activeHPSprite : inactiveHPSprite;
        }
    }

    public void OnPauseBtnClicked()
    {
        isPaused = !isPaused;
        PauseUI.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
        ToggleGameElementsActive(!isPaused);
    }

    public void ShowGameClear()
    {
        GameSettings.SetDialogueType(GameSettings.DialogueType.Outro);
        StartDialogue("Outro", currentStage);
        InGameUI.SetActive(false);
        // 클리어 시 대화가 시작되므로 게임 요소 정지
        SetGameActive(false);
    }

    public void ShowGameOver()
    {
        ToggleGameElementsActive(false);
        InGameUI.SetActive(false);
        GameOverUI.SetActive(true);
        currentFlowState = GameFlowState.FailScreen;
        ColorManager.Instance.SetGameOverGauge();
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnNextBtnClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SelectScene");
    }

    public void StartDialogue(string type, int stage = -1)
    {
        currentFlowState = GameFlowState.Dialogue;
        int dialogueStage = (stage != -1) ? stage : currentStage;
        string startId = "";
        bool isOutro = (type == "Outro");

        if (type == "Intro")
        {
            if (dialogueStage == 1) startId = "1-1_1";
            else if (dialogueStage == 2) startId = "2-1_1";
            else if (dialogueStage == 3) startId = "3-1_1";
        }
        else if (isOutro)
        {
            int nextChapter = (dialogueStage <= 3) ? 2 : 0;
            if (nextChapter > 0) startId = $"{dialogueStage}-{nextChapter}_1";
        }

        if (!string.IsNullOrEmpty(startId) && dialogueManager != null)
        {
            dialogueManager.StartDialogue(startId, dialogueStage);
        }
        else
        {
            HandleDialogueAction(isOutro ? "SHOW_CLEAR_UI" : "START_GAME");
        }
    }

    public void HandleDialogueAction(string actionType)
    {
        switch (actionType)
        {
            case "START_GAME":
                SetGameActive(true);
                break;
            case "SHOW_CLEAR_UI":
                InGameUI.SetActive(false);
                GameClearUI.SetActive(true);
                currentFlowState = GameFlowState.ClearScreen;

                if (maxComboText != null && ComboManager.Instance != null)
                {
                    maxComboText.text = $"{ComboManager.Instance.maxComboCount}";
                }
                break;
            default:
                SetGameActive(true);
                break;
        }
    }

    private void SetGameActive(bool isActive)
    {
        GameSettings.SetGameplayActive(isActive);
        InGameUI.SetActive(isActive);
        currentFlowState = isActive ? GameFlowState.Gameplay : GameFlowState.Dialogue;

        if (playerController != null)
        {
            playerController.gameObject.SetActive(isActive);
        }

        ToggleGameElementsActive(isActive);
    }

    private void ToggleGameElementsActive(bool isActive)
    {
        //if (noteManager != null) noteManager.SetGameActive(isActive);

        // 추가: StoryUI(대화)나 Pause 시에 ColorManager의 게이지 업데이트를 멈춤
        if (ColorManager.Instance != null)
        {
            ColorManager.Instance.SetColorUpdateActive(isActive);
        }

        if (backgroundSpawners != null)
        {
            foreach (BackgroundSpawner spawner in backgroundSpawners)
            {
                if (spawner != null) spawner.SetGameActive(isActive);
            }
        }
    }
}