using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;

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

    private void Awake()
    {
        mainUIManager = FindFirstObjectByType<MainUIManager>();
        if (mainUIManager == null)
        {
            Debug.LogError("MainUIManager를 씬에서 찾을 수 없습니다.");
        }

        LoadDialogueData("Dialogue");
        LoadCharacterSprites();
        dialoguePanel.SetActive(false);
    }

    void Start()
    {
        activeStage = GameSettings.SelectedStage;

        if (GameSettings.CurrentDialogueType == GameSettings.DialogueType.Intro)
        {
            mainUIManager.StartDialogue("Intro");
        }
        else if (GameSettings.CurrentDialogueType == GameSettings.DialogueType.Outro)
        {
            mainUIManager.StartDialogue("Outro");
        }
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

    private void SetDialogue(Dialogue dialogue)
    {
        currentDialogue = dialogue;

        // 1. 대사창 정보 업데이트
        nameText.text = dialogue.name;
        dialogueText.text = dialogue.text;

        // 2. 스탠딩 일러스트 활성화/위치/투명도 조정

        // 모든 캐릭터 패널 비활성화
        leftCharacterPanel.SetActive(false);
        rightCharacterPanel.SetActive(false);

        // 현재 캐릭터 설정
        GameObject activePanel = null;
        Image activeImage = null;
        Image inactiveImage = null;

        if (dialogue.position == "Left")
        {
            activePanel = leftCharacterPanel;
            activeImage = leftCharacterImage;
            inactiveImage = rightCharacterImage;
        }
        else if (dialogue.position == "Right")
        {
            activePanel = rightCharacterPanel;
            activeImage = rightCharacterImage;
            inactiveImage = leftCharacterImage;
        }

        // 현재 대사하는 캐릭터 활성화 및 밝게 표시
        if (activePanel != null)
        {
            activePanel.SetActive(true);
            activeImage.color = Color.white;

            if (characterSprites.ContainsKey(dialogue.speaker))
            {
                activeImage.sprite = characterSprites[dialogue.speaker];
                activeImage.SetNativeSize(); // 일러스트 크기를 원본 크기에 맞춤 (선택 사항)
            }
            // ------------------

            // 반대편 캐릭터는 어둡게 -> 안나옴
            if (inactiveImage != null)
            {
                if (inactiveImage.gameObject.activeSelf)
                {
                    inactiveImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }
        }
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
}