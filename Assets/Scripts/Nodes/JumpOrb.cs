using UnityEngine;
using System.Collections;

public class JumpOrb : MonoBehaviour
{
    public float disableDuration = 0.5f;

    private Collider2D orbCollider;
    private SpriteRenderer spriteRenderer;
    private bool isUsed = false;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        orbCollider = GetComponent<Collider2D>();

        // Collider2D가 Is Trigger로 설정되어 있는지 확인 (충돌 시 튕기는 대신 통과하도록)
        if (orbCollider == null || !orbCollider.isTrigger)
        {
            Debug.LogWarning("JumpOrb requires a Collider2D with Is Trigger checked.");
        }

        orbCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isUsed)
        {
            //SimplePlayerController player = other.GetComponent<SimplePlayerController>();
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                // 1. 플레이어에게 즉시 점프 실행 요청
                player.PerformAirJumpOnContact();

                if (audioSource != null)
                {
                    audioSource.Play();
                    Debug.Log("jump 재생!");
                }

                // 2. 연속 사용 방지 코루틴 시작
                StartCoroutine(DisableForTime());
            }
        }
    }

    // 일정 시간 동안 오브젝트를 비활성화 (사라지지는 않음)
    IEnumerator DisableForTime()
    {
        isUsed = true;

        // 시각적으로 밟았음을 보여주기 위해 콜라이더 및 렌더러 비활성화
        if (orbCollider != null) orbCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(disableDuration);

        // 재활성화
        isUsed = false;
        if (orbCollider != null) orbCollider.enabled = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }
}
