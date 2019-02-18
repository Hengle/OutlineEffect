using UnityEngine;
using JSLCore.Event;

public class OutlineEffectComponent : MonoBehaviour
{
    [SerializeField] private Color m_outlineColor = Color.white;
    [SerializeField] private bool m_clickTurnOn = false;

    private bool m_effectOn;

    private void OnMouseDown()
    {
        if(m_clickTurnOn)
        {
            m_effectOn = !m_effectOn;

            if(m_effectOn)
            {
                EventManager.Instance.SendEvent((int)OutlineEvents.AddOutline, new OutlineData(m_outlineColor, gameObject));
            }
            else
            {
                EventManager.Instance.SendEvent((int)OutlineEvents.RemoveOutline, gameObject);
            }
        }
        else
        {
            EventManager.Instance.SendEvent((int)OutlineEvents.AddOutline, new OutlineData(m_outlineColor, gameObject));
        }
    }

    private void OnMouseUp()
    {
        if (!m_clickTurnOn)
        {
            EventManager.Instance.SendEvent((int)OutlineEvents.RemoveOutline, gameObject);
        }
    }
}
