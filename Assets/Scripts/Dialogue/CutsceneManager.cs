using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    [Header("Video Cutscene")]
    public GameObject videoPanel;
    public RawImage videoDisplay;
    public VideoPlayer videoPlayer;

    [Header("Image Cutscene (레거시 / 필요 시 사용)")]
    public GameObject cutscenePanel;
    public Image cutsceneImage;

    [Header("Fade")]
    public Image fadePanel;

    private Action _onComplete;
    private bool _skippable;
    private bool _isPlaying;

    [SerializeField] private DialogueManager _dialogueManager;

    private void Awake()
    {
        _dialogueManager = _dialogueManager ?? FindFirstObjectByType<DialogueManager>();
        if (videoPanel != null) videoPanel.SetActive(false);
        if (cutscenePanel != null) cutscenePanel.SetActive(false);
        if (fadePanel != null) fadePanel.gameObject.SetActive(false);
    }

    // ───────────────────────────────────────────
    // 외부 호출 진입점
    // ───────────────────────────────────────────

    /// <summary>mp4 영상 컷씬 재생</summary>
    public void PlayVideo(string videoFileName, bool skippable, Action onComplete)
    {
        _onComplete = onComplete;
        _skippable = skippable;
        StartCoroutine(VideoRoutine(videoFileName));
    }

    /// <summary>이미지 컷씬 재생 (기존 방식 유지)</summary>
    public void PlayImage(ImageCutsceneConfig config, Action onComplete)
    {
        _onComplete = onComplete;
        _skippable = true;
        StartCoroutine(ImageCutsceneRoutine(config));
    }

    /// <summary>플레이어가 컷씬 클릭 시 스킵</summary>
    public void OnCutsceneClicked()
    {
        if (!_isPlaying || !_skippable) return;
        StopAllCoroutines();
        CleanUp();
        Complete();
    }

    // ───────────────────────────────────────────
    // 영상 컷씬
    // ───────────────────────────────────────────

    private IEnumerator VideoRoutine(string videoFileName)
    {
        _isPlaying = true;

        string path;
#if UNITY_ANDROID && !UNITY_EDITOR
    path = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
#else
        path = "file://" + System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
#endif

        videoPlayer.url = path;
        videoPlayer.Prepare();

        // 영상 재생 전 대화창 숨기기
        if (_dialogueManager != null) _dialogueManager.HideDialogue();
        videoPanel.SetActive(true);

        while (!videoPlayer.isPrepared)
            yield return null;

        videoPlayer.Play();

        while (videoPlayer.isPlaying)
            yield return null;

        CleanUp();
        Complete();  // → SequenceRunner.ProcessNext() 호출
                     // → 다음 DIALOGUE 이벤트에서 dialoguePanel이 다시 켜짐
    }

    // ───────────────────────────────────────────
    // 이미지 컷씬 (DialogueManager에서 이전한 로직)
    // ───────────────────────────────────────────

    private IEnumerator ImageCutsceneRoutine(ImageCutsceneConfig config)
    {
        _isPlaying = true;

        // 스프라이트 로드
        Sprite sprite = Resources.Load<Sprite>(config.imageName);
        if (sprite == null)
        {
            Debug.LogError($"[CutsceneManager] 이미지 없음: {config.imageName}");
            Complete();
            yield break;
        }

        cutsceneImage.sprite = sprite;

        // 초기 alpha 0
        SetAlpha(cutsceneImage, 0f);
        cutscenePanel.SetActive(true);

        // 페이드 인
        if (config.fadeFromBlackOnStart && fadePanel != null)
        {
            yield return StartCoroutine(FadeToBlack(config.fadeInDuration));
            SetAlpha(cutsceneImage, 1f);
            yield return StartCoroutine(FadeFromBlack(config.fadeOutDuration));
        }
        else
        {
            yield return StartCoroutine(FadeImage(cutsceneImage, 0f, 1f, config.fadeInDuration));
        }

        // 표시 유지
        yield return new WaitForSeconds(config.displayDuration);

        // 페이드 아웃
        if (config.fadeToBlackOnEnd && fadePanel != null)
        {
            yield return StartCoroutine(FadeToBlack(config.fadeOutDuration));
        }
        else
        {
            yield return StartCoroutine(FadeImage(cutsceneImage, 1f, 0f, config.fadeOutDuration));
        }

        cutscenePanel.SetActive(false);

        // fadeToBlackOnEnd 이후 빠른 복귀
        if (config.fadeToBlackOnEnd && fadePanel != null)
        {
            yield return StartCoroutine(FadeFromBlack(0.1f));
            fadePanel.gameObject.SetActive(false);
        }

        _isPlaying = false;
        Complete();
    }

    // ───────────────────────────────────────────
    // 페이드 헬퍼 (DialogueManager에서 이전)
    // ───────────────────────────────────────────

    public IEnumerator FadeToBlack(float duration)
    {
        if (fadePanel == null) yield break;
        fadePanel.gameObject.SetActive(true);
        yield return StartCoroutine(FadeImage(fadePanel, 0f, 1f, duration));
    }

    public IEnumerator FadeFromBlack(float duration)
    {
        if (fadePanel == null) yield break;
        yield return StartCoroutine(FadeImage(fadePanel, 1f, 0f, duration));
    }

    public IEnumerator FadeImage(Image image, float from, float to, float duration)
    {
        image.gameObject.SetActive(true);
        float elapsed = 0f;
        SetAlpha(image, from);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(image, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetAlpha(image, to);
        if (to <= 0f) image.gameObject.SetActive(false);
    }

    // ───────────────────────────────────────────
    // 내부 유틸
    // ───────────────────────────────────────────

    private void CleanUp()
    {
        _isPlaying = false;
        if (videoPanel != null) videoPanel.SetActive(false);
        if (videoPlayer != null) videoPlayer.Stop();
        if (cutscenePanel != null) cutscenePanel.SetActive(false);
    }

    private void Complete()
    {
        _onComplete?.Invoke();
        _onComplete = null;
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }
}

/// <summary>이미지 컷씬 설정값 (SequenceRunner → CutsceneManager 전달용)</summary>
[System.Serializable]
public class ImageCutsceneConfig
{
    public string imageName;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public float displayDuration = 2.0f;
    public bool fadeFromBlackOnStart = false;
    public bool fadeToBlackOnEnd = false;
}