using UnityEngine;

[CreateAssetMenu(fileName = "StageBCGData", menuName = "Scriptable Objects/StageBCGData")]
public class StageBCGData : ScriptableObject
{
    [Header("배경 세팅")]
    public GameObject[] backgroundPrefabs; // 스테이지에 생성할 배경 레이어들
    public float[] scrollFactors;          // 각 레이어의 패럴렉스 속도 (배경 수와 일치해야 함)

    [Header("데코레이션 세팅")]
    public DecorationData[] decorationDatas; // 이 스테이지에서 사용할 데코 데이터들
    public float minInterval = 5f;          // 전체적인 스폰 최소 간격
    public float maxInterval = 15f;
}
