using System;
using System.Collections;
using UnityEngine;

public class SequenceRunner : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private MainUIManager mainUIManager;

    private StageSequence _currentSequence;
    private int _currentIndex;
    private bool _isRunning;

    // ───────────────────────────────────────────
    // 초기화
    // ───────────────────────────────────────────

    private void Awake()
    {
        mainUIManager = mainUIManager ?? FindFirstObjectByType<MainUIManager>();
        dialogueManager = dialogueManager ?? FindFirstObjectByType<DialogueManager>();
        cutsceneManager = cutsceneManager ?? FindFirstObjectByType<CutsceneManager>();
    }

    // ───────────────────────────────────────────
    // 외부 호출
    // ───────────────────────────────────────────

    /// <summary>
    /// Resources/ 하위 경로의 JSON을 로드하고
    /// scriptType("Intro" or "Outro")에 맞는 시퀀스 실행
    /// </summary>
    public void RunScript(string scriptPath, string scriptType)
    {
        TextAsset json = Resources.Load<TextAsset>(scriptPath);
        if (json == null)
        {
            Debug.LogError($"[SequenceRunner] 스크립트 없음: {scriptPath}");
            return;
        }

        StageScript script = JsonUtility.FromJson<StageScript>(json.text);

        StageSequence sequence = script.sequences.Find(s => s.scriptType == scriptType);
        if (sequence == null)
        {
            Debug.LogWarning($"[SequenceRunner] '{scriptType}' 시퀀스 없음: {scriptPath}");
            HandleAction(scriptType == "Outro" ? "SHOW_CLEAR_UI" : "START_GAME");
            return;
        }

        _currentSequence = sequence;
        _currentIndex = 0;
        _isRunning = true;
        ProcessNext();
    }

    /// <summary>현재 시퀀스 스킵 (스킵 버튼 연결용)</summary>
    public void Skip()
    {
        if (!_isRunning) return;
        StopAllCoroutines();

        dialogueManager.HideDialogue();
        cutsceneManager.OnCutsceneClicked();

        HandleAction(FindLastAction());
        _isRunning = false;
    }

    // ───────────────────────────────────────────
    // 시퀀스 진행
    // ───────────────────────────────────────────

    private void ProcessNext()
    {
        if (_currentIndex >= _currentSequence.events.Count)
        {
            OnSequenceEnd();
            return;
        }

        SequenceEvent evt = _currentSequence.events[_currentIndex++];

        switch (evt.type)
        {
            case "DIALOGUE":
                // 이벤트 객체를 직접 전달 (id 조회 없음)
                dialogueManager.ShowDialogue(evt, () =>
                {
                    if (!string.IsNullOrEmpty(evt.actionAfter))
                        HandleAction(evt.actionAfter);
                    else
                        ProcessNext();
                });
                break;

            case "VIDEO":
                cutsceneManager.PlayVideo(evt.videoFileName, evt.skippable, ProcessNext);
                break;

            default:
                Debug.LogWarning($"[SequenceRunner] 알 수 없는 타입: {evt.type}");
                ProcessNext();
                break;
        }
    }

    private void OnSequenceEnd()
    {
        _isRunning = false;
        dialogueManager.HideDialogue();
        HandleAction(FindLastAction());
    }

    // ───────────────────────────────────────────
    // 유틸
    // ───────────────────────────────────────────

    private void HandleAction(string action)
    {
        if (string.IsNullOrEmpty(action) || action == "None") return;
        mainUIManager?.HandleDialogueAction(action);
    }

    /// <summary>시퀀스 내 마지막 actionAfter 값을 찾아 반환</summary>
    private string FindLastAction()
    {
        for (int i = _currentSequence.events.Count - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(_currentSequence.events[i].actionAfter))
                return _currentSequence.events[i].actionAfter;
        }
        return "START_GAME";
    }
}