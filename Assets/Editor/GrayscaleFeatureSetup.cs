using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 실행 후 이 파일은 삭제해도 됩니다
public static class GrayscaleFeatureSetup
{
    [MenuItem("Tools/Meowody/Setup Grayscale Renderer Feature")]
    public static void Setup()
    {
        // 1. Renderer2D 에셋 로드
        var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
            "Assets/Settings/Renderer2D.asset");

        if (rendererData == null)
        {
            Debug.LogError("[Setup] Renderer2D.asset을 찾을 수 없습니다.");
            return;
        }

        // 2. 이미 등록되어 있으면 머티리얼만 재할당
        foreach (var existing in rendererData.rendererFeatures)
        {
            if (existing is GrayscaleRendererFeature existingFeature)
            {
                Debug.Log("[Setup] GrayscaleRendererFeature가 이미 등록되어 있습니다. 머티리얼만 재할당합니다.");
                AssignMaterial(existingFeature, rendererData);
                return;
            }
        }

        // 3. Feature 인스턴스 생성
        var feature = ScriptableObject.CreateInstance<GrayscaleRendererFeature>();
        feature.name = "GrayscaleRendererFeature";

        // 4. Renderer Data 에셋에 서브 에셋으로 추가
        AssetDatabase.AddObjectToAsset(feature, rendererData);

        // 5. rendererFeatures 리스트에 추가
        rendererData.rendererFeatures.Add(feature);

        // 6. 머티리얼 할당
        AssignMaterial(feature, rendererData);

        // 7. 저장
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Setup] GrayscaleRendererFeature 등록 완료! Renderer2D.asset을 확인하세요.");
    }

    private static void AssignMaterial(GrayscaleRendererFeature feature, ScriptableRendererData rendererData)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/GrayscaleStencilMat.mat");

        if (mat == null)
        {
            Debug.LogWarning("[Setup] GrayscaleStencilMat.mat을 찾을 수 없습니다.");
            return;
        }

        feature.settings.grayscaleMaterial = mat;
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Setup] GrayscaleStencilMat 머티리얼 할당 완료!");
    }
}
