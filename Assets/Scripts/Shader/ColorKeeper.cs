using System;
using UnityEngine;

// ──────────────────────────────────────────────────────────────────
// ColorKeeper  (GRIS 스타일 개별 컬러 복원)
//
// 동작:
//   - colorGauge가 restoreThreshold에 도달하면 컬러 복원 시작
//   - transitionRange 구간에 걸쳐 흑백 → 풀컬러로 부드럽게 전환
//   - SpriteColorKeeper 셰이더의 _Saturation을 MaterialPropertyBlock으로 갱신
//     (머티리얼 인스턴스를 생성하지 않아 퍼포먼스 안전)
//
// 사용 방법:
//   1. SpriteColorKeeper 셰이더로 머티리얼 생성 (ColorKeeperMat)
//   2. 컬러 복원할 오브젝트에 이 컴포넌트 추가
//   3. colorKeeperMaterial에 ColorKeeperMat 할당
//   4. restoreThreshold 로 복원 시작 게이지 설정 (0~100)
// ──────────────────────────────────────────────────────────────────
[RequireComponent(typeof(SpriteRenderer))]
public class ColorKeeper : MonoBehaviour
{
    [Header("Material")]
    [Tooltip("SpriteColorKeeper 셰이더로 만든 머티리얼")]
    [SerializeField] private Material colorKeeperMaterial;

    [Header("GRIS Color Restore Settings")]
    [Tooltip("이 값에 도달하면 컬러 복원이 시작됩니다 (0~100)")]
    [Range(0f, 100f)]
    public float restoreThreshold = 30f;

    [Tooltip("복원 전환이 완료되기까지의 게이지 구간 (0이면 즉시 전환)")]
    [Range(0f, 50f)]
    public float transitionRange = 10f;

    // ── 내부 상태 ────────────────────────────────────────────────
    private SpriteRenderer        _spriteRenderer;
    private Material              _originalMaterial;
    private MaterialPropertyBlock _mpb;

    private static readonly int SaturationID = Shader.PropertyToID("_Saturation");

    /// <summary>
    /// 한번 true가 되면 게이지가 내려가도 컬러화 상태를 유지합니다.
    /// ForceFullColor() 호출 또는 게이지 기반으로 완전히 복원된 경우에 true가 됩니다.
    /// </summary>
    public bool IsColorized { get; private set; } = false;

    /// <summary>컬러화가 완료될 때 발생하는 이벤트</summary>
    public event Action OnColorized;

    // ── 생명주기 ────────────────────────────────────────────────
    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalMaterial = _spriteRenderer.material;
        _mpb = new MaterialPropertyBlock();

        if (colorKeeperMaterial == null)
        {
            Debug.LogWarning($"[ColorKeeper] '{name}': colorKeeperMaterial이 비어 있습니다.", this);
            return;
        }

        _spriteRenderer.material = colorKeeperMaterial;

        // 처음엔 흑백
        SetSaturation(0f);
    }

    void OnEnable()
    {
        NewColorManager.RegisterColorKeeper(this);
    }

    void OnDisable()
    {
        NewColorManager.UnregisterColorKeeper(this);
    }

    void OnDestroy()
    {
        if (_spriteRenderer != null && _originalMaterial != null)
            _spriteRenderer.material = _originalMaterial;
    }

    // ── 핵심: NewColorManager가 매 프레임 호출 ──────────────────

    /// <summary>
    /// colorGauge (0~100) 를 받아 개별 채도를 계산·적용합니다.
    /// </summary>
    public void UpdateColor(float colorGauge)
    {
        if (colorKeeperMaterial == null) return;

        // 이미 컬러화됐으면 유지
        if (IsColorized) { SetSaturation(1f); return; }

        float saturation;

        if (transitionRange <= 0f)
        {
            saturation = colorGauge >= restoreThreshold ? 1f : 0f;
        }
        else
        {
            saturation = Mathf.Clamp01(
                (colorGauge - restoreThreshold) / transitionRange
            );
        }

        SetSaturation(saturation);

        // 완전히 복원되면 영구 컬러화 상태로 전환
        if (saturation >= 1f)
        {
            IsColorized = true;
            OnColorized?.Invoke();
        }
    }

    // ── 내부 헬퍼 ───────────────────────────────────────────────

    private void SetSaturation(float value)
    {
        _spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(SaturationID, value);
        _spriteRenderer.SetPropertyBlock(_mpb);
    }

    // ── ColorRestoreSequencer에서 호출 ──────────────────────────

    /// <summary>
    /// ColorRestoreSequencer가 AddComponent 후 호출합니다.
    /// 인스펙터에서 머티리얼을 할당하지 않은 경우에도 동작합니다.
    /// </summary>
    public void Initialize(Material mat)
    {
        colorKeeperMaterial = mat;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.material = mat;
            SetSaturation(0f);
        }
    }

    // ── 외부 토글 (연출용) ───────────────────────────────────────

    /// <summary>즉시 풀컬러로 설정합니다 — 영구 컬러화.</summary>
    public void ForceFullColor()
    {
        IsColorized = true;
        SetSaturation(1f);
    }

    /// <summary>즉시 흑백으로 설정합니다 (연출용, IsColorized는 건드리지 않음).</summary>
    public void ForceGrayscale()  => SetSaturation(0f);
}
