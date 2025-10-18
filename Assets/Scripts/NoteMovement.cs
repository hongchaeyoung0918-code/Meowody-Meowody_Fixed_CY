using UnityEngine;

public class NoteMovement : MonoBehaviour
{
    private float moveSpeed;
    private bool isMoving = false;

    public void Initialize(float speed)
    {
        moveSpeed = speed;
        isMoving = true;
    }


    void Update()
    {
        if (isMoving)
        {
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

            if(transform.position.x < -10f) // 화면 밖으로 나가면 오브젝트 제거
            {
                gameObject.SetActive(false); // 오브젝트 풀링을 고려하여 비활성화
                Destroy(gameObject);
            }
        }
    }
}
