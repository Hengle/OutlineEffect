using UnityEngine;
using System.Collections.Generic;
using JSLCore.Event;

namespace TA.OutlineEffect
{
    public class OutlinePostEffect : BasePostEffect
    {
        private readonly int OFFSET_ID = Shader.PropertyToID("_Offset");
        private readonly int BLUR_TEX_ID = Shader.PropertyToID("_BlurTex");
        private readonly int OUTLINE_STRENGTH_ID = Shader.PropertyToID("_OutlineStrength");

        [SerializeField] private Shader m_prepassShader = null;
        [SerializeField] private float m_samplerScale = 2;
        [SerializeField] private int m_downSample = 2;
        [SerializeField] private int m_iteration = 1;
        [Range(0.0f, 10.0f)]
        [SerializeField] private float m_outlineStrength = 5.0f;

        private EventListener m_eventListener;
        private RenderTexture m_renderTexture;
        private Dictionary<GameObject, OutlineCommandBuffer> m_commandBuffers;
        private List<Renderer> m_cacheRenderers = new List<Renderer>();

        private void Awake()
        {
            m_eventListener = new EventListener();
            m_eventListener.ListenForEvent((int)OutlineEvents.AddOutline, OnAddOutline);
            m_eventListener.ListenForEvent((int)OutlineEvents.RemoveOutline, OnRemoveOutline);
        }

        private void OnDestroy()
        {
            if (m_eventListener != null)
            {
                m_eventListener.Destroy();
                m_eventListener = null;
            }
        }

        private EventResult OnAddOutline(object eventData)
        {
            OutlineData outlineData = (OutlineData)eventData;
            AddOutlineEffect(outlineData.Target, outlineData.Color);

            return null;
        }

        private EventResult OnRemoveOutline(object eventData)
        {
            RemoveOutlineEffect((GameObject)eventData);
            return null;
        }

        private void AddOutlineEffect(GameObject targetObject, Color color)
        {
            m_cacheRenderers.Clear();
            m_cacheRenderers.AddRange(targetObject.GetComponentsInChildren<SkinnedMeshRenderer>());
            m_cacheRenderers.AddRange(targetObject.GetComponentsInChildren<MeshRenderer>());

            if (m_cacheRenderers == null || m_cacheRenderers.Count == 0)
            {
                return;
            }

            if (m_commandBuffers.ContainsKey(targetObject))
            {
                m_commandBuffers[targetObject].UpdateColor(color);
            }
            else
            {
                m_commandBuffers.Add(targetObject, new OutlineCommandBuffer(m_cacheRenderers.ToArray(), m_renderTexture, new Material(m_prepassShader), color));
            }

            UpdateCommandBuffers();
        }

        private void RemoveOutlineEffect(GameObject targetObject)
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

        private void OnEnable()
        {
            if (m_renderTexture == null)
            {
                m_renderTexture = RenderTexture.GetTemporary(Screen.width >> m_downSample, Screen.height >> m_downSample, 0);
            }

            m_commandBuffers = new Dictionary<GameObject, OutlineCommandBuffer>();
        }

        private void OnDisable()
        {
            if (m_commandBuffers == null || m_commandBuffers.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<GameObject, OutlineCommandBuffer> kvp in m_commandBuffers)
            {
                kvp.Value.Destroy();
            }

            m_commandBuffers.Clear();
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

            if (PostEffectMaterial && m_renderTexture)
            {
                RenderTexture temp1 = RenderTexture.GetTemporary(source.width >> m_downSample, source.height >> m_downSample, 0);
                RenderTexture temp2 = RenderTexture.GetTemporary(source.width >> m_downSample, source.height >> m_downSample, 0);

                PostEffectMaterial.SetVector(OFFSET_ID, new Vector4(0, m_samplerScale, 0, 0));
                Graphics.Blit(m_renderTexture, temp1, PostEffectMaterial, 0);
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
                Graphics.Blit(m_renderTexture, temp1, PostEffectMaterial, 1);

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