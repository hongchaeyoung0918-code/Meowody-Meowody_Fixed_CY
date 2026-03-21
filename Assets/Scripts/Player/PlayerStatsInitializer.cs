using UnityEngine;

public class PlayerStatsInitializer : MonoBehaviour
{
    private void Start()
    {
        // 대화 시스템 테스트할 때 플래그 확인
        MainUIManager uiManager = FindFirstObjectByType<MainUIManager>();
        if (uiManager != null && uiManager.isDialogueTestScene)
        {
            Debug.Log("[PlayerStatsInitializer] 대화 테스트 씬 → 초기화 스킵");
            return;
        }

        // 씬 시작 시 PlayerStats를 찾아서 초기화
        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        if (stats != null)
        {
            stats.ResetHP();
            Debug.Log("PlayerStats 초기화 완료: HP = " + stats.HP);
        }
        else
        {
            Debug.LogWarning("씬에서 PlayerStats를 찾을 수 없습니다.");
        }
    }
}
