using UnityEngine;

public class PlayerStatsInitializer : MonoBehaviour
{
    private void Start()
    {
        // 씬 시작 시 PlayerStats를 찾아서 초기화
        // 대화 시스템 테스트로 인해 잠시 주석 처리 (2026-03-15)
        /*        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
                if (stats != null)
                {
                    stats.ResetHP();
                    Debug.Log("PlayerStats 초기화 완료: HP = " + stats.HP);
                }
                else
                {
                    Debug.LogWarning("씬에서 PlayerStats를 찾을 수 없습니다.");
                }*/
    }
}
