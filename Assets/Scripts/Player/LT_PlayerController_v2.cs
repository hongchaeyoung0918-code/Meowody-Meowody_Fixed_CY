using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

/// <summary>
/// 플레이어 이동, 입력, 전투, HP 관리, 애니메이션 담당.
/// 노트 수집은 LT_NoteInteraction에서 처리합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class LT_PlayerController_v2 : MonoBehaviour
{
    #region [1. Movement & Physics Settings]
    [Header("--- Movement & Physics ---")]
    public float moveSpeed = 10f;
    public float jumpHeight = 3.5f;
    public float timeToJumpApex = 0.45f;
    public float fallGravityMultiplier = 1.5f;
    public float fallYThreshold = -8f;

    [HideInInspector] private float calculatedGravityScale;
    [HideInInspector] private float calculatedJumpForce;
    #endregion

    #region [2. Gameplay Settings]
    [Header("--- Colliders ---")]
    [Tooltip("플레이어 물리 충돌용 (IsTrigger = false)")]
    public CapsuleCollider2D playerCollider;
    [Tooltip("장애물 충돌용 (IsTrigger = true)")]
    public CapsuleCollider2D interactionCollider;

    [Header("--- Action Settings ---")]
    public int maxJumpCount = 2;
    private int currentJumpCount = 0;

    [Header("Slide Settings")]
    [Range(0.1f, 1f)]
    public float slideSizeMultiplier = 0.5f;

    [Header("Collision & Health")]
    public LayerMask groundLayer;
    public float groundCheckSize = 0.2f;
    public float invincibilityDuration = 2.0f;
    public int initialHP = 3;

    [Header("VFX & Audio")]
    public Image redFlashImage;
    public AudioClip jumpSound;
    public AudioClip slideSound;
    public AudioClip hitSound;
    #endregion

    #region [3. Components & State]
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private Animator anim;
    private SpriteRenderer[] renderers;
    private LT_MainUIManager _UIManager;

    private LT_MainUIManager UIManager
    {
        get
        {
            if (_UIManager == null)
                _UIManager = FindFirstObjectByType<LT_MainUIManager>();
            return _UIManager;
        }
    }

    private bool isGrounded = true;
    private bool isSliding;
    private bool isInvincible;
    private bool isGameOver;
    private bool isInsideObstacle;
    private bool jumpTriggered;
    private int hp;

    private Vector2 originalInteractionSize;
    private Vector2 originalInteractionOffset;

    private GameObject nearbyDecorObject;
    private ColorKeeper[] nearbyDecorKeepers;

    private LT_NoteInteraction _noteInteraction;
    private ObjectSwitcher _objectSwitcher;

    private bool isFeverMode = false;
    private float baseMoveSpeed;
    private float baseJumpTime;

    private readonly int HashRun = Animator.StringToHash("IsRunning");
    private readonly int HashJump = Animator.StringToHash("IsJumping");
    private readonly int HashJumpTrigger = Animator.StringToHash("IsJump");
    private readonly int HashSlide = Animator.StringToHash("IsSliding");
    private readonly int HashGuitar = Animator.StringToHash("IsGuitar");
    private readonly int HashHit = Animator.StringToHash("IsHitting");
    #endregion

    // =========================================================================
    //  HP
    // =========================================================================

    public static event Action<int> OnHPChanged;

    public int HP
    {
        get => hp;
        private set
        {
            hp = Mathf.Max(0, value);
            OnHPChanged?.Invoke(hp);
        }
    }

    public void ResetHP() => HP = initialHP;
    public void DecreaseHP(int amount = 1) => HP -= amount;
    public void IncreaseHP(int amount = 1) => HP += amount;

    // =========================================================================
    //  Lifecycle
    // =========================================================================

    private void OnValidate()
    {
        CalculatePhysics();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        anim = GetComponentInChildren<Animator>();
        renderers = GetComponentsInChildren<SpriteRenderer>();

        _noteInteraction = GetComponent<LT_NoteInteraction>();
        _objectSwitcher = GetComponent<ObjectSwitcher>();

        if (interactionCollider != null)
        {
            originalInteractionSize = interactionCollider.size;
            originalInteractionOffset = interactionCollider.offset;
        }

        if (redFlashImage != null)
        {
            Color c = redFlashImage.color;
            c.a = 0f;
            redFlashImage.color = c;
        }
    }

    private void Start()
    {
        baseMoveSpeed = moveSpeed;
        baseJumpTime = timeToJumpApex;
        CalculatePhysics();
        rb.gravityScale = calculatedGravityScale;
        isGameOver = false;
        ResetHP();
    }

    private void FixedUpdate()
    {
        if (isGameOver)
        {
            SetVelocity(Vector2.zero);
            return;
        }

        CheckGround();
        CheckFall();
        ApplyCustomGravity();
        Move();
    }

    private void Update()
    {
        if (isGameOver) return;

        HandleInput();
        UpdateAnimation();
    }

    // =========================================================================
    //  Physics
    // =========================================================================

    private void CalculatePhysics()
    {
        float gravity = (2 * jumpHeight) / (timeToJumpApex * timeToJumpApex);
        calculatedJumpForce = gravity * timeToJumpApex;

        float standardGravity = Mathf.Abs(Physics2D.gravity.y);
        if (standardGravity < 0.01f) standardGravity = 9.81f;

        calculatedGravityScale = gravity / standardGravity;
    }

    private void SetVelocity(Vector2 velocity)
    {
#if UNITY_2023_1_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }

    private Vector2 GetVelocity()
    {
#if UNITY_2023_1_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void Move()
    {
        SetVelocity(new Vector2(moveSpeed, GetVelocity().y));
    }

    private void ApplyCustomGravity()
    {
        if (isGameOver) return;

        bool isFalling = !isGrounded && GetVelocity().y < 0;
        rb.gravityScale = isFalling
            ? calculatedGravityScale * fallGravityMultiplier
            : calculatedGravityScale;
    }

    private void CheckGround()
    {
        if (playerCollider == null) return;

        Bounds bounds = playerCollider.bounds;
        Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, groundCheckSize);

        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, groundCheckSize, groundLayer);

        if (hit.collider != null)
        {
            if (!isGrounded)
            {
                currentJumpCount = 0;
                jumpTriggered = false;
                if (anim != null) anim.ResetTrigger(HashJumpTrigger);
            }
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
            jumpTriggered = false;
        }
    }

    private void CheckFall()
    {
        if (transform.position.y < fallYThreshold)
            Die();
    }

    // =========================================================================
    //  Input
    // =========================================================================

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
            TryJump();

        bool isDownPressed = Input.GetKey(KeyCode.J);

        if (isDownPressed && !isSliding && isGrounded)
            StartSlide();
        else if (!isDownPressed && isSliding)
            EndSlide();

        if (Input.GetKeyDown(KeyCode.Space))
            TryRestoreDecor();
    }

    // =========================================================================
    //  Jump (원본과 동일)
    // =========================================================================

    private void TryJump()
    {
        if (isSliding) return;

        if (isGrounded || currentJumpCount < maxJumpCount)
        {
            currentJumpCount++;
            jumpTriggered = true;
            SetVelocity(new Vector2(GetVelocity().x, 0f));
            SetVelocity(GetVelocity() + Vector2.up * calculatedJumpForce);
            if (anim != null) anim.SetTrigger(HashJumpTrigger);
            PlaySound(jumpSound);
        }
    }

    // =========================================================================
    //  Decor Restore
    // =========================================================================

    private void TryRestoreDecor()
    {
        bool switched = false;

        // 1. 노트 스위칭 시도
        if (_noteInteraction != null && _noteInteraction.TrySwitchNote())
            switched = true;

        // 2. 오브젝트 스위칭 시도
        if (_objectSwitcher != null && _objectSwitcher.TrySwitchObject())
            switched = true;

        // 3. Decor 컬러 복원 시도
        if (nearbyDecorKeepers != null && nearbyDecorKeepers.Length > 0)
        {
            foreach (var keeper in nearbyDecorKeepers)
            {
                if (keeper != null && !keeper.IsColorized)
                    keeper.ForceFullColor();
            }
            nearbyDecorObject = null;
            nearbyDecorKeepers = null;
            switched = true;
        }

        if (switched && anim != null)
            anim.SetTrigger(HashGuitar);
    }

    // =========================================================================
    //  Slide (원본과 동일)
    // =========================================================================

    private void StartSlide()
    {
        if (isSliding) return;

        isSliding = true;
        PlaySound(slideSound);

        if (interactionCollider != null)
        {
            float bottomY = originalInteractionOffset.y - (originalInteractionSize.y * 0.5f);
            float newHeight = originalInteractionSize.y * slideSizeMultiplier;
            float newOffsetY = bottomY + (newHeight * 0.5f);

            interactionCollider.size = new Vector2(originalInteractionSize.x, newHeight);
            interactionCollider.offset = new Vector2(originalInteractionOffset.x, newOffsetY);
        }

        anim?.SetBool(HashSlide, true);
    }

    private void EndSlide()
    {
        if (!isSliding) return;

        isSliding = false;

        if (interactionCollider != null)
        {
            interactionCollider.size = originalInteractionSize;
            interactionCollider.offset = originalInteractionOffset;
        }

        anim?.SetBool(HashSlide, false);
    }

    // =========================================================================
    //  Fever Mode (FeverManager에서 호출)
    // =========================================================================

    public void SetFeverMode(bool active, float multiplier = 1.5f)
    {
        if (isFeverMode == active) return;

        isFeverMode = active;
        moveSpeed = active ? baseMoveSpeed * multiplier : baseMoveSpeed;
        timeToJumpApex = active ? baseJumpTime / multiplier : baseJumpTime;

        if (anim != null) anim.speed = active ? multiplier : 1.0f;

        CalculatePhysics();
        if (rb != null) rb.gravityScale = calculatedGravityScale;
    }

    // =========================================================================
    //  Collision & Damage
    // =========================================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle"))
        {
            isInsideObstacle = true;
            if (!isInvincible)
            {
                OnHitObstacle();
                // 장애물 회피 실패 표시
                var colorizer = other.GetComponentInParent<ObstacleDodgeColorizer>();
                if (colorizer != null) colorizer.wasHit = true;
            }
        }
        else if (other.CompareTag("EndFlag"))
        {
            isGameOver = true;
            if (UIManager != null) UIManager.ShowGameClear();
        }
        else if (other.CompareTag("Decor"))
        {
            GameObject root = other.transform.root.gameObject;
            ColorKeeper[] keepers = root.GetComponentsInChildren<ColorKeeper>();
            if (keepers.Length > 0)
            {
                nearbyDecorObject = root;
                nearbyDecorKeepers = keepers;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle"))
        {
            isInsideObstacle = false;
        }
        else if (other.CompareTag("Decor"))
        {
            GameObject root = other.transform.root.gameObject;
            if (nearbyDecorObject != null && nearbyDecorObject == root)
            {
                nearbyDecorObject = null;
                nearbyDecorKeepers = null;
            }
        }
    }

    public void OnHitObstacle()
    {
        if (isInvincible || isGameOver) return;

        if (redFlashImage != null)
            StartCoroutine(ScreenFlashRoutine(0.2f));

        if (isSliding) EndSlide();

        DecreaseHP(1);
        if (HP <= 0)
        {
            Die();
            return;
        }

        anim?.SetTrigger(HashHit);
        PlaySound(hitSound);
        StartCoroutine(InvincibilityRoutine());
    }

    public void Die()
    {
        if (isGameOver) return;

        isGameOver = true;
        SetVelocity(Vector2.zero);
        rb.gravityScale = 0f;
        if (UIManager != null) UIManager.ShowGameOver();
    }

    // =========================================================================
    //  Invincibility
    // =========================================================================

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float timer = 0f;

        while (timer < invincibilityDuration)
        {
            SetAlpha(0.6f);
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;

            if (timer >= invincibilityDuration) break;

            SetAlpha(1.0f);
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        SetAlpha(1.0f);
        isInvincible = false;

        if (isInsideObstacle) OnHitObstacle();
    }

    private IEnumerator ScreenFlashRoutine(float duration)
    {
        float elapsed = 0f;
        Color c = redFlashImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0.3f, 0f, elapsed / duration);
            redFlashImage.color = c;
            yield return null;
        }
    }

    // =========================================================================
    //  Animation & Audio
    // =========================================================================

    private void UpdateAnimation()
    {
        if (anim == null) return;

        bool jumping = !isSliding && (jumpTriggered || !isGrounded);
        bool running = !isSliding && isGrounded && !jumpTriggered;

        anim.SetBool(HashSlide, isSliding);
        anim.SetBool(HashRun, running);
        anim.SetBool(HashJump, jumping);

        // DEBUG
        if (jumping)
            Debug.Log($"[Anim] IsJumping=true | isGrounded:{isGrounded} jumpTriggered:{jumpTriggered} vel.y:{GetVelocity().y:F2}");
    }

    private void SetAlpha(float alpha)
    {
        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    // =========================================================================
    //  Gizmos
    // =========================================================================

    private void OnDrawGizmos()
    {
        if (playerCollider == null) return;

        Bounds bounds = playerCollider.bounds;
        Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, groundCheckSize);

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}
