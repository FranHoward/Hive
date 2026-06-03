using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    bool _won;

    void OnTriggerEnter(Collider other)
    {
        if (_won) return;
        if (other.GetComponentInParent<PlayerMovement>() != null)
            _won = true;
    }

    void OnGUI()
    {
        if (!_won) return;
        float w = 600, h = 200;
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;
        GUI.Box(new Rect(x, y, w, h), "");
        var style = new GUIStyle(GUI.skin.label)
        { fontSize = 40, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(x, y, w, h), "You Escaped!", style);
    }
}