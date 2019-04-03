using JSLCore;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

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
    private Outline m_outline;
    private bool m_initialized;

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
        for(int i = 0, count = m_outlineDatas.Count; i < count; i++)
        {
            if(m_outlineDatas[i].parent == parent)
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
        GetOutline();

        if (m_outline == null)
        {
            return;
        }
        
        m_outline.active = m_outlineDatas.Count > 0;
    }

    private void GetOutline()
    {
        if(m_initialized)
        {
            return;
        }

        m_initialized = true;

        //Find Outline Settings from Main Camera
        if (m_outline == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            PostProcessVolume postProcessVolume = mainCamera.GetComponent<PostProcessVolume>();
            if (postProcessVolume == null || postProcessVolume.sharedProfile == null)
            {
                return;
            }

            m_outline = postProcessVolume.sharedProfile.GetSetting<Outline>();
        }

        //Find Outline Settings from PostProcessVolume
        if (m_outline == null)
        {
            PostProcessVolume postProcessVolume = GameObject.FindObjectOfType<PostProcessVolume>();
            if (postProcessVolume == null || postProcessVolume.sharedProfile == null)
            {
                return;
            }

            m_outline = postProcessVolume.sharedProfile.GetSetting<Outline>();
        }
    }
}
