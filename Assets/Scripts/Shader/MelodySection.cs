using System.Collections.Generic;
using UnityEngine;

// ──────────────────────────────────────────────────────────────────
// MelodySection
//
// 사용법:
//   1. 빈 GameObject에 이 컴포넌트를 추가
//   2. noteObjects[] 에 이 구간의 originalObjects를 순서대로 드래그
//   3. gaugePerNote  : 노트 1개 수집 시 게이지 증가량
//      completionBonus : 구간 전체 수집 완료 시 추가 보너스
//
// LT_PlayerController 에서 original object 수집 시
//   MelodySection.NotifyNoteCollected(collectedObj) 를 호출하면
//   해당 오브젝트가 속한 섹션이 자동으로 게이지를 올림.
// ──────────────────────────────────────────────────────────────────
public class MelodySection : MonoBehaviour
{
    [Header("이 구간에 포함된 노트 originalObjects (순서대로)")]
    public GameObject[] noteObjects;

    [Header("게이지 보상")]
    [Tooltip("노트 1개 수집 시 게이지 증가량 (%p)")]
    public float gaugePerNote = 5f;
    [Tooltip("구간 전체 수집 완료 시 추가 보너스 (%p)")]
    public float completionBonus = 15f;

    private readonly HashSet<int> _collectedIndices = new();
    private bool _sectionCompleted = false;

    // 씬에 존재하는 모든 MelodySection 정적 목록
    private static readonly List<MelodySection> _allSections = new();

    void OnEnable()  => _allSections.Add(this);
    void OnDisable() => _allSections.Remove(this);

    // ── 외부 호출 API ────────────────────────────────────────────

    /// <summary>
    /// LT_PlayerController에서 original object 수집 시 호출.
    /// 등록된 모든 MelodySection에 통보 → 해당 섹션이 처리.
    /// </summary>
    public static void NotifyNoteCollected(GameObject noteObj)
    {
        foreach (var section in _allSections)
            section.TryCollect(noteObj);
    }

    // ── 내부 로직 ────────────────────────────────────────────────

    private void TryCollect(GameObject noteObj)
    {
        if (_sectionCompleted || noteObjects == null) return;

        for (int i = 0; i < noteObjects.Length; i++)
        {
            if (noteObjects[i] != noteObj) continue;
            if (_collectedIndices.Contains(i)) return;

            _collectedIndices.Add(i);
            NewColorManager.Instance?.AddGauge(gaugePerNote);

            // 전체 수집 완료 체크
            if (_collectedIndices.Count == noteObjects.Length)
            {
                _sectionCompleted = true;
                NewColorManager.Instance?.AddGauge(completionBonus);
                Debug.Log($"[MelodySection] '{gameObject.name}' 완성! 보너스 +{completionBonus}%p");
            }
            return;
        }
    }

    /// <summary>씬 재시작 등 외부에서 상태 초기화 시 호출</summary>
    public void ResetSection()
    {
        _collectedIndices.Clear();
        _sectionCompleted = false;
    }
}
