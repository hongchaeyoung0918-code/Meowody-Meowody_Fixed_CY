using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;

    [Header("¸Ê °æ°è ¼³Á¤")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    private Vector3 offset;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("Player Transform is not assigned to the CameraFollow script.");
            enabled = false;
            return;
        }

        offset = transform.position - player.position;
    }

    void FixedUpdate()
    {
        Vector3 targetPosition = player.position + offset;

        float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
        //float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);
        float clampedY = 0f;

        transform.position = new Vector3(clampedX, clampedY, targetPosition.z);
    }
}
