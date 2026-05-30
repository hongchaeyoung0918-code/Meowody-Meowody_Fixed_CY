using UnityEngine;
using UnityEngine.UI;

public class LT_SettingsManager : MonoBehaviour
{
    // 일단 싱글톤 설정, 나중에 다른 세팅 스크립트랑 합칠듯
    public static LT_SettingsManager Instance { get; private set; }

    [Header("UI Components")]
    [Tooltip("모션 전환 켜기/끄기 토글 UI")]
    public Toggle transitionToggle;

    [Header("Settings State")]
    [Tooltip("체크 시 기존처럼 부드러운 전환, 해제 시 쿠키런처럼 즉시 전환")]
    public bool useSmoothTransitions = true;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            // 씬이 넘어가도 설정을 유지하고 싶다면 아래 주석을 해제하세요.
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 토글 UI 초기화 및 이벤트 연결
        if (transitionToggle != null)
        {
            transitionToggle.isOn = useSmoothTransitions;
            transitionToggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }

    private void OnToggleValueChanged(bool isOn)
    {
        useSmoothTransitions = isOn;
        Debug.Log($"애니메이션 부드러운 전환 사용 여부: {isOn}");
    }
}
