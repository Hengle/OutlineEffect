using UnityEngine;
using System.Collections.Generic;

public class OutlineComponent : MonoBehaviour
{
    [SerializeField] public Color color = Color.white;
    [SerializeField] public Renderer[] renderers;

    private void Reset()
    {
        List<Renderer> cacheRenderers = new List<Renderer>();
        cacheRenderers.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>());
        cacheRenderers.AddRange(GetComponentsInChildren<MeshRenderer>());

        renderers = cacheRenderers.ToArray();
    }

    private void OnEnable()
    {
        if(renderers == null || renderers.Length == 0)
        {
            return;
        }

        OutlineManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        OutlineManager.Instance.Unregister(this);
    }
}
