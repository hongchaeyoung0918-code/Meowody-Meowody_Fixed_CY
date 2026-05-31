using UnityEngine;

// ──────────────────────────────────────────────────────────────────
// ObstacleDodgeColorizer
//
// 장애물에 부착하여, 플레이어가 점프/슬라이드로 회피 성공하면
// 하위 ColorKeeper들의 색상을 복원합니다.
//
// 판정: 장애물의 x좌표가 플레이어 뒤로 지나갔을 때
//       wasHit == false → 회피 성공 → ForceFullColor()
//
// 사용법:
//   1. 장애물 프리팹에 이 컴포넌트 추가
//   2. 하위 SpriteRenderer에 ColorKeeper + colorKeeperMaterial 설정
//   3. LT_PlayerController_v2가 피격 시 wasHit = true로 설정
// ──────────────────────────────────────────────────────────────────
public class ObstacleDodgeColorizer : MonoBehaviour
{
    [Tooltip("플레이어 x좌표보다 이 값만큼 뒤로 지나가면 회피 성공 판정")]
    public float passOffset = 1f;

    /// <summary>플레이어 피격 시 true로 설정됩니다.</summary>
    [HideInInspector] public bool wasHit = false;

    private Transform _playerTransform;
    private ColorKeeper[] _keepers;
    private bool _resolved = false;

    void Start()
    {
        _keepers = GetComponentsInChildren<ColorKeeper>();

        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerTransform = player.transform;
    }

    void Update()
    {
        if (_resolved || _playerTransform == null || _keepers == null || _keepers.Length == 0)
            return;

        // 장애물이 플레이어 뒤로 지나갔는지 확인
        if (transform.position.x < _playerTransform.position.x - passOffset)
        {
            _resolved = true;

            if (!wasHit)
            {
                // 회피 성공 → 컬러 복원
                foreach (var keeper in _keepers)
                {
                    if (keeper != null && !keeper.IsColorized)
                        keeper.ForceFullColor();
                }
            }
        }
    }
}
