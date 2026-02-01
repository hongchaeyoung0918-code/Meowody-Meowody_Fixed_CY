using UnityEngine;

public class FeverTrigger : MonoBehaviour
{
    private bool isTriggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player") || other.GetComponent<LT_PlayerController>() != null)
        {
            isTriggered = true;

            // FeverManager를 찾아서 피버 발동 요청
            FeverManager feverManager = FindFirstObjectByType<FeverManager>();
            if (feverManager != null)
            {
                feverManager.ActivateFeverByDistance(); // 거리(위치) 기반 피버 발동
            }

            // 역할 끝났으니 제거 (선택 사항)
            // Destroy(gameObject); 
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}