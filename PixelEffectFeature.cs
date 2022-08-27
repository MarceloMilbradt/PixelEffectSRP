using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public class PixelEffectFeature : ScriptableRendererFeature {

        [System.Serializable]
        public class PixelEffectFeatureSettings {

            public LayerMask layerMask = 0;

            public RenderPassEvent Event = RenderPassEvent.BeforeRenderingTransparents;
            
            public Material blitMat = null;

            [Range(1f, 15f)]
            public float pixelDensity = 1f;
        }

        public PixelEffectFeatureSettings settings = new PixelEffectFeatureSettings();

        PixelEffectFeaturePass pass;

        public override void Create() {
            pass = new PixelEffectFeaturePass(settings.Event, settings.blitMat, settings.pixelDensity, settings.layerMask);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(pass);
        }

    }
}
