using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public int maxJumpCount = 1;
    private int currentJumpCount = 0;
    public float trampolineJumpForce = 15f;

    [Header("Invincibility Settings")]
    public float invincibilityDuration = 2.0f;
    private bool isInvincible = false;

    [Header("Slide Settings")]
    public float slideSpeedMultiplier = 1.5f;
    public float slideDuration = 0.5f;
    public float slideHeightScale = 0.5f;

    [Header("Collider Settings")]
    public float colliderHeightAdjustment = 0.5f;

    [Header("Note Attack Settings")]
    public GameObject notePrefab;
    public float noteSpawnOffset = 0.8f;
    public float noteSpawnHeight = 0.5f;

    [Header("Ground Check Settings")]
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer;

    [Header("Wall Check Settings")]
    public float wallCheckDistance = 0.15f;

    [Header("Game State")]
    public Transform respawnPoint;

    [Header("Respawn Settings")]
    public float respawnGraceTime = 0.5f;
    private float respawnTimer = 0f;

    [Header("Audio Settings")]
    public AudioClip jumpSound;
    public AudioClip slideSound;
    public AudioClip noteAttackSound;
    public AudioClip hitSound;
    private AudioSource audioSource;

    private float failCheckTime = 0.2f;
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
    private ColorManager colorManager;

    [Header("Animation Settings")]
    public Animator anim;
    public GameObject currentRigObject;
    [Header("Death Prefab")]
    public GameObject deathRigPrefab;
    private float deathAnimationDuration = 1.5f;

    private readonly string IsRunningParam = "IsRunning";
    private readonly string IsJumpingParam = "IsJumping";
    private readonly string IsSlidingParam = "IsSliding";
    private readonly string IsHittingParam = "IsHitting";
    private readonly string IsGuitarParam = "IsGuitar";

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            Debug.LogWarning("PlayerController requires an AudioSource component.");

        if (currentRigObject == null)
        {
            Transform rigTransform = transform.Find("Player_Normal");
            if (rigTransform != null) currentRigObject = rigTransform.gameObject;
        }

        if (currentRigObject != null)
            anim = currentRigObject.GetComponent<Animator>();
        else
            anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        originalScale = transform.localScale;

        if (capsuleCollider != null)
        {
            originalColliderSize = capsuleCollider.size;
            originalColliderOffset = capsuleCollider.offset;
            wallCheckDistance = (capsuleCollider.size.x / 2f) + 0.05f;
        }

        uiManager = FindFirstObjectByType<MainUIManager>();
        playerStats = FindFirstObjectByType<PlayerStats>();
        colorManager = ColorManager.Instance;

        Vector3 startPosition = transform.position;
        transform.position = new Vector3(startPosition.x, 0f, startPosition.z);
        initialXPosition = transform.position.x;

        SetAnimationBool(IsRunningParam, true);
    }

    void Update()
    {
        // 1. 튜토리얼 일시정지 중 처리
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsPaused())
        {
            string allowed = TutorialManager.Instance.TargetAction;
            if (allowed == "UP")
            {
                if (!(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))) return;
            }
            else if (allowed == "DOWN")
            {
                if (!(Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))) return;
            }
            else return;
        }

        if (!isInvincible) CheckForFailure();

        CheckIfGrounded();

        // 2. 해금 상태 체크가 포함된 입력 처리
        HandleSlide();
        HandleJump();
        HandleNoteShoot();

        UpdateAnimationState();

        if (!isGameOver)
        {
            transform.position = new Vector3(initialXPosition, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void CheckIfGrounded()
    {
        Vector2 raycastOrigin = new Vector2(capsuleCollider.bounds.center.x, capsuleCollider.bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.down, groundCheckDistance, groundLayer);

        if (hit.collider != null)
        {
            if (!isGrounded) currentJumpCount = 0;
            isGrounded = true;
        }
        else
            isGrounded = false;
    }

    void CheckForFailure()
    {
        if (respawnTimer > 0f || playerStats == null || isGameOver || isFailing) return;

        Vector2 wallRaycastOrigin = capsuleCollider.bounds.center;
        RaycastHit2D wallHit = Physics2D.Raycast(wallRaycastOrigin, Vector2.right, wallCheckDistance, groundLayer);

        if (isGrounded && wallHit.collider != null)
        {
            stopTimer += Time.deltaTime;
            if (stopTimer >= failCheckTime)
            {
                isFailing = true;
                ProcessFailure();
                stopTimer = 0f;
            }
        }
        else stopTimer = 0f;
    }

    void ProcessFailure()
    {
        if (isInvincible) return;

        SetAnimationTrigger(IsHittingParam);
        PlaySound(hitSound);
        if (colorManager != null) colorManager.DecreaseGaugeOnHit();

        playerStats.HP--;
        if (playerStats.HP <= 0) StartCoroutine(HandleDeathSequence());
        else StartCoroutine(InvincibilityCoroutine());

        isFailing = false;
    }

    // 외부 스크립트(CitizenController, Obstacles) 호출용 함수들
    public void ProcessFailureFromCitizenCollision() { if (!isInvincible && !isGameOver) ProcessFailure(); }
    public void ProcessFailureFromObstacle() { if (!isInvincible && !isGameOver) ProcessFailure(); }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            foreach (var sr in renderers)
            {
                if (sr == null) continue;
                Color c = sr.color;
                c.a = 0.6f;
                sr.color = c;
            }
            yield return new WaitForSeconds(0.1f);

            foreach (var sr in renderers)
            {
                if (sr == null) continue;
                Color c = sr.color;
                c.a = 1.0f;
                sr.color = c;
            }
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.2f;
        }

        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = 1.0f;
            sr.color = c;
        }

        isInvincible = false;
        currentJumpCount = 0;
    }

    void HandleJump()
    {
        // 튜토리얼 해금 상태 확인
        if (TutorialManager.Instance != null && !TutorialManager.Instance.canJump) return;

        bool jumpInput = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        if (!isSliding && jumpInput && isGrounded && currentJumpCount == 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            currentJumpCount = 1;
            PlaySound(jumpSound);
        }
    }

    public void PerformAirJumpOnContact()
    {
        // JumpOrb 등에서 호출 시 점프 실행
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        currentJumpCount = 1;
        PlaySound(jumpSound);
    }

    void HandleSlide()
    {
        // 튜토리얼 해금 상태 확인
        if (TutorialManager.Instance != null && !TutorialManager.Instance.canSlide) return;

        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && isGrounded && !isSliding)
        {
            StartSlide();
        }
        if (isSliding && (Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow)))
        {
            EndSlide();
        }
    }

    void StartSlide()
    {
        isSliding = true;
        PlaySound(slideSound);
        if (capsuleCollider != null)
        {
            float newHeight = originalColliderSize.y * slideHeightScale;
            float yOffsetAdjustment = (originalColliderSize.y - newHeight) / 2f;
            capsuleCollider.size = new Vector2(originalColliderSize.x, newHeight);
            capsuleCollider.offset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - yOffsetAdjustment);
            rb.WakeUp();
        }
    }

    void EndSlide()
    {
        isSliding = false;
        if (capsuleCollider != null)
        {
            capsuleCollider.size = originalColliderSize;
            capsuleCollider.offset = originalColliderOffset;
            rb.WakeUp();
        }
    }

    void HandleNoteShoot()
    {
        if (!isGrounded || isSliding) return;

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (notePrefab == null) return;

            SetAnimationTrigger(IsGuitarParam);
            PlaySound(noteAttackSound);

            Vector3 spawnPosition = transform.position
                                  + Vector3.right * noteSpawnOffset
                                  + Vector3.up * noteSpawnHeight;

            GameObject note = Instantiate(notePrefab, spawnPosition, Quaternion.identity);
            NoteProjectile noteProjectile = note.GetComponent<NoteProjectile>();
            if (noteProjectile != null)
                noteProjectile.Launch(10.0f);
        }
    }

    void UpdateAnimationState()
    {
        if (anim == null || isGameOver) return;
        bool inAir = !isGrounded && !isSliding;
        SetAnimationBool(IsJumpingParam, inAir);
        SetAnimationBool(IsSlidingParam, isSliding);
        if (!isSliding && !inAir) SetAnimationBool(IsRunningParam, !isGameOver);
    }

    void SetAnimationBool(string paramName, bool value) { if (anim != null) anim.SetBool(paramName, value); }
    void SetAnimationTrigger(string paramName) { if (anim != null) anim.SetTrigger(paramName); }

    void HandleDeathModelChange()
    {
        if (currentRigObject != null) currentRigObject.SetActive(false);
        if (deathRigPrefab != null)
        {
            GameObject deathModel = Instantiate(deathRigPrefab, transform.position, transform.rotation, transform);
            deathModel.SetActive(true);
        }
    }

    IEnumerator HandleDeathSequence()
    {
        isGameOver = true;
        enabled = false;
        rb.linearVelocity = Vector2.zero;
        if (uiManager != null && uiManager.noteManager != null) uiManager.noteManager.StopGame();
        HandleDeathModelChange();
        yield return new WaitForSeconds(deathAnimationDuration);
        if (uiManager != null) uiManager.ShowGameOver();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0 || collision.gameObject.CompareTag("Ground"))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            if (!isGrounded)
            {
                currentJumpCount = 0;
                isGrounded = true;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Note_Obstacle"))
        {
            ProcessFailureFromObstacle();
            Destroy(other.gameObject);
        }
        else if (other.gameObject.CompareTag("Note_Obstacle_Persistent")) ProcessFailureFromObstacle();
        else if (other.gameObject.CompareTag("EndFlag"))
        {
            if (uiManager != null) uiManager.ShowGameClear();
            enabled = false;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}