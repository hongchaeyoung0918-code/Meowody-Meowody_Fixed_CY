using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [Header("트램펄린 설정")]
    public float bounceForce = 20f;

    public LayerMask targetLayer;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();

        if (playerRb != null && IsLayerInMask(other.gameObject.layer, targetLayer))
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);

            playerRb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

            //Debug.Log("트램펄린 작동!");

            if (audioSource != null)
            {
                audioSource.Play();
                //Debug.Log("트램펄린 소리 재생!");
            }
        }
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) > 0;
    }
}