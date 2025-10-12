using UnityEngine;

public class CitizenController : MonoBehaviour
{
    public bool isHappy = false; // 현재 상태 (시작은 false)
    public Sprite happySprite;   // 행복한 상태일 때의 스프라이트 (유니티에서 지정)
    public Sprite sadSprite;     // 회색(슬픈) 상태일 때의 스프라이트 (유니티에서 지정)

    private SpriteRenderer sr;
    private Collider2D citizenCollider;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        citizenCollider = GetComponent<Collider2D>();

        // 초기 상태 설정
        UpdateCitizenState(isHappy);

        // 플레이어와의 충돌 판정은 OnCollisionEnter2D에서 처리되므로, 
        // 콜라이더는 Is Trigger를 체크하지 않습니다.
    }

    // 음표에 맞았을 때 호출되는 함수
    public void ChangeToHappyCitizen()
    {
        if (!isHappy)
        {
            isHappy = true;
            UpdateCitizenState(true);
            Debug.Log(gameObject.name + ": 행복한 시민으로 변경!");
        }
    }

    // 시민의 상태에 따라 콜라이더 및 스프라이트를 업데이트
    void UpdateCitizenState(bool happy)
    {
        // 1. 스프라이트 변경
        if (sr != null)
        {
            sr.sprite = happy ? happySprite : sadSprite;
        }

        // 2. 콜라이더 판정 변경
        if (citizenCollider != null)
        {
            // 회색(슬플 때): 플레이어에게 벽 판정 (Is Trigger = false, 일반 충돌)
            // 행복할 때: 플레이어가 통과 (Is Trigger = true)
            citizenCollider.isTrigger = happy;
        }
    }

    // 플레이어가 회색 시민에 닿았을 때 HP를 깎는 로직
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 플레이어와 충돌했는지 확인하고, 시민이 슬픈 상태(벽 판정)인지 확인
        if (collision.gameObject.CompareTag("Player") && !isHappy)
        {
            // 플레이어 컨트롤러를 가져와서 ProcessFailure() 호출 (HP 감소 및 리스폰)
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                // ProcessFailure()를 직접 호출하여 벽에 막힌 것과 동일하게 처리
                player.ProcessFailureFromCitizenCollision();
            }
        }
    }
}
