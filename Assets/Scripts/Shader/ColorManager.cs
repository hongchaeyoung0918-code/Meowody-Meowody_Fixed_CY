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

    // ���� �÷���
    private bool isGameActive = false;   // ��ȭ/�Ͻ����� �� ����
    private bool isComboActive = false; // �޺� ���� ���� (ComboManager ����)

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
            Destroy(this);
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
        // ��ȭ ���� �ƴϰ� ������ Ȱ��ȭ ������ ���� ������Ʈ
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

    // --- �ܺ� ���� �Լ��� ---

    // MainUIManager���� ȣ�� (��ȭ �� ������ ����)
    public void SetColorUpdateActive(bool isActive)
    {
        isGameActive = isActive;
    }

    // ComboManager���� ȣ�� (���� �ذ� ����Ʈ)
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