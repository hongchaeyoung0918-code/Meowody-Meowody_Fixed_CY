using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class LT_MainUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject InGameUI;
    public GameObject GameClearUI;
    public GameObject GameOverUI;
    public GameObject PauseUI;

    [Header("HP UI Settings")]
    public Image[] hpIcons;
    public Sprite activeHPSprite;
    public Sprite inactiveHPSprite;

    [Header("Manager References")]
    private LT_PlayerController playerController;
    private PlayerStats playerStats;
    private BackgroundSpawner[] backgroundSpawners; // 배경 스크롤은 유지 (원근감용)

    private bool isPaused = false;
    private bool isGameActive = false;

    void Awake()
    {
        // 1. 매니저 및 컴포넌트 캐싱
        playerStats = FindFirstObjectByType<PlayerStats>();
        playerController = FindFirstObjectByType<LT_PlayerController>();

        // 배경 스크롤러 찾기 (필수 아님)
        backgroundSpawners = FindObjectsByType<BackgroundSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // ColorManager 볼륨 참조 연결 (안전장치) -> 채영님 버전으로 변경 필요
        if (ColorManager.Instance != null && ColorManager.Instance.volume == null)
        {
            ColorManager.Instance.volume = FindFirstObjectByType<Volume>();
        }
    }

    void Start()
    {
        Time.timeScale = 1f;

        // 2. UI 초기화
        GameClearUI.SetActive(false);
        GameOverUI.SetActive(false);
        if (PauseUI != null) PauseUI.SetActive(false);

        // 3. 플레이어 상태 초기화
        if (playerStats != null)
        {
            playerStats.ResetHP();
            UpdateHPUi(playerStats.HP);
        }

        // 4. 게임 즉시 시작
        StartGame();
    }

    private void OnEnable()
    {
        PlayerStats.OnHPChanged += UpdateHPUi;
    }

    private void OnDisable()
    {
        PlayerStats.OnHPChanged -= UpdateHPUi;
    }

    // --- Core Game Flow ---

    public void StartGame()
    {
        isGameActive = true;
        InGameUI.SetActive(true);
        SetGameElementsActive(true);
    }

    public void ShowGameClear()
    {
        isGameActive = false;
        SetGameElementsActive(false);

        InGameUI.SetActive(false);
        GameClearUI.SetActive(true);

    }

    public void ShowGameOver()
    {
        isGameActive = false;
        SetGameElementsActive(false);

        InGameUI.SetActive(false);
        GameOverUI.SetActive(true);

        if (ColorManager.Instance != null)
        {
            ColorManager.Instance.SetGameOverGauge();
        }
    }

    // --- Helper Methods ---

    private void SetGameElementsActive(bool isActive)
    {
        // 1. 플레이어 활성/비활성
        if (playerController != null)
        {
            playerController.enabled = isActive;

            // 게임 종료 시 제자리에 멈추게 하려면 리지드바디 제어 추가
            if (!isActive)
            {
                Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero; // Unity 6
            }
        }

        // 2. 배경 스크롤 등 환경 요소
        if (backgroundSpawners != null)
        {
            foreach (var spawner in backgroundSpawners)
            {
                if (spawner != null) spawner.SetGameActive(isActive);
            }
        }

        // 3. 컬러(체력/시간) 매니저
        if (ColorManager.Instance != null)
        {
            ColorManager.Instance.SetColorUpdateActive(isActive);
        }
    }

    // --- UI Event Handlers ---

    public void UpdateHPUi(int currentHP)
    {
        if (hpIcons == null) return;

        for (int i = 0; i < hpIcons.Length; i++)
        {
            if (hpIcons[i] == null) continue;
            hpIcons[i].sprite = (i < currentHP) ? activeHPSprite : inactiveHPSprite;
        }
    }

    public void OnPauseBtnClicked()
    {
        if (!isGameActive) return;

        isPaused = !isPaused;

        if (PauseUI != null) PauseUI.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        // 일시정지 시 플레이어 스크립트 등은 멈추지 않아도 TimeScale 0 덕분에 멈춤
        // 하지만 배경 음악이나 코루틴 등은 별도 처리가 필요할 수 있음
    }

    public void OnRetryBtnClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHomeBtnClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SelectScene"); // 혹은 메인 메뉴
    }
}