using UnityEngine;
using System.Collections;

public class SimplePlayerController : MonoBehaviour
{
    // 이동은 이 게임의 특성상 대부분 고정되어 있어 moveSpeed는 단순화
    public float moveSpeed = 5f;

    [Header("Jump & Double Jump")]
    public float jumpForce = 10f;
    public int maxJumpCount = 1;
    private int currentJumpCount = 0; // 이 변수는 땅에 닿았는지 확인하는 용도로만 사용됨

    [Header("Slide Settings")]
    public float slideDuration = 0.5f;
    public float slideHeightScale = 0.5f;

    [Header("Ground Check Settings")]
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer;

    [Header("Note Attack Settings")]
    public GameObject notePrefab;
    public float noteSpawnOffset = 0.8f;
    public float noteSpawnHeight = -5f;

    [Header("Animation Settings")]
    public Animator anim;

    private readonly string IsRunningParam = "IsRunning";
    private readonly string IsJumpingParam = "IsJumping";
    private readonly string IsSlidingParam = "IsSliding";
    private readonly string IsHittingParam = "IsHitting";
    private readonly string IsDeathParam = "IsDeath";
    private readonly string IsGuitarParam = "IsGuitar";

    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    private bool isGrounded = false;
    private bool isSliding = false;
    private float slideTimer = 0f;

    private float initialXPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();

        anim = GetComponentInChildren<Animator>();
        if (anim == null)
        {
            Debug.LogError("SimplePlayerController: Animator 컴포넌트가 플레이어나 자식 오브젝트에 없습니다!");
        }

        if (capsuleCollider != null)
        {
            originalColliderSize = capsuleCollider.size;
            originalColliderOffset = capsuleCollider.offset;
        }
        else
        {
            Debug.LogError("SimplePlayerController requires a CapsuleCollider2D.");
            enabled = false;
        }

        if (rb == null)
        {
            Debug.LogError("SimplePlayerController requires a Rigidbody2D.");
            enabled = false;
        }

        initialXPosition = transform.position.x;
        SetAnimationBool(IsRunningParam, true);
    }

    void Update()
    {
        CheckIfGrounded();

        HandleSlide();
        HandleJump();
        HandleNoteShoot();

        UpdateAnimationState();

        transform.position = new Vector3(
            initialXPosition,
            transform.position.y,
            transform.position.z
        );
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // --- Ground Check ---
    void CheckIfGrounded()
    {
        // 개선된 Raycast 원점: 콜라이더 하단 중앙에서 시작
        Vector2 raycastOrigin = new Vector2(
            capsuleCollider.bounds.center.x,
            capsuleCollider.bounds.min.y
        );

        // 레이캐스트 실행: 아래 방향으로 groundCheckDistance만큼 쏨
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.down, groundCheckDistance, groundLayer);

        // 디버그 시각화 (선택 사항)
        Debug.DrawRay(raycastOrigin, Vector2.down * groundCheckDistance, hit.collider != null ? Color.green : Color.red);

        if (hit.collider != null)
        {
            if (!isGrounded)
            {
                // 땅에 닿는 순간 속도를 0으로 만들어야 조작 씹힘을 줄일 수 있음
                // rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // 수직 속도를 0으로 설정

                // 점프 카운트 리셋
                currentJumpCount = 0;
            }
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    // --- Jump Logic (기본 점프) ---
    void HandleJump()
    {
        bool jumpInput = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);

        if (!isSliding && jumpInput)
        {
            // 수정: currentJumpCount가 0일 때 (즉, 땅에 닿았을 때)만 점프 허용
            if (isGrounded && currentJumpCount == 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

                currentJumpCount = 1; // 점프 횟수 1로 설정 (공중 상태)
                //isGrounded = false;
            }
        }
    }

    // --- Double Jump Orb Contact Logic ---
    // Orb 스크립트(JumpOrb.cs)에서 호출됨
    public void PerformAirJumpOnContact()
    {
        // 공중에서 Orb에 닿았을 때만 실행
        if (!isGrounded)
        {
            // 수정: currentJumpCount를 0으로 강제 리셋하여 점프 기회를 '재충전'
            // Orb를 밟았을 때만 점프 카운터를 리셋하고 점프를 실행하면,
            // 플레이어는 땅에서 1번, Orb를 밟을 때마다 1번씩 점프할 수 있게 됨
            currentJumpCount = 0;

            // 수직 속도 리셋 후 점프
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // Orb 점프도 하나의 점프이므로 카운트를 1로 설정
            currentJumpCount = 1;

            Debug.Log("Jump Orb Jump 실행! 점프 카운트 리셋 및 점프.");
        }
    }

    // --- Slide Logic ---
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

        SetAnimationBool(IsSlidingParam, true);

        if (capsuleCollider != null)
        {
            float newHeight = originalColliderSize.y * slideHeightScale;
            float heightDifference = originalColliderSize.y - newHeight;
            float yOffsetAdjustment = heightDifference / 2f;

            capsuleCollider.size = new Vector2(originalColliderSize.x, newHeight);
            capsuleCollider.offset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - yOffsetAdjustment);
        }
    }

    void EndSlide()
    {
        SetAnimationBool(IsSlidingParam, false);

        isSliding = false;

        if (capsuleCollider != null)
        {
            capsuleCollider.size = originalColliderSize;
            capsuleCollider.offset = originalColliderOffset;
        }
    }

    // --- Note Shoot Logic (애니메이션 및 NoteProjectile 처리) ---
    void HandleNoteShoot()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (notePrefab == null)
            {
                Debug.LogWarning("Note Prefab이 SimplePlayerController에 설정되지 않았습니다!");
                return;
            }

            SetAnimationTrigger(IsGuitarParam);

            // 플레이어 오른쪽 (앞)에 음표 생성
            Vector3 spawnPosition = transform.position
                             + Vector3.right * noteSpawnOffset
                             + Vector3.up * noteSpawnHeight;

            GameObject note = Instantiate(notePrefab, spawnPosition, Quaternion.identity);

            // PlayerController와 동일하게 NoteProjectile 컴포넌트 처리
            NoteProjectile noteProjectile = note.GetComponent<NoteProjectile>();
            if (noteProjectile != null)
            {
                const float attackProjectileSpeed = 10.0f;
                noteProjectile.Launch(attackProjectileSpeed);
                Debug.Log("SimplePlayerController: 음표 발사 및 NoteProjectile 초기화 완료!");
            }
            else
            {
                Debug.LogWarning("Note Prefab에 NoteProjectile 컴포넌트가 없습니다.");
            }
        }
    }


    // --- Animation Updates ---
    void UpdateAnimationState()
    {
        if (anim == null) return;

        bool inAir = !isGrounded && !isSliding;
        SetAnimationBool(IsJumpingParam, inAir);

        if (!isSliding && !inAir)
        {
            SetAnimationBool(IsRunningParam, true);
        }
    }

    void SetAnimationBool(string paramName, bool value)
    {
        if (anim != null)
        {
            anim.SetBool(paramName, value);
        }
    }

    void SetAnimationTrigger(string paramName)
    {
        if (anim != null)
        {
            anim.SetTrigger(paramName);
        }
    }

    // --- 외부 호출 가능한 애니메이션 트리거 함수 ---
    public void TriggerHitAnimation()
    {
        SetAnimationTrigger(IsHittingParam);
    }

    public void TriggerDeathAnimation()
    {
        SetAnimationTrigger(IsDeathParam);
    }


    // --- 콜라이더 이벤트 ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0 || collision.gameObject.CompareTag("Ground"))
        {
            // 땅에 닿는 순간 수직 속도(Y)를 0으로 설정하여 미끄러짐 방지
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            // 조작 씹힘 방지 핵심: 점프 카운트를 강제 리셋하여 다음 프레임에 점프 가능하게 함
            if (!isGrounded)
            {
                currentJumpCount = 0;
                isGrounded = true;
            }
        }
    }
}