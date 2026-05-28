using UnityEngine;

public class PickableGlow : MonoBehaviour
{
    public Color glowColor = Color.yellow;
    public float glowIntensity = 2f;

    Renderer[] renderers;
    MaterialPropertyBlock block;

    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
    }

    public void SetGlow(bool on)
    {
        foreach (Renderer r in renderers)
        {
            r.GetPropertyBlock(block);

            Color c = on ? glowColor * glowIntensity : Color.black;
            block.SetColor(EmissionColor, c);

            r.SetPropertyBlock(block);
        }
    }
}