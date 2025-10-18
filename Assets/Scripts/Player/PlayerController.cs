using UnityEngine;
using System.Collections;
using TMPro.Examples;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public int maxJumpCount = 1; //더블 점프 비활성화
    private int currentJumpCount = 0;

    public float trampolineJumpForce = 15f;

    [Header("Invincibility Settings")]
    public float invincibilityDuration = 2.0f; //피격 후 무적
    private bool isInvincible = false;

    [Header("Slide Settings")]
    public float slideSpeedMultiplier = 1.5f;
    public float slideDuration = 0.5f;
    public float slideHeightScale = 0.5f;

    [Header("Collider Settings")]
    public float colliderHeightAdjustment = 0.5f;

    [Header("Note Attack Settings")]
    public GameObject notePrefab;     // 유니티에서 지정할 음표 프리팹
    public float noteSpawnOffset = 0.8f;
    public float noteSpawnHeight = 0.5f;

    [Header("Ground Check Settings")]
    public float groundCheckDistance = 0.1f; // 땅을 감지할 거리 (작을수록 정확)
    public LayerMask groundLayer;

    [Header("Wall Check Settings")]
    public float wallCheckDistance = 0.15f;

    [Header("Game State")] 
    public Transform respawnPoint;

    [Header("Respawn Settings")]
    public float respawnGraceTime = 0.5f; // 리스폰 후 실패 감지 무시 시간 (0.5초)
    private float respawnTimer = 0f; // 리스폰 무적 타이머

    private float failCheckTime = 0.2f; // 멈춤 감지 시간
    private float stopTimer = 0f;
    private bool isGameOver = false;

    private float currentMoveSpeed;
    private bool isGrounded = false;
    private bool isSliding = false;
    private float slideTimer = 0f;

    private Rigidbody2D rb;
    private PlayerStats playerStats;
    private MainUIManager uiManager;

    private CapsuleCollider2D capsuleCollider;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    private Vector3 originalScale;

    private bool isFailing = false;

    private float initialXPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();

        originalScale = transform.localScale;

        if(capsuleCollider != null)
        {
            originalColliderSize = capsuleCollider.size;
            originalColliderOffset = capsuleCollider.offset;
            wallCheckDistance = (capsuleCollider.size.x / 2f) + 0.05f; // 0.05f는 미세한 여유 공간
        }
        else
        {
            Debug.LogError("PlayerController requires a CapsuleCollider2D component on the same GameObject.");
        }

        if (rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody2D component on the same GameObject.");
            enabled = false;
        }

        uiManager = FindFirstObjectByType<MainUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("MainUIManager를 씬에서 찾을 수 없습니다! MainScene에 배치했는지 확인하세요.");
        }

        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats를 씬에서 찾을 수 없습니다!");
            enabled = false;
        }

        //위치 보정
        Vector3 startPosition = transform.position;
        transform.position = new Vector3(startPosition.x, 0f, startPosition.z);

        initialXPosition = transform.position.x;
    }

    void Update()
    {
        if (!isInvincible)
        {
            CheckForFailure();
        }

        CheckIfGrounded();

        HandleSlide();
        HandleJump();
        HandleNoteShoot();

        if (!isGameOver)
        {
            transform.position = new Vector3(
            initialXPosition,
            transform.position.y,
            transform.position.z
            );

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            // 게임 오버 시 완전히 멈춤
            rb.linearVelocity = Vector2.zero;
        }
    }

    void CheckIfGrounded()
    {
        Vector2 raycastOrigin = capsuleCollider.bounds.center;

        raycastOrigin.y = capsuleCollider.bounds.min.y;

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.down, groundCheckDistance, groundLayer);

        Debug.DrawRay(raycastOrigin, Vector2.down * groundCheckDistance, hit.collider != null ? Color.green : Color.red);

        if (hit.collider != null)
        {
            if (!isGrounded)
            {
                currentJumpCount = 0;
            }
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    void CheckForFailure()
    {
        if (respawnTimer > 0f) return;

        if (playerStats == null || isGameOver || isFailing) return;

        Vector2 wallRaycastOrigin = capsuleCollider.bounds.center;

        RaycastHit2D wallHit = Physics2D.Raycast(wallRaycastOrigin, Vector2.right, wallCheckDistance, groundLayer);

        // 디버그용 라인
        Debug.DrawRay(wallRaycastOrigin, Vector2.right * wallCheckDistance, wallHit.collider != null ? Color.blue : Color.yellow);

        bool isStuck = isGrounded && wallHit.collider != null;

        if (isStuck) // (속도 조건 제거 버전 사용)
        {
            stopTimer += Time.deltaTime;

            if (stopTimer >= failCheckTime) // failCheckTime = 0.2f
            {
                isFailing = true;
                ProcessFailure();
                stopTimer = 0f;
            }
        }
        else
        {
            stopTimer = 0f;
        }
    }

    // ProcessFailure() 함수를 무적 로직에 맞춰 수정
    void ProcessFailure()
    {
        // 이미 무적 상태이면 피격 무시
        if (isInvincible) return;

        playerStats.HP--;
        Debug.Log($"플레이어 피격! 남은 HP: {playerStats.HP}");

        if (playerStats.HP <= 0)
        {
            isGameOver = true;
            currentMoveSpeed = 0f;
            enabled = false;
            // UI Manager Game Over 호출
            if (uiManager != null) uiManager.ShowGameOver();
        }
        else
        {
            // HP가 남았다면 무적 상태로 전환
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    // ProcessFailureFromCitizenCollision() 함수를 ProcessFailure()로 연결
    public void ProcessFailureFromCitizenCollision()
    {
        // 시민 충돌은 벽에 박는 것과 동일하게 처리하되, isInvincible을 확인해야 합니다.
        if (isInvincible || isGameOver) return;

        ProcessFailure();

        Debug.Log("시민과 충돌로 인한 피격 처리 완료.");
    }

    // ProcessFailureFromObstacle() 함수 (일반 장애물)를 ProcessFailure()로 연결
    public void ProcessFailureFromObstacle()
    {
        // 일반 장애물 충돌 처리
        if (isInvincible || isGameOver) return;

        ProcessFailure();

        Debug.Log("장애물과 충돌로 인한 피격 처리 완료.");
    }

    // 무적 상태 코루틴
    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        Debug.Log("무적 상태 시작!");

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        //Collider2D col = GetComponent<Collider2D>(); // 일반 콜라이더 (벽 충돌용)

        // 깜빡임 효과 (선택 사항)
        if (sr != null)
        {
            for (float t = 0; t < invincibilityDuration; t += 0.15f) // 0.15초 간격으로 깜빡임
            {
                sr.enabled = !sr.enabled;
                yield return new WaitForSeconds(0.075f);
            }
            sr.enabled = true; // 무적 종료 후 다시 보이게 설정
        }

        isInvincible = false;
        Debug.Log("무적 상태 종료");

        currentJumpCount = 0;
    }


    void HandleJump()
    {
        bool jumpInput = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);

        if (!isSliding && jumpInput)
        {
            // 수정: isGrounded 일 때만 점프를 허용
            if (isGrounded && currentJumpCount == 0)
            {
                // 수직 속도 리셋 후 점프
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

                currentJumpCount = 1; // 점프 횟수 1로 설정 (공중에 있음을 표시)
                isGrounded = false;

                Debug.Log("일반 점프 실행.");
            }
        }
    }

    public void PerformAirJumpOnContact()
    {
        if (!isGrounded)
        {
            // 1. 수직 속도 리셋
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            // 2. 점프 실행 (점프 궤적을 갱신)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            Debug.Log("Jump Orb Jump 실행!");
        }
    }

    void HandleSlide()
    {
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && isGrounded && !isSliding)
        {
            StartSlide();
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            if (slideTimer <= 0 || (Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow)))
            {
                EndSlide();
            }
        }
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;

        //currentMoveSpeed = moveSpeed * slideSpeedMultiplier;

        transform.localScale = originalScale * slideHeightScale;

        if (capsuleCollider != null)
        {
            float newHeight = originalColliderSize.y * slideHeightScale;
            float newWidth = originalColliderSize.x * slideHeightScale;

            float heightDifference = originalColliderSize.y - newHeight;
            float yOffsetAdjustment = heightDifference / 2f;

            capsuleCollider.size = new Vector2(newWidth, newHeight);

            capsuleCollider.offset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - yOffsetAdjustment);


            if (rb != null)
            {
                rb.WakeUp();
            }
        }
    }

    void EndSlide()
    {
        isSliding = false;

        //currentMoveSpeed = moveSpeed;

        transform.localScale = originalScale;

        if (capsuleCollider != null)
        {
            capsuleCollider.size = originalColliderSize;
            capsuleCollider.offset = originalColliderOffset;

            if (rb != null)
            {
                rb.WakeUp();
            }
        }
    }

    void HandleNoteShoot()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (notePrefab == null)
            {
                Debug.LogError("Note Prefab이 설정되지 않았습니다!");
                return;
            }

            // 플레이어 오른쪽 (앞)에 음표 생성
            Vector3 spawnPosition = transform.position
                      + Vector3.right * noteSpawnOffset
                      + Vector3.up * noteSpawnHeight;

            // 음표 인스턴스화
            GameObject note = Instantiate(notePrefab, spawnPosition, Quaternion.identity);

            // NoteProjectile 스크립트에 발사 신호 전달
            NoteProjectile noteProjectile = note.GetComponent<NoteProjectile>();
            if (noteProjectile != null)
            {
                const float attackProjectileSpeed = 10.0f;
                noteProjectile.Launch(attackProjectileSpeed);
            }

            Debug.Log("음표 발사!");
        }
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
/*        if (collision.gameObject.CompareTag("Note_Obstacle"))
        {
            ProcessFailureFromObstacle();
            // 충돌한 장애물 제거 (옵션)
            Destroy(collision.gameObject);
        }*/

        if (collision.gameObject.CompareTag("Ground"))
        {
            if (!isGameOver)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Note_Obstacle"))
        {
            ProcessFailureFromObstacle();

            // 피격 후 노드 제거 (풀링에 반환)
            other.gameObject.SetActive(false);
            Destroy(other.gameObject);
            return;
        }

        if (other.gameObject.CompareTag("EndFlag"))
        {
            Debug.Log("Trigger with EndFlag");
            currentMoveSpeed = 0f;

            if (uiManager != null)
            {
                uiManager.ShowGameClear();
            }

            enabled = false;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
    }
}
