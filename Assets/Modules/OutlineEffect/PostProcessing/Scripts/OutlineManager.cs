using JSLCore;
using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    public class OutlineManager : Singleton<OutlineManager>
    {
        public readonly int COLOR_ID = Shader.PropertyToID("_Color");
        private readonly Dictionary<OutlinePrepassType, Shader> m_prepassShaders = new Dictionary<OutlinePrepassType, Shader>
    {
        { OutlinePrepassType.SolidColor, Shader.Find("Outline/Prepass/SolidColor") },
        { OutlinePrepassType.SolidColorDepth, Shader.Find("Outline/Prepass/SolidColorDepth") },
        { OutlinePrepassType.Alpha, Shader.Find("Outline/Prepass/Alpha") },
        { OutlinePrepassType.AlphaDepth, Shader.Find("Outline/Prepass/AlphaDepth") },
    };

        private List<OutlineData> m_outlineDatas = new List<OutlineData>();
        private bool m_initialized;

        public Camera camera
        {
            get
            {
                if (m_camera == null)
                {
                    m_camera = Camera.main;
                }

                return m_camera;
            }
        }
        private Camera m_camera;

        public PostProcessVolume postProcessVolume
        {
            get
            {
                if (m_postProcessVolume == null)
                {
                    if (camera != null)
                    {
                        m_postProcessVolume = camera.GetComponent<PostProcessVolume>();
                    }
                    else
                    {
                        m_postProcessVolume = GameObject.FindObjectOfType<PostProcessVolume>();
                    }
                }

                return m_postProcessVolume;
            }
        }
        private PostProcessVolume m_postProcessVolume;

        public Outline outline
        {
            get
            {
                if (postProcessVolume != null)
                {
                    m_outline = postProcessVolume.profile.GetSetting<Outline>();

                    if (m_outline == null)
                    {
                        m_outline = postProcessVolume.profile.AddSettings<Outline>();
                    }
                }

                return m_outline;
            }
        }
        private Outline m_outline;

        public Shader GetPrepassShader(OutlinePrepassType outlinePrepassType)
        {
            return m_prepassShaders[outlinePrepassType];
        }

        public void Register(GameObject parent, Color color, OutlinePrepassType outlinePrepassType)
        {
            Register(new OutlineData(parent, color, outlinePrepassType));
        }

        public void Register(OutlineData outlineData)
        {
            if (outlineData.renderers == null || outlineData.renderers.Length == 0)
            {
                return;
            }

            if (m_outlineDatas.Contains(outlineData))
            {
                return;
            }

            m_outlineDatas.Add(outlineData);
            UpdateOutlineActive();
        }

        public void Unregister(OutlineData outlineData)
        {
            Unregister(outlineData.parent);
        }

        public void Unregister(GameObject parent)
        {
            for (int i = 0, count = m_outlineDatas.Count; i < count; i++)
            {
                if (m_outlineDatas[i].parent == parent)
                {
                    m_outlineDatas.RemoveAt(i);
                    UpdateOutlineActive();
                    break;
                }
            }
        }

        public void ExecuteCommandBuffer(CommandBuffer commandBuffer)
        {
            for (int i = 0, count = m_outlineDatas.Count; i < count; i++)
            {
                for (int j = 0; j < m_outlineDatas[i].renderers.Length; j++)
                {
                    commandBuffer.DrawRenderer(m_outlineDatas[i].renderers[j], m_outlineDatas[i].prepassMaterial);
                }
            }
        }

        private void UpdateOutlineActive()
        {
            if (outline == null)
            {
                return;
            }

            outline.active = m_outlineDatas.Count > 0;
        }
    }
}