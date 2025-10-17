using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    private float startPos; // 스프라이트의 시작 X 위치
    private float length;   // 스프라이트의 길이 (X축)
    public GameObject cam;  // 메인 카메라 GameObject 참조
    public float parallaxEffect; // 패럴렉스 효과 계수 (0~1 사이)

    void Start()
    {
        // 시작 위치와 스프라이트의 길이 저장
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;

        // cam이 설정되지 않은 경우, 기본적으로 메인 카메라를 찾습니다.
        if (cam == null)
        {
            cam = Camera.main.gameObject;
        }
    }

    void Update()
    {
        // 카메라의 현재 X 위치
        float cameraX = cam.transform.position.x;

        // 배경이 카메라보다 느리게 움직이게 하여 원근감을 만듭니다.
        // 먼 레이어일수록 parallaxEffect 값을 0에 가깝게 설정합니다.
        float dist = (cameraX * parallaxEffect);

        // 배경의 새로운 위치를 설정
        transform.position = new Vector3(startPos + dist, transform.position.y, transform.position.z);

        // 무한 스크롤을 위한 배경 반복 처리 (선택 사항)
        // 카메라가 배경 스프라이트의 절반 길이를 벗어났을 때, 배경을 이동시켜 반복되게 합니다.
        float temp = (cameraX * (1 - parallaxEffect));

        if (temp > startPos + length)
        {
            // 오른쪽으로 이동 (오른쪽으로 쭉 이동하는 경우)
            startPos += length;
        }
        else if (temp < startPos - length)
        {
            // 왼쪽으로 이동 (필요하다면)
            startPos -= length;
        }
    }
}
