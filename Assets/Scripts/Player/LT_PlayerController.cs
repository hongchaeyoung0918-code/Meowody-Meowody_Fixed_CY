using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // TextMeshPro 사용을 위해 필수

[RequireComponent(typeof(Rigidbody2D))]
public class LT_PlayerController : MonoBehaviour
{
    #region [1. Level Test & Physics Settings]
    [Header("--- Movement & Physics ---")]
    public float moveSpeed = 10f;
    public float jumpHeight = 3.5f;
    public float timeToJumpApex = 0.45f;
    public float fallGravityMultiplier = 1.5f;
    public Vector3 characterScale = Vector3.one;
    public float fallYThreshold = -8f;

    [Header("--- Debug Info (Read Only) ---")]
    [SerializeField] private float calculatedJumpDistance;
    [SerializeField] private float calculatedGravityScale;
    [SerializeField] private float calculatedJumpForce;
    #endregion

    #region [2. Gameplay Settings]
    [Header("--- Colliders ---")]
    [Tooltip("바닥 체크용 (IsTrigger = false)")]
    public CapsuleCollider2D groundCollider;
    [Tooltip("장애물 충돌용 (IsTrigger = true)")]
    public CapsuleCollider2D interactionCollider;

    [Header("--- Action Settings ---")]
    public int maxJumpCount = 2;
    private int currentJumpCount = 0;

    [Header("Slide Settings")]
    public float slideDuration = 0.5f;
    [Range(0.1f, 1f)]
    public float slideSizeMultiplier = 0.5f;

    [Header("Collision & Health")]
    public LayerMask groundLayer;
    public float groundCheckSize = 0.05f;
    public float invincibilityDuration = 2.0f;

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
    private LT_MainUIManager uiManager;
    private PlayerStats playerStats;

    private bool isGrounded;
    private bool isSliding;
    private bool isInvincible;
    private bool isGameOver;
    private bool isInsideObstacle;

    private Vector2 originalInteractionSize;
    private Vector2 originalInteractionOffset;

    private bool isFeverMode = false;
    private float baseMoveSpeed;
    private float baseJumpTime;

    private readonly int HashRun = Animator.StringToHash("IsRunning");
    private readonly int HashJump = Animator.StringToHash("IsJumping");
    private readonly int HashSlide = Animator.StringToHash("IsSliding");
    private readonly int HashHit = Animator.StringToHash("IsHitting");
    #endregion

    #region [4. Object Switcher & Tag Settings]
    [Header("--- Object Switcher ---")]
    public GameObject[] originalObjects;
    public GameObject[] matchedObjects;
    [Tooltip("자식 오브젝트 중 텍스트를 찾을 태그")]
    public string scoreTextTag = "ScoreText";
    public GameObject auraEffect;

    private readonly int HashPlayMusic = Animator.StringToHash("PlayMusic");
    private bool isPlayingMusic = false;
    private int activeSwitchCount = 0;
    #endregion

    private void OnValidate() { CalculatePhysics(); }

    private void CalculatePhysics()
    {
        float gravity = (2 * jumpHeight) / (timeToJumpApex * timeToJumpApex);
        calculatedJumpForce = gravity * timeToJumpApex;
        float standardGravity = Mathf.Abs(Physics2D.gravity.y);
        if (standardGravity < 0.01f) standardGravity = 9.81f;
        calculatedGravityScale = gravity / standardGravity;
        calculatedJumpDistance = moveSpeed * (timeToJumpApex * 2);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        anim = GetComponentInChildren<Animator>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        
        if (interactionCollider != null)
        {
            originalInteractionSize = interactionCollider.size;
            originalInteractionOffset = interactionCollider.offset;
        }

        uiManager = FindFirstObjectByType<LT_MainUIManager>();
        playerStats = FindAnyObjectByType<PlayerStats>();

        if (redFlashImage != null)
        {
            Color c = redFlashImage.color;
            c.a = 0f;
            redFlashImage.color = c;
        }
    }

    void Start()
    {
        baseMoveSpeed = moveSpeed;
        baseJumpTime = timeToJumpApex;
        CalculatePhysics();
        rb.gravityScale = calculatedGravityScale;
        transform.localScale = characterScale;
        isGameOver = false;
        if (auraEffect != null) auraEffect.SetActive(false);
    }

    void FixedUpdate()
    {
        if (isGameOver) { SetVelocity(Vector2.zero); return; }
        CheckGround();
        CheckFall();
        ApplyCustomGravity();
        Move();
    }

    void Update()
    {
        if (isGameOver) return;
        HandleInput();
        UpdateAnimation();
        CheckDistanceToOriginalObjects();
    }

    // Unity 버전에 따른 velocity 처리 (linearVelocity 지원 유무 대응)
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

    private void Move() { SetVelocity(new Vector2(moveSpeed, GetVelocity().y)); }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) TryJump();
        bool isDownPressed = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        if (isDownPressed && !isSliding && isGrounded) StartSlide();
        else if (!isDownPressed && isSliding) EndSlide();
    }

    private void TryJump()
    {
        if (isSliding) return;
        if (isGrounded || currentJumpCount < maxJumpCount)
        {
            currentJumpCount++;
            SetVelocity(new Vector2(GetVelocity().x, 0));
            SetVelocity(GetVelocity() + Vector2.up * calculatedJumpForce);
            PlaySound(jumpSound);
            anim?.Play("JUMP_start", 0, 0f);
        }
    }

    private void ApplyCustomGravity()
    {
        if (isGameOver) return;
        rb.gravityScale = (!isGrounded && GetVelocity().y < 0) ? calculatedGravityScale * fallGravityMultiplier : calculatedGravityScale;
    }

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

    // FeverManager에서 호출하는 필수 함수
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        for (int i = 0; i < originalObjects.Length; i++)
        {
            if (other.gameObject == originalObjects[i])
            {
                originalObjects[i].SetActive(false);
                matchedObjects[i].SetActive(true);

                // 자식 중에서 태그로 텍스트 오브젝트 검색
                GameObject textObj = null;
                foreach (Transform child in matchedObjects[i].transform)
                {
                    if (child.CompareTag(scoreTextTag))
                    {
                        textObj = child.gameObject;
                        textObj.SetActive(true);
                        break;
                    }
                }

                StartCoroutine(ScaleBounceAndScoreRoutine(matchedObjects[i].transform, textObj));
                MelodySection.NotifyNoteCollected(originalObjects[i]);
                return;
            }
        }
        
        if (other.CompareTag("Note_Obstacle_Persistent"))
        {
            isInsideObstacle = true;
            if (!isInvincible) OnHitObstacle();
        }
        else if (other.CompareTag("EndFlag")) { isGameOver = true; uiManager?.ShowGameClear(); }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Note_Obstacle_Persistent"))
        {
            isInsideObstacle = false;
        }
    }

    private IEnumerator ScaleBounceAndScoreRoutine(Transform target, GameObject textObj)
    {   
        activeSwitchCount++;
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 baseScale = new Vector3(0.2f, 0.2f, 0.2f);
        Vector3 peakScale = baseScale * 1.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeOut = 1f - Mathf.Pow(1f - t, 2f);
            float bounce = Mathf.Sin(t * Mathf.PI * 4f) * 0.05f;
            if (target != null)
                target.localScale = Vector3.Lerp(baseScale, peakScale, easeOut) + Vector3.one * bounce;
            yield return null;
        }
        if (target != null) target.localScale = baseScale;

        if (textObj != null)
        {
            float floatDuration = 0.5f;
            float floatElapsed = 0f;
            Vector3 startPos = textObj.transform.localPosition;
            Vector3 endPos = startPos + Vector3.up * 1.0f;

            TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
            TextMeshProUGUI tmpUI = textObj.GetComponent<TextMeshProUGUI>();
            CanvasGroup group = textObj.GetComponent<CanvasGroup>();

            while (floatElapsed < floatDuration)
            {
                floatElapsed += Time.deltaTime;
                float t = floatElapsed / floatDuration;
                textObj.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                
                float alpha = 1f - t;
                if (group != null) group.alpha = alpha;
                else
                {
                    if (tmp != null) { Color c = tmp.color; c.a = alpha; tmp.color = c; }
                    if (tmpUI != null) { Color c = tmpUI.color; c.a = alpha; tmpUI.color = c; }
                }
                yield return null;
            }
            textObj.SetActive(false);
            textObj.transform.localPosition = startPos;
        }

        activeSwitchCount--;
        if (activeSwitchCount == 0) ResetMusicState();
    }

    #region [Helper Methods]
    private void CheckDistanceToOriginalObjects()
    {
        if (activeSwitchCount > 0) return;
        bool isClose = false;
        foreach (var obj in originalObjects)
        {
            if (obj != null && obj.activeSelf && Vector2.Distance(transform.position, obj.transform.position) <= 6f)
            {
                isClose = true; break;
            }
        }
        if (isClose && !isPlayingMusic) TriggerMusic();
        else if (!isClose && isPlayingMusic) ResetMusicState();
    }

    private void TriggerMusic() { isPlayingMusic = true; auraEffect?.SetActive(true); anim?.SetTrigger(HashPlayMusic); }
    private void ResetMusicState() { isPlayingMusic = false; auraEffect?.SetActive(false); }

    public void OnHitObstacle()
    {
        if (isInvincible || isGameOver) return;
        if (redFlashImage != null) StartCoroutine(ScreenFlashRoutine(0.2f));
        if (isSliding) EndSlide();
        if (playerStats != null) { playerStats.DecreaseHP(1); if (playerStats.HP <= 0) { Die(); return; } }
        anim?.SetTrigger(HashHit);
        PlaySound(hitSound);
        StartCoroutine(InvincibilityRoutine());
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

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float timer = 0f;
        while (timer < invincibilityDuration)
        {
            SetAlpha(0.6f); yield return new WaitForSeconds(0.1f); timer += 0.1f;
            if (timer >= invincibilityDuration) break;
            SetAlpha(1.0f); yield return new WaitForSeconds(0.1f); timer += 0.1f;
        }
        SetAlpha(1.0f); isInvincible = false;
        if (isInsideObstacle) OnHitObstacle();
    }

    private void SetAlpha(float alpha) { foreach (var sr in renderers) if (sr != null) { Color c = sr.color; c.a = alpha; sr.color = c; } }
    private void CheckGround() { if (groundCollider == null) return; Vector2 boxSize = new Vector2(groundCollider.size.x * 0.9f, groundCheckSize); Vector2 boxCenter = (Vector2)transform.position + groundCollider.offset + (Vector2.down * (groundCollider.size.y * 0.5f)); RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, 0.05f, groundLayer); if (hit.collider != null) { if (!isGrounded) currentJumpCount = 0; isGrounded = true; } else isGrounded = false; }
    private void CheckFall() { if (transform.position.y < fallYThreshold) Die(); }
    public void Die() { if (isGameOver) return; isGameOver = true; SetVelocity(Vector2.zero); rb.gravityScale = 0f; uiManager?.ShowGameOver(); }
    private void UpdateAnimation() { if (anim == null) return; anim.SetBool(HashSlide, isSliding); anim.SetBool(HashRun, !isSliding && isGrounded); anim.SetBool(HashJump, !isSliding && !isGrounded); }
    private void PlaySound(AudioClip clip) { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }
    private void OnDrawGizmos() { if (groundCollider == null) return; Gizmos.color = isGrounded ? Color.green : Color.red; Vector2 boxCenter = (Vector2)transform.position + groundCollider.offset + (Vector2.down * (groundCollider.size.y * 0.5f)); Gizmos.DrawWireCube(boxCenter, new Vector2(groundCollider.size.x * 0.9f, groundCheckSize)); Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, 6f); }
    #endregion
}