using UnityEngine;
using System.Collections;

public class ObjectSwitcher : MonoBehaviour
{
    public GameObject[] originalObjects;
    public GameObject[] matchedObjects;

    /// <summary>현재 근처에 있는 originalObject의 인덱스 (-1이면 없음)</summary>
    private int nearbyObjectIndex = -1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        for (int i = 0; i < originalObjects.Length; i++)
        {
            if (collision.gameObject == originalObjects[i])
            {
                nearbyObjectIndex = i;
                break;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (nearbyObjectIndex >= 0 && nearbyObjectIndex < originalObjects.Length
            && collision.gameObject == originalObjects[nearbyObjectIndex])
        {
            nearbyObjectIndex = -1;
        }
    }

    /// <summary>
    /// 스페이스바 입력 시 LT_PlayerController_v2에서 호출합니다.
    /// 근처에 originalObject가 있으면 스위칭을 수행합니다.
    /// </summary>
    public bool TrySwitchObject()
    {
        if (nearbyObjectIndex < 0) return false;

        int i = nearbyObjectIndex;
        nearbyObjectIndex = -1;

        originalObjects[i].SetActive(false);
        matchedObjects[i].SetActive(true);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddHandshake();

        StartCoroutine(ScaleBounce(matchedObjects[i].transform));
        return true;
    }

    private IEnumerator ScaleBounce(Transform target)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 peakScale = Vector3.one * 1.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeOut = 1f - Mathf.Pow(1f - t, 2f);
            float bounce = Mathf.Sin(t * Mathf.PI * 4f) * 0.05f;

            if (target != null)
                target.localScale = Vector3.Lerp(startScale, peakScale, easeOut) + Vector3.one * bounce;

            yield return null;
        }

        if (target != null) target.localScale = Vector3.one;
    }
}