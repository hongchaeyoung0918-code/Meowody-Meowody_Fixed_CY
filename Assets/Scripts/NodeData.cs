using UnityEngine;

public enum NodeType
{
    // 이동/방어 관련
    SLIDE_OBSTACLE, // 슬라이딩으로 피해야 하는 장애물
    JUMP_PLATFORM,  // 일반 점프를 유도하는 플랫폼
    TRAMPOLINE,     // 트램폴린 (추가 점프력)
    JUMP_ORB,       // 공중 점프 오브젝트

    // 공격/퍼즐 관련
    CITIZEN         // 음표를 쏴야 하는 회색 시민
}

// 각 노드의 정보를 담는 데이터 클래스
[System.Serializable]
public class Node
{
    public NodeType type;
    public float beatNumber;   // 생성될 박자 번호 (예: 1, 1.5, 2)
    public float relativeY;    // 플레이어의 땅 높이 대비 상대적인 Y축 위치 (예: 0.5f, 1.5f)
    public bool isGenerated;   // 생성 여부 플래그

    public Node(NodeType t, float beat, float y)
    {
        type = t;
        beatNumber = beat;
        relativeY = y;
        isGenerated = false;
    }
}