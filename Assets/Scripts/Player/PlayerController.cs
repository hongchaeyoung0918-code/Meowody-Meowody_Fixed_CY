using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Jump Settings")]
    public int maxJumpCount = 1; //더블 점프 비활성화
    private int currentJumpCount = 0;

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
        HandleNoteShoot();


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

    void ProcessFailure()
    {
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
                uiManager.ShowGameOver();
            }
        }
        else
        {
            Respawn();
        }
    }

    public void ProcessFailureFromCitizenCollision()
    {
        // 이미 실패 처리 중이거나 게임 오버 상태가 아니며, respawnTimer가 0일 때만 처리
        if (playerStats == null || isGameOver || isFailing || respawnTimer > 0f) return;

        // isFailing 플래그를 바로 설정하여 중복 호출 방지
        isFailing = true;

        // ProcessFailure() 로직을 직접 실행
        ProcessFailure();

        // 참고: ProcessFailure()가 Respawn()을 호출하면 isFailing은 다시 false가 됩니다.
    }

    public void ProcessFailureFromObstacle()
    {
        // 이미 실패 처리 중이거나 게임 오버 상태가 아니며, respawnTimer가 0일 때만 처리
        if (playerStats == null || isGameOver || isFailing || respawnTimer > 0f) return;

        // 실패 처리 로직 실행 (벽 충돌, 시민 충돌과 동일하게 HP 감소 및 리스폰)
        isFailing = true;
        ProcessFailure();

        Debug.Log("플레이어가 장애물에 부딪혀 실패했습니다.");
    }

    void Respawn()
    {

        rb.linearVelocity = Vector2.zero;
        currentMoveSpeed = moveSpeed;

        if (respawnPoint != null)
        {
            Vector3 spawnPosition = respawnPoint.position;
            transform.position = new Vector3(spawnPosition.x - 1.0f, spawnPosition.y, spawnPosition.z);

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
        bool jumpInput = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);

        if (!isSliding && jumpInput)
        {
            if (isGrounded) // 1. 땅에서 점프 (currentJumpCount 대신 isGrounded만 사용)
            {
                // 점프 직후 isGrounded = false가 되므로, 연속 점프를 막습니다.
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

                isGrounded = false;
                currentJumpCount = 1; // 점프 사용 플래그로 활용 (1: 사용함, 0: 땅에 닿아 리셋됨)
            }
        }
    }

    public void PerformAirJumpOnContact()
    {
        // 땅에 닿아있지 않을 때만 점프 (공중에 있을 때만 Orb Jump)
        if (!isGrounded)
        {
            // 1. 수직 속도 리셋
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            // 2. 점프 실행 (점프 높이는 jumpForce 변수와 동일)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // 3. 디버그 로그
            Debug.Log("Orb Jump 실행!");

            // currentJumpCount는 건드리지 않습니다. (땅에 닿아야 리셋됨)
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
                // NoteProjectile에 플레이어의 Rigidbody를 전달하여 현재 속도를 참고할 수 있게 할 수도 있습니다.
                // 여기서는 단순하게 일정한 속도로 발사하도록 구현합니다.
                noteProjectile.Launch(moveSpeed * 2.0f); // 이동 속도의 2배로 발사 (예시)
            }

            Debug.Log("음표 발사!");
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
