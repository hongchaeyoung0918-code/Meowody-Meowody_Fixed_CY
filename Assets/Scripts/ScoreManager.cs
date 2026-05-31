using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Current Game Data")]
    public int currentScore = 0;
    public int buskingScore = 0;
    public int handshakeCount = 0;
    public int highlightCount = 0;

    public ResultUI uiManager;

    void Awake() => Instance = this;

    public void AddHandshake()
    {
        handshakeCount++;
        int addedScore = 1000;
        currentScore += addedScore;

        // 실시간 UI 업데이트 (ResultUI에 이 함수가 있어야 함)
        if (uiManager != null)
            uiManager.UpdateRealTimeScore(currentScore);
    }

    public void StageClear()
    {
        int finalHandshake = handshakeCount * 1000;
        int finalHighlight = highlightCount * 50;

        if (uiManager != null)
            uiManager.ShowResult(buskingScore, finalHandshake, finalHighlight);
    }
}