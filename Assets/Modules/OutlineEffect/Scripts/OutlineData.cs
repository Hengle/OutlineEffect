using UnityEngine;

namespace TA.OutlineEffect
{
    public class OutlineData
    {
        public Color Color;
        public GameObject Target;

        public OutlineData(Color color, GameObject target)
        {
            Color = color;
            Target = target;
        }
    }
}