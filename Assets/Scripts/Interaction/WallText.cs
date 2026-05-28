using UnityEngine;

public class WallText : MonoBehaviour
{
    [TextArea(2, 6)] public string message = "...";
    public Color color = new Color(0.9f, 0.7f, 0.4f, 1f);
    public int fontSize = 60;
    public float characterSize = 0.04f;

    void Start()
    {
        var tm = GetComponent<TextMesh>();
        if (tm == null) tm = gameObject.AddComponent<TextMesh>();

        if (tm.font == null)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                tm.font = font;
                var mr = GetComponent<MeshRenderer>();
                if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
                mr.sharedMaterial = font.material;
            }
        }

        tm.text = message;
        tm.color = color;
        tm.fontSize = fontSize;
        tm.characterSize = characterSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
    }
}
