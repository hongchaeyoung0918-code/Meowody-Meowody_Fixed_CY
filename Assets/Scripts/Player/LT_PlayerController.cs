using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class LT_PlayerController : MonoBehaviour
{
    #region [1. Level Test & Physics Settings]
    [Tooltip("이동 속도")]
    public float moveSpeed = 10f;

    public float jumpHeight = 3.5f;
    public float timeToJumpApex = 0.45f;
    public float fallGravityMultiplier = 1.5f; // 낙하 속도 증가 비율

    [Tooltip("캐릭터 크기 스케일")]
    public Vector3 characterScale = Vector3.one;

    public float fallYThreshold = -8f; // 이 높이 아래로 떨어지면 게임 오버 처리

    [Header("--- Debug Info (Read Only) ---")]
    private float calculatedJumpDistance; // 점프 비거리
    private float calculatedGravityScale; // 중력 스케일
    private float calculatedJumpForce; // 점프 힘
    #endregion

    #region [2. Gameplay Settings]
    [Header("--- Action Settings ---")]
    public int maxJumpCount = 2; // 2단 점프
    private int currentJumpCount = 0;

    [Header("Slide Settings")]
    public float slideDuration = 0.5f; // 슬라이딩 지속시간 (키 떼면 취소? 시간제?)
    [Range(0.1f, 1f)]
    public float slideSizeMultiplier = 0.5f; // 슬라이딩 시 콜라이더 크기 비율

    [Header("Note Attack")]
    public GameObject notePrefab;
    public float noteSpawnOffset = 1.0f;
    public float noteSpawnHeight = 0.5f;
    public float noteLaunchSpeed = 15.0f;

    [Header("Collision & Health")]
    public LayerMask groundLayer;
    public float groundCheckSize = 0.05f; // 바닥 체크 박스 두께
    public float invincibilityDuration = 2.0f;
    #endregion

    #region [3. Components & State]
    // Components
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private AudioSource audioSource;
    private Animator anim;
    private SpriteRenderer[] renderers; // 깜빡임 효과용
    private LT_MainUIManager uiManager;
    private PlayerStats playerStats;

    // State
    private bool isGrounded;
    private bool isSliding;
    private bool isInvincible;
    private bool isGameOver;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    // fever mode
    private bool isFeverMode = false;
    private float baseMoveSpeed;
    private float baseJumpTime;
    private float currentFeverMultiplier = 1f;

    // Audio Clips (필요한 것만 남김)
    [Header("Audio")]
    public AudioClip jumpSound;
    public AudioClip slideSound;
    public AudioClip hitSound;
    public AudioClip GuitarSound;

    // Animation Parameters
    private readonly int HashRun = Animator.StringToHash("IsRunning");
    private readonly int HashJump = Animator.StringToHash("IsJumping");
    private readonly int HashSlide = Animator.StringToHash("IsSliding");
    private readonly int HashHit = Animator.StringToHash("IsHitting");
    private readonly int HashGuitar = Animator.StringToHash("IsGuitar");
    #endregion

    // 인스펙터 값이 바뀔 때마다 물리 수치 재계산 (레벨 디자인 핵심)
    private void OnValidate()
    {
        CalculatePhysics();
    }

    private void CalculatePhysics()
    {
        // h = 1/2 * g * t^2  =>  g = 2h / t^2
        float gravity = (2 * jumpHeight) / (timeToJumpApex * timeToJumpApex);

        // v = g * t
        calculatedJumpForce = gravity * timeToJumpApex;
        
        // GravityScale 적용
        // Default Gravity Y = -9.81
        float standardGravity = Mathf.Abs(Physics2D.gravity.y);
        if (standardGravity < 0.01f) standardGravity = 9.81f;
        calculatedGravityScale = gravity / standardGravity;

        // 점프 비거리 = 체공 시간 * 2 * 속도 -> 체공시간은 올라갈때 t, 내려갈때 t
        calculatedJumpDistance = moveSpeed * (timeToJumpApex * 2);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();
        anim = GetComponentInChildren<Animator>(); // 자식 오브젝트에 모델이 있는 경우 대비
        renderers = GetComponentsInChildren<SpriteRenderer>();

        originalColliderSize = col.size;
        originalColliderOffset = col.offset;

        uiManager = FindObjectOfType<LT_MainUIManager>();
        playerStats = FindAnyObjectByType<PlayerStats>();
    }

    void Start()
    {
        // 시작 시 물리 설정 적용
        baseMoveSpeed = moveSpeed;
        baseJumpTime = timeToJumpApex;
        CalculatePhysics();
        rb.gravityScale = calculatedGravityScale;

        // 캐릭터 크기 및 위치 초기화 (레벨 테스트용)
        transform.localScale = characterScale;

        // 게임 시작
        isGameOver = false;
    }

    void FixedUpdate()
    {
        if (isGameOver)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        CheckGround();
        CheckFall();
        ApplyCustomGravity();
        Move();
    }

    void Update()
    {
        if (isGameOver) return;

        // 입력은 반응성이 중요하므로 Update에서 처리
        HandleInput();
        UpdateAnimation();
    }

    private void Move()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);
    }

    private void HandleInput()
    {
        // 점프 (W, UpArrow)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryJump();
        }

        // 노트 발사 (D, RightArrow)
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            HandleNoteShoot();
        }

        // 슬라이드 (S, DownArrow)
        bool isDownPressed = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        if (isDownPressed && !isSliding && isGrounded)
        {
            StartSlide();
        }
        else if (!isDownPressed && isSliding)
        {
            EndSlide();
        }
    }

    private void TryJump()
    {
        if (isSliding) return; // 슬라이드 중 점프 불가

        if (isGrounded || currentJumpCount < maxJumpCount)
        {
            currentJumpCount++;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.linearVelocity += Vector2.up * calculatedJumpForce;

            PlaySound(jumpSound);

            anim?.Play("JUMP_start", 0, 0f);
        }
    }

    private void ApplyCustomGravity()
    {
        if (isGameOver) return;

        // 떨어지는 중
        if (!isGrounded && rb.linearVelocity.y < 0)
        {
            // 기본 중력 스케일에 낙하 배율을 곱하기
            rb.gravityScale = calculatedGravityScale * fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = calculatedGravityScale;
        }
    }

    private void StartSlide()
    {
        if (isSliding) return;
        isSliding = true;
        PlaySound(slideSound);

        // 발바닥 월드 좌표 고정
        float bottomY = originalColliderOffset.y - (originalColliderSize.y * 0.5f);
        float newHeight = originalColliderSize.y * slideSizeMultiplier;
        float newOffsetY = bottomY + (newHeight * 0.5f);

        col.size = new Vector2(originalColliderSize.x, newHeight);
        col.offset = new Vector2(originalColliderOffset.x, newOffsetY);

        // 바닥 체크 박스 강제 접지
        rb.position = new Vector2(rb.position.x, rb.position.y - 0.01f);

        anim?.SetBool(HashSlide, true);
        rb.WakeUp();
    }

    private void EndSlide()
    {
        if (!isSliding) return;
        isSliding = false;

        // 원상 복구
        col.size = originalColliderSize;
        col.offset = originalColliderOffset;

        rb.WakeUp();
    }

    private void HandleNoteShoot()
    {
        // 공중이거나 슬라이드 중이면 발사 불가
        if (!isGrounded || isSliding) return;
        if (notePrefab == null) return;

        anim?.SetTrigger(HashGuitar);
        PlaySound(GuitarSound);

        // 생성 위치
        Vector3 spawnPosition = transform.position
                              + (Vector3.right * noteSpawnOffset)
                              + (Vector3.up * noteSpawnHeight);

        // 생성
        GameObject note = Instantiate(notePrefab, spawnPosition, Quaternion.identity);

        NoteProjectile noteProjectile = note.GetComponent<NoteProjectile>();
        if (noteProjectile != null)
        {
            noteProjectile.Launch(noteLaunchSpeed);
        }
    }

    public void SetFeverMode(bool active, float multiplier = 1.5f)
    {
        if (isFeverMode == active) return;

        isFeverMode = active;
        currentFeverMultiplier = active ? multiplier : 1.0f;

        if (active)
        {
            moveSpeed = baseMoveSpeed * multiplier;

            timeToJumpApex = baseJumpTime / multiplier;

            if (anim != null) anim.speed = multiplier;

            Debug.Log($"Fever ON. Speed: x{multiplier}");
        }
        else
        {
            moveSpeed = baseMoveSpeed;
            timeToJumpApex = baseJumpTime;

            if (anim != null) anim.speed = 1.0f;

            Debug.Log("Fever OFF");
        }

        CalculatePhysics();
        if (rb != null) rb.gravityScale = calculatedGravityScale;
    }

    private void CheckGround()
    {
        // 박스 콜라이더의 가로 폭 90% 정도만 사용하여 모서리 걸림 방지
        Vector2 boxSize = new Vector2(col.size.x * 0.9f, groundCheckSize);
        Vector2 boxCenter = (Vector2)transform.position + col.offset + (Vector2.down * (col.size.y * 0.5f));

        // 체크 거리를 0.05f 정도로 주어 바닥 감지
        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, 0.05f, groundLayer);

        bool wasGrounded = isGrounded;
        isGrounded = hit.collider != null;

        if (!wasGrounded && isGrounded)
        {
            currentJumpCount = 0;
        }
    }

    private void CheckFall()
    {
        if (transform.position.y < fallYThreshold)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isGameOver) return;

        Debug.Log("Player Died (Fall or Hit)");
        isGameOver = true;

        // 물리 정지
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        if (uiManager != null)
        {
            uiManager.ShowGameOver();
        }
    }

    private void UpdateAnimation()
    {
        if (anim == null) return;

        // 슬라이드 중이면 무조건 슬라이드 애니메이션만
        if (isSliding)
        {
            anim.SetBool(HashSlide, true);
            anim.SetBool(HashRun, false);
            anim.SetBool(HashJump, false);
        }
        else
        {
            anim.SetBool(HashSlide, false);
            anim.SetBool(HashRun, isGrounded);
            anim.SetBool(HashJump, !isGrounded);
        }
    }

    public void OnHitObstacle()
    {
        if (isInvincible || isGameOver) return;

        if (playerStats != null)
        {
            playerStats.DecreaseHP(1);

            if (playerStats.HP <= 0)
            {
                Die();
                return;
            }
        }

        Debug.Log("Hit Obstacle!");

        anim?.SetTrigger(HashHit);
        PlaySound(hitSound);
        StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float timer = 0;
        while (timer < invincibilityDuration)
        {
            SetAlpha(0.5f);
            yield return new WaitForSeconds(0.1f);
            SetAlpha(1.0f);
            yield return new WaitForSeconds(0.1f);
            timer += 0.2f;
        }

        SetAlpha(1.0f);
        isInvincible = false;
    }

    private void SetAlpha(float alpha)
    {
        foreach (var sr in renderers)
        {
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Note_Obstacle_Persistent"))
        {
            OnHitObstacle();
        }
        else if (other.CompareTag("EndFlag"))
        {
            Debug.Log("Stage Clear!");
            isGameOver = true;
            uiManager.ShowGameClear();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    // 디버그용: 씬 뷰에서 바닥 체크 박스 그리기
    private void OnDrawGizmos()
    {
        if (col == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;

        // col.size.x * 0.9f → 직접 원하는 크기로 고정
        // 캐릭터 scale을 0.16 이런 식으로 하면서 크기 및 위치가 이상하게 보이나 물리에는 문제 없음
        // 나중에 구조 정리하면서 같이 정리 필요함
        Vector2 boxSize = new Vector2(0.5f, groundCheckSize); // ← 이 x값 조절
        Vector2 boxCenter = (Vector2)transform.position + col.offset + (Vector2.down * (col.size.y * 0.5f));
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}
