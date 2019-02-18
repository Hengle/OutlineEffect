using UnityEngine;
using UnityEngine.Rendering;

public class OutlinePrepassCommandBuffer
{
    private readonly int OUTLINE_COLOR_ID = Shader.PropertyToID("_OutlineCol");
    
    private RenderTexture m_renderTexture;
    private Material m_prepassMaterial;
    private bool m_clear;
    private CommandBuffer m_commandBuffer;

    public OutlinePrepassCommandBuffer(Renderer[] renderers, RenderTexture renderTexture, Material prepassMaterial, Color color, bool clear)
    {   
        m_renderTexture = renderTexture;
        m_prepassMaterial = prepassMaterial;
        m_prepassMaterial.SetColor(OUTLINE_COLOR_ID, color);
        m_clear = clear;

        UpdateCommandBuffer(renderers);
    }
    
    private void UpdateCommandBuffer(Renderer[] renderers)
    {
        m_commandBuffer = new CommandBuffer();
        m_commandBuffer.SetRenderTarget(m_renderTexture);

        if(m_clear)
        {
            m_commandBuffer.ClearRenderTarget(true, true, Color.black);
        }
        
        foreach (Renderer r in renderers)
        {
            m_commandBuffer.DrawRenderer(r, m_prepassMaterial);
        }
    }

    public void Execute()
    {
        Graphics.ExecuteCommandBuffer(m_commandBuffer);
    }

    public void Destroy()
    {
        if (m_prepassMaterial)
        {
            GameObject.DestroyImmediate(m_prepassMaterial);
            m_prepassMaterial = null;
        }
        
        if (m_commandBuffer != null)
        {
            m_commandBuffer.Release();
            m_commandBuffer = null;
        }

    }
}
