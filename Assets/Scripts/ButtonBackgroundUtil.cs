using UnityEngine;
using UnityEngine.UI;

public static class ButtonBackgroundUtil
{
    // 버튼 배경만 흐리게 (255 기준 알파값 적용)
    public static void SetButtonBackgroundAlpha(Button btn, int alpha255)
    {
        float a = Mathf.Clamp(alpha255, 0, 255) / 255f;

        // 버튼의 배경 이미지(targetGraphic)만 처리
        if (btn.targetGraphic != null)
        {
            Color c = btn.targetGraphic.color;
            c.a = a;
            btn.targetGraphic.color = c;
        }
    }

    // 잠금 상태: 배경 흐리게 (알파 50/255)
    public static void SetButtonLockedBackground(Button btn)
    {
        SetButtonBackgroundAlpha(btn, 50);
    }

    // 해금 상태: 배경 원래대로 (알파 255/255)
    public static void SetButtonUnlockedBackground(Button btn)
    {
        SetButtonBackgroundAlpha(btn, 255);
    }

    // 버튼 배열 전체를 잠금/해금 상태에 맞게 자동 적용
    public static void ApplyStageLockState(Button[] stageButtons, int currentStageUnlocked)
    {
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stageIndex = i + 1;
            bool isUnlocked = stageIndex <= currentStageUnlocked;

            if (isUnlocked)
            {
                SetButtonUnlockedBackground(stageButtons[i]);
            }
            else
            {
                SetButtonLockedBackground(stageButtons[i]);
            }
        }
    }
}