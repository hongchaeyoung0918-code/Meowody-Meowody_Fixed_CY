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

    private LT_PlayerController playerController;
    private PlayerStats playerStats;
    private BackgroundSpawner[] backgroundSpawners; // 배경 스크롤러 참조 (선택사항)

    private bool isPaused = false;
    private bool isGameActive = false;

    void Awake()
    {
        // 매니저 및 컴포넌트 캐시
        playerStats      = FindFirstObjectByType<PlayerStats>();
        playerController = FindFirstObjectByType<LT_PlayerController>();

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

        // 3. �÷��̾� ���� �ʱ�ȭ
        if (playerStats != null)
        {
            playerStats.ResetHP();
            UpdateHPUi(playerStats.HP);
        }

        // 4. ���� ��� ����
        StartGame();
    }

    private void OnEnable()
    {
        PlayerStats.OnHPChanged += UpdateHPUi;
        Debug.Log("[LT_UI] OnEnable - subscribed to OnHPChanged");
    }

    private void OnDisable()
    {
        PlayerStats.OnHPChanged -= UpdateHPUi;
        Debug.Log("[LT_UI] OnDisable - unsubscribed from OnHPChanged");
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
        if (!isGameActive) return;

        isPaused = !isPaused;

        if (PauseUI != null) PauseUI.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        // �Ͻ����� �� �÷��̾� ��ũ��Ʈ ���� ������ �ʾƵ� TimeScale 0 ���п� ����
        // ������ ��� �����̳� �ڷ�ƾ ���� ���� ó���� �ʿ��� �� ����
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