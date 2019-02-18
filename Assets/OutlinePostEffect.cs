using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class OutlinePostEffect : PostEffectBase
{
    [SerializeField] private Transform[] m_targets;
    [SerializeField] private Color[] m_colors;
    private OutlinePrepassCommandBuffer[] m_outlinePrepassCommandBuffers;
    
    public Shader prepassShader = null;
    //采样率
    public float samplerScale = 1;
    //降采样
    public int downSample = 1;
    //迭代次数
    public int iteration = 2;
    //描边颜色
    public Color outLineColor = Color.green;
    //描边强度
    [Range(0.0f, 10.0f)]
    public float outLineStrength = 3.0f;

    private RenderTexture m_renderTexture;

    private void Awake()
    {
        if (m_renderTexture == null)
        {
            m_renderTexture = RenderTexture.GetTemporary(Screen.width >> downSample, Screen.height >> downSample, 0);
        }

        m_outlinePrepassCommandBuffers = new OutlinePrepassCommandBuffer[m_targets.Length];
        for (int i = 0; i < m_outlinePrepassCommandBuffers.Length; i++)
        {
            m_outlinePrepassCommandBuffers[i] = new OutlinePrepassCommandBuffer(m_targets[i], prepassShader, m_colors[i], downSample);
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < m_outlinePrepassCommandBuffers.Length; i++)
        {
            m_outlinePrepassCommandBuffers[i].Destroy();
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        for(int i = 0; i < m_outlinePrepassCommandBuffers.Length; i++)
        {
            m_outlinePrepassCommandBuffers[i].Execute();
        }

        if (_Material && m_renderTexture)
        {
            //对RT进行Blur处理
            RenderTexture temp1 = RenderTexture.GetTemporary(source.width >> downSample, source.height >> downSample, 0);
            RenderTexture temp2 = RenderTexture.GetTemporary(source.width >> downSample, source.height >> downSample, 0);

            //高斯模糊，两次模糊，横向纵向，使用pass0进行高斯模糊
            _Material.SetVector("_offsets", new Vector4(0, samplerScale, 0, 0));
            Graphics.Blit(m_renderTexture, temp1, _Material, 0);
            _Material.SetVector("_offsets", new Vector4(samplerScale, 0, 0, 0));
            Graphics.Blit(temp1, temp2, _Material, 0);

            //如果有叠加再进行迭代模糊处理
            for (int i = 0; i < iteration; i++)
            {
                _Material.SetVector("_offsets", new Vector4(0, samplerScale, 0, 0));
                Graphics.Blit(temp2, temp1, _Material, 0);
                _Material.SetVector("_offsets", new Vector4(samplerScale, 0, 0, 0));
                Graphics.Blit(temp1, temp2, _Material, 0);
            }

            //用模糊图和原始图计算出轮廓图
            _Material.SetTexture("_BlurTex", temp2);
            Graphics.Blit(m_renderTexture, temp1, _Material, 1);

            //轮廓图和场景图叠加
            _Material.SetTexture("_BlurTex", temp1);
            _Material.SetFloat("_OutlineStrength", outLineStrength);
            Graphics.Blit(source, destination, _Material, 2);

            RenderTexture.ReleaseTemporary(temp1);
            RenderTexture.ReleaseTemporary(temp2);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }


}