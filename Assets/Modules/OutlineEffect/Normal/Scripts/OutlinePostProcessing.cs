using UnityEngine;
using System.Collections.Generic;
using JSLCore;

namespace TA.PostProcessing.Outline
{
    public class OutlinePostProcessing : MonoSingleton<OutlinePostProcessing>
    {
        private readonly int OFFSET_ID = Shader.PropertyToID("_Offset");
        private readonly int BLUR_TEX_ID = Shader.PropertyToID("_BlurTex");
        private readonly int OUTLINE_STRENGTH_ID = Shader.PropertyToID("_OutlineStrength");

        public Shader postEffectShader = null;
        public Shader prepassSolidColorShader = null;
        [SerializeField] [Range(1f, 10f)]private float m_samplerScale = 2f;
        [SerializeField] [Range(1, 5)] private int m_downsample = 1;
        [SerializeField] [Range(1, 10)] private int m_iteration = 1;
        [Range(1f, 10f)]
        [SerializeField] private float m_outlineStrength = 5f;

        private Camera m_camera;
        private int m_currentDownsample = -1;
        private RenderTexture m_prepassRenderTexture;
        private Dictionary<GameObject, OutlineCommandBuffer> m_commandBuffers = new Dictionary<GameObject, OutlineCommandBuffer>();
        private List<Renderer> m_cacheRenderers = new List<Renderer>();

        public Material PostEffectMaterial
        {
            get
            {
                if (m_postEffectMaterial == null)
                {
                    m_postEffectMaterial = CreateMaterial(postEffectShader);
                }

                return m_postEffectMaterial;
            }
        }
        private Material m_postEffectMaterial;

        private Material CreateMaterial(Shader shader)
        {
            if (shader == null)
            {
                return null;
            }

            if (!shader.isSupported)
            {
                return null;
            }

            Material material = new Material(shader);
            material.hideFlags = HideFlags.DontSave;

            return material;
        }

        private void Awake()
        {
            m_camera = GetComponent<Camera>();
            GeneratePrePassRenderTexture();
        }

        private void GeneratePrePassRenderTexture()
        {
            if(m_downsample <= 0)
            {
                m_downsample = 0;
            }

            if(m_currentDownsample == m_downsample)
            {
                return;
            }

            m_currentDownsample = m_downsample;

            if(m_prepassRenderTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_prepassRenderTexture);
                m_prepassRenderTexture = null;
            }

            if(m_camera != null)
            {
                m_prepassRenderTexture = RenderTexture.GetTemporary(m_camera.pixelWidth >> m_currentDownsample, m_camera.pixelHeight >> m_currentDownsample, 0);
            }
            else
            {
                m_prepassRenderTexture = RenderTexture.GetTemporary(Screen.width >> m_currentDownsample, Screen.height >> m_currentDownsample, 0);
            }

            foreach (KeyValuePair<GameObject, OutlineCommandBuffer> kvp in m_commandBuffers)
            {
                kvp.Value.SetRenderTexture(m_prepassRenderTexture);
            }

            UpdateCommandBuffers();
        }

        private void OnValidate()
        {
            GeneratePrePassRenderTexture();
        }

        protected override void OnDestroy()
        {
            if (m_commandBuffers != null)
            {
                foreach (KeyValuePair<GameObject, OutlineCommandBuffer> kvp in m_commandBuffers)
                {
                    kvp.Value.Destroy();
                }

                m_commandBuffers.Clear();
            }

            if(m_prepassRenderTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_prepassRenderTexture);
                m_prepassRenderTexture = null;
            }
        }

        public void Add(GameObject targetObject, Color color)
        {
            if (m_commandBuffers.ContainsKey(targetObject))
            {
                m_commandBuffers[targetObject].SetColor(color);
            }
            else
            {
                m_cacheRenderers.Clear();
                m_cacheRenderers.AddRange(targetObject.GetComponentsInChildren<SkinnedMeshRenderer>());
                m_cacheRenderers.AddRange(targetObject.GetComponentsInChildren<MeshRenderer>());

                if (m_cacheRenderers == null || m_cacheRenderers.Count == 0)
                {
                    return;
                }

                m_commandBuffers.Add(targetObject, new OutlineCommandBuffer(m_cacheRenderers.ToArray(), m_prepassRenderTexture, new Material(prepassSolidColorShader), color));
                m_cacheRenderers.Clear();
            }

            UpdateCommandBuffers();
        }

        public void Remove(GameObject targetObject)
        {
            m_commandBuffers.Remove(targetObject);
            UpdateCommandBuffers();
        }

        private void UpdateCommandBuffers()
        {
            int i = 0;
            foreach (KeyValuePair<GameObject, OutlineCommandBuffer> kvp in m_commandBuffers)
            {
                kvp.Value.UpdateCommandBuffer(i == 0);
                i++;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (m_commandBuffers == null || m_commandBuffers.Count == 0)
            {
                Graphics.Blit(source, destination);
                return;
            }

            foreach (KeyValuePair<GameObject, OutlineCommandBuffer> kvp in m_commandBuffers)
            {
                kvp.Value.Execute();
            }

            if (PostEffectMaterial && m_prepassRenderTexture)
            {
                RenderTexture temp1 = RenderTexture.GetTemporary(source.width >> m_downsample, source.height >> m_downsample, 0);
                RenderTexture temp2 = RenderTexture.GetTemporary(source.width >> m_downsample, source.height >> m_downsample, 0);

                PostEffectMaterial.SetVector(OFFSET_ID, new Vector4(0, m_samplerScale, 0, 0));
                Graphics.Blit(m_prepassRenderTexture, temp1, PostEffectMaterial, 0);
                PostEffectMaterial.SetVector(OFFSET_ID, new Vector4(m_samplerScale, 0, 0, 0));
                Graphics.Blit(temp1, temp2, PostEffectMaterial, 0);

                for (int i = 0; i < m_iteration; i++)
                {
                    PostEffectMaterial.SetVector(OFFSET_ID, new Vector4(0, m_samplerScale, 0, 0));
                    Graphics.Blit(temp2, temp1, PostEffectMaterial, 0);
                    PostEffectMaterial.SetVector(OFFSET_ID, new Vector4(m_samplerScale, 0, 0, 0));
                    Graphics.Blit(temp1, temp2, PostEffectMaterial, 0);
                }

                PostEffectMaterial.SetTexture(BLUR_TEX_ID, temp2);
                Graphics.Blit(m_prepassRenderTexture, temp1, PostEffectMaterial, 1);

                PostEffectMaterial.SetTexture(BLUR_TEX_ID, temp1);
                PostEffectMaterial.SetFloat(OUTLINE_STRENGTH_ID, m_outlineStrength);
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
}