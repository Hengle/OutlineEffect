using System;

namespace UnityEngine.Rendering.PostProcessing
{
    public enum PrepassType
    {
        SolidColor,
        SolidColorDepth,
        Alpha,
        AlphaDepth,
    }

    [Serializable]
    public class PrepassTypeParamter : ParameterOverride<PrepassType>
    {

    }

    [Serializable]
    [PostProcess(typeof(OutlineRenderer), PostProcessEvent.AfterStack, "Custom/Outline")]
    public sealed class Outline : PostProcessEffectSettings
    {
        public PrepassTypeParamter prepassType = new PrepassTypeParamter { value = PrepassType.SolidColor };

        [Range(1, 5)]
        public IntParameter downsample = new IntParameter { value = 1 };

        [Range(1f, 10f)]
        public FloatParameter blurOffset = new FloatParameter { value = 1f };

        [Range(1, 10)]
        public IntParameter iteration = new IntParameter { value = 1 };

        [Range(1f, 10f)]
        public FloatParameter strength = new FloatParameter { value = 5f };

        [Header("Debugs")]
        public BoolParameter debugPrepass = new BoolParameter { value = false };
        public BoolParameter debugBlur = new BoolParameter { value = false };
        public BoolParameter debugCulling = new BoolParameter { value = false };
    }

    internal sealed class OutlineRenderer : PostProcessEffectRenderer<Outline>
    {
        private const string SHADER_NAME_OUTLINE = "Hidden/Custom/Outline";
        private const string SHADER_NAME_PREPASS_SOLID_COLOR = "Outline/Prepass/SolidColor";
        private const string SHADER_NAME_PREPASS_SOLID_COLOR_DEPTH = "Outline/Prepass/SolidColorDepth";
        private const string SHADER_NAME_PREPASS_ALPHA = "Outline/Prepass/Alpha";
        private const string SHADER_NAME_PREPASS_ALPHA_DEPTH = "Outline/Prepass/AlphaDepth";

        private const string PROPERTY_NAME_PREPASS_RT = "_PrepassRT";
        private const string PROPERTY_NAME_OFFSET = "_Offset";
        private const string PROPERTY_NAME_BLUR_TEX = "_BlurTex";
        private const string PROPERTY_NAME_STRENGTH = "_Strength";

        private Shader m_shader;
        private Shader m_prepassShader;
        private PrepassType m_lastPrepassType = PrepassType.SolidColor;
        private int m_prepassRT;
        private int m_tempRT1;
        private int m_tempRT2;
        private int m_offsetID;
        private int m_blurTexID;
        private int m_strengthID;

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        public override void Init()
        {
            UpdatePrepassShader();

            m_shader = Shader.Find(SHADER_NAME_OUTLINE);
            m_prepassRT = Shader.PropertyToID(PROPERTY_NAME_PREPASS_RT);
            m_offsetID = Shader.PropertyToID(PROPERTY_NAME_OFFSET);
            m_blurTexID = Shader.PropertyToID(PROPERTY_NAME_BLUR_TEX);
            m_strengthID = Shader.PropertyToID(PROPERTY_NAME_STRENGTH);

            base.Init();
        }

        private void UpdatePrepassShader()
        {
            if(m_prepassShader != null && m_lastPrepassType == settings.prepassType)
            {
                return;
            }

            m_lastPrepassType = settings.prepassType;
            switch(m_lastPrepassType)
            {
                case PrepassType.SolidColor:
                    m_prepassShader = Shader.Find(SHADER_NAME_PREPASS_SOLID_COLOR);
                    break;
                case PrepassType.SolidColorDepth:
                    m_prepassShader = Shader.Find(SHADER_NAME_PREPASS_SOLID_COLOR_DEPTH);
                    break;
                case PrepassType.Alpha:
                    m_prepassShader = Shader.Find(SHADER_NAME_PREPASS_ALPHA);
                    break;
                case PrepassType.AlphaDepth:
                    m_prepassShader = Shader.Find(SHADER_NAME_PREPASS_ALPHA_DEPTH);
                    break;
            }

            OutlineManager.Instance.SetPrepassShader(m_prepassShader);
        }

        public override void Render(PostProcessRenderContext context)
        {
            UpdatePrepassShader();

            var sheet = context.propertySheets.Get(m_shader);

            context.command.GetTemporaryRT(m_prepassRT, context.camera.pixelWidth >> settings.downsample, context.camera.pixelHeight >> settings.downsample);
            context.command.SetRenderTarget(m_prepassRT);
            context.command.ClearRenderTarget(false, true, Color.clear);

            OutlineManager.Instance.ExecuteCommandBuffer(context.command);

            if (settings.debugPrepass)
            {
                context.command.BlitFullscreenTriangle(m_prepassRT, context.destination);
                return;
            }

            context.GetScreenSpaceTemporaryRT(context.command, m_tempRT1);
            context.GetScreenSpaceTemporaryRT(context.command, m_tempRT2);

            sheet.properties.SetVector(m_offsetID, new Vector4(0, settings.blurOffset, 0, 0));
            context.command.BlitFullscreenTriangle(m_prepassRT, m_tempRT1, sheet, 0);
            sheet.properties.SetVector(m_offsetID, new Vector4(settings.blurOffset, 0, 0, 0));
            context.command.BlitFullscreenTriangle(m_tempRT1, m_tempRT2, sheet, 0);

            if(settings.iteration > 0)
            {
                for(int i = 0; i < settings.iteration; i++)
                {
                    sheet.properties.SetVector(m_offsetID, new Vector4(0, settings.blurOffset, 0, 0));
                    context.command.BlitFullscreenTriangle(m_tempRT2, m_tempRT1, sheet, 0);
                    sheet.properties.SetVector(m_offsetID, new Vector4(settings.blurOffset, 0, 0, 0));
                    context.command.BlitFullscreenTriangle(m_tempRT1, m_tempRT2, sheet, 0);
                }
            }

            if(settings.debugBlur)
            {
                context.command.BlitFullscreenTriangle(m_tempRT2, context.destination);
                context.command.ReleaseTemporaryRT(m_tempRT1);
                context.command.ReleaseTemporaryRT(m_tempRT2);
                return;
            }

            context.command.SetGlobalTexture(m_blurTexID, m_tempRT2);
            context.command.BlitFullscreenTriangle(m_prepassRT, m_tempRT1, sheet, 1);

            if(settings.debugCulling)
            {
                context.command.BlitFullscreenTriangle(m_tempRT1, context.destination);
                context.command.ReleaseTemporaryRT(m_tempRT1);
                context.command.ReleaseTemporaryRT(m_tempRT2);
                return;
            }

            sheet.properties.SetFloat(m_strengthID, settings.strength);
            context.command.SetRenderTarget(m_blurTexID, m_tempRT1);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 2);
            context.command.ReleaseTemporaryRT(m_tempRT1);
            context.command.ReleaseTemporaryRT(m_tempRT2);
        }
    }
}
