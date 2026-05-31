using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// ──────────────────────────────────────────────────────────────────
// GrayscaleRendererFeature  (URP 17 / RenderGraph 네이티브)
//
// 세팅 방법:
//   1. URP Universal Renderer Data 에셋 선택
//   2. Add Renderer Feature → GrayscaleRendererFeature 추가
//   3. Grayscale Material 필드에 GrayscaleStencilBlit 셰이더 머티리얼 할당
//
// 동작:
//   - Pass 1: 화면 색상을 임시 RT에 복사
//   - Pass 2: 임시 RT → 화면에 그레이스케일 블릿
//             Stencil=1인 픽셀 (ColorKeeper 오브젝트)은 셰이더가 건너뜀
// ──────────────────────────────────────────────────────────────────
public class GrayscaleRendererFeature : ScriptableRendererFeature
{
    // ── 인스펙터 설정 ────────────────────────────────────────────
    [Serializable]
    public class Settings
    {
        [Tooltip("GrayscaleStencilBlit 셰이더로 만든 머티리얼을 할당하세요")]
        public Material grayscaleMaterial;

        [Range(0f, 1f)]
        [Tooltip("0 = 완전 흑백 / 1 = 풀컬러 (NewColorManager가 자동 갱신)")]
        public float saturation = 0f;
    }

    public Settings settings = new Settings();

    // NewColorManager에서 참조
    public static GrayscaleRendererFeature Instance { get; private set; }

    private GrayscaleBlitPass _blitPass;

    // ── ScriptableRendererFeature ────────────────────────────────
    public override void Create()
    {
        Instance = this;
        _blitPass = new GrayscaleBlitPass(settings)
        {
            // 투명 오브젝트 렌더링 완료 후, 포스트 프로세싱 전에 실행
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.grayscaleMaterial == null)
        {
            Debug.LogWarning("[GrayscaleRendererFeature] grayscaleMaterial이 비어 있습니다.", this);
            return;
        }
        // 게임 카메라에만 적용
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        renderer.EnqueuePass(_blitPass);
    }

    /// <summary>NewColorManager에서 매 프레임 호출됩니다.</summary>
    public void SetSaturation(float value)
    {
        settings.saturation = Mathf.Clamp01(value);
    }

    protected override void Dispose(bool disposing) { }

    // ══════════════════════════════════════════════════════════════
    // 내부 렌더 패스 (RenderGraph 네이티브)
    // ══════════════════════════════════════════════════════════════
    private class GrayscaleBlitPass : ScriptableRenderPass
    {
        private readonly Settings _settings;

        // ── PassData 정의 ─────────────────────────────────────────
        private class CopyPassData
        {
            internal TextureHandle source;
        }

        private class GrayscalePassData
        {
            internal TextureHandle source;
            internal Material      material;
            internal float         saturation;
        }

        public GrayscaleBlitPass(Settings settings)
        {
            _settings = settings;
        }

        // ── RenderGraph 진입점 ────────────────────────────────────
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_settings.grayscaleMaterial == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData   = frameData.Get<UniversalCameraData>();

            TextureHandle activeColor = resourceData.activeColorTexture;
            TextureHandle activeDepth = resourceData.activeDepthTexture;

            // 임시 텍스처 생성 (색상 복사용, 깊이 없음)
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            TextureHandle tempTexture = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_GrayscaleTemp", false, FilterMode.Bilinear
            );

            // ── Pass 1: 현재 화면 색상 → 임시 RT ─────────────────
            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>(
                "GrayscaleCopyColor", out var passData))
            {
                passData.source = activeColor;

                builder.UseTexture(activeColor, AccessFlags.Read);
                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.WriteAll);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((CopyPassData data, RasterGraphContext ctx) =>
                {
                    ExecuteCopyPass(ctx.cmd, data.source);
                });
            }

            // ── Pass 2: 임시 RT → 화면 블릿 (스텐실 제외) ────────
            using (var builder = renderGraph.AddRasterRenderPass<GrayscalePassData>(
                "GrayscaleStencilBlit", out var passData))
            {
                passData.source     = tempTexture;
                passData.material   = _settings.grayscaleMaterial;
                passData.saturation = _settings.saturation;

                builder.UseTexture(tempTexture, AccessFlags.Read);
                builder.SetRenderAttachment(activeColor, 0, AccessFlags.WriteAll);
                // Stencil 값이 보존된 depth 버퍼를 읽기 전용으로 바인딩
                builder.SetRenderAttachmentDepth(activeDepth, AccessFlags.Read);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((GrayscalePassData data, RasterGraphContext ctx) =>
                {
                    ExecuteGrayscalePass(ctx.cmd, data.source, data.material, data.saturation);
                });
            }
        }

        // ── 정적 실행 함수 ────────────────────────────────────────

        private static void ExecuteCopyPass(RasterCommandBuffer cmd, RTHandle source)
        {
            Blitter.BlitTexture(cmd, source, new Vector4(1f, 1f, 0f, 0f), 0, false);
        }

        private static void ExecuteGrayscalePass(
            RasterCommandBuffer cmd, RTHandle source, Material material, float saturation)
        {
            material.SetFloat("_Saturation", saturation);
            Blitter.BlitTexture(cmd, source, new Vector4(1f, 1f, 0f, 0f), material, 0);
        }
    }
}
