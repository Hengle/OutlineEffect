using UnityEngine;

namespace TA.PostProcessing.Outline
{
    public class OutlineData
    {
        public Color color;
        public GameObject target;

        public OutlineData(Color color, GameObject target)
        {
            this.color = color;
            this.target = target;
        }
    }
}