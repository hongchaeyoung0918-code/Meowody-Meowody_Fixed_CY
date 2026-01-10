using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class ColorManager : MonoBehaviour
{
    [Header("Stage Progress Settings")]
    [Tooltip("스테이지 완료까지 걸리는 총 시간 (초)")]
    public float stageDuration = 60f;
    private float elapsedTime = 0f;

    [Header("Post Processing Settings")]
    public Volume volume;
    private ColorAdjustments colorAdjustments;

    [Header("Current State")]
    [Range(0f, 100f)]
    public float colorGauge = 0f;

    public static ColorManager Instance;

    // 타 스크립트(ComboManager 등)와의 호환성을 위한 변수
    private bool isComboActive = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndSetupVolume();
        ResetProgress();
    }

    void Start()
    {
        FindAndSetupVolume();
        ResetProgress();
    }

    private void ResetProgress()
    {
        elapsedTime = 0f;
        colorGauge = 0f;
    }

    private void FindAndSetupVolume()
    {
        if (volume == null)
        {
            volume = FindFirstObjectByType<Volume>();
        }

        if (volume != null)
        {
            if (volume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
            {
                Debug.Log("ColorAdjustments 연결 성공!");
            }
            else
            {
                Debug.LogError("Volume Profile에 Color Adjustments가 없습니다!");
            }
        }
    }

    void Update()
    {
        UpdateProgress();
        UpdateVisualEffect();
    }

    private void UpdateProgress()
    {
        // 1분(stageDuration) 동안 시간이 흐름에 따라 게이지 상승
        if (elapsedTime < stageDuration)
        {
            elapsedTime += Time.deltaTime;

            // 현재 경과 시간을 0~100 사이의 진행도로 변환
            colorGauge = Mathf.Clamp((elapsedTime / stageDuration) * 100f, 0f, 100f);
        }
    }

    private void UpdateVisualEffect()
    {
        if (colorAdjustments == null) return;

        // colorGauge(0~100)를 0~1 비율로 변환
        float normalizedGauge = colorGauge / 100f;

        // Saturation: -100(흑백) -> 0(컬러)
        float saturation = Mathf.Lerp(-100f, 0f, normalizedGauge);
        colorAdjustments.saturation.value = saturation;

        // 선택 사항: 진행도에 따라 밝기나 대조를 추가로 조절하고 싶다면 여기에 작성
    }

    // --- 외부 호출 함수 (Interaction) ---

    /// <summary>
    /// 장애물 충돌 시 호출. 게이지를 10 감소시킵니다. (시간 10% 페널티)
    /// </summary>
    public void DecreaseGaugeOnHit()
    {
        // 게이지 10 감소는 전체 시간의 10% 페널티와 같음
        float penaltyTime = stageDuration * 0.1f;
        elapsedTime = Mathf.Max(0f, elapsedTime - penaltyTime);

        // 즉시 게이지 계산 반영
        colorGauge = Mathf.Clamp((elapsedTime / stageDuration) * 100f, 0f, 100f);

        if (ComboManager.Instance != null)
            ComboManager.Instance.ResetCombo();

        Debug.Log("피격: 게이지 10 감소!");
    }

    /// <summary>
    /// 기존 콤보 시스템과의 에러 방지를 위한 함수
    /// </summary>
    public void SetComboGraceState(bool isActive)
    {
        isComboActive = isActive;
    }

    /// <summary>
    /// 게임 오버 시 화면을 완전히 컬러로 바꿈
    /// </summary>
    public void SetGameOverGauge()
    {
        elapsedTime = stageDuration;
        colorGauge = 100f;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}