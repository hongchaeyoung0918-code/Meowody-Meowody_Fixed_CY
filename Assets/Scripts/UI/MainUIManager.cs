using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public enum GameFlowState
{
    Dialogue,
    Gameplay,
    ClearScreen,
    FailScreen
}

public class MainUIManager : MonoBehaviour
{
    //public static MainUIManager instance;

    public GameObject InGameUI;
    public GameObject GameClearUI;
    public GameObject GameOverUI;
    public GameObject PauseUI;
    //public GameObject Startillust;

    [Header("HP UI Settings")]
    public Image[] hpIcons;
    public Color activeHPColor = Color.white;
    public Color inactiveHPColor = Color.gray;

    private PlayerStats playerStats;
    private bool isPaused = false;

    [Header("Manager References")]
    public DialogueManager dialogueManager;
    private PlayerController playerController;

    private GameFlowState currentFlowState = GameFlowState.Dialogue;
    private int currentStage;

    private void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats를 씬에서 찾을 수 없어 HP UI를 업데이트할 수 없습니다.");
        }

        dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (dialogueManager == null) Debug.LogError("DialogueManager를 씬에서 찾을 수 없습니다.");

        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null) Debug.LogError("PlayerController를 씬에서 찾을 수 없습니다.");

    }

    void Start()
    {
        currentStage = GameSettings.SelectedStage;

        // 초기 UI 상태 설정
        GameClearUI.SetActive(false);
        GameOverUI.SetActive(false);
        InGameUI.SetActive(false);

        // 씬 로드 시 현재 HP로 UI를 한번 업데이트합니다.
        if (playerStats != null)
        {
            UpdateHPUi(playerStats.HP);
        }
    }

    private void OnEnable()
    {
        // 씬이 활성화될 때 PlayerStats의 이벤트 구독
        if (playerStats != null)
        {
            PlayerStats.OnHPChanged += UpdateHPUi;
        }
    }

    private void OnDisable()
    {
        // 씬이 비활성화되거나 오브젝트가 파괴될 때 이벤트 구독 해제
        if (playerStats != null)
        {
            PlayerStats.OnHPChanged -= UpdateHPUi;
        }
    }

    public void UpdateHPUi(int currentHP)
    {
        // HP 아이콘이 3개 미만이면 오류 방지
        if (hpIcons == null || hpIcons.Length == 0) return;

        // HP 아이콘 배열을 순회하며 색상 변경
        for (int i = 0; i < hpIcons.Length; i++)
        {
            if (i < currentHP)
            {
                // 현재 HP보다 인덱스가 작으면 활성화 (흰색)
                hpIcons[i].color = activeHPColor;
            }
            else
            {
                // 현재 HP보다 인덱스가 크거나 같으면 비활성화 (회색)
                hpIcons[i].color = inactiveHPColor;
            }
        }
    }

    public void OnPauseBtnClicked()
    {
        if (isPaused)
        {
            // 이미 일시정지 상태라면
            PauseUI.SetActive(false);
            Time.timeScale = 1f; // 게임 재개
            isPaused = false;
            return;
        }
        else
        {
            PauseUI.SetActive(true);
            Time.timeScale = 0f; // 게임 일시정지
            isPaused = true;
        }
    }

    public void ShowGameClear()
    {
        GameSettings.SetDialogueType(GameSettings.DialogueType.Outro);

        StartDialogue("Outro");

        if (currentFlowState != GameFlowState.Dialogue)
        {
            // Outro 대화가 없는 경우: 바로 클리어 UI 표시
            InGameUI.SetActive(false);
            GameClearUI.SetActive(true);
            currentFlowState = GameFlowState.ClearScreen;
        }
    }

    public void ShowGameOver()
    {
        InGameUI.SetActive(false);
        GameOverUI.SetActive(true);
    }

    public void OnNextBtnClicked()
    {
        GameClearUI.SetActive(false);
        //Startillust.SetActive(true);
        SceneManager.LoadScene("StartScene");
    }

    public void StartDialogue(string type)
    {
        currentFlowState = GameFlowState.Dialogue;

        string startId = "";

        if (type == "Intro")
        {
            // Stage 1 Intro 시작 ID 예시
            if (currentStage == 1) startId = "1-2_1";
            else if (currentStage == 2) startId = "2-2_1"; // Stage 2는 바로 소녀 팬 대화로 시작한다고 가정
            else if (currentStage == 3) startId = "3-3_1"; // Stage 3는 라이벌 늑대 대화로 시작한다고 가정
            // ...
        }
        else if (type == "Outro")
        {
            // Stage 1은 Outro 대화가 없다고 가정하고 바로 클리어 UI로 넘어갈 수 있습니다.
            // Stage 2의 Outro (가상의 대사 ID)
            if (currentStage == 2) startId = "2-4_1_Outro";
        }

        if (!string.IsNullOrEmpty(startId) && dialogueManager != null)
        {
            // DialogueManager에게 해당 스테이지의 대화를 시작하도록 요청
            dialogueManager.StartDialogue(startId, currentStage);
        }
        else
        {
            // 대화가 없으면 바로 게임 시작
            HandleDialogueAction("START_GAME");
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
                ShowGameClear(); // 대화가 끝나고 게임 클리어 화면 표시
                break;
            case "CONTINUE":
                // 다음 대화나 이벤트가 코드로 이어질 경우 (여기서는 무시하고 게임 활성화)
                SetGameActive(true);
                break;
            default:
                SetGameActive(true);
                break;
        }
    }

    // 게임 플레이 활성화/비활성화 (DialogueManager 및 MainUIManager에서 사용)
    private void SetGameActive(bool isActive)
    {
        if (playerController != null)
        {
            // PlayerController에 움직임 활성화/비활성화 기능을 구현해야 합니다.
            // (이전 답변에서 제공된 PlayerController.SetMovementActive(isActive)를 사용)
            // playerController.SetMovementActive(isActive); 
        }

        InGameUI.SetActive(isActive);
        currentFlowState = isActive ? GameFlowState.Gameplay : GameFlowState.Dialogue;
    }
}
