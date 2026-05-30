using TMPro;
using UnityEngine;

// ──────────────────────────────────────────────────────────────────
// DecoColorProgressUI
//
// 사용법:
//   - TextMeshProUGUI 오브젝트에 이 컴포넌트를 추가
//   - percentText 에 표시할 TMP 텍스트를 할당
//   - countText (선택) 에 "3/10" 형식으로 표시할 TMP 텍스트를 할당
//
// 표시 예시:
//   percentText → "72%" / "Perfect!" (90% 이상)
//   countText   → "7 / 10"
//
// 배치 위치:
//   A) InGameUI 하위 → 인게임 실시간 표시
//   B) GameClearUI 하위 → 클리어 결과 화면에만 표시
// ──────────────────────────────────────────────────────────────────
public class DecoColorProgressUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("컬러화 % 를 표시할 TextMeshProUGUI")]
    public TextMeshProUGUI percentText;

    [Tooltip("'컬러화수 / 전체수' 를 표시할 TextMeshProUGUI (선택)")]
    public TextMeshProUGUI countText;

    [Header("Settings")]
    [Tooltip("갱신 간격 (초). 0이면 매 프레임 갱신")]
    public float updateInterval = 0.2f;

    [Tooltip("100% 달성 시 표시할 문자열")]
    public string perfectLabel = "Perfect!";

    private float _timer;

    void Update()
    {
        _timer += Time.deltaTime;
        if (updateInterval > 0f && _timer < updateInterval) return;
        _timer = 0f;

        Refresh();
    }

    /// <summary>외부에서 즉시 갱신이 필요할 때 호출 (클리어 화면 등)</summary>
    public void Refresh()
    {
        if (NewColorManager.Instance == null) return;

        float pct   = NewColorManager.Instance.GetColorizationPercent();
        int total   = NewColorManager.Instance.GetTotalDecoCount();
        int colored = NewColorManager.Instance.GetColorizedDecoCount();

        if (percentText != null)
            percentText.text = pct >= 100f ? perfectLabel : $"{pct:F0}%";

        if (countText != null)
            countText.text = $"{colored} / {total}";
    }
}
