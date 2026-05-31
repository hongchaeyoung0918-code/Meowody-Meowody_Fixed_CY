using System.Collections;
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

    [Header("Countdown UI")]
    public GameObject countdownBubble;    // 말풍선 오브젝트 (자식에 숫자 오브젝트 포함)
    public GameObject[] countdownNumbers; // 0: 3, 1: 2, 2: 1 (3→2→1 순서로 각각의 게임오브젝트)

    private LT_PlayerController playerController;
    private LT_PlayerController_v2 playerControllerV2;
    private PlayerStats playerStats;
    private BackgroundSpawner[] backgroundSpawners; // 배경 스크롤러 참조 (선택사항)

    private bool isPaused = false;
    private bool isGameActive = false;
    private bool isCountingDown = false;

    void Awake()
    {
        // 매니저 및 컴포넌트 캐시
        playerStats        = FindFirstObjectByType<PlayerStats>();
        playerController   = FindFirstObjectByType<LT_PlayerController>();
        playerControllerV2 = FindFirstObjectByType<LT_PlayerController_v2>();

        // 배경 스크롤러 탐색 (선택사항)
        backgroundSpawners = FindObjectsByType<BackgroundSpawner>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        // ColorManager 볼륨 연결 (씬 전환 후 재연결용)
        if (ColorManager.Instance != null && ColorManager.Instance.volume == null)
            ColorManager.Instance.volume = FindFirstObjectByType<Volume>();
    }

    void Start()
    {
        Time.timeScale = 1f;

        // 2. UI �ʱ�ȭ
        GameClearUI.SetActive(false);
        GameOverUI.SetActive(false);
        if (PauseUI != null) PauseUI.SetActive(false);
        if (countdownBubble != null) countdownBubble.SetActive(false);

        // 3. �÷��̾� ���� �ʱ�ȭ
        if (playerControllerV2 != null)
        {
            playerControllerV2.ResetHP();
            UpdateHPUi(playerControllerV2.HP);
        }
        else if (playerStats != null)
        {
            playerStats.ResetHP();
            UpdateHPUi(playerStats.HP);
        }

        // 4. ���� ��� ����
        StartGame();
    }

    private void OnEnable()
    {
        LT_PlayerController_v2.OnHPChanged += UpdateHPUi;
        PlayerStats.OnHPChanged += UpdateHPUi;
    }

    private void OnDisable()
    {
        LT_PlayerController_v2.OnHPChanged -= UpdateHPUi;
        PlayerStats.OnHPChanged -= UpdateHPUi;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPauseBtnClicked();
        }
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
            ColorManager.Instance.SetGameOverGauge();
    }

    // --- Helper Methods ---

    private void SetGameElementsActive(bool isActive)
    {
        // 1. �÷��̾� Ȱ��/��Ȱ��
        if (playerController != null)
        {
            playerController.enabled = isActive;

            // ���� ���� �� ���ڸ��� ���߰� �Ϸ��� ������ٵ� ���� �߰�
            if (!isActive)
            {
                Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero; // Unity 6
            }
        }

        // 2. ��� ��ũ�� �� ȯ�� ���
        if (backgroundSpawners != null)
        {
            foreach (var spawner in backgroundSpawners)
            {
                if (spawner != null) spawner.SetGameActive(isActive);
            }
        }

        // 3. �÷�(ü��/�ð�) �Ŵ���
        if (ColorManager.Instance != null)
        {
            ColorManager.Instance.SetColorUpdateActive(isActive);
        }

        if (NewColorManager.Instance != null)
        {
            NewColorManager.Instance.SetColorUpdateActive(isActive);
        }
    }

    // --- UI Event Handlers ---

    public void UpdateHPUi(int currentHP)
    {
        Debug.Log($"[LT_UI] UpdateHPUi called - HP: {currentHP}, hpIcons: {(hpIcons != null ? hpIcons.Length.ToString() : "NULL")}, activeSprite: {activeHPSprite != null}, inactiveSprite: {inactiveHPSprite != null}");

        if (hpIcons == null) return;

        for (int i = 0; i < hpIcons.Length; i++)
        {
            if (hpIcons[i] == null) continue;
            hpIcons[i].sprite = (i < currentHP) ? activeHPSprite : inactiveHPSprite;
        }
    }

    public void OnPauseBtnClicked()
    {
        if (!isGameActive || isCountingDown) return;

        if (!isPaused)
        {
            // 일시정지 진입
            isPaused = true;
            if (PauseUI != null) PauseUI.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            // 일시정지 해제 → 카운트다운 시작
            if (PauseUI != null) PauseUI.SetActive(false);
            StartCoroutine(ResumeCountdown());
        }
    }

    private IEnumerator ResumeCountdown()
    {
        isCountingDown = true;

        if (countdownBubble != null) countdownBubble.SetActive(true);

        // 3 → 2 → 1 카운트다운 (각 숫자 오브젝트를 순서대로 표시)
        for (int i = 0; i < countdownNumbers.Length; i++)
        {
            // 모든 숫자 비활성화 후 현재 숫자만 활성화
            for (int j = 0; j < countdownNumbers.Length; j++)
            {
                if (countdownNumbers[j] != null)
                    countdownNumbers[j].SetActive(j == i);
            }

            yield return new WaitForSecondsRealtime(1f);
        }

        if (countdownBubble != null) countdownBubble.SetActive(false);

        // 게임 재개
        isPaused = false;
        isCountingDown = false;
        Time.timeScale = 1f;
    }

    public void OnRetryBtnClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHomeBtnClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SelectScene"); // Ȥ�� ���� �޴�
    }
}