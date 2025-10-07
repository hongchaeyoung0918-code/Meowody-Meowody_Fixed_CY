using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

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

    private void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats를 씬에서 찾을 수 없어 HP UI를 업데이트할 수 없습니다.");
        }
    }

    void Start()
    {
        // 초기 UI 상태 설정
        GameClearUI.SetActive(false);
        GameOverUI.SetActive(false);
        InGameUI.SetActive(true);

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
        InGameUI.SetActive(false);
        GameClearUI.SetActive(true);
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
}
