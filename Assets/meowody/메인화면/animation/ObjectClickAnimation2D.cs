using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IndividualLayerAnimation : MonoBehaviour
{
    [Header("클릭할 오브젝트의 콜라이더들을 순서대로 넣어주세요")]
    public List<Collider2D> targetColliders = new List<Collider2D>();

    [Header("설정")]
    [SerializeField] private float duration = 2.0f;

    private bool isAnyAnimationPlaying = false;

    void Update()
    {
        // 애니메이션 재생 중이면 클릭 무시
        if (isAnyAnimationPlaying) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            for (int i = 0; i < targetColliders.Count; i++)
            {
                if (targetColliders[i] != null && targetColliders[i].OverlapPoint(mousePos))
                {
                    StartCoroutine(PlayChildAnimation(i));
                    break;
                }
            }
        }
    }

    IEnumerator PlayChildAnimation(int index)
    {
        isAnyAnimationPlaying = true;

        // 리스트 순서에 따라 isClicked1, isClicked2... 호출
        string paramName = "isClicked" + (index + 1);
        Animator anim = targetColliders[index].GetComponent<Animator>();

        if (anim != null)
        {
            // Base Layer의 Idle은 계속 돌아가는 상태에서 
            // Click Layer의 애니메이션을 트리거함
            anim.SetBool(paramName, true);

            yield return new WaitForSeconds(duration);

            anim.SetBool(paramName, false);
        }

        isAnyAnimationPlaying = false;
    }
}