using UnityEngine;

public class FeverManager : MonoBehaviour
{
    [Header("Settings")]
    public float feverSpeedMultiplier = 1.5f; // 피버 시 1.5배
    public float feverDuration = 5.0f; // 피버 지속 시간

    [Header("References")]
    public LT_PlayerController player;

    private bool isFeverActive = false;
    private float feverTimer = 0f;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<LT_PlayerController>();
    }

    void Update()
    {
        // 피버 상태일 때만 타이머 작동
        if (isFeverActive)
        {
            feverTimer -= Time.deltaTime;

            if (feverTimer <= 0f)
            {
                EndFever();
            }
        }
    }

    // 트리거 발동
    public void ActivateFeverByDistance()
    {
        if (isFeverActive) return;
        Debug.Log("피버 타임 발동: 구간 통과");
        StartFever();
    }

    // 게이지 발동
    public void ActivateFeverByGauge()
    {
        if (isFeverActive) return;
        Debug.Log("피버 타임 발동: 게이지 MAX");
        StartFever();
    }

    private void StartFever()
    {
        isFeverActive = true;
        feverTimer = feverDuration;

        if (player != null)
        {
            player.SetFeverMode(true, feverSpeedMultiplier);
        }

        // 피버 음악 or 이팩트
    }

    private void EndFever()
    {
        isFeverActive = false;
        feverTimer = 0f;

        Debug.Log("Fever time over");

        if (player != null)
        {
            player.SetFeverMode(false);
        }
    }

    public float GetRemainingFeverTime()
    {
        return Mathf.Max(0f, feverTimer);
    }
}