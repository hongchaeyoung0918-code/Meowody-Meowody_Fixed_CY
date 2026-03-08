using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private ToonyVoices _toonyVoices;

    [SerializeField] private TMP_Text _dialogueTextUI;

    private string _currentFullText;
    private int _characterIndex;

    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Image dialoguePanelBackground;

    [Header("Cutscene UI References")]
    public GameObject cutscenePanel;
    public Image cutsceneImage;

    [Header("Fade Transition Refences")]
    public Image fadePanel;
    public Image startBackgroundImage;

    [Header("Dialogue Background Reference")]
    public Image dialogueBackgroundImage;

    [Header("Character Sprites")]
    public GameObject leftCharacterPanel;
    public Image leftCharacterImage;
    public GameObject rightCharacterPanel;
    public Image rightCharacterImage;

    public Dictionary<string, Sprite> characterSprites = new Dictionary<string, Sprite>();

    private Dictionary<string, Dialogue> dialogueDictionary;
    private Dialogue currentDialogue;
    private MainUIManager mainUIManager;
    private int activeStage;

    private int _lastDisplayedIndex = -1;

    private bool _isSpeaking = false;
    private bool _isSentenceFinished = false;

    //
    public VideoPlayer videoPlayer; // 컷씬 영상 재생용 VideoPlayer 컴포넌트 참조
    public RawImage videoDisplay;
    public GameObject videoPanel;


    private void Awake()
    {
        mainUIManager = FindFirstObjectByType<MainUIManager>();
        if (mainUIManager == null)
        {
            Debug.LogError("MainUIManager를 씬에서 찾을 수 없습니다.");
        }

        LoadDialogueData("Dialogue");
        LoadCharacterSprites();
        //dialoguePanel.SetActive(false);

        if (cutscenePanel != null) cutscenePanel.SetActive(false);

        if (dialoguePanelBackground != null)
        {
            Color color = dialoguePanelBackground.color;
            color.a = 1.0f; // A 값을 1.0 (불투명)으로 강제 설정
            dialoguePanelBackground.color = color;
        }
    }

    void Start()
    {
        activeStage = GameSettings.SelectedStage;

/*        if (mainUIManager == null)
        {
            mainUIManager = FindFirstObjectByType<MainUIManager>();
        }*/

        if (mainUIManager != null)
        {
            if (GameSettings.CurrentDialogueType == GameSettings.DialogueType.Intro)
            {
                mainUIManager.StartDialogue("Intro", activeStage);
            }
            else if (GameSettings.CurrentDialogueType == GameSettings.DialogueType.Outro)
            {
                mainUIManager.StartDialogue("Outro", activeStage);
            }
            else
            {
                mainUIManager.HandleDialogueAction("START_GAME");
            }
        }

        if (fadePanel != null)
        {
            Color color = fadePanel.color;
            color.a = 0.0f;
            fadePanel.color = color;
            fadePanel.gameObject.SetActive(false); // 초기에는 비활성화
        }

        // ToonyVoices 이벤트 구독
        if (_toonyVoices == null)
        {
            _toonyVoices = FindFirstObjectByType<ToonyVoices>();
        }

        // 2. ToonyVoices 이벤트에 함수 연결
        _toonyVoices.CharacterSounded.AddListener(OnCharacterSounded);
        _toonyVoices.SentenceFinished.AddListener(OnDialogueFinished);
    }

    void LoadDialogueData(string jsonFileName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFileName);
        if (jsonFile == null)
        {
            Debug.LogError("JSON file not found in Resources: " + jsonFileName);
            return;
        }

        DialogueContainer container = JsonUtility.FromJson<DialogueContainer>(jsonFile.text);
        dialogueDictionary = container.dialogues.ToDictionary(d => d.id, d => d);
    }

    void LoadCharacterSprites()
    {
        var speakers = dialogueDictionary.Values.Select(d => d.speaker).Distinct();

        foreach (var speakerName in speakers)
        {
            Sprite characterSprite = Resources.Load<Sprite>(speakerName);

            if (characterSprite != null)
            {
                characterSprites.Add(speakerName, characterSprite);
            }
            else
            {
                Debug.LogWarning($"Character Sprite not found in Resources for speaker: {speakerName}. Please check if an image named '{speakerName}' exists and is imported correctly.");
            }
        }
    }

    // 대화 시작
    public void StartDialogue(string startDialogueId, int requiredStage)
    {
        dialoguePanel.SetActive(true);
        activeStage = requiredStage;

        if (dialogueDictionary.ContainsKey(startDialogueId))
        {
            Dialogue startDialogue = dialogueDictionary[startDialogueId];
            if (startDialogue.stage == activeStage)
            {
                SetDialogue(startDialogue);
            }
            else
            {
                //해당 스테이지의 대화가 아님: 바로 게임 시작
                Debug.LogWarning($"Intro dialogue for stage {activeStage} not found. Starting game directly.");
                EndDialogue(true);
            }
        }
        else
        {
            Debug.LogError("Dialogue ID not found: " + startDialogueId);
            EndDialogue(true); // 대사 ID가 없으면 바로 종료하고 게임 시작
        }
    }

    public void OnNextButtonClick()
    {
        if (_isSpeaking)
        {
            if (_toonyVoices != null)
            {
                _toonyVoices.Stop();
            }

            dialogueText.text = _currentFullText;
            _lastDisplayedIndex = _currentFullText.Length - 1;

            _isSpeaking = false;
            _isSentenceFinished = true;
            return;
        }

        if (_isSentenceFinished)
        {
            _isSentenceFinished = false;

            if (currentDialogue != null)
            {
                string nextId = currentDialogue.nextDialogueId;

                if (nextId == "End")
                {
                    EndDialogue();
                }
                else if (dialogueDictionary.ContainsKey(nextId))
                {
                    Dialogue nextDialogue = dialogueDictionary[nextId];

                    // 다음 대사가 현재 스테이지와 일치할 때만 진행
                    if (nextDialogue.stage == activeStage)
                    {
                        SetDialogue(nextDialogue);
                    }
                    else
                    {
                        // 대화 흐름이 끝남 (다음 스테이지로 넘어가야 함)
                        EndDialogue();
                    }
                }
                else
                {
                    Debug.LogError("Next Dialogue ID not found: " + nextId);
                    EndDialogue();
                }
            }
        }
    }

    private void SetDialogue(Dialogue dialogue)
    {
        StopAllCoroutines();

        if (_toonyVoices != null)
        {
            _toonyVoices.Stop();
        }

        currentDialogue = dialogue;

        // 피치 설정
        //string speakerName = dialogue.speaker;
        //float customPitch = GetPitchForSpeaker(speakerName);

        // 1. 대사창 정보 업데이트
        nameText.text = dialogue.name;
        //dialogueText.text = dialogue.text;
        dialogueText.text = ""; // 초기화 (OnCharacterSounded 이벤트로 채워짐)
        _currentFullText = dialogue.text;

        nameText.gameObject.SetActive(true);
        dialogueText.gameObject.SetActive(true);
        dialoguePanel.SetActive(true);

        _lastDisplayedIndex = -1;
        _isSpeaking = true;
        _isSentenceFinished = false;

        _toonyVoices.Speak(_currentFullText, 3.5f, 0.5f, 0.3f); // 피치 통일

        // 컷씬 시작 조건 확인
        if (dialogue.isCutscene && cutscenePanel != null && !string.IsNullOrEmpty(dialogue.cutsceneImage))
        {
            StartCutscene(dialogue);
            return;
        }

        if (!string.IsNullOrEmpty(dialogue.hideCharacterPosition))
        {
            if (dialogue.hideCharacterPosition == "Left" && leftCharacterPanel != null)
            {
                leftCharacterPanel.SetActive(false);
                // 숨겨진 캐릭터의 스프라이트도 null로 설정하여 다음 대화에서 다시 등장하지 않게 합니다.
                leftCharacterImage.sprite = null;
            }
            else if (dialogue.hideCharacterPosition == "Right" && rightCharacterPanel != null)
            {
                rightCharacterPanel.SetActive(false);
                rightCharacterImage.sprite = null;
            }
        }

        // 2. 스탠딩 일러스트 활성화/위치/투명도 조정

        // [수정] 2-1. 모든 캐릭터 패널 비활성화로 초기화 (1인 대화 처리의 시작)
        leftCharacterPanel.SetActive(false);
        rightCharacterPanel.SetActive(false);

        // 2-2. 기준 색상 및 크기 설정
        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        Vector3 activeScale = Vector3.one;
        Vector3 inactiveScale = new Vector3(0.9f, 0.9f, 1f);

        Image speakerImage = null;
        Image companionImage = null;
        GameObject speakerPanel = null;
        GameObject companionPanel = null;
        string activeSpeakerName = dialogue.speaker;

        // 2-3. 패널 구분 및 참조 설정
        if (dialogue.position == "Left")
        {
            speakerPanel = leftCharacterPanel;
            speakerImage = leftCharacterImage;
            companionPanel = rightCharacterPanel;
            companionImage = rightCharacterImage;
        }
        else if (dialogue.position == "Right")
        {
            speakerPanel = rightCharacterPanel;
            speakerImage = rightCharacterImage;
            companionPanel = leftCharacterPanel;
            companionImage = leftCharacterImage;
        }

        // 2-4. 스피커 처리 (Active Character)
        if (speakerImage != null && characterSprites.ContainsKey(activeSpeakerName))
        {
            // 1. 스피커 이미지 설정
            speakerImage.sprite = characterSprites[activeSpeakerName];
            // speakerImage.SetNativeSize(); // (필요에 따라 주석 처리 유지)

            // 2. 스피커 강조 및 활성화
            speakerPanel.SetActive(true);
            speakerImage.color = activeColor;
            speakerImage.transform.localScale = activeScale;
        }
        else
        {
            // 스피커 정보가 없거나 스프라이트 로드 오류
            Debug.LogWarning($"Speaker sprite not found for: {activeSpeakerName} or position not set.");
        }

        // 2-5. 컴패니언 처리 (Inactive Character/Listener)
        if (companionImage != null)
        {
            // [핵심 로직] 컴패니언 이미지에 스프라이트가 할당되어 있다면 (이전 대화의 캐릭터가 남아있다면) 청자로 유지합니다.
            if (companionImage.sprite != null)
            {
                // 컴패니언 활성화
                companionPanel.SetActive(true);

                // 컴패니언 약화 (어둡고 작게)
                companionImage.color = inactiveColor;
                companionImage.transform.localScale = inactiveScale;
            }
            // companionImage.sprite가 null인 경우: 2-1에서 비활성화된 상태로 유지됩니다 (1인 대화)
        }
    }

    private void StartCutscene(Dialogue dialogue)
    {
        StopAllCoroutines();
        StartCoroutine(CutsceneFlowCoroutine(dialogue));
    }

    private IEnumerator CutsceneFlowCoroutine(Dialogue dialogue)
    {
        // A. 대화창 숨기기 및 기본 패널 설정
        //dialoguePanel.SetActive(false);

        dialoguePanelBackground.gameObject.SetActive(true);
        dialogueText.gameObject.SetActive(false);
        nameText.gameObject.SetActive(false);

        leftCharacterPanel.SetActive(false);
        rightCharacterPanel.SetActive(false);

        LoadCutsceneImage(dialogue.cutsceneImage);

        Color initialColor = cutsceneImage.color;
        initialColor.a = 0f;
        cutsceneImage.color = initialColor;

        if (cutscenePanel != null) cutscenePanel.SetActive(true);

        if (dialogue.fadeFromBlackOnStart && fadePanel != null)
        {
            // 1. 검은 화면 페이드 인 (화면을 완전히 덮음)
            yield return StartCoroutine(FadeToBlack(dialogue.fadeInDuration));

            Color color = cutsceneImage.color;
            color.a = 1f;
            cutsceneImage.color = color;

            // 3. 검은 화면 페이드 아웃 (컷씬이 나타남)
            yield return StartCoroutine(FadeFromBlack(dialogue.fadeOutDuration));

        }
        else
        {
            if (cutsceneImage != null)
            {
                yield return StartCoroutine(FadeImage(cutsceneImage, 0f, 1f, dialogue.fadeInDuration));
            }
        }

        yield return new WaitForSeconds(dialogue.cutsceneDisplayTime);

        yield return StartCoroutine(EndCutsceneCoroutine(dialogue, dialogue.nextDialogueId));

    }

    private void LoadCutsceneImage(string imageName)
    {
        Sprite cutsceneSprite = Resources.Load<Sprite>(imageName);

        if (cutsceneSprite != null && cutsceneImage != null)
        {
            cutsceneImage.sprite = cutsceneSprite;
            // cutsceneImage.SetNativeSize(); // 앵커 문제 방지를 위해 제거

            // [수정] Load에서는 스프라이트만 설정하고, Alpha는 코루틴 내에서 제어합니다.
            Color color = cutsceneImage.color;
            color.a = 1f; // 스프라이트가 완전히 투명해지는 것을 막기 위해 임시로 1로 설정
            cutsceneImage.color = color;
        }
        else
        {
            Debug.LogError($"Cutscene Image not found: {imageName}.");
        }

        //startBackgroundImage.gameObject.SetActive(false);
    }

    // 대화 종료
    private void EndDialogue(bool skipAction = false)
    {
        dialoguePanel.SetActive(false);
        leftCharacterPanel.SetActive(false);
        rightCharacterPanel.SetActive(false);

        if (!skipAction && currentDialogue != null && mainUIManager != null)
        {
            // 대화 종료 후 행동을 MainUIManager에 전달
            mainUIManager.HandleDialogueAction(currentDialogue.actionAfterDialogue);
        }
        else if (mainUIManager != null)
        {
            // skipAction일 경우 (대화가 바로 없을 때) 기본적으로 게임 플레이 시작
            mainUIManager.HandleDialogueAction("START_GAME");
        }

        Debug.Log("대화가 종료되었습니다. Action: " + (currentDialogue != null ? currentDialogue.actionAfterDialogue : "START_GAME"));
    }

    public void OnCutsceneClicked()
    {
        if (cutscenePanel == null || !cutscenePanel.activeSelf) return;

        StopAllCoroutines();
        string nextId = currentDialogue.nextDialogueId;

        StartCoroutine(EndCutsceneCoroutine(currentDialogue, nextId));
    }

    private IEnumerator EndCutsceneCoroutine(Dialogue dialogue, string nextId)
    {
        // 1. 컷씬 이미지 페이드 아웃 및 화면 전환 준비
        if (cutsceneImage != null)
        {
            if (dialogue.fadeToBlackOnEnd && fadePanel != null)
            {
                // [수정] 검은 화면으로 페이드 인 (화면을 가리고 다음 전환 준비)
                yield return StartCoroutine(FadeToBlack(dialogue.fadeOutDuration));
            }
            else
            {
                // [수정] 일반 컷씬: 이미지 자체를 페이드 아웃
                yield return StartCoroutine(FadeImage(cutsceneImage, 1f, 0f, dialogue.fadeOutDuration));
            }
        }

        // 2. 컷씬 패널 비활성화
        if (cutscenePanel != null) cutscenePanel.SetActive(false);

        // 3. 컷씬 이미지를 다이얼로그 배경으로 설정 (페이드 인 없음)
        if (cutsceneImage.sprite != null && dialogueBackgroundImage != null)
        {
            dialogueBackgroundImage.sprite = cutsceneImage.sprite;

            // [핵심 수정] 배경 이미지 페이드 인 제거. 투명도를 즉시 1.0으로 설정합니다.
            Color color = dialogueBackgroundImage.color;
            color.a = 1.0f;
            dialogueBackgroundImage.color = color;

            // 시작 배경 비활성화
            if (startBackgroundImage != null)
            {
                startBackgroundImage.gameObject.SetActive(false);
            }
        }

        // 4. 다음 대화로 이동 (기존 로직 유지)
        if (nextId == "End")
        {
            EndDialogue();
        }
        else if (dialogueDictionary.ContainsKey(nextId))
        {
            Dialogue nextDialogue = dialogueDictionary[nextId];

            if (nextDialogue.stage == activeStage)
            {
                SetDialogue(nextDialogue);
                dialoguePanel.SetActive(true);
            }
            else
            {
                EndDialogue();
            }
        }
        else
        {
            Debug.LogError("Cutscene Next Dialogue ID not found: " + nextId);
            EndDialogue();
        }

        if (dialogue.fadeToBlackOnEnd && fadePanel != null)
        {
            // 검은 화면으로 전환이 완료되었으므로, 다음 대화가 시작될 때 검은 화면을 투명화합니다.
            yield return StartCoroutine(FadeFromBlack(0.1f)); // 빠른 페이드 아웃
            fadePanel.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeImage(Image image, float startAlpha, float endAlpha, float duration)
    {
        float startTime = Time.time;
        Color color = image.color;
        color.a = startAlpha;
        image.color = color;

        // 활성화 상태가 아닐 경우 명시적으로 켜줘야 함 (FadeToBlack이 Panel을 켰으므로)
        image.gameObject.SetActive(true);

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            image.color = color;
            yield return null;
        }

        color.a = endAlpha;
        image.color = color;

        if (endAlpha == 0)
        {
            image.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeToBlack(float duration)
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        fadePanel.color = new Color(0f, 0f, 0f, 0f);

        yield return StartCoroutine(FadeImage(fadePanel, 0f, 1f, duration));
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        if (fadePanel == null) yield break;

        yield return StartCoroutine(FadeImage(fadePanel, 1f, 0f, duration));
        // 패널 자체는 EndCutsceneCoroutine에서 비활성화
    }


    // ToonyVoices 이벤트 핸들러
    // DialogueManager.cs 파일 내
    public void OnCharacterSounded(int originalIndex)
    {
        // 1. 전달받은 인덱스가 이전 출력 인덱스와 같거나 작다면 무시합니다. 
        //    (하나의 음절(자모 3개)이 발음되는 동안 같은 인덱스(i)가 반복되는 것을 방지)
        if (originalIndex <= _lastDisplayedIndex)
        {
            return;
        }

        // 2. 새로운 글자를 출력해야 할 때만 로직을 실행합니다.
        string currentMessage = _currentFullText;

        // 이전에 덮어쓰기 방식으로 잘 작동했으므로, 텍스트 길이만 확인합니다.
        if (originalIndex + 1 <= currentMessage.Length)
        {
            // 원본 텍스트의 처음부터 현재 인덱스까지의 문자열로 UI를 덮어씁니다.
            dialogueText.text = currentMessage.Substring(0, originalIndex + 1);

            // UI가 갱신되었으므로 인덱스를 업데이트합니다.
            _lastDisplayedIndex = originalIndex;
        }
    }

    private float GetPitchForSpeaker(string speakerName)
    {
        // [기본 피치]: ToonyVoices 컴포넌트에 설정된 기본 값 (예: 2.0f)
        float defaultPitch = 2.0f;

        // 피치 값 가이드: 
        // - 값이 높을수록 (예: 2.5f ~ 3.5f) 소리가 빠르고 가늘어집니다. (어린이, 경쾌함)
        // - 값이 낮을수록 (예: 1.0f ~ 1.8f) 소리가 느리고 굵어집니다. (성인, 중후함)

        switch (speakerName)
        {
            case "Player":
                return 2.4f;

            case "Manager":
                return 2.0f;

            case "GirlFan":
                return 3.5f;

            case "RivalWolf":
                return 1.8f;

            case "GrayCitizen":
                return 1.4f;

            default:
                return defaultPitch + 0.4f;
        }
    }

    // 문장 전체 음성 재생이 끝났을 때 호출됨
    public void OnDialogueFinished()
    {
        _isSpeaking = false;
        _isSentenceFinished = true;

        dialogueText.text = _currentFullText;

        Debug.Log("대화 문장 끝! 다음으로 진행 가능.");
        // ... (nextDialogueId를 기반으로 다음 대화 로드 준비) ...
    }

    public void DisplayDialogue(Dialogue dialogue) // JSON 데이터 구조를 나타내는 클래스라고 가정
    {
        // 1. 현재 대화 텍스트 저장 및 UI 초기화
        _currentFullText = dialogue.text;
        _dialogueTextUI.text = ""; // 출력 전 UI를 깨끗하게 비웁니다.

        _lastDisplayedIndex = -1;

        _toonyVoices.Speak(_currentFullText);

        // 3. (옵션) UI 출력 속도를 빠르게 하고 싶다면:
        // _toonyVoices.Speak(_currentFullText, 3f); // 피치를 높여 더 빠르게 말하게 함
    }

    public void SkipDialogue()
    {
        // 1. 현재 진행 중인 모든 코루틴 및 음성 중지
        StopAllCoroutines();
        if (_toonyVoices != null) _toonyVoices.Stop();

        // 2. 모든 UI 패널 즉시 정리
        dialoguePanel.SetActive(false);
        leftCharacterPanel.SetActive(false);
        rightCharacterPanel.SetActive(false);
        if (cutscenePanel != null) cutscenePanel.SetActive(false);

        if (fadePanel != null)
        {
            Color color = fadePanel.color;
            color.a = 0.0f;
            fadePanel.color = color;
            fadePanel.gameObject.SetActive(false);
        }

        // 3. 흐름 제어 (방법 B: 데이터 우선 및 상태 초기화)
        if (mainUIManager != null)
        {
            // 우선순위 1: 현재 대사 데이터에 특정 액션이 설정되어 있는지 확인
            if (currentDialogue != null && !string.IsNullOrEmpty(currentDialogue.actionAfterDialogue) && currentDialogue.actionAfterDialogue != "None")
            {
                Debug.Log($"[Skip] 대사 액션 실행: {currentDialogue.actionAfterDialogue}");
                mainUIManager.HandleDialogueAction(currentDialogue.actionAfterDialogue);
            }
            // 우선순위 2: 아웃트로 상태라면 클리어 UI 출력
            else if (GameSettings.CurrentDialogueType == GameSettings.DialogueType.Outro)
            {
                Debug.Log("[Skip] 아웃트로 종료: 클리어 UI 출력");
                mainUIManager.HandleDialogueAction("SHOW_CLEAR_UI");
            }
            // 우선순위 3: 그 외(인트로 등) 상황에서는 게임 시작
            else
            {
                Debug.Log("[Skip] 일반 시작: START_GAME 실행");
                mainUIManager.HandleDialogueAction("START_GAME");
            }

            // [핵심] 처리가 끝난 후 대화 타입을 None으로 초기화하여 다음 판에 영향을 주지 않도록 함
            GameSettings.SetDialogueType(GameSettings.DialogueType.None);
        }

        Debug.Log("대화/컷씬 스킵 및 GameSettings 초기화 완료");
    }
}