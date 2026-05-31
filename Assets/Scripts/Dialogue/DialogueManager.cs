using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Inspector ÂüÁ¶
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    [SerializeField] private ToonyVoices _toonyVoices;

    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Image dialoguePanelBackground;

    [Header("Character Panels")]
    public GameObject leftCharacterPanel;
    public GameObject rightCharacterPanel;

    [Header("Character Objects (¸®±ë Ä³¸¯ÅÍ)")]
    [SerializeField] private CharacterEntry[] characterEntries;

    [Header("Dialogue Background")]
    public Image dialogueBackgroundImage;
    public Image startBackgroundImage;

    [Header("Position Anchors")]
    public Transform leftAnchor;  // ¾À¿¡ ¼³Ä¡ÇÑ ¿ÞÂÊ ºó ¿ÀºêÁ§Æ®
    public Transform rightAnchor; // ¾À¿¡ ¼³Ä¡ÇÑ ¿À¸¥ÂÊ ºó ¿ÀºêÁ§Æ®

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ³»ºÎ »óÅÂ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    private string _currentFullText;
    private int _lastDisplayedIndex = -1;
    private bool _isSpeaking = false;
    private bool _isSentenceFinished = false;
    private bool _ignoreNextSentenceFinished = false;
    private bool _isProcessingNext = false;

    private Dictionary<string, CharacterEntry> _characterDict;
    private Action _onDialogueSequenceComplete;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÃÊ±âÈ­
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    private void Awake()
    {
        // Ä³¸¯ÅÍ µñ¼Å³Ê¸® ÃÊ±âÈ­
        _characterDict = new Dictionary<string, CharacterEntry>();
        foreach (var entry in characterEntries)
        {
            if (!string.IsNullOrEmpty(entry.speakerKey))
                _characterDict[entry.speakerKey] = entry;

            // ½ÃÀÛ ½Ã ¸ðµç Ä³¸¯ÅÍ ¼û±â±â
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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¿ÜºÎ È£Ãâ (SequenceRunner ¡æ DialogueManager)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

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

        // ¸ðµç Ä³¸¯ÅÍ ¼û±â±â
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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ´ÙÀ½ ¹öÆ° Å¬¸¯
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ´ëÈ­ Ç¥½Ã ³»ºÎ ·ÎÁ÷
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

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
            Debug.LogWarning("[DialogueManager] SentenceFinished Å¸ÀÓ¾Æ¿ô ¡æ °­Á¦ ¿Ï·á");
            OnSentenceFinished();
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ä³¸¯ÅÍ ÆÐ³Î ¾÷µ¥ÀÌÆ®
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    private void UpdateCharacterPanels(SequenceEvent evt)
    {
        // 1. ¸ðµç Ä³¸¯ÅÍ¸¦ È­¸é ¹Û(¶Ç´Â ºñÈ°¼ºÈ­)À¸·Î Á¤¸®
        foreach (var entry in characterEntries)
        {
            if (entry.characterObject != null)
                entry.characterObject.SetActive(false);
        }

        // 2. ÆÐ³Î ÃÊ±âÈ­
        leftCharacterPanel.SetActive(false);
        rightCharacterPanel.SetActive(false);

        if (!string.IsNullOrEmpty(evt.hidePosition)) return;

        // 3. ¹ßÈ­ÀÚ Ã³¸®
        if (_characterDict.TryGetValue(evt.speaker, out CharacterEntry speakerEntry))
        {
            speakerEntry.characterObject.SetActive(true);

            // À§Ä¡ °áÁ¤
            Transform targetAnchor = (evt.position == "Left") ? leftAnchor : rightAnchor;
            GameObject targetPanel = (evt.position == "Left") ? leftCharacterPanel : rightCharacterPanel;

            // È¸Àü°ª ¸ÕÀú Àû¿ë
            speakerEntry.characterObject.transform.rotation = targetAnchor.rotation;

            // [ÇÙ½É] Ä³¸¯ÅÍÀÇ ÀüÃ¼ ·»´õ·¯¸¦ ±â¹ÝÀ¸·Î °¡Àå ³ôÀº Y°ª(¸Ó¸® ³¡) Ã£±â
            var renderers = speakerEntry.characterObject.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length > 0)
            {
                float highestY = float.MinValue;

                // ¸ðµç ½ºÇÁ¶óÀÌÆ®ÀÇ »ó´Ü °æ°è¼±(bounds.max.y) Áß °¡Àå ³ôÀº °÷À» ±¸ÇÔ
                foreach (var r in renderers)
                {
                    if (r.bounds.max.y > highestY)
                    {
                        highestY = r.bounds.max.y;
                    }
                }

                // Ä³¸¯ÅÍ ÇöÀç À§Ä¡¿¡¼­ ¸Ó¸® ³¡±îÁöÀÇ YÃà °Å¸®(¿ÀÇÁ¼Â) °è»ê
                float yOffset = highestY - speakerEntry.characterObject.transform.position.y;

                // ¾ÞÄ¿ À§Ä¡¿¡¼­ Y ¿ÀÇÁ¼Â¸¸Å­ ¾Æ·¡·Î ³»¸° ÀÚ¸®¿¡ Ä³¸¯ÅÍ¸¦ ¹èÄ¡ (¸Ó¸® ³¡À» ¾ÞÄ¿¿¡ °íÁ¤)
                Vector3 targetPosition = targetAnchor.position;
                targetPosition.y -= yOffset;

                // ZÃàÀº ¾Æ±î ÇØ°áÇÑ ¾ÞÄ¿ÀÇ Z°ªÀ» ±×´ë·Î À¯Áö
                targetPosition.z = targetAnchor.position.z;

                speakerEntry.characterObject.transform.position = targetPosition;
            }
            else
            {
                // ½ºÇÁ¶óÀÌÆ® ·»´õ·¯°¡ ¾øÀ¸¸é ¿¹¿Ü Ã³¸®·Î ±âº» ¾ÞÄ¿ À§Ä¡ ¹èÄ¡
                speakerEntry.characterObject.transform.position = targetAnchor.position;
            }

            targetPanel.SetActive(true);
            ApplyExpression(speakerEntry.animator, evt.expression);
            SetCharacterColor(speakerEntry.characterObject, Color.white);
        }

        // 4. Ã»ÀÚ Ã³¸® (¿É¼Ç: ÀÌÀü ´ëÈ­ »ó´ë¸¦ ¹Ý´ëÆí¿¡ À¯ÁöÇÏ°í ½ÍÀ» ¶§)
        // ÀÌ ºÎºÐÀº ±âÈ¹¿¡ µû¶ó 'ÀÌÀü È­ÀÚ'¸¦ ±â¾ïÇØµ×´Ù°¡ ¹Ý´ëÆí ¾ÞÄ¿¿¡ ¼¼¿öµÎ¸é µË´Ï´Ù.
    }

    private void ApplyExpression(Animator animator, string expression)
    {
        if (animator == null) return;

        int expressionId = expression switch
        {
            "Normal" => 0,
            "ClosedEye" => 1,
            "Surprised" => 2,
            "Sad" => 3,
            "Excited" => 4,
            "Cry" => 5,
            "DeadEye" => 6,
            "Hit" => 7,
            "Hehe" => 8,
            "Hmm" => 9,
            _ => 0  // Normal
        };

        animator.SetInteger("expression", expressionId);
    }

    private void SetCharacterColor(GameObject charObj, Color color)
    {
        foreach (var r in charObj.GetComponentsInChildren<SpriteRenderer>())
            r.color = color;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ToonyVoices ÀÌº¥Æ®
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÇÇÄ¡ ¼³Á¤
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

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
    public string speakerKey;          // JSONÀÇ speaker °ª ("Player", "Manager" µî)
    public GameObject characterObject; // ¸®±ëµÈ Ä³¸¯ÅÍ ¿ÀºêÁ§Æ®
    public Animator animator;          // Ä³¸¯ÅÍÀÇ Animator ÄÄÆ÷³ÍÆ®
}