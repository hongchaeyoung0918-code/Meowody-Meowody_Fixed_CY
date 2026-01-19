using System;
using System.Collections.Generic;

[Serializable]
public class Dialogue
{
    public string id;
    public int stage;
    public string speaker;
    public string name;
    public string text;
    public string position; // "Left" or "Right"
    public string nextDialogueId;
    public string actionAfterDialogue;
    public string hideCharacterPosition;

    // 컷씬
    public bool isCutscene;        // 컷씬을 시작할지 여부
    public string cutsceneImage;   // Resources 폴더 내 컷씬 이미지 이름
    public string nextCutsceneId;

    // 검은 화면 및 페이드 인/아웃
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public bool useBlackScreenTransition = false;
    public float cutsceneDisplayTime = 2.0f;

    public bool fadeFromBlackOnStart = false;
    public bool fadeToBlackOnEnd = false;
}

// JSON 파일 전체를 로드하기 위한 Wrapper 클래스
[Serializable]
public class DialogueContainer
{
    public List<Dialogue> dialogues;
}