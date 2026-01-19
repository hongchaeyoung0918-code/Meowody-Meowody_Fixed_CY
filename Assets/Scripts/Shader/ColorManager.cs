using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class ColorManager : MonoBehaviour
{
    [Header("Stage Progress Settings")]
    public float stageDuration = 60f;
    private float elapsedTime = 0f;

    [Header("Post Processing Settings")]
    public Volume volume;
    private ColorAdjustments colorAdjustments;

    [Header("Current State")]
    [Range(0f, 100f)]
    public float colorGauge = 0f;

    // 제어 플래그
    private bool isGameActive = false;   // 대화/일시정지 중 여부
    private bool isComboActive = false; // 콤보 유지 여부 (ComboManager 연동)

    public static ColorManager Instance;

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
        isGameActive = false;
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
        if (volume == null) volume = FindFirstObjectByType<Volume>();
        if (volume != null) volume.profile.TryGet(out colorAdjustments);
    }

    void Update()
    {
        // 대화 중이 아니고 게임이 활성화 상태일 때만 업데이트
        if (isGameActive)
        {
            UpdateProgress();
            UpdateVisualEffect();
        }
    }

    private void UpdateProgress()
    {
        if (elapsedTime < stageDuration)
        {
            elapsedTime += Time.deltaTime;
            colorGauge = Mathf.Clamp((elapsedTime / stageDuration) * 100f, 0f, 100f);
        }
    }

    private void UpdateVisualEffect()
    {
        if (colorAdjustments == null) return;
        float normalizedGauge = colorGauge / 100f;
        float saturation = Mathf.Lerp(-100f, 0f, normalizedGauge);
        colorAdjustments.saturation.value = saturation;
    }

    // --- 외부 연동 함수들 ---

    // MainUIManager에서 호출 (대화 중 게이지 멈춤)
    public void SetColorUpdateActive(bool isActive)
    {
        isGameActive = isActive;
    }

    // ComboManager에서 호출 (에러 해결 포인트)
    public void SetComboGraceState(bool isActive)
    {
        isComboActive = isActive;
    }

    public void DecreaseGaugeOnHit()
    {
        float penaltyTime = stageDuration * 0.1f;
        elapsedTime = Mathf.Max(0f, elapsedTime - penaltyTime);
        colorGauge = Mathf.Clamp((elapsedTime / stageDuration) * 100f, 0f, 100f);

        if (ComboManager.Instance != null) ComboManager.Instance.ResetCombo();
    }

    public void SetGameOverGauge()
    {
        elapsedTime = stageDuration;
        colorGauge = 100f;
        UpdateVisualEffect();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}