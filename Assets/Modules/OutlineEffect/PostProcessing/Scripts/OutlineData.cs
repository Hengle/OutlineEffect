using UnityEngine;

public class OutlineData
{
    public Renderer[] renderers;
    public Color color;

    public OutlineData(Renderer[] renderers, Color color)
    {
        this.renderers = renderers;
        this.color = color;
    }

    public void SetColor(Color color)
    {
        if(this.color == color)
        {
            return;
        }

        this.color = color;
    }
}
