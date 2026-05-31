using UnityEngine;

[CreateAssetMenu(fileName = "NewDecoData", menuName = "Stage/Deco Data")]
public class DecorationData : ScriptableObject
{
    public GameObject prefab;         // 프리팹 외형
    public float yOffset = -2f;        // 이 프랍만의 고유 높이
    [Range(1, 10)] public int spawnWeight = 5; // 스폰 확률 (높을수록 자주 나옴)
}
