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

        if (citizenCollider != null)
        {
            citizenCollider.isTrigger = true; // 항상 트리거로 설정
        }

        // 초기 상태 설정
        UpdateCitizenState(isHappy);
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
            citizenCollider.enabled = !happy;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌했는지 확인하고, 시민이 슬픈 상태(콜라이더가 켜진 상태)인지 확인
        if (other.CompareTag("Player") && !isHappy)
        {
            // 플레이어 컨트롤러를 가져와서 ProcessFailure() 호출 (HP 감소 및 무적)
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                // ProcessFailure()를 호출하여 HP를 깎고 무적 상태로 전환
                player.ProcessFailureFromCitizenCollision();

                // 충돌 후 시민 오브젝트를 제거 (선택 사항)
                // HP를 깎은 후 시민을 남겨둘지, 다른 노드처럼 없앨지는 기획에 따라 결정합니다.
                // 여기서는 피격 후 시민이 바로 사라지도록 처리하겠습니다.
                citizenCollider.enabled = false;
                gameObject.SetActive(false);
                Destroy(gameObject); // (오브젝트 풀링 사용 시 pool.Return(gameObject)으로 대체)
            }
        }
    }
}
