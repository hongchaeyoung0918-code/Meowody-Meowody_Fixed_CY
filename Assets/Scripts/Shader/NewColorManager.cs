using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ──────────────────────────────────────────────────────────────────
// NewColorManager  (GRIS 스타일 컬러 시스템)
//
// 역할:
//   - colorGauge (0~100) 를 멜로디 구간 노트 수집 기반으로 관리
//   - GrayscaleRendererFeature → 배경 글로벌 채도 제어 (항상 0 = 흑백)
//   - ColorKeeper 오브젝트들에게 매 프레임 colorGauge 전달
//     → 각 오브젝트가 자신의 restoreThreshold 기준으로 개별 컬러 복원
//
// 게이지 변화 조건:
//   증가 — MelodySection.NotifyNoteCollected() → AddGauge()
//   감소 — 장애물 피격 시 DecreaseGaugeOnHit()
// ──────────────────────────────────────────────────────────────────
public class NewColorManager : MonoBehaviour
{
    [Header("Current State")]
    [Range(0f, 100f)]
    [Tooltip("0~100 범위의 컬러 게이지 위치")]
    public float colorGauge = 0f;

    [Header("Hit Penalty Settings")]
    [Tooltip("피격 시 감소할 게이지 퍼센트포인트 (%p)")]
    public float hitPenaltyPercentage = 15f;

    public static NewColorManager Instance;

    // ── ColorKeeper 등록 관리 ────────────────────────────────────
    private static readonly List<ColorKeeper> _colorKeepers = new();

    /// <summary>외부에서 ColorKeeper 목록 조회에 사용</summary>
    public static IReadOnlyList<ColorKeeper> AllColorKeepers => _colorKeepers;

    public static void RegisterColorKeeper(ColorKeeper keeper)
    {
        if (!_colorKeepers.Contains(keeper))
            _colorKeepers.Add(keeper);
    }

    public static void UnregisterColorKeeper(ColorKeeper keeper)
    {
        _colorKeepers.Remove(keeper);
    }

    // ── 생명주기 ────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        colorGauge = 0f;
        UpdateVisualEffect();
    }

    void Start()
    {
        colorGauge = 0f;
        UpdateVisualEffect();
    }

    void Update()
    {
        UpdateVisualEffect();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── 시각 효과 ────────────────────────────────────────────────

    private void UpdateVisualEffect()
    {
        if (GrayscaleRendererFeature.Instance != null)
            GrayscaleRendererFeature.Instance.SetSaturation(0f);

        for (int i = 0; i < _colorKeepers.Count; i++)
        {
            if (_colorKeepers[i] != null)
                _colorKeepers[i].UpdateColor(colorGauge);
        }
    }

    // ── 외부 제어 API ────────────────────────────────────────────

    /// <summary>MainUIManager에서 호출 (현재는 게이지가 노트 기반이므로 예약)</summary>
    public void SetColorUpdateActive(bool _) { }

    /// <summary>ComboManager에서 호출 (예약)</summary>
    public void SetComboGraceState(bool _) { }

    /// <summary>MelodySection에서 호출: 노트 수집 시 게이지 증가</summary>
    public void AddGauge(float amount)
    {
        colorGauge = Mathf.Clamp(colorGauge + amount, 0f, 100f);
        Debug.Log($"[ColorManager] +{amount:F1}%p → 현재 게이지: {colorGauge:F1}%");
        UpdateVisualEffect();
    }

    /// <summary>피격 시 호출: 게이지 감소 + 콤보 리셋</summary>
    public void DecreaseGaugeOnHit()
    {
        colorGauge = Mathf.Clamp(colorGauge - hitPenaltyPercentage, 0f, 100f);
        Debug.Log($"[ColorManager] Hit! -{hitPenaltyPercentage}%p → 현재 게이지: {colorGauge:F1}%");
        UpdateVisualEffect();

        if (ComboManager.Instance != null) ComboManager.Instance.ResetCombo();
    }

    /// <summary>게임오버 시 호출: 게이지 강제 100%</summary>
    public void SetGameOverGauge()
    {
        colorGauge = 100f;
        UpdateVisualEffect();
    }

    // ── Deco 컬러화 통계 ─────────────────────────────────────────

    /// <summary>
    /// 컬러화된 Deco 프랍 비율 (0~100).
    /// 관용 처리: 90% 이상 → 100 반환.
    /// IsColorized 기준 (탄환 히트 or 게이지 완전 복원 모두 포함).
    /// </summary>
    public float GetColorizationPercent()
    {
        int total = 0, colorized = 0;

        foreach (var keeper in _colorKeepers)
        {
            if (keeper == null) continue;
            total++;
            if (keeper.IsColorized) colorized++;
        }

        if (total == 0) return 0f;

        float raw = (float)colorized / total * 100f;
        return raw >= 90f ? 100f : Mathf.Floor(raw);
    }

    /// <summary>등록된 Deco 프랍 총 개수</summary>
    public int GetTotalDecoCount() => _colorKeepers.Count;

    /// <summary>현재 컬러화된 Deco 프랍 개수</summary>
    public int GetColorizedDecoCount()
    {
        int count = 0;
        foreach (var keeper in _colorKeepers)
            if (keeper != null && keeper.IsColorized) count++;
        return count;
    }
}
