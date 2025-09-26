using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Slide Settings")]
    public float slideSpeedMultiplier = 1.5f;
    public float slideDuration = 0.5f;
    public float slideHeightScale = 0.5f;

    [Header("Collider Settings")]
    public float colliderHeightAdjustment = 0.5f;

    private float currentMoveSpeed;
    private bool isGrounded = false;
    private bool isSliding = false;
    private float slideTimer = 0f;

    private Rigidbody2D rb;
    private MainUIManager uiManager;

    private CapsuleCollider2D capsuleCollider;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    private Vector3 originalScale;

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
        }
        else
        {
            Debug.LogError("PlayerController requires a CapsuleCollider2D component on the same GameObject.");
        }
        originalScale = transform.localScale;

        if (rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody2D component on the same GameObject.");
            enabled = false;
        }

        uiManager = FindObjectOfType<MainUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("MainUIManager를 씬에서 찾을 수 없습니다! MainScene에 배치했는지 확인하세요.");
        }

        //위치 보정
        Vector3 startPosition = transform.position;
        transform.position = new Vector3(startPosition.x, 0f, startPosition.z);

    }

    void Update()
    {
        HandleSlide();

        if (currentMoveSpeed > 0)
        {
            rb.linearVelocity = new Vector2(currentMoveSpeed, rb.linearVelocity.y);

            if (!isSliding && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
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
            isGrounded = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("EndFlag"))
        {
            Debug.Log("Trigger with EndFlag");
            moveSpeed = 0f;

            if (uiManager != null)
            {
                uiManager.ShowGameClear();
            }

            enabled = false;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
