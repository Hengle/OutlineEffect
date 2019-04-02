using UnityEngine;

namespace TA.PostProcessing.Outline
{
    public class OutlineEffectComponent : MonoBehaviour
    {
        [SerializeField] private Color m_outlineColor = Color.white;

        private void OnEnable()
        {
            OutlinePostProcessing.Instance.Add(gameObject, m_outlineColor);
        }

        private void OnDisable()
        {
            OutlinePostProcessing.Instance.Remove(gameObject);
        }
    }
}