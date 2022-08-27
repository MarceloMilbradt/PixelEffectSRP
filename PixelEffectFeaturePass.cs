using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public class PixelEffectFeaturePass : ScriptableRenderPass
    {
        Material blitMat;
        float pixelDensity;
        
        ProfilingSampler m_ProfilingSampler;
        RenderStateBlock m_RenderStateBlock;
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        FilteringSettings m_FilteringSettings;

        static int pixelTexID = Shader.PropertyToID("_PixelTexture");
        static int pixelDepthID = Shader.PropertyToID("_DepthTex");

        private RenderTargetIdentifier _target;
        public void SetTarget(RenderTargetIdentifier target)
        {
            _target = target;
        }
        private RenderTextureDescriptor _BaseDescriptor;
        public PixelEffectFeaturePass(RenderPassEvent renderEvent, Material bM, float pD, int lM) {
            m_ProfilingSampler = new ProfilingSampler("PixelEffect");
            this.renderPassEvent = renderEvent;
            blitMat = bM;
            pixelDensity = pD;
            blitMat.SetFloat("_PixelDensity", pixelDensity);

            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, lM);

            m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));

            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        }
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _BaseDescriptor = cameraTextureDescriptor;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = SortingCriteria.CommonTransparent;

            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            Rect pixelRect = camera.pixelRect;
            int pixelWidth = (int) (camera.pixelWidth / pixelDensity);
            int pixelHeight = (int) (camera.pixelHeight / pixelDensity);
            CommandBuffer cmd = CommandBufferPool.Get("PixelEffect");
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                

                cmd.GetTemporaryRT(pixelTexID, pixelWidth, pixelHeight, 0, FilterMode.Point);
                cmd.GetTemporaryRT(pixelDepthID, pixelWidth, pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);

                cmd.SetRenderTarget(pixelTexID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                                   pixelDepthID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

                cmd.ClearRenderTarget(true, true, Color.clear);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock);

                cmd.SetRenderTarget(cameraData.renderer.cameraColorTargetHandle, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);

                cmd.Blit(new RenderTargetIdentifier(pixelTexID), BuiltinRenderTextureType.CurrentActive, blitMat);

                cmd.ReleaseTemporaryRT(pixelTexID);
                cmd.ReleaseTemporaryRT(pixelDepthID);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
            }
            context.ExecuteCommandBuffer(cmd);
            context.Submit();
            CommandBufferPool.Release(cmd);
        }
    }
}
