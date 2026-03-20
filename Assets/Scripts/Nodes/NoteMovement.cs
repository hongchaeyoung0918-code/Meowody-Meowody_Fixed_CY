using UnityEngine;

public class NoteMovement : MonoBehaviour
{
    private float moveSpeed;
    private bool isMoving = false;

    // 튜토리얼 관련 변수
    private string noteType;
    private bool tutorialChecked = false;
    private NoteManager noteManager;

    // [수정됨] 이제 매개변수를 2개(속도, 타입) 받습니다.
    public void Initialize(float speed, string type)
    {
        moveSpeed = speed;
        noteType = type;
        isMoving = true;

        // 씬에서 NoteManager를 찾아 참조를 보관합니다.
        noteManager = Object.FindFirstObjectByType<NoteManager>();
    }

    void Update()
    {
        if (isMoving)
        {
            // 이동 로직
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

            // 스테이지 1이고 아직 체크하지 않은 경우 거리 확인
            if (GameSettings.SelectedStage == 1 && !tutorialChecked && noteManager != null)
            {
                // 플레이어 근처(x=3.0)에 도달했을 때 튜토리얼 트리거
                if (transform.position.x <= 1.8f)
                {
                    tutorialChecked = true;
                    noteManager.TriggerTutorialIfFirstTime(noteType);
                }
            }

            // 화면 밖으로 나가면 제거
            if (transform.position.x < -10f)
            {
                Destroy(gameObject);
            }
        }
    }
}