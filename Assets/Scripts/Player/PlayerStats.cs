using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    // HP 변경 시 UI 갱신을 위해 이벤트 발행
    public static event Action<int> OnHPChanged;

    [field: SerializeField] public int InitialHP { get; private set; } = 3;

    private int hp;

    public int HP
    {
        get { return hp; }
        set
        {
            hp = Mathf.Max(0, value);
            OnHPChanged?.Invoke(hp); // HP 변경 시 UI 갱신
        }
    }

    private void Start()
    {
        ResetHP(); // 씬 시작 시 체력 초기화
    }

    /// <summary>
    /// 체력을 초기화하는 메서드
    /// </summary>
    public void ResetHP()
    {
        HP = InitialHP; // 프로퍼티 사용 → 이벤트 발생 → UI 갱신
    }

    /// <summary>
    /// 체력을 감소시키는 메서드 (안전하게 UI 갱신)
    /// </summary>
    public void DecreaseHP(int amount = 1)
    {
        HP -= amount;
    }

    /// <summary>
    /// 체력을 증가시키는 메서드 (회복용)
    /// </summary>
    public void IncreaseHP(int amount = 1)
    {
        HP += amount;
    }
}