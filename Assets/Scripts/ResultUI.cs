using UnityEngine;
using TMPro;
using System.Collections;

public class ResultUI : MonoBehaviour
{
    [Header("In-Game UI")]
    public TextMeshProUGUI txtRealTimeScore;

    [Header("Result UI Panel")]
    public GameObject resultPanel;
    public TextMeshProUGUI txtBusking;
    public TextMeshProUGUI txtHandshake;
    public TextMeshProUGUI txtHighlight;
    public TextMeshProUGUI txtTotalScore;
    public RectTransform rankStamp; // 도장 (Scale 조절용)
    public GameObject navigationButtons;

    // 1. 실시간 점수 업데이트 (게임 중)
    public void UpdateRealTimeScore(int score)
    {
        txtRealTimeScore.text = $"{score:N0} 명";
        // 간단한 펀치 효과
        txtRealTimeScore.transform.localScale = Vector3.one * 1.1f;
        StopCoroutine("Co_ResetScale");
        StartCoroutine("Co_ResetScale", txtRealTimeScore.transform);
    }

    IEnumerator Co_ResetScale(Transform target)
    {
        yield return new WaitForSeconds(0.1f);
        target.localScale = Vector3.one;
    }

    // 2. 결과 창 표시 시작
    public void ShowResult(int bScore, int hScore, int hiScore)
    {
        resultPanel.SetActive(true);
        StartCoroutine(Co_ResultSequence(bScore, hScore, hiScore));
    }

    IEnumerator Co_ResultSequence(int bScore, int hScore, int hiScore)
    {
        int total = bScore + hScore + hiScore;

        // [시퀀스 1] 드르르륵 카운팅
        float duration = 1.5f;
        StartCoroutine(Co_CountNumber(txtBusking, bScore, duration));
        StartCoroutine(Co_CountNumber(txtHandshake, hScore, duration));
        yield return StartCoroutine(Co_CountNumber(txtHighlight, hiScore, duration));

        // [시퀀스 2] 합산 및 TOTAL 강조
        yield return new WaitForSeconds(0.2f);
        txtTotalScore.text = $"최종 관객 수: {total:N0} 명";
        txtTotalScore.transform.localScale = Vector3.one * 1.2f;

        // [시퀀스 3] 최종 판정 (0.5초 대기 후 도장 쾅!)
        yield return new WaitForSeconds(0.5f);
        rankStamp.gameObject.SetActive(true);

        // 랭크 결정 로직 (기획안 기준)
        // 여기서 점수에 따라 별 1~3개 이미지를 교체하는 로직 추가 가능

        // 쾅! 연출 (큰 크기 -> 원래 크기)
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float curve = elapsed / 0.15f;
            rankStamp.localScale = Vector3.Lerp(Vector3.one * 3f, Vector3.one, curve);
            yield return null;
        }

        // 마무리: 버튼 활성화
        navigationButtons.SetActive(true);
    }

    IEnumerator Co_CountNumber(TextMeshProUGUI targetText, int targetValue, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int current = (int)Mathf.Lerp(0, targetValue, elapsed / duration);
            targetText.text = current.ToString("N0");
            yield return null;
        }
        targetText.text = targetValue.ToString("N0");
    }
}