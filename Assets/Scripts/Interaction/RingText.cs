using UnityEngine;

// Lays a string out as individual 3D-text glyphs in a ring around this object's
// local Y axis, so the message wraps around a curved prop (e.g. the floating egg).
// Built when the object is enabled, like WallText. Each glyph is its own TextMesh
// facing outward; the player rotates / inspects the prop to read each side.
//
// By default the glyphs use a depth-writing text material (textMaterial) so an
// opaque prop in front of them hides them -- letters on the far side of the egg
// are occluded instead of bleeding through. Falls back to Unity's built-in font
// material (which renders through geometry) when no material is assigned.
public class RingText : MonoBehaviour
{
    [TextArea] public string message = "code: 1987";

    [Header("Ring shape")]
    [Tooltip("Distance from the local Y axis to each glyph (sit it on the prop surface).")]
    public float radius = 0.133f;
    [Tooltip("Vertical offset of the band of text.")]
    public float height = 0f;
    [Tooltip("Degrees between glyph centres. 0 = spread the whole string evenly around 360.")]
    public float degreesPerChar = 12f;

    [Header("Glyph look")]
    public float characterSize = 0.0075f;
    public int fontSize = 70;
    public Color color = new Color(0.85f, 0.8f, 0.6f, 1f);

    [Tooltip("Flip if the letters read mirrored / face inward.")]
    public bool faceOutward = false;

    [Tooltip("Depth-tested text material so the prop occludes far-side letters. " +
             "Leave empty to use the built-in font material (renders through geometry).")]
    public Material textMaterial;

    Font _font;
    Material _runtimeMat;

    void OnEnable()
    {
        Font.textureRebuilt += OnFontTextureRebuilt;
        Build();
    }

    void OnDisable()
    {
        Font.textureRebuilt -= OnFontTextureRebuilt;
    }

    // A dynamic font can rebuild/resize its atlas as glyphs are requested elsewhere;
    // when ours does, re-bind the texture and rebuild so the UVs stay valid.
    void OnFontTextureRebuilt(Font f)
    {
        if (f == _font) Build();
    }

    void Build()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) return;

        int n = message.Length;
        if (n == 0) return;

        // Make sure the atlas holds our glyphs before we bind it to the material.
        _font.RequestCharactersInTexture(message, fontSize, FontStyle.Normal);

        Material glyphMat;
        if (textMaterial != null)
        {
            if (_runtimeMat == null) _runtimeMat = new Material(textMaterial);
            _runtimeMat.mainTexture = _font.material.mainTexture;   // live font atlas
            glyphMat = _runtimeMat;
        }
        else
        {
            glyphMat = _font.material;
        }

        float step = degreesPerChar > 0f ? degreesPerChar : 360f / n;
        // Centre the string on local +Z; lay char 0 on the viewer's left so the
        // word reads left-to-right when viewed from the front (outside) of the ring.
        float start = step * (n - 1) * 0.5f;

        for (int i = 0; i < n; i++)
        {
            float ang = (start - i * step) * Mathf.Deg2Rad;
            Vector3 outward = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));

            var go = new GameObject("g" + i);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(outward.x * radius, height, outward.z * radius);
            go.transform.localRotation = Quaternion.LookRotation(faceOutward ? outward : -outward, Vector3.up);

            var tm = go.AddComponent<TextMesh>();
            tm.text = message[i].ToString();
            tm.font = _font;
            tm.fontSize = fontSize;
            tm.characterSize = characterSize;
            tm.color = color;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            go.GetComponent<MeshRenderer>().sharedMaterial = glyphMat;
        }
    }
}
