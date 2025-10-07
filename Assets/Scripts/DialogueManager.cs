using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialoguePanel;      // 대화창 전체 Panel
    public TMP_Text nameText;
    public TMP_Text dialogueText;

    [Header("Character Sprites")]
    public GameObject leftCharacterPanel;  // 왼쪽 스탠딩 일러스트 패널
    public Image leftCharacterImage;      // 왼쪽 캐릭터 Image 컴포넌트
    public GameObject rightCharacterPanel; // 오른쪽 스탠딩 일러스트 패널
    public Image rightCharacterImage;

    // 스프라이트 딕셔너리 (캐릭터 이름으로 스프라이트를 관리)
    public Dictionary<string, Sprite> characterSprites = new Dictionary<string, Sprite>();

    private Dictionary<string, Dialogue> dialogueDictionary;
    private Dialogue currentDialogue;
    void Start()
    {
        // 1. JSON 데이터 로드 및 파싱
        LoadDialogueData("Dialogue");

        // 2. 캐릭터 스프라이트 리소스 로드
        LoadCharacterSprites();

        // 초기 대화 시작
        StartDialogue("1-2_1");
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

    // 캐릭터 스프라이트를 Resources 폴더에서 동적으로 로드하는 함수
    void LoadCharacterSprites()
    {
        // JSON에 정의된 모든 캐릭터 이름을 추출 (중복 제거)
        var speakers = dialogueDictionary.Values.Select(d => d.speaker).Distinct();

        foreach (var speakerName in speakers)
        {
            // Resources.Load<Sprite>를 사용하여 'speakerName'과 이름이 같은 이미지를 로드
            // 예: Resources/Player.png -> Sprite
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
    public void StartDialogue(string startDialogueId)
    {
        dialoguePanel.SetActive(true);
        if (dialogueDictionary.ContainsKey(startDialogueId))
        {
            SetDialogue(dialogueDictionary[startDialogueId]);
        }
        else
        {
            Debug.LogError("Dialogue ID not found: " + startDialogueId);
            EndDialogue();
        }
    }

    // 다음 버튼 클릭 시 호출될 함수 (혹은 화면 터치)
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
                SetDialogue(dialogueDictionary[nextId]);
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

            // 반대편 캐릭터는 어둡게
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
    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        leftCharacterPanel.SetActive(false);
        rightCharacterPanel.SetActive(false);
        Debug.Log("대화가 종료되었습니다.");
    }
}