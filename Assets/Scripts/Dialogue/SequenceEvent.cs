using System;
using System.Collections.Generic;
using UnityEngine;

// ===== 시퀀스 이벤트 (연출 흐름 정의) =====
[Serializable]
public class SequenceEvent
{
    public string type;          // "DIALOGUE" | "VIDEO"

    // DIALOGUE
    public string speaker;
    public string speakerName;
    public string text;
    public string position;
    public string hidePosition;
    public string expression;    // 추후 표정 변화용

    // VIDEO
    public string videoFileName;
    public bool skippable = true;

    // 공통
    public string actionAfter;

}

[Serializable]
public class StageSequence
{
    public string scriptType;    // "Intro" | "Outro"
    public List<SequenceEvent> events;
}

[Serializable]
public class StageScript
{
    public int stage;
    public List<StageSequence> sequences;
}
