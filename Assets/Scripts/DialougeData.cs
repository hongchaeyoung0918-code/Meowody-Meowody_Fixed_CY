using System;
using System.Collections.Generic;

[Serializable]
public class Dialogue
{
    public string id;
    public string speaker;
    public string name;
    public string text;
    public string position; // "Left" or "Right"
    public string nextDialogueId;
}

// JSON 파일 전체를 로드하기 위한 Wrapper 클래스
[Serializable]
public class DialogueContainer
{
    public List<Dialogue> dialogues;
}