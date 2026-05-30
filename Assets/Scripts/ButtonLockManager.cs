using UnityEngine;
using UnityEngine.UI;

public class ButtonLockManager : MonoBehaviour
{
    [Header("스테이지 버튼 배열")]
    public Button[] stageButtons;

    [Range(0, 255)]
    public int lockedAlpha = 50;   // 잠긴 버튼 알파값 (255 기준)
    [Range(0, 255)]
    public int unlockedAlpha = 255; // 해금 버튼 알파값 (255 기준)

    void Start()
    {
        ApplyLockState();
    }

    void Update()
    {
        // 필요하다면 매 프레임 반영 (해금 상태가 실시간 변할 경우)
        ApplyLockState();
    }

    // 버튼 배열 전체를 잠금/해금 상태에 맞게 자동 적용
    public void ApplyLockState()
    {
        int currentStageUnlocked = GameSettings.CurrentStageUnlocked; // 자동으로 받아오기

        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stageIndex = i + 1;
            bool isUnlocked = stageIndex <= currentStageUnlocked;

            if (stageButtons[i].targetGraphic != null)
            {
                Color c = stageButtons[i].targetGraphic.color;
                c.a = isUnlocked ? unlockedAlpha / 255f : lockedAlpha / 255f;
                stageButtons[i].targetGraphic.color = c;
            }
        }
    }
}