using JSLCore;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class OutlineManager : Singleton<OutlineManager>
{
    private readonly int COLOR_ID = Shader.PropertyToID("_Color");

    private List<OutlineComponent> m_outlineComponents = new List<OutlineComponent>();
    private Dictionary<Color, Material> m_prepassMaterials = new Dictionary<Color, Material>();
    private List<Material> m_materials = new List<Material>();
    private Material m_prepassMaterial;
    private Shader m_prepassShader;

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

    public void SetPrepassShader(Shader shader)
    {
        m_prepassShader = shader;

        for(int i = 0, count = m_materials.Count; i < count; i++)
        {
            if(m_materials[i] == null)
            {
                continue;
            }

            Color color = m_materials[i].GetColor(COLOR_ID);

            m_materials[i] = new Material(m_prepassShader);
            m_materials[i].SetColor(COLOR_ID, color);

            m_prepassMaterials[color] = m_materials[i];
        }
    }

    public void ExecuteCommandBuffer(CommandBuffer commandBuffer)
    {
        for (int i = 0, count = m_outlineComponents.Count; i < count; i++)
        {
            m_prepassMaterials.TryGetValue(m_outlineComponents[i].color, out m_prepassMaterial);
            if(m_prepassMaterial == null)
            {
                m_prepassMaterial = new Material(m_prepassShader);
                m_prepassMaterial.SetColor(COLOR_ID, m_outlineComponents[i].color);

                m_prepassMaterials.Add(m_outlineComponents[i].color, m_prepassMaterial);
                m_materials.Add(m_prepassMaterial);
            }

            for (int j = 0; j < m_outlineComponents[i].renderers.Length; j++)
            {
                commandBuffer.DrawRenderer(m_outlineComponents[i].renderers[j], m_prepassMaterial);
            }
        }
    }
}
