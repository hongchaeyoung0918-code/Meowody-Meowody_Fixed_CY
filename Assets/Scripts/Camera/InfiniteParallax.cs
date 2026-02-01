using UnityEngine;

public class InfiniteParallax : MonoBehaviour
{
    [Header("설정")]
    public GameObject cam; // 메인 카메라
    public float parallaxEffect; // 0: 배경 고정(같이 이동), 1: 맵 바닥과 동일 속도(안 움직이는 것처럼 보임), 0.5: 중간 속도

    private float length; // 이미지의 가로 길이
    private float startPos; // 시작 X 위치

    void Start()
    {
        if (cam == null) cam = Camera.main.gameObject;

        startPos = transform.position.x;

        // 스프라이트의 가로 길이를 자동으로 구함
        if (GetComponent<SpriteRenderer>() != null)
            length = GetComponent<SpriteRenderer>().bounds.size.x;
        else if (GetComponentInChildren<SpriteRenderer>() != null) // 자식에 있는 경우
            length = GetComponentInChildren<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        // 1. 카메라가 이동한 거리만큼 배경도 따라가야 함 (Parallax 효과 적용)
        // dist: 실제 오브젝트가 이동해야 할 목표 위치 (카메라 이동 거리 * 패럴렉스 계수)
        float dist = (cam.transform.position.x * parallaxEffect);

        // 2. 무한 스크롤 계산 (카메라가 배경을 얼마나 지나쳤는지)
        // temp: 현재 카메라 위치 대비 배경의 상대적 위치 계산용
        float temp = (cam.transform.position.x * (1 - parallaxEffect));

        // 3. 배경 위치 업데이트
        transform.position = new Vector3(startPos + dist, transform.position.y, transform.position.z);

        // 4. 배경이 카메라 범위를 벗어나면 위치를 재조정 (무한 루프 핵심)
        if (temp > startPos + length)
        {
            startPos += length;
        }
        else if (temp < startPos - length)
        {
            startPos -= length;
        }
    }
}