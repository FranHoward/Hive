using UnityEngine;

[ExecuteAlways]
public class Tinter : MonoBehaviour
{
    public Color color = Color.white;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId     = Shader.PropertyToID("_Color");

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    void Apply()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        var props = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            // Don't tint renderers owned by a nested Tinter (e.g. a pupil under a
            // tinted eye) — let that child Tinter set its own colour instead.
            if (r.GetComponentInParent<Tinter>() != this) continue;

            r.GetPropertyBlock(props);
            props.SetColor(BaseColorId, color);
            props.SetColor(ColorId, color);
            r.SetPropertyBlock(props);
        }
    }
}
