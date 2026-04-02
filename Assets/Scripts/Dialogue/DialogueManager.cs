using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    // ───────────────────────────────────────────
    // Inspector 참조
    // ───────────────────────────────────────────

    [SerializeField] private ToonyVoices _toonyVoices;

    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Image dialoguePanelBackground;

    [Header("Character Panels")]
    public GameObject leftCharacterPanel;
    public GameObject rightCharacterPanel;

    [Header("Character Objects (리깅 캐릭터)")]
    [SerializeField] private CharacterEntry[] characterEntries;

    [Header("Dialogue Background")]
    public Image dialogueBackgroundImage;
    public Image startBackgroundImage;

    // ───────────────────────────────────────────
    // 내부 상태
    // ───────────────────────────────────────────

    private string _currentFullText;
    private int _lastDisplayedIndex = -1;
    private bool _isSpeaking = false;
    private bool _isSentenceFinished = false;
    private bool _ignoreNextSentenceFinished = false;
    private bool _isProcessingNext = false;

    private Dictionary<string, CharacterEntry> _characterDict;
    private Action _onDialogueSequenceComplete;

    // ───────────────────────────────────────────
    // 초기화
    // ───────────────────────────────────────────

    private void Awake()
    {
        // 캐릭터 딕셔너리 초기화
        _characterDict = new Dictionary<string, CharacterEntry>();
        foreach (var entry in characterEntries)
        {
            if (!string.IsNullOrEmpty(entry.speakerKey))
                _characterDict[entry.speakerKey] = entry;

            // 시작 시 모든 캐릭터 숨기기
            if (entry.characterObject != null)
                entry.characterObject.SetActive(false);
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private void Start()
    {
        if (_toonyVoices == null)
            _toonyVoices = FindFirstObjectByType<ToonyVoices>();

        _toonyVoices.CharacterSounded.AddListener(OnCharacterSounded);
        _toonyVoices.SentenceFinished.AddListener(OnSentenceFinished);
    }

    // ───────────────────────────────────────────
    // 외부 호출 (SequenceRunner → DialogueManager)
    // ───────────────────────────────────────────

    public void ShowDialogue(SequenceEvent evt, Action onComplete)
    {
        _onDialogueSequenceComplete = onComplete;
        dialoguePanel.SetActive(true);
        ApplyDialogue(evt);
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
        leftCharacterPanel.SetActive(false);
        rightCharacterPanel.SetActive(false);

        // 모든 캐릭터 숨기기
        foreach (var entry in characterEntries)
            if (entry.characterObject != null)
                entry.characterObject.SetActive(false);
    }

    public void SetDialogueBackground(Sprite sprite)
    {
        if (dialogueBackgroundImage == null || sprite == null) return;

        dialogueBackgroundImage.sprite = sprite;
        Color c = dialogueBackgroundImage.color;
        c.a = 1f;
        dialogueBackgroundImage.color = c;

        if (startBackgroundImage != null)
            startBackgroundImage.gameObject.SetActive(false);
    }

    // ───────────────────────────────────────────
    // 다음 버튼 클릭
    // ───────────────────────────────────────────

    public void OnNextButtonClick()
    {
        if (_isProcessingNext) return;

        if (_isSpeaking)
        {
            _ignoreNextSentenceFinished = true;
            _toonyVoices?.Stop();
            StopAllCoroutines();

            dialogueText.text = _currentFullText;
            _lastDisplayedIndex = _currentFullText.Length - 1;
            _isSpeaking = false;
            _isSentenceFinished = true;
            _ignoreNextSentenceFinished = false;
            return;
        }

        if (_isSentenceFinished)
        {
            _isProcessingNext = true;
            _isSentenceFinished = false;

            Action callback = _onDialogueSequenceComplete;
            _onDialogueSequenceComplete = null;
            callback?.Invoke();

            _isProcessingNext = false;
        }
    }

    // ───────────────────────────────────────────
    // 대화 표시 내부 로직
    // ───────────────────────────────────────────

    private void ApplyDialogue(SequenceEvent evt)
    {
        StopAllCoroutines();
        _isProcessingNext = false;
        _ignoreNextSentenceFinished = true;
        _toonyVoices?.Stop();

        _currentFullText = evt.text;
        _lastDisplayedIndex = -1;
        _isSpeaking = true;
        _isSentenceFinished = false;

        nameText.text = evt.speakerName;
        dialogueText.text = "";
        nameText.gameObject.SetActive(true);
        dialogueText.gameObject.SetActive(true);

        UpdateCharacterPanels(evt);

        if (string.IsNullOrEmpty(_currentFullText))
        {
            _ignoreNextSentenceFinished = false;
            _isSpeaking = false;
            _isSentenceFinished = true;
            return;
        }

        StartCoroutine(SpeakNextFrame());
    }

    private IEnumerator SpeakNextFrame()
    {
        yield return null;

        _ignoreNextSentenceFinished = false;
        _toonyVoices?.Speak(_currentFullText, 3.5f, 0.5f, 0.3f);
        StartCoroutine(SentenceFinishedTimeout());
    }

    private IEnumerator SentenceFinishedTimeout()
    {
        float timeout = Mathf.Clamp(_currentFullText.Length * 0.1f, 1f, 10f);
        yield return new WaitForSeconds(timeout);

        if (_isSpeaking)
        {
            Debug.LogWarning("[DialogueManager] SentenceFinished 타임아웃 → 강제 완료");
            OnSentenceFinished();
        }
    }

    // ───────────────────────────────────────────
    // 캐릭터 패널 업데이트
    // ───────────────────────────────────────────

    private void UpdateCharacterPanels(SequenceEvent evt)
    {
        // 모든 캐릭터 숨기기
        foreach (var entry in characterEntries)
            if (entry.characterObject != null)
                entry.characterObject.SetActive(false);

        leftCharacterPanel.SetActive(false);
        rightCharacterPanel.SetActive(false);

        // hidePosition만 처리하고 종료
        if (!string.IsNullOrEmpty(evt.hidePosition))
            return;

        // 발화자 패널/오브젝트 활성화
        if (string.IsNullOrEmpty(evt.speaker) ||
            !_characterDict.TryGetValue(evt.speaker, out CharacterEntry speakerEntry))
            return;

        GameObject speakerPanel = evt.position == "Left" ? leftCharacterPanel : rightCharacterPanel;
        GameObject companionPanel = evt.position == "Left" ? rightCharacterPanel : leftCharacterPanel;

        speakerEntry.characterObject.SetActive(true);
        speakerPanel.SetActive(true);
        SetCharacterColor(speakerEntry.characterObject, Color.white);
        speakerEntry.characterObject.transform.localScale = Vector3.one;
        ApplyExpression(speakerEntry.animator, evt.expression);

        // 청자: 이전 대화에서 남아있는 캐릭터 어둡게
        foreach (var entry in characterEntries)
        {
            if (entry.speakerKey == evt.speaker) continue;
            if (entry.characterObject == null || !entry.characterObject.activeSelf) continue;

            companionPanel.SetActive(true);
            SetCharacterColor(entry.characterObject, new Color(0.5f, 0.5f, 0.5f, 1f));
            entry.characterObject.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            ApplyExpression(entry.animator, "Normal");
        }
    }

    private void ApplyExpression(Animator animator, string expression)
    {
        if (animator == null) return;

        int expressionId = expression switch
        {
            "Happy" => 1,
            "Sad" => 2,
            "Surprised" => 3,
            "Angry" => 4,
            _ => 0  // Normal
        };

        animator.SetInteger("expression", expressionId);
    }

    private void SetCharacterColor(GameObject charObj, Color color)
    {
        foreach (var r in charObj.GetComponentsInChildren<SpriteRenderer>())
            r.color = color;
    }

    // ───────────────────────────────────────────
    // ToonyVoices 이벤트
    // ───────────────────────────────────────────

    public void OnCharacterSounded(int originalIndex)
    {
        if (originalIndex <= _lastDisplayedIndex) return;
        if (originalIndex + 1 > _currentFullText.Length) return;

        dialogueText.text = _currentFullText.Substring(0, originalIndex + 1);
        _lastDisplayedIndex = originalIndex;
    }

    public void OnSentenceFinished()
    {
        if (_ignoreNextSentenceFinished) return;

        _isSpeaking = false;
        _isSentenceFinished = true;
        dialogueText.text = _currentFullText;
    }

    // ───────────────────────────────────────────
    // 피치 설정
    // ───────────────────────────────────────────

    private float GetPitchForSpeaker(string speakerName)
    {
        return speakerName switch
        {
            "Player" => 2.4f,
            "Manager" => 2.0f,
            "GirlFan" => 3.5f,
            "RivalWolf" => 1.8f,
            "GrayCitizen" => 1.4f,
            _ => 2.4f
        };
    }
}

[System.Serializable]
public class CharacterEntry
{
    public string speakerKey;          // JSON의 speaker 값 ("Player", "Manager" 등)
    public GameObject characterObject; // 리깅된 캐릭터 오브젝트
    public Animator animator;          // 캐릭터의 Animator 컴포넌트
}