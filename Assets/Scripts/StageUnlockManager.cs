using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StageUnlockManager : MonoBehaviour
{
    [Header("현재 진행 스테이지 (1~5)")]
    public int currentStage = 1;

    [Header("UI References")]
    public Button[] stageButtons;

    void Start()
    {
        UpdateStageButtons();
    }

    /// <summary>
    /// 스테이지 버튼 클릭 시 호출
    /// </summary>
    public void OnStageButtonClicked(int stageNumber)
    {
        if (stageNumber <= currentStage)
        {
            SceneManager.LoadScene($"Stage{stageNumber}");
        }
        else
        {
            Debug.Log($"Stage {stageNumber}는 아직 잠겨있습니다!");
        }
    }

    /// <summary>
    /// 스테이지 클리어 시 호출
    /// </summary>
    public void OnStageCleared(int clearedStage)
    {
        if (clearedStage == currentStage)
        {
            currentStage++;
            if (currentStage > stageButtons.Length)
                currentStage = stageButtons.Length; // 최대 스테이지 제한
            UpdateStageButtons();
        }
    }

    /// <summary>
    /// 버튼 잠금/해제 갱신
    /// </summary>
    private void UpdateStageButtons()
    {
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stageIndex = i + 1;
            stageButtons[i].interactable = (stageIndex <= currentStage);

            // 버튼 클릭 이벤트 연결
            int capturedIndex = stageIndex;
            stageButtons[i].onClick.RemoveAllListeners();
            stageButtons[i].onClick.AddListener(() => OnStageButtonClicked(capturedIndex));
        }
    }
}
