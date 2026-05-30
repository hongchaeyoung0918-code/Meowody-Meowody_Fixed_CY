// ===== 대화 데이터 (기존에서 컷씬 제거) =====
using System;
using System.Collections.Generic;

[Serializable]
public class DialogueData
{
    public string id;
    public string speaker;       // 스프라이트 키
    public string speakerName;   // 화면에 표시될 이름
    public string text;
    public string position;      // "Left" | "Right"
    public string expression;    // 표정: "Normal" | "Happy" | "Sad" (추후 확장용)
    public string hidePosition;  // 이 대사 시작 시 숨길 캐릭터 위치
}

[Serializable]
public class DialogueDataContainer
{
    public List<DialogueData> dialogues;
}