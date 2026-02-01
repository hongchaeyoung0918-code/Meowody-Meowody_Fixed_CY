using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class LT_PlayerController : MonoBehaviour
{
    #region [1. Level Test & Physics Settings]
    [Tooltip("이동 속도")]
    public float moveSpeed = 10f;

    [Tooltip("점프 최대 높이")]
    public float jumpHeight = 3.5f;

    [Tooltip("점프 최고점 도달 시간(초)")]
    public float timeToJumpApex = 0.45f;

    [Tooltip("캐릭터 크기 스케일")]
    public Vector3 characterScale = Vector3.one;

    [Header("--- Debug Info (Read Only) ---")]
    [SerializeField, Tooltip("점프 비거리")]
    private float calculatedJumpDistance;
    [SerializeField, Tooltip("중력 계수")]
    private float calculatedGravityScale;
    [SerializeField, Tooltip("점프 힘")]
    private float calculatedJumpForce;
    #endregion

    #region [2. Gameplay Settings]
    [Header("--- Action Settings ---")]
    public int maxJumpCount = 2; // 쿠키런은 보통 2단 점프
    private int currentJumpCount = 0;

    [Header("Slide Settings")]
    public float slideDuration = 0.5f; // 슬라이딩 지속시간 (키 떼면 취소되게 할지, 시간제일지 결정 필요)
    public float slideSizeMultiplier = 0.5f; // 슬라이딩 시 크기 비율
    public float slideCenterOffsetY = -0.25f; // 콜라이더 위치 보정

    [Header("Collision & Health")]
    public LayerMask groundLayer;
    public float groundCheckSize = 0.05f; // 바닥 체크 박스 두께
    public float invincibilityDuration = 2.0f;
    #endregion

    #region [3. Components & State]
    // Components
    private Rigidbody2D rb;
    private CapsuleCollider2D col;
    private AudioSource audioSource;
    private Animator anim;
    private SpriteRenderer[] renderers; // 깜빡임 효과용

    // State
    private bool isGrounded;
    private bool isSliding;
    private bool isInvincible;
    private bool isGameOver;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    // Audio Clips (필요한 것만 남김)
    [Header("Audio")]
    public AudioClip jumpSound;
    public AudioClip slideSound;
    public AudioClip hitSound;

    // Animation Parameters
    private readonly int HashRun = Animator.StringToHash("IsRunning");
    private readonly int HashJump = Animator.StringToHash("IsJumping");
    private readonly int HashSlide = Animator.StringToHash("IsSliding");
    private readonly int HashHit = Animator.StringToHash("IsHitting");
    #endregion

    // 인스펙터 값이 바뀔 때마다 물리 수치 재계산 (레벨 디자인 핵심)
    private void OnValidate()
    {
        CalculatePhysics();
    }

    private void CalculatePhysics()
    {
        // 1. 중력 계산: h = 1/2 * g * t^2  =>  g = 2h / t^2
        float gravity = (2 * jumpHeight) / (timeToJumpApex * timeToJumpApex);

        // 2. 점프 힘 계산: v = g * t
        calculatedJumpForce = gravity * timeToJumpApex;

        // 3. Rigidbody의 GravityScale에 적용 (Physics2D.gravity.y는 보통 -9.81)
        // 유니티 중력 공식에 맞게 변환 (Default Gravity Y가 -9.81이라고 가정)
        float standardGravity = Mathf.Abs(Physics2D.gravity.y);
        calculatedGravityScale = gravity / standardGravity;

        // 4. 예상 점프 비거리 계산 (체공 시간 * 2 * 속도) -> 체공시간은 올라갈때 t, 내려갈때 t
        calculatedJumpDistance = moveSpeed * (timeToJumpApex * 2);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        audioSource = GetComponent<AudioSource>();
        anim = GetComponentInChildren<Animator>(); // 자식 오브젝트에 모델이 있는 경우 대비
        renderers = GetComponentsInChildren<SpriteRenderer>();

        originalColliderSize = col.size;
        originalColliderOffset = col.offset;
    }

    void Start()
    {
        // 시작 시 물리 설정 적용
        CalculatePhysics();
        rb.gravityScale = calculatedGravityScale;

        // 캐릭터 크기 및 위치 초기화 (레벨 테스트용)
        transform.localScale = characterScale;

        // 게임 시작
        isGameOver = false;
    }

    void Update()
    {
        if (isGameOver)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 1. 상태 체크
        CheckGround();

        // 2. 이동 (러너 게임은 등속 운동)
        Move();

        // 3. 입력 처리
        HandleInput();

        // 4. 애니메이션 업데이트
        UpdateAnimation();
    }

    private void Move()
    {
        // X축은 등속, Y축은 물리 엔진에 맡김
        rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);
    }

    private void HandleInput()
    {
        // 점프 (W, UpArrow)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryJump();
        }

        // 슬라이드 (S, DownArrow) - 누르고 있는 동안만 슬라이드
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
        if (isSliding) return; // 슬라이드 중 점프 불가 (기획에 따라 변경 가능)

        // 바닥에 있거나, 점프 횟수가 남아있을 때
        if (isGrounded || currentJumpCount < maxJumpCount)
        {
            currentJumpCount++;

            // 점프 힘 적용 (Y축 속도 초기화 후 적용하여 일정한 높이 보장)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.linearVelocity += Vector2.up * calculatedJumpForce;

            PlaySound(jumpSound);

            // 2단 점프 등 공중 점프 시 애니메이션 다시 트리거
            anim?.Play("Jump", 0, 0f);
        }
    }

    private void StartSlide()
    {
        isSliding = true;
        PlaySound(slideSound);

        // 콜라이더 크기 조절 (납작하게)
        col.size = new Vector2(originalColliderSize.x, originalColliderSize.y * slideSizeMultiplier);
        col.offset = new Vector2(originalColliderOffset.x, slideCenterOffsetY);
    }

    private void EndSlide()
    {
        isSliding = false;

        // 콜라이더 복구
        col.size = originalColliderSize;
        col.offset = originalColliderOffset;
    }

    private void CheckGround()
    {
        // BoxCast를 사용하여 발 밑을 넓게 체크 (Raycast보다 안정적)
        Vector2 boxSize = new Vector2(col.size.x * 0.9f, groundCheckSize);
        Vector2 boxCenter = (Vector2)transform.position + col.offset + (Vector2.down * (col.size.y * 0.5f));

        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, 0f, groundLayer);

        bool wasGrounded = isGrounded;
        isGrounded = hit.collider != null;

        // 착지 순간 처리
        if (!wasGrounded && isGrounded)
        {
            currentJumpCount = 0; // 점프 횟수 초기화
        }
    }

    private void UpdateAnimation()
    {
        if (anim == null) return;

        anim.SetBool(HashRun, isGrounded && !isSliding);
        anim.SetBool(HashJump, !isGrounded);
        anim.SetBool(HashSlide, isSliding);
    }

    // --- 충돌 및 게임 오버 처리 ---

    public void OnHitObstacle()
    {
        if (isInvincible || isGameOver) return;

        Debug.Log("Hit Obstacle!");
        // 여기에 체력 감소 로직 연결 (GameManager 등)

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
        // 장애물 태그 확인
        if (other.CompareTag("Obstacle"))
        {
            OnHitObstacle();
        }
        else if (other.CompareTag("EndFlag"))
        {
            Debug.Log("Stage Clear!");
            isGameOver = true;
            // GameManager.Instance.ClearGame(); // 게임 클리어 호출
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
        Vector2 boxSize = new Vector2(col.size.x * 0.9f, groundCheckSize);
        Vector2 boxCenter = (Vector2)transform.position + col.offset + (Vector2.down * (col.size.y * 0.5f));
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}
