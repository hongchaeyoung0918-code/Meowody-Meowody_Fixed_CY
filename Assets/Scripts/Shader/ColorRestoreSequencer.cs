using System.Collections.Generic;
using UnityEngine;

// ──────────────────────────────────────────────────────────────────
// ColorRestoreSequencer
//
// 지정한 오브젝트들을 **등록 순서대로만** 컬러 복원합니다.
// 앞 오브젝트가 완전히 복원(IsColorized)되어야 다음 오브젝트가 시작됩니다.
//
// 사용법:
//   1. 이 컴포넌트를 아무 GameObject에 추가 (예: NewColorManager)
//   2. colorKeeperMaterial 에 ColorKeeperMat 할당
//   3. objects 리스트에 복원할 오브젝트를 순서대로 드래그
//   4. startGauge / endGauge 로 전체 복원 구간 조정
//   5. transitionRange 로 한 오브젝트당 부드럽게 전환될 구간 조정
//   6. Play → 게이지가 오르면 목록 순서대로 하나씩 컬러 복원
// ──────────────────────────────────────────────────────────────────
public class ColorRestoreSequencer : MonoBehaviour
{
    [Header("머티리얼 설정")]
    [Tooltip("ColorKeeperMat (Custom/SpriteColorKeeper 셰이더 머티리얼)")]
    public Material colorKeeperMaterial;

    [Header("복원 대상 오브젝트 (순서대로)")]
    public List<GameObject> objects = new();

    [Header("게이지 구간 설정 (0 ~ 100)")]
    [Tooltip("첫 번째 오브젝트가 복원 시작되는 게이지")]
    [Range(0f, 100f)] public float startGauge = 0f;

    [Tooltip("마지막 오브젝트가 복원 완료되는 게이지")]
    [Range(0f, 100f)] public float endGauge = 100f;

    [Tooltip("한 오브젝트가 흑백→풀컬러로 전환되는 게이지 구간\n" +
             "0 = 즉시 전환, 5~15 = 부드럽게 전환")]
    [Range(0f, 30f)] public float transitionRange = 5f;

    // ── 내부 상태 ──────────────────────────────────────────────────
    private readonly List<ColorKeeper> _keepers = new();
    private readonly List<float> _originalThresholds = new();
    private int _currentIndex = 0;

    private const float LOCKED_THRESHOLD = 9999f;

    // ── 초기화 ──────────────────────────────────────────────────
    void Awake()
    {
        ApplySequence();
    }

    /// <summary>
    /// objects 목록 순서에 따라 각 오브젝트에 threshold 를 배분하고,
    /// 첫 번째만 잠금 해제합니다.
    /// </summary>
    public void ApplySequence()
    {
        _keepers.Clear();
        _originalThresholds.Clear();
        _currentIndex = 0;

        if (colorKeeperMaterial == null)
        {
            Debug.LogWarning("[ColorRestoreSequencer] colorKeeperMaterial이 비어 있습니다.", this);
            return;
        }

        if (objects == null || objects.Count == 0) return;

        // 유효한(SpriteRenderer가 있는) 오브젝트만 추림
        var valid = new List<GameObject>();
        foreach (var obj in objects)
        {
            if (obj != null && obj.GetComponent<SpriteRenderer>() != null)
                valid.Add(obj);
        }

        if (valid.Count == 0) return;

        float totalRange = Mathf.Max(0f, endGauge - startGauge);
        float spacing = valid.Count > 1 ? totalRange / (valid.Count - 1) : 0f;

        for (int i = 0; i < valid.Count; i++)
        {
            GameObject obj = valid[i];

            ColorKeeper keeper = obj.GetComponent<ColorKeeper>();
            if (keeper == null)
                keeper = obj.AddComponent<ColorKeeper>();

            keeper.Initialize(colorKeeperMaterial);

            float threshold = startGauge + i * spacing;
            _originalThresholds.Add(threshold);
            _keepers.Add(keeper);

            // 모든 오브젝트를 잠금 (threshold를 도달 불가능 값으로)
            keeper.restoreThreshold = LOCKED_THRESHOLD;
            keeper.transitionRange  = transitionRange;

            Debug.Log($"[ColorRestoreSequencer] [{i}] '{obj.name}'" +
                      $" → 예정 threshold={threshold:F1}");
        }

        // 첫 번째 오브젝트만 잠금 해제
        if (_keepers.Count > 0)
            _keepers[0].restoreThreshold = _originalThresholds[0];
    }

    // ── 매 프레임: 현재 오브젝트 완료 → 다음 잠금 해제 ────────────
    void Update()
    {
        AdvanceSequence();
    }

    private void AdvanceSequence()
    {
        // 이미 탄환(ForceFullColor)으로 순서 건너뛴 경우도 처리
        while (_currentIndex < _keepers.Count)
        {
            if (_keepers[_currentIndex] == null || _keepers[_currentIndex].IsColorized)
            {
                _currentIndex++;

                // 다음 오브젝트 잠금 해제
                if (_currentIndex < _keepers.Count)
                {
                    _keepers[_currentIndex].restoreThreshold = _originalThresholds[_currentIndex];
                    Debug.Log($"[ColorRestoreSequencer] [{_currentIndex}] '{_keepers[_currentIndex].name}' 잠금 해제" +
                              $" → threshold={_originalThresholds[_currentIndex]:F1}");
                }
            }
            else
            {
                break; // 현재 오브젝트가 아직 미완성 → 대기
            }
        }
    }

    /// <summary>현재 복원 진행 중인 인덱스 (외부 조회용)</summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>전체 시퀀스 완료 여부</summary>
    public bool IsComplete => _currentIndex >= _keepers.Count;
}
