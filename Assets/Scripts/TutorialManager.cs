using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Tutorial UI Canvases")]
    public GameObject jumpTutorialCanvas;
    public GameObject slideTutorialCanvas;
    public GameObject jump2TutorialCanvas;

    [Header("Ingame UI")]
    public GameObject ingameUI;

    private bool isPaused = false;
    private string targetAction = ""; // "UP", "DOWN" 등을 저장
    private AudioSource gameAudio;

    // PlayerController에서 접근하기 위한 프로퍼티
    public string TargetAction => targetAction;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowTutorial(string type, AudioSource audio)
    {
        if (isPaused) return;

        gameAudio = audio;
        GameObject targetCanvas = null;

        switch (type)
        {
            case "SmallThorn":
            case "BigThorn":
                targetCanvas = jumpTutorialCanvas;
                targetAction = "UP";
                break;
            case "Slide":
                targetCanvas = slideTutorialCanvas;
                targetAction = "DOWN";
                break;
            case "DoubleJump":
                targetCanvas = jump2TutorialCanvas;
                targetAction = "UP";
                break;
        }

        if (targetCanvas != null)
        {
            StartPause(targetCanvas);
        }
    }

    private void StartPause(GameObject canvas)
    {
        isPaused = true;
        canvas.SetActive(true);
        if (ingameUI) ingameUI.SetActive(false);

        Time.timeScale = 0f; // 물리와 시간 정지
        if (gameAudio != null) gameAudio.Pause();
    }

    private void Update()
    {
        if (!isPaused) return;

        // PlayerController에서 실제 로직을 처리하게 하려면 
        // 여기서 Resume만 호출해 주는 것이 깔끔합니다.
        if (targetAction == "UP")
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                Resume();
        }
        else if (targetAction == "DOWN")
        {
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                Resume();
        }
    }

    public void Resume()
    {
        isPaused = false;
        targetAction = ""; // 액션 초기화

        if (jumpTutorialCanvas) jumpTutorialCanvas.SetActive(false);
        if (slideTutorialCanvas) slideTutorialCanvas.SetActive(false);
        if (jump2TutorialCanvas) jump2TutorialCanvas.SetActive(false);

        if (ingameUI) ingameUI.SetActive(true);

        Time.timeScale = 1f; // 게임 다시 시작
        if (gameAudio != null) gameAudio.UnPause();
    }

    public bool IsPaused() => isPaused;
}