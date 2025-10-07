// PlayerStats.cs (»õ ÆÄÀÏ)
using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public static event Action<int> OnHPChanged;

    [field: SerializeField] public int InitialHP { get; private set; } = 3;

    private int hp;

    [HideInInspector]
    private int comboCount = 0;

    public int HP
    {
        get { return hp; }
        set
        {
            hp = Mathf.Max(0, value);
            OnHPChanged?.Invoke(hp);
        }
    }

    private void Start()
    {
        HP = InitialHP;
    }
}