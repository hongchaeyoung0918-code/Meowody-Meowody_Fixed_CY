using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Character Sprites")]
    public GameObject leftCharacterPanel;
    public RawImage leftCharacterRawImage; // Image -> RawImage º¯°æ
    public GameObject rightCharacterPanel;
    public RawImage rightCharacterRawImage; // Image -> RawImage º¯°æ
    public Animator leftCharacterAnimator;
    public Animator rightCharacterAnimator;


    [Header("Dialogue Background")]
    public Image dialogueBackgroundImage;
    public Image startBackgroundImage;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ³»ºÎ »óÅÂ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    private Dictionary<string, DialogueData> _dialogueDict;
    private DialogueData _currentDialogue;

    private string _currentFullText;
    private int _lastDisplayedIndex = -1;
    private bool _isSpeaking = false;
    private bool _isSentenceFinished = false;
    //???????????
    private bool _ignoreNextSentenceFinished = false;
    private bool _isProcessingNext = false;

    public Dictionary<string, Sprite> characterSprites = new Dictionary<string, Sprite>();

    // ´ëÈ­ ÇÑ ¹®Àå ¿Ï·á ½Ã SequenceRunner¿¡ ¾Ë¸®´Â ÄÝ¹é
    private Action _onDialogueSequenceComplete;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÃÊ±âÈ­
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    private void Awake()
    {
        //LoadDialogueData();
        //LoadCharacterSprites();
        Sprite[] allSprites = Resources.LoadAll<Sprite>("");
        foreach (var sprite in allSprites)
            characterSprites[sprite.name] = sprite;

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
    // µ¥ÀÌÅÍ ·Îµå
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    /// <summary>
    /// ½ºÅ×ÀÌÁöº° ÆÄÀÏ ·Îµå.
    /// Resources/Dialogues/Stage{stage}_Dialogue.json
    /// ¾øÀ¸¸é °øÅë Dialogue.json Æú¹é
    /// </summary>
    public void LoadDialogueData(int stage = 0)
    {
        string path = stage > 0
            ? $"Dialogues/Stage{stage}_Dialogue"
            : "Dialogue";

        TextAsset jsonFile = Resources.Load<TextAsset>(path);

        // Æú¹é: ±âÁ¸ ÅëÇÕ ÆÄÀÏ
        if (jsonFile == null && stage > 0)
            jsonFile = Resources.Load<TextAsset>("Dialogue");

        if (jsonFile == null)
        {
            Debug.LogError($"[DialogueManager] JSON ÆÄÀÏ ¾øÀ½: {path}");
            return;
        }

        var container = JsonUtility.FromJson<DialogueDataContainer>(jsonFile.text);
        _dialogueDict = container.dialogues.ToDictionary(d => d.id, d => d);
    }

    private void LoadCharacterSprites()
    {
        if (_dialogueDict == null) return;

        var speakers = _dialogueDict.Values
            .Select(d => d.speaker)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct();

        foreach (var speaker in speakers)
        {
            Sprite sprite = Resources.Load<Sprite>(speaker);
            if (sprite != null)
                characterSprites[speaker] = sprite;
            else
                Debug.LogWarning($"[DialogueManager] ½ºÇÁ¶óÀÌÆ® ¾øÀ½: {speaker}");
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¿ÜºÎ È£Ãâ (SequenceRunner ¡æ DialogueManager)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    /// <summary>Æ¯Á¤ IDÀÇ ´ëÈ­ ÇÑ ÁÙÀ» Ç¥½Ã. ¿Ï·á ½Ã onComplete È£Ãâ</summary>
    public void ShowDialogue(SequenceEvent evt, Action onComplete)
    {
        _onDialogueSequenceComplete = onComplete;
        dialoguePanel.SetActive(true);
        ApplyDialogue(evt);
    }

    /// <summary>´ëÈ­Ã¢ ´Ý±â</summary>
    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
        leftCharacterPanel.SetActive(false);
        rightCharacterPanel.SetActive(false);
    }

    /// <summary>ÄÆ¾À Á÷ÈÄ ¹è°æ ÀÌ¹ÌÁö¸¦ ±³Ã¼ÇÒ ¶§ »ç¿ë</summary>
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
        // ÀÌ¹Ì Ã³¸® ÁßÀÌ¸é ¹«½Ã
        if (_isProcessingNext) return;

        Debug.Log($"[Next] isSpeaking={_isSpeaking}, isSentenceFinished={_isSentenceFinished}");

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

        // ÀÌÀü SpeakÀÇ SentenceFinished°¡ µÚ´Ê°Ô ¿Ã ¼ö ÀÖÀ¸¹Ç·Î ¹«½Ã ÇÃ·¡±× ¼³Á¤
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

        // ÇÑ ÇÁ·¹ÀÓ µÚ¿¡ Speak È£Ãâ ¡æ ÀÌÀü SentenceFinished°¡ ¸ÕÀú ¼ÒÈ­µÈ ÈÄ »õ ¹®Àå ½ÃÀÛ
        StartCoroutine(SpeakNextFrame(evt));
    }

    private IEnumerator SpeakNextFrame(SequenceEvent evt)
    {
        yield return null; // ÇÑ ÇÁ·¹ÀÓ ´ë±â

        _ignoreNextSentenceFinished = false;
        _toonyVoices?.Speak(_currentFullText, 3.5f, 0.5f, 0.3f);

        StartCoroutine(SentenceFinishedTimeout());
    }

    public void OnSentenceFinished()
    {
        // ¹«½Ã ÇÃ·¡±×°¡ ÄÑÁ® ÀÖÀ¸¸é ÀÌÀü ¹®ÀåÀÇ ÀÜ¿© ÀÌº¥Æ®ÀÌ¹Ç·Î ¹«½Ã
        if (_ignoreNextSentenceFinished)
        {
            Debug.Log("[DialogueManager] SentenceFinished ¹«½Ã (ÀÌÀü ¹®Àå ÀÜ¿©)");
            return;
        }

        _isSpeaking = false;
        _isSentenceFinished = true;
        dialogueText.text = _currentFullText;
    }

    private IEnumerator SentenceFinishedTimeout()
    {
        // ÅØ½ºÆ® ±æÀÌ¿¡ ºñ·ÊÇÑ ´ë±â ½Ã°£ (ÃÖ¼Ò 1ÃÊ, ÃÖ´ë 10ÃÊ)
        float timeout = Mathf.Clamp(_currentFullText.Length * 0.1f, 1f, 10f);
        yield return new WaitForSeconds(timeout);

        if (_isSpeaking)
        {
            Debug.LogWarning("[DialogueManager] SentenceFinished Å¸ÀÓ¾Æ¿ô ¡æ °­Á¦ ¿Ï·á");
            OnSentenceFinished();
        }
    }

    private void UpdateCharacterPanels(SequenceEvent evt)  // DialogueData ¡æ SequenceEvent
    {
        if (!string.IsNullOrEmpty(evt.hidePosition))
        {
            if (evt.hidePosition == "Left")
            {
                leftCharacterPanel.SetActive(false);
                leftCharacterRawImage.sprite = null;
            }
            else if (evt.hidePosition == "Right")
            {
                rightCharacterPanel.SetActive(false);
                rightCharacterRawImage.sprite = null;
            }
        }

        leftCharacterPanel.SetActive(false);
        rightCharacterPanel.SetActive(false);

        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        Vector3 activeScale = Vector3.one;
        Vector3 inactiveScale = new Vector3(0.9f, 0.9f, 1f);

        Image speakerImage = null, companionImage = null;
        GameObject speakerPanel = null, companionPanel = null;

        if (evt.position == "Left")
        {
            speakerPanel = leftCharacterPanel; speakerImage = leftCharacterRawImage;
            companionPanel = rightCharacterPanel; companionImage = rightCharacterRawImage;
        }
        else if (evt.position == "Right")
        {
            speakerPanel = rightCharacterPanel; speakerImage = rightCharacterRawImage;
            companionPanel = leftCharacterPanel; companionImage = leftCharacterRawImage;
        }

        if (speakerImage != null && characterSprites.TryGetValue(evt.speaker, out Sprite sprite))
        {
            speakerImage.sprite = sprite;
            speakerPanel.SetActive(true);
            speakerImage.color = activeColor;
            speakerImage.transform.localScale = activeScale;

            // TODO: Ä³¸¯ÅÍ Ç¥Á¤ ±¸Çö
            // ApplyExpression(speakerImage, evt.expression);
        }

        if (companionImage != null && companionImage.sprite != null)
        {
            companionPanel.SetActive(true);
            companionImage.color = inactiveColor;
            companionImage.transform.localScale = inactiveScale;
        }
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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÇÇÄ¡ ¼³Á¤ (À¯Áö)
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