using UnityEngine;
using UnityEngine.Events;

public class Keypad : MonoBehaviour
{
    public string requiredCode = "1987";
    public UnityEvent onCorrect;

    [Range(0f, 1f)]
    public float clickVolume = 0.45f;

    public bool IsOpen { get; private set; }
    public bool IsSolved { get; private set; }

    string _entry = "";
    string _flash = "";
    float _flashUntil;
    InteractionSystem _owner;

    AudioClip _clickClip;
    AudioSource _clickSource;

    void Awake()
    {
        _clickClip = Resources.Load<AudioClip>("Audio/keyPadFinal");
        _clickSource = gameObject.AddComponent<AudioSource>();
        _clickSource.playOnAwake = false;
        _clickSource.spatialBlend = 0f; // 2D UI click
    }

    void PlayClick()
    {
        if (_clickClip != null) _clickSource.PlayOneShot(_clickClip, clickVolume);
    }

    public void Activate(InteractionSystem system)
    {
        if (IsSolved || IsOpen) return;
        _owner = system;
        IsOpen = true;
        _entry = "";
        if (system != null) system.SuspendForUI();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        if (_owner != null) _owner.ResumeFromUI();
        _owner = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }

        foreach (char c in Input.inputString)
        {
            if (c == '\b')
            {
                if (_entry.Length > 0) { _entry = _entry.Substring(0, _entry.Length - 1); PlayClick(); }
            }
            else if (c == '\n' || c == '\r') { PlayClick(); Submit(); }
            else if (c >= '0' && c <= '9' && _entry.Length < requiredCode.Length) { _entry += c; PlayClick(); }
        }
    }

    void Submit()
    {
        if (_entry == requiredCode)
        {
            IsSolved = true;
            onCorrect?.Invoke();
            Close();
        }
        else
        {
            _flash = "WRONG";
            _flashUntil = Time.unscaledTime + 1.2f;
            _entry = "";
        }
    }

    void OnGUI()
    {
        if (!IsOpen) return;

        float w = 460f;
        float h = 620f;
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        GUI.Box(new Rect(x, y, w, h), "");

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(x, y + 16, w, 50), "KEYPAD", titleStyle);

        string display = _entry.PadRight(requiredCode.Length, '_');
        var dispStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 48, alignment = TextAnchor.MiddleCenter
        };
        GUI.Box(new Rect(x + 40, y + 80, w - 80, 80), display, dispStyle);

        float btnSize = 100f;
        float btnGap = 14f;
        float gridW = 3 * btnSize + 2 * btnGap;
        float startX = x + (w - gridW) * 0.5f;
        float startY = y + 200f;
        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 32 };

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int num = row * 3 + col + 1;
                var rect = new Rect(startX + col * (btnSize + btnGap), startY + row * (btnSize + btnGap), btnSize, btnSize);
                if (GUI.Button(rect, num.ToString(), btnStyle))
                {
                    PlayClick();
                    if (_entry.Length < requiredCode.Length) _entry += num.ToString();
                }
            }
        }

        float row4Y = startY + 3 * (btnSize + btnGap);
        if (GUI.Button(new Rect(startX, row4Y, btnSize, btnSize), "C", btnStyle)) { PlayClick(); _entry = ""; }
        if (GUI.Button(new Rect(startX + (btnSize + btnGap), row4Y, btnSize, btnSize), "0", btnStyle))
        {
            PlayClick();
            if (_entry.Length < requiredCode.Length) _entry += "0";
        }
        if (GUI.Button(new Rect(startX + 2 * (btnSize + btnGap), row4Y, btnSize, btnSize), "OK", btnStyle)) { PlayClick(); Submit(); }

        if (Time.unscaledTime < _flashUntil)
        {
            var flashStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.red }, fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(x, y + h - 70, w, 30), _flash, flashStyle);
        }

        var hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16, alignment = TextAnchor.LowerCenter,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };
        GUI.Label(new Rect(x, y + h - 32, w, 24), "Esc to cancel", hintStyle);
    }
}
