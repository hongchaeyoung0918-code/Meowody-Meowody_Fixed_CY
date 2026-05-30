using UnityEngine;
using System.Collections;

public class ObjectSwitcher : MonoBehaviour
{
    public GameObject[] originalObjects;
    public GameObject[] matchedObjects;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        for (int i = 0; i < originalObjects.Length; i++)
        {
            if (collision.gameObject == originalObjects[i])
            {
                // 1. 시각적 교체
                originalObjects[i].SetActive(false);
                matchedObjects[i].SetActive(true);

                // ---------------------------------------------------------
                // ★ 2. 점수 추가 로직 (데이터 반영) ★
                // ---------------------------------------------------------
                if (ScoreManager.Instance != null)
                {
                    // 팬을 만났을 때 점수를 올리는 함수 호출
                    ScoreManager.Instance.AddHandshake();
                }
                // ---------------------------------------------------------

                // 3. 애니메이션 실행
                StartCoroutine(ScaleBounce(matchedObjects[i].transform));
                break; // 찾았으면 루프 탈출
            }
        }
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