using UnityEngine;

public class CitizenController : MonoBehaviour
{
    public int citizenTypeIndex = 0;

    public bool isHappy = false; // 현재 상태 (시작은 false)
    public Sprite[] happySprites;
    public Sprite[] sadSprites;

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

        if (sadSprites != null && sadSprites.Length > 0)
        {
            citizenTypeIndex = Random.Range(0, sadSprites.Length);
            // happySprites의 길이도 동일하다고 가정합니다.
        }
        else
        {
            Debug.LogError("Sad Sprites 배열이 비어있거나 할당되지 않았습니다!");
            // 안전을 위해 기본값 0 유지
            citizenTypeIndex = 0;
        }

        // 초기 상태 설정: isHappy를 false로 강제 설정하고 상태 업데이트 (무작위 슬픈 스프라이트 적용)
        isHappy = false;

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
        if (sr != null && citizenTypeIndex >= 0 &&
                    (happy ? happySprites.Length : sadSprites.Length) > citizenTypeIndex)
        {
            // 선택된 citizenTypeIndex를 사용하여 짝이 맞는 스프라이트를 가져옵니다.
            Sprite targetSprite = happy ? happySprites[citizenTypeIndex] : sadSprites[citizenTypeIndex];
            sr.sprite = targetSprite;
        }

        // 2. 콜라이더 판정 변경 (기존 로직과 동일)
        if (citizenCollider != null)
        {
            // 회색(슬플 때): 플레이어에게 벽 판정 (Is Trigger = false)
            // 행복할 때: 플레이어가 통과 (Is Trigger = true)
            // 주의: IsTrigger = true로 설정되어 있어도 Collider2D.enabled = false로 하면 충돌 판정 자체가 꺼집니다.
            // 기존 스크립트에서는 Start()에서 isTrigger=true로 설정했으므로,
            // OnTriggerEnter2D가 아닌 OnCollisionEnter2D를 사용해야 벽 판정 로직이 맞습니다.
            // 일단 기존 코드의 의도대로 citizenCollider.enabled를 사용합니다.
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
