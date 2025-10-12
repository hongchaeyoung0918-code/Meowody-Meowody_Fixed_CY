using UnityEngine;

public class NoteProjectile : MonoBehaviour
{
    private float projectileSpeed = 10f;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
        }

        Destroy(gameObject, 3f);
    }

    public void Launch(float speed)
    {
        projectileSpeed = speed;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.right * projectileSpeed;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        CitizenController citizen = other.GetComponent<CitizenController>();

        if (citizen != null)
        {
            citizen.ChangeToHappyCitizen();

            Destroy(gameObject);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
