using UnityEngine;

public class Tinter : MonoBehaviour
{
    public Color color = Color.white;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId     = Shader.PropertyToID("_Color");

    void Start()
    {
        Apply();
    }

    void OnValidate()
    {
        if (Application.isPlaying) Apply();
    }

    void Apply()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        var props = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(props);
            props.SetColor(BaseColorId, color);
            props.SetColor(ColorId, color);
            r.SetPropertyBlock(props);
        }
    }
}
