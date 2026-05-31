using UnityEngine;

[CreateAssetMenu(fileName = "StageBCGData", menuName = "Scriptable Objects/StageBCGData")]
public class StageBCGData : ScriptableObject
{
    [Header("��� ����")]
    public GameObject[] backgroundPrefabs; // ���������� ������ ��� ���̾��
    public float[] scrollFactors;          // �� ���̾��� �з����� �ӵ� (��� ���� ��ġ�ؾ� ��)
    public float[] backgroundYPositions;   // 각 배경 레이어의 Y 위치

    [Header("���ڷ��̼� ����")]
    public DecorationData[] decorationDatas; // �� ������������ ����� ���� �����͵�
    public float minInterval = 5f;          // ��ü���� ���� �ּ� ����
    public float maxInterval = 15f;
}
