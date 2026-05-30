using UnityEngine;

public class InfiniteParallax : MonoBehaviour
{
    [Header("설정")]
    public GameObject cam;
    public float parallaxEffect; // 0: 월드 고정, 1: 카메라와 완전 동기, 0.5: 중간 속도

    private float length;       // 스프라이트 가로 길이
    private float startPos;     // 오브젝트 초기 X 위치 (world)
    private float startCamPos;  // 카메라 초기 X 위치

    void Start()
    {
        if (cam == null) cam = Camera.main.gameObject;

        startPos    = transform.position.x;
        startCamPos = cam.transform.position.x;

        if (GetComponent<SpriteRenderer>() != null)
            length = GetComponent<SpriteRenderer>().bounds.size.x;
        else if (GetComponentInChildren<SpriteRenderer>() != null)
            length = GetComponentInChildren<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        // 카메라가 시작 위치에서 얼마나 이동했는지 (delta)
        float camDelta = cam.transform.position.x - startCamPos;

        // 시차 적용 위치
        float dist = camDelta * parallaxEffect;
        transform.position = new Vector3(startPos + dist, transform.position.y, transform.position.z);

        // parallaxEffect == 1이면 카메라와 완전 동기 → 루프 불필요
        if (Mathf.Approximately(parallaxEffect, 1f)) return;

        // 오브젝트의 겉보기 드리프트 (카메라 delta 기준)
        // camDelta가 length보다 커지면 타일 한 칸 재배치
        float drift    = camDelta * (1f - parallaxEffect);
        float loopStep = length / (1f - parallaxEffect); // 타일 하나 이동에 해당하는 카메라 거리

        if (drift > length)
            startCamPos += loopStep;
        else if (drift < -length)
            startCamPos -= loopStep;
    }
}
