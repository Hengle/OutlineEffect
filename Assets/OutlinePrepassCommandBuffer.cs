using UnityEngine;
using UnityEngine.Rendering;

public class OutlinePrepassCommandBuffer
{
    private Transform m_target;
    private Shader m_prepassShader;
    private Color m_color;
    private int m_downsample;

    public OutlinePrepassCommandBuffer(Transform target, Shader prepassShader, Color color, int downsample)
    {
        if(m_target == null || prepassShader == null)
        {
            return;
        }

        m_target = target;
        m_prepassShader = prepassShader;
        m_color = color;
        m_downsample = downsample;

        UpdateCommandBuffer();
    }

    private Material m_prepassMaterial;
    private RenderTexture m_renderTexture;
    private CommandBuffer m_commandBuffer;
    private void UpdateCommandBuffer()
    {
        if (m_prepassMaterial == null)
        {
            m_prepassMaterial = new Material(m_prepassShader);
        }
            
        Renderer[] renderers = m_target.GetComponentsInChildren<Renderer>();
        if (m_renderTexture == null)
        {
            m_renderTexture = RenderTexture.GetTemporary(Screen.width >> m_downsample, Screen.height >> m_downsample, 0);
        }

        m_commandBuffer = new CommandBuffer();
        m_commandBuffer.SetRenderTarget(m_renderTexture);
        m_commandBuffer.ClearRenderTarget(true, true, Color.black);
        foreach (Renderer r in renderers)
        {
            m_commandBuffer.DrawRenderer(r, m_prepassMaterial);
        }
    }

    public void Execute()
    {
        m_prepassMaterial.SetColor("_OutlineCol", m_color);
        Graphics.ExecuteCommandBuffer(m_commandBuffer);
    }

    public void Destroy()
    {
        if (m_prepassMaterial)
        {
            GameObject.DestroyImmediate(m_prepassMaterial);
            m_prepassMaterial = null;
        }

        if (m_renderTexture)
        {
            RenderTexture.ReleaseTemporary(m_renderTexture);
            m_renderTexture = null;
        }
        
        if (m_commandBuffer != null)
        {
            m_commandBuffer.Release();
            m_commandBuffer = null;
        }

    }
}
