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
//   5. Play → 게이지가 오르면 목록 순서대로 하나씩 컬러 복원
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

    // ── 내부 상태 ──────────────────────────────────────────────────
    private readonly List<ColorKeeper> _keepers = new();
    private readonly List<float> _thresholds = new();
    private int _currentIndex = 0;

    // ── 초기화 ──────────────────────────────────────────────────
    void Awake()
    {
        ApplySequence();
    }

    public void ApplySequence()
    {
        _keepers.Clear();
        _thresholds.Clear();
        _currentIndex = 0;

        if (colorKeeperMaterial == null)
        {
            Debug.LogWarning("[ColorRestoreSequencer] colorKeeperMaterial이 비어 있습니다.", this);
            return;
        }

        if (objects == null || objects.Count == 0) return;

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
            _thresholds.Add(threshold);
            _keepers.Add(keeper);

            Debug.Log($"[ColorRestoreSequencer] [{i}] '{obj.name}'" +
                      $" → threshold={threshold:F1}");
        }
    }

    // ── 매 프레임: 현재 오브젝트 완료 → 다음으로 진행 ────────────
    void Update()
    {
        AdvanceSequence();
    }

    private void AdvanceSequence()
    {
        while (_currentIndex < _keepers.Count)
        {
            if (_keepers[_currentIndex] == null || _keepers[_currentIndex].IsColorized)
            {
                _currentIndex++;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// NewColorManager에서 호출하여 현재 순서의 keeper에게만 게이지를 전달합니다.
    /// 순서가 안 된 keeper는 갱신하지 않습니다.
    /// </summary>
    public void UpdateSequence(float colorGauge)
    {
        for (int i = 0; i < _keepers.Count; i++)
        {
            if (_keepers[i] == null) continue;

            if (i < _currentIndex)
            {
                // 이미 완료된 것은 풀컬러 유지
                continue;
            }
            else if (i == _currentIndex)
            {
                // 현재 복원 대상: 게이지가 threshold 이상이면 컬러 전달
                float threshold = _thresholds[i];
                if (colorGauge >= threshold)
                    _keepers[i].UpdateColor(colorGauge);
            }
            // 나머지는 아직 잠금 상태 → 아무것도 안 함
        }
    }

    /// <summary>현재 복원 진행 중인 인덱스 (외부 조회용)</summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>전체 시퀀스 완료 여부</summary>
    public bool IsComplete => _currentIndex >= _keepers.Count;
}
