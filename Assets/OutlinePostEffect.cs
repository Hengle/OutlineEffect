using UnityEngine;
using System.Collections.Generic;

public class OutlinePostEffect : BasePostEffect
{
    [SerializeField] private Shader m_prepassShader = null;
    [SerializeField] private float m_samplerScale = 1;
    [SerializeField] private int m_downSample = 1;
    [SerializeField] private int m_iteration = 2;
    [Range(0.0f, 10.0f)]
    [SerializeField] private float m_outlineStrength = 3.0f;
    [SerializeField] private Transform[] m_targets;
    [SerializeField] private Color[] m_outlineColors;

    private List<OutlinePrepassCommandBuffer> m_outlinePrepassCommandBuffers;
    private RenderTexture m_renderTexture;

    private void Awake()
    {
        if (m_renderTexture == null)
        {
            m_renderTexture = RenderTexture.GetTemporary(Screen.width >> m_downSample, Screen.height >> m_downSample, 0);
        }

        m_outlinePrepassCommandBuffers = new List<OutlinePrepassCommandBuffer>();

        Renderer[] cacheRenderers;
        for (int i = 0; i < m_targets.Length; i++)
        {
            cacheRenderers = m_targets[i].GetComponentsInChildren<Renderer>();
            if(cacheRenderers == null || cacheRenderers.Length == 0)
            {
                continue;
            }

            m_outlinePrepassCommandBuffers.Add(new OutlinePrepassCommandBuffer(cacheRenderers, m_renderTexture, new Material(m_prepassShader), m_outlineColors[i], i == 0));
        }
    }

    private void OnDisable()
    {
        if(m_outlinePrepassCommandBuffers == null || m_outlinePrepassCommandBuffers.Count == 0)
        {
            return;
        }

        for (int i = 0; i < m_outlinePrepassCommandBuffers.Count; i++)
        {
            if(m_outlinePrepassCommandBuffers[i] == null)
            {
                continue;
            }

            m_outlinePrepassCommandBuffers[i].Destroy();
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(m_outlinePrepassCommandBuffers == null || m_outlinePrepassCommandBuffers.Count == 0)
        {
            Graphics.Blit(source, destination);
            return;
        }

        for (int i = 0; i < m_outlinePrepassCommandBuffers.Count; i++)
        {
            m_outlinePrepassCommandBuffers[i].Execute();
        }

        if (PostEffectMaterial && m_renderTexture)
        {
            //对RT进行Blur处理
            RenderTexture temp1 = RenderTexture.GetTemporary(source.width >> m_downSample, source.height >> m_downSample, 0);
            RenderTexture temp2 = RenderTexture.GetTemporary(source.width >> m_downSample, source.height >> m_downSample, 0);

            //高斯模糊，两次模糊，横向纵向，使用pass0进行高斯模糊
            PostEffectMaterial.SetVector("_offsets", new Vector4(0, m_samplerScale, 0, 0));
            Graphics.Blit(m_renderTexture, temp1, PostEffectMaterial, 0);
            PostEffectMaterial.SetVector("_offsets", new Vector4(m_samplerScale, 0, 0, 0));
            Graphics.Blit(temp1, temp2, PostEffectMaterial, 0);

            //如果有叠加再进行迭代模糊处理
            for (int i = 0; i < m_iteration; i++)
            {
                PostEffectMaterial.SetVector("_offsets", new Vector4(0, m_samplerScale, 0, 0));
                Graphics.Blit(temp2, temp1, PostEffectMaterial, 0);
                PostEffectMaterial.SetVector("_offsets", new Vector4(m_samplerScale, 0, 0, 0));
                Graphics.Blit(temp1, temp2, PostEffectMaterial, 0);
            }

            //用模糊图和原始图计算出轮廓图
            PostEffectMaterial.SetTexture("_BlurTex", temp2);
            Graphics.Blit(m_renderTexture, temp1, PostEffectMaterial, 1);

            //轮廓图和场景图叠加
            PostEffectMaterial.SetTexture("_BlurTex", temp1);
            PostEffectMaterial.SetFloat("_OutlineStrength", m_outlineStrength);
            Graphics.Blit(source, destination, PostEffectMaterial, 2);

            RenderTexture.ReleaseTemporary(temp1);
            RenderTexture.ReleaseTemporary(temp2);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }


}