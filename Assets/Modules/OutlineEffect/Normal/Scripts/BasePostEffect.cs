using UnityEngine;
using JSLCore;

namespace TA.PostProcessing.Outline
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class BasePostEffect : MonoSingleton<BasePostEffect>
    {
        public Shader postEffectShader = null;
        private Material m_postEffectMaterial = null;
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

    }
}