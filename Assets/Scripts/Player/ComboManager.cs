using UnityEngine;
using System.Collections;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance;

    [Header("Combo Settings")]
    [Range(0, 1000)]
    public int currentComboCount = 0;
    public float comboGraceTime = 0.5f; // 조작이 끊기기 전 허용 시간
    private float comboTimer = 0f;
    private bool isGraceTimeActive = false;

    [HideInInspector] // 인스펙터에 노출은 되지만 편집은 막아 콤보 진행 중 변화를 관찰할 수 있도록 합니다.
    public int maxComboCount = 0;

    private ColorManager colorManager;

    // 콤보 연출 기준
    private const int BRIGHTNESS_COMBO = 20; // 캐릭터 빛남 시작
    private const int SKY_CHANGE_COMBO = 40; // 하늘 색깔 영구 전환

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        maxComboCount = 0;
        currentComboCount = 0;

        colorManager = ColorManager.Instance;
        if (colorManager == null)
        {
            Debug.LogError("ColorManager를 찾을 수 없습니다! 씬에 배치했는지 확인하세요.");
        }
    }

    void Update()
    {
        if (isGraceTimeActive)
        {
            comboTimer -= Time.deltaTime;

            if (comboTimer <= 0)
            {
                // Grace Time 초과! 콤보가 끊어지지 않게 관리하기 위해 타이머만 0으로 설정
                isGraceTimeActive = false;
            }
        }

        // 콤보가 Grace Time이 아닐 때만 게이지를 감소시키도록 신호를 보냄
        if (!isGraceTimeActive && colorManager != null)
        {
            colorManager.SetComboGraceState(false);
        }
    }

    /// <summary>
    /// 플레이어의 성공적인 키 입력 시 호출됩니다.
    /// </summary>
    public void RegisterKeySuccess()
    {
        // Grace Time 이내에 입력했는지 확인 (사실상 입력이 들어오면 콤보 유지)
        if (comboTimer > 0 || currentComboCount == 0)
        {
            currentComboCount++;

            if (currentComboCount > maxComboCount)
            {
                maxComboCount = currentComboCount;
            }

            // 콤보 유지 시 Grace Time 리셋
            comboTimer = comboGraceTime;
            isGraceTimeActive = true;

            // 콤보가 유지되는 동안은 컬러 게이지 감소를 멈춤
            if (colorManager != null)
            {
                colorManager.SetComboGraceState(true);
            }

            // 콤보 연출 확인
            CheckComboEffects();
        }
        // Grace Time이 끝난 후 첫 입력은 새로운 콤보 시작으로 간주 (콤보 리셋은 피격시에만)
        else if (!isGraceTimeActive)
        {
            // 새로운 콤보 시작
            currentComboCount = 1;

            if (currentComboCount > maxComboCount)
            {
                maxComboCount = currentComboCount;
            }

            comboTimer = comboGraceTime;
            isGraceTimeActive = true;
            if (colorManager != null)
            {
                colorManager.SetComboGraceState(true);
            }
        }
    }

    public void ResetCombo()
    {
        if (currentComboCount == 0) return;

        currentComboCount = 0;
        isGraceTimeActive = false;
        comboTimer = 0f;

        // 콤보 초기화 시 컬러 게이지 감소를 재개하도록 신호를 보냄
        if (colorManager != null)
        {
            colorManager.SetComboGraceState(false);
            // 하늘 색깔 복구 로직 호출 (TODO: SkyManager 필요)
        }

        Debug.Log("콤보 초기화!");
    }

    void CheckComboEffects()
    {
        if (currentComboCount == BRIGHTNESS_COMBO)
        {
            Debug.Log("20 콤보 달성! 캐릭터 빛남 연출 시작.");
            // TODO: 캐릭터 빛남/파티클 연출 로직 추가
        }
        else if (currentComboCount == SKY_CHANGE_COMBO)
        {
            Debug.Log("40 콤보 달성! 소프트 플래시 및 하늘 색깔 영구 전환.");
            // TODO: 소프트 플래시 및 하늘 전환 로직 호출
        }
    }
}
