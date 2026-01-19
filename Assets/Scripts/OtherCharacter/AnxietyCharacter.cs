using UnityEngine;

public class AnxietyCharacter : MonoBehaviour
{
    // === 외부에서 Inspector 설정 ===
    [Header("Dependencies")]
    private Transform playerTransform;
    private PlayerController playerController;

    [Header("Position & Movement")]
    [Tooltip("불안 캐릭터가 평소에 대기할 월드 X 위치 (맨 왼쪽 땅).")]
    public float fixedWorldXPosition = 0f;
    [Tooltip("불안 캐릭터의 고정된 Y 위치 (땅에 디딘 채)")]
    public float fixedYPosition = 0f;
    [Tooltip("불안 캐릭터의 고정된 Z 위치")]
    public float fixedZPosition = 0f;

    [Tooltip("피격 시 다가갈 월드 X 위치 (fixedWorldXPosition + 이 값)")]
    public float approachDistanceX = 1.5f;

    [Tooltip("불안 캐릭터가 최대로 다가갈 수 있는 월드 X 위치 (예: 1.5f)")]
    public float maxApproachX = 1.5f;
    [Tooltip("움직임을 부드럽게 하는 Lerp 계수")]
    public float smoothSpeed = 8.0f;

    [Header("Timing")] // 공격 후 대기 시간 설정
    [Tooltip("공격 후 원래 자리로 즉시 돌아가기 전 대기 시간 (초)")]
    public float retreatDelay = 1.0f; // 공격 모션 시간 등을 고려하여 설정하세요.

    [Header("Animation Settings")]
    public Animator anim;
    private readonly string AttackingParameter = "IsAttacking";

    // === 내부 상태 ===
    private float targetWorldX;
    private float lastPlayerHP;
    private float retreatTimer = 0f;


    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("AnxietyCharacter: 씬에서 PlayerController를 찾을 수 없습니다! 스크립트 비활성화.");
            enabled = false;
            return;
        }

        playerTransform = playerController.transform;

        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("AnxietyCharacter: 씬에서 PlayerStats를 찾을 수 없습니다! HP 감지 불가능.");
            enabled = false;
            return;
        }

        // Animator 컴포넌트 찾기 (이 스크립트가 붙은 오브젝트 또는 그 자식에서)
        if (anim == null) // Inspector에서 할당되지 않았다면
        {
            anim = GetComponent<Animator>(); // 현재 오브젝트에서 찾기
            if (anim == null)
            {
                anim = GetComponentInChildren<Animator>(); // 자식 오브젝트에서 찾기
            }
        }

        if (anim == null)
        {
            Debug.LogWarning("AnxietyCharacter: Animator 컴포넌트를 찾을 수 없습니다! 애니메이션이 재생되지 않습니다.");
        }

        lastPlayerHP = playerStats.HP;
        targetWorldX = fixedWorldXPosition;

        transform.position = new Vector3(
            fixedWorldXPosition,
            fixedYPosition,
            fixedZPosition
        );

        // 초기엔 달리기 모션 시작
        if (anim != null)
        {
            anim.SetBool(AttackingParameter, false);
        }
    }

    void Update()
    {
        if (playerController == null) return;

        // 1. 피격 여부 확인 및 다가오기
        CheckForFailure();

        // 2. 공격 후 대기 및 즉시 복귀 로직
        // 캐릭터가 고정 위치(fixedWorldXPosition)보다 앞에 나와 있고 (공격 위치), 타이머가 켜져 있다면
        if (targetWorldX > fixedWorldXPosition && retreatTimer > 0f)
        {
            // 타이머 카운트다운 (대기 시간)
            retreatTimer -= Time.deltaTime;

            if (retreatTimer <= 0f)
            {
                targetWorldX = fixedWorldXPosition;
                Debug.Log("불안 캐릭터: 공격 대기 시간 종료. 원래 위치로 즉시 복귀 목표 설정.");

                if (anim != null)
                {
                    anim.SetBool(AttackingParameter, false);
                }
            }
        }

        // 3. 실제 유니티 위치 업데이트 (부드러운 이동)
        UpdatePosition();
    }

    /// <summary>
    /// PlayerStats의 HP 변화를 감지하여 피격 여부를 확인합니다.
    /// </summary>
    void CheckForFailure()
    {
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null) return;

        if (playerStats.HP < lastPlayerHP)
        {
            // HP가 감소했으므로 피격(미스) 발생 -> 앞으로 다가옴
            ApproachOnFailure();
        }

        lastPlayerHP = playerStats.HP;
    }

    /// <summary>
    /// 플레이어 미스 시 앞으로 다가와서 공격 모션을 취합니다.
    /// </summary>
    public void ApproachOnFailure()
    {
        Debug.Log("불안 캐릭터: 플레이어 미스 감지! 앞으로 다가가 공격 모션을 취하고 타이머 시작.");

        targetWorldX = fixedWorldXPosition + approachDistanceX;

        targetWorldX = Mathf.Min(targetWorldX, maxApproachX);

        retreatTimer = retreatDelay;

        if (anim != null)
        {
            anim.SetBool(AttackingParameter, true);
        }
    }

    /// <summary>
    /// 현재 목표 월드 X 위치를 사용하여 불안 캐릭터의 위치를 부드럽게 업데이트합니다.
    /// </summary>
    private void UpdatePosition()
    {
        float currentWorldX = transform.position.x;

        // Lerp를 사용하여 목표 위치로 부드럽게 이동
        float newWorldX = Mathf.Lerp(currentWorldX, targetWorldX, Time.deltaTime * smoothSpeed);

        // X 위치는 계산된 값, Y와 Z 위치는 고정된 값으로 설정
        transform.position = new Vector3(newWorldX, fixedYPosition, fixedZPosition);
    }
}