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

    // --- [해금 상태 변수 추가] ---
    public bool canJump = false;
    public bool canSlide = false;
    public bool canDoubleJump = false;

    private bool isPaused = false;
    private string targetAction = "";
    private AudioSource gameAudio;

    public string TargetAction => targetAction;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 스테이지 1에서만 기능을 잠그고 나머지는 모두 해금
        if (GameSettings.SelectedStage == 1)
        {
            canJump = false;
            canSlide = false;
            canDoubleJump = false;
        }
        else
        {
            canJump = true;
            canSlide = true;
            canDoubleJump = true;
        }
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
            case "Jump2": // NoteManager의 타입과 일치시킴
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

        Time.timeScale = 0f;
        if (gameAudio != null) gameAudio.Pause();
    }

    private void Update()
    {
        if (!isPaused) return;

        // 튜토리얼 중 올바른 키를 눌렀을 때만 Resume 호출
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
        // --- [실제 기능 해금 처리] ---
        if (targetAction == "UP")
        {
            // 2단점프 캔버스가 켜져있었다면 2단점프 해금, 아니면 일반점프 해금
            if (jump2TutorialCanvas != null && jump2TutorialCanvas.activeSelf)
                canDoubleJump = true;
            else
                canJump = true;
        }
        else if (targetAction == "DOWN")
        {
            canSlide = true;
        }

        isPaused = false;
        targetAction = "";

        if (jumpTutorialCanvas) jumpTutorialCanvas.SetActive(false);
        if (slideTutorialCanvas) slideTutorialCanvas.SetActive(false);
        if (jump2TutorialCanvas) jump2TutorialCanvas.SetActive(false);

        if (ingameUI) ingameUI.SetActive(true);

        Time.timeScale = 1f;
        if (gameAudio != null) gameAudio.UnPause();
    }

    public bool IsPaused() => isPaused;
}