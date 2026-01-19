using UnityEngine;

public static class GameSettings
{
    // 현재 선택된 스테이지
    public static int SelectedStage { get; private set; } = 1;

    // 현재까지 해금된 최대 스테이지
    public static int CurrentStageUnlocked { get; private set; } = 1;

    // None을 추가하여 초기화 상태를 명확히 함
    public enum DialogueType { None, Intro, Outro }
    public static DialogueType CurrentDialogueType = DialogueType.None;

    private static bool isGameplayActive = false;

    // 선택된 스테이지 설정
    public static void SetSelectedStage(int stage)
    {
        SelectedStage = stage;
        PlayerPrefs.SetInt("SelectedStage", stage);
        PlayerPrefs.Save();
    }

    // 해금된 스테이지 갱신
    public static void UnlockStage(int stage)
    {
        if (stage > CurrentStageUnlocked)
        {
            CurrentStageUnlocked = stage;
            PlayerPrefs.SetInt("CurrentStageUnlocked", CurrentStageUnlocked);
            PlayerPrefs.Save();
        }
    }

    // 대화 타입 설정
    public static void SetDialogueType(DialogueType type)
    {
        CurrentDialogueType = type;
    }

    // 게임플레이 상태 확인
    public static bool IsGameplayActive
    {
        get { return isGameplayActive; }
    }

    // 게임플레이 상태 설정
    public static void SetGameplayActive(bool isActive)
    {
        isGameplayActive = isActive;
    }

    // 저장된 데이터 불러오기
    public static void LoadSettings()
    {
        SelectedStage = PlayerPrefs.GetInt("SelectedStage", 1);
        CurrentStageUnlocked = PlayerPrefs.GetInt("CurrentStageUnlocked", 1);
    }
}