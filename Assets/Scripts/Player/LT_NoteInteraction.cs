using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// 노트 수집 처리 (오브젝트 스위칭, 바운스 애니메이션, 점수 텍스트, 음악 트리거)
/// TestPlayer 프리팹에 LT_PlayerController_v2와 함께 추가합니다.
/// </summary>
public class LT_NoteInteraction : MonoBehaviour
{
    [Header("--- Object Switcher ---")]
    public GameObject[] originalObjects;
    public GameObject[] matchedObjects;

    [Tooltip("자식 오브젝트 중 텍스트를 찾을 태그")]
    public string scoreTextTag = "ScoreText";

    [Header("--- Music Trigger ---")]
    public GameObject auraEffect;
    public float detectDistance = 6f;

    private Animator anim;
    private readonly int HashGuitar = Animator.StringToHash("IsGuitar");
    private bool isPlayingMusic = false;
    private int activeSwitchCount = 0;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (auraEffect != null) auraEffect.SetActive(false);
    }

    private void Update()
    {
        CheckDistanceToOriginalObjects();
    }

    // =========================================================================
    //  Trigger (근처 감지 → 스페이스바로 스위칭)
    // =========================================================================

    /// <summary>현재 근처에 있는 originalObject의 인덱스 (-1이면 없음)</summary>
    private int nearbyNoteIndex = -1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        for (int i = 0; i < originalObjects.Length; i++)
        {
            if (other.gameObject == originalObjects[i])
            {
                nearbyNoteIndex = i;
                return;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyNoteIndex >= 0 && nearbyNoteIndex < originalObjects.Length
            && other.gameObject == originalObjects[nearbyNoteIndex])
        {
            nearbyNoteIndex = -1;
        }
    }

    /// <summary>
    /// 스페이스바 입력 시 LT_PlayerController_v2에서 호출합니다.
    /// 근처에 originalObject가 있으면 스위칭을 수행합니다.
    /// </summary>
    public bool TrySwitchNote()
    {
        if (nearbyNoteIndex < 0) return false;

        int i = nearbyNoteIndex;
        nearbyNoteIndex = -1;

        originalObjects[i].SetActive(false);
        matchedObjects[i].SetActive(true);

        GameObject textObj = FindChildWithTag(matchedObjects[i].transform, scoreTextTag);
        if (textObj != null) textObj.SetActive(true);

        StartCoroutine(ScaleBounceAndScoreRoutine(matchedObjects[i].transform, textObj));
        MelodySection.NotifyNoteCollected(originalObjects[i]);
        return true;
    }

    // =========================================================================
    //  Music Trigger (거리 기반)
    // =========================================================================

    private void CheckDistanceToOriginalObjects()
    {
        if (activeSwitchCount > 0) return;

        bool isClose = false;
        foreach (var obj in originalObjects)
        {
            if (obj != null && obj.activeSelf
                && Vector2.Distance(transform.position, obj.transform.position) <= detectDistance)
            {
                isClose = true;
                break;
            }
        }

        if (isClose && !isPlayingMusic)
            TriggerMusic();
        else if (!isClose && isPlayingMusic)
            ResetMusicState();
    }

    private void TriggerMusic()
    {
        isPlayingMusic = true;
        auraEffect?.SetActive(true);
        anim?.SetTrigger(HashGuitar);
    }

    private void ResetMusicState()
    {
        isPlayingMusic = false;
        auraEffect?.SetActive(false);
    }

    // =========================================================================
    //  VFX Coroutines
    // =========================================================================

    private IEnumerator ScaleBounceAndScoreRoutine(Transform target, GameObject textObj)
    {
        activeSwitchCount++;

        // 바운스 애니메이션
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 baseScale = new Vector3(0.2f, 0.2f, 0.2f);
        Vector3 peakScale = baseScale * 1.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeOut = 1f - Mathf.Pow(1f - t, 2f);
            float bounce = Mathf.Sin(t * Mathf.PI * 4f) * 0.05f;

            if (target != null)
                target.localScale = Vector3.Lerp(baseScale, peakScale, easeOut) + Vector3.one * bounce;

            yield return null;
        }

        if (target != null) target.localScale = baseScale;

        // 점수 텍스트 페이드아웃
        if (textObj != null)
        {
            float floatDuration = 0.5f;
            float floatElapsed = 0f;
            Vector3 startPos = textObj.transform.localPosition;
            Vector3 endPos = startPos + Vector3.up * 1.0f;

            TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
            TextMeshProUGUI tmpUI = textObj.GetComponent<TextMeshProUGUI>();
            CanvasGroup group = textObj.GetComponent<CanvasGroup>();

            while (floatElapsed < floatDuration)
            {
                floatElapsed += Time.deltaTime;
                float t = floatElapsed / floatDuration;
                textObj.transform.localPosition = Vector3.Lerp(startPos, endPos, t);

                float alpha = 1f - t;
                if (group != null)
                {
                    group.alpha = alpha;
                }
                else
                {
                    if (tmp != null) { Color c = tmp.color; c.a = alpha; tmp.color = c; }
                    if (tmpUI != null) { Color c = tmpUI.color; c.a = alpha; tmpUI.color = c; }
                }

                yield return null;
            }

            textObj.SetActive(false);
            textObj.transform.localPosition = startPos;
        }

        activeSwitchCount--;
        if (activeSwitchCount == 0) ResetMusicState();
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private GameObject FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
                return child.gameObject;
        }
        return null;
    }
}
