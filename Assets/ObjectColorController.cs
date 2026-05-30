using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ObjectColorController : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private MaterialPropertyBlock _propBlock;

    [Range(0f, 1f)]
    public float saturation = 0f; // 0: 흑백, 1: 원본 컬러
    public Color tintColor = Color.white;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        // 실시간으로 채도 값을 업데이트 (에디터 테스트용)
        ApplyColorSettings();
    }

    public void ApplyColorSettings()
    {
        // 쉐이더의 프로퍼티 값을 가져와서 적용
        _spriteRenderer.GetPropertyBlock(_propBlock);
        
        // 쉐이더 그래프에서 설정한 변수 이름(_Saturation)과 일치해야 합니다.
        _propBlock.SetFloat("_Saturation", saturation);
        _propBlock.SetColor("_Color", tintColor);
        
        _spriteRenderer.SetPropertyBlock(_propBlock);
    }

    // 외부(탄환 충돌 등)에서 채도를 부드럽게 올릴 때 사용할 메서드
    public void SetSaturation(float value)
    {
        saturation = Mathf.Clamp01(value);
        ApplyColorSettings();
    }
}