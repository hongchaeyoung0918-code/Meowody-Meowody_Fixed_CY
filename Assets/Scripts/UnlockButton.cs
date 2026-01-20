using UnityEngine;
using UnityEngine.UI;

public class UnlockButton : MonoBehaviour
{
    [Header("해금할 스테이지 번호")]
    public int stageToUnlock = 2;   // 버튼을 누르면 열릴 스테이지 번호

    private Button unlockBtn;

    void Start()
    {
        unlockBtn = GetComponent<Button>();
        if (unlockBtn != null)
        {
            unlockBtn.onClick.AddListener(OnUnlockClicked);
        }
    }

    void OnUnlockClicked()
    {
        // GameSettings에 해금 요청
        GameSettings.UnlockStage(stageToUnlock);

        Debug.Log($"Stage {stageToUnlock} 해금 완료!");
    }
}