using UnityEngine;

public class CollisionSound : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        // audioSource.clip = Resources.Load<AudioClip>("Sounds/impact");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // if (collision.gameObject.CompareTag("Player"))
        Debug.Log("CollisionSound: Collision detected with " + collision.gameObject.name);
        // 효과음 재생
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log("CollisionSound: Impact sound played on collision.");
        }
    }
}
