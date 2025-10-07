using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Jump Settings")]
    public int maxJumpCount = 2;
    private int currentJumpCount = 0;

    [Header("Slide Settings")]
    public float slideSpeedMultiplier = 1.5f;
    public float slideDuration = 0.5f;
    public float slideHeightScale = 0.5f;

    [Header("Collider Settings")]
    public float colliderHeightAdjustment = 0.5f;

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
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();

        currentMoveSpeed = moveSpeed;
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

    }

    void Update()
    {
        if (respawnTimer > 0f)
        {
            respawnTimer -= Time.deltaTime;
        }
        else
        {
            CheckForFailure(); // 타이머가 0 이하일 때만 실패 감지
        }

        CheckIfGrounded();

        HandleSlide();
        HandleJump();

        if (currentMoveSpeed > 0)
        {
            rb.linearVelocity = new Vector2(currentMoveSpeed, rb.linearVelocity.y);

        }
        else
        {
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

            Debug.Log($"플레이어 멈춤 감지 중... 타이머: {stopTimer:F2}s");

            if (stopTimer >= failCheckTime) // failCheckTime = 0.2f
            {
                Debug.Log("플레이어 실패 감지됨!");
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

    void ProcessFailure()
    {
        Debug.Log("플레이어 실패 처리 시작");

        rb.linearVelocity = Vector2.zero;
        currentMoveSpeed = 0f;

        playerStats.HP--;

        Debug.Log($"플레이어 실패! 남은 HP: {playerStats.HP}");

        if (playerStats.HP <= 0)
        {
            isGameOver = true;
            currentMoveSpeed = 0f;
            enabled = false;

            if (uiManager != null)
            {
                uiManager.ShowGameOver(); // Game Over UI 호출
            }
        }
        else
        {
            Respawn(); // 4. HP가 남아있으면 리스폰
        }
    }

    void Respawn()
    {
        Debug.Log("플레이어 리스폰");

        rb.linearVelocity = Vector2.zero;
        currentMoveSpeed = moveSpeed;

        if (respawnPoint != null)
        {
            Vector3 spawnPosition = respawnPoint.position;
            transform.position = new Vector3(spawnPosition.x - 1.0f, spawnPosition.y, spawnPosition.z);

            Debug.Log($"리스폰 위치: {transform.position}");

            if (isSliding)
            {
                EndSlide();
            }
        }
        else
        {
            Debug.LogError("리스폰 지점이 설정되지 않았습니다! 리스폰 불가.");
        }
        
        isFailing = false;
        respawnTimer = respawnGraceTime; // 리스폰 무적 시간 설정

        currentJumpCount = 0;
    }


    void HandleJump()
    {
        if (!isSliding && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && currentJumpCount < maxJumpCount)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            currentJumpCount++;
            isGrounded = false;
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

        currentMoveSpeed = moveSpeed * slideSpeedMultiplier;

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

        currentMoveSpeed = moveSpeed;

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


    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            //isGrounded = true;
            //currentJumpCount = 0;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
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
