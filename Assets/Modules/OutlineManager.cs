using JSLCore;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class OutlineManager : Singleton<OutlineManager>
{
    private readonly int COLOR_ID = Shader.PropertyToID("_Color");

    private List<OutlineComponent> m_outlineComponents = new List<OutlineComponent>();
    private List<Renderer> m_cacheRenderers = new List<Renderer>();
    private Material m_prepassMaterial;

    public void Register(OutlineComponent outlineComponent)
    {
        if (m_outlineComponents.Contains(outlineComponent))
        {
            return;
        }

        m_outlineComponents.Add(outlineComponent);
    }

    public void Unregister(OutlineComponent outlineComponent)
    {
        m_outlineComponents.Remove(outlineComponent);
    }

    public void ExecuteCommandBuffer(CommandBuffer commandBuffer)
    {
        if (m_prepassMaterial == null)
        {
            m_prepassMaterial = new Material(Shader.Find("FS/Outline/Prepass/SolidColor"));
        }

        for (int i = 0, count = m_outlineComponents.Count; i < count; i++)
        {
            m_prepassMaterial.SetColor(COLOR_ID, m_outlineComponents[i].color);
            for (int j = 0; j < m_outlineComponents[i].renderers.Length; j++)
            {
                commandBuffer.DrawRenderer(m_outlineComponents[i].renderers[j], m_prepassMaterial);
            }
        }
    }
}
