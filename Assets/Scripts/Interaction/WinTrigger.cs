using UnityEngine;
using UnityEngine.SceneManagement;

public class WinTrigger : MonoBehaviour
{
    bool _won;
    float _timer = 0f;
    float _blackAlpha = 0f;
    float _typeTimer = 0f;
    float _buttonAlpha = 0f;
    int _charIndex = 0;
    bool _showButtons = false;

    string _fullText = "YOU ESCAPED";
    string _displayText = "";

    // 故障系统
    float _glitchTimer = 0f;
    float _glitchDuration = 0f;
    float _glitchIntensity = 0f;
    bool _glitching = false;
    float[] _scanlineOffsets = new float[8];
    Color[] _glitchColors = new Color[]
    {
        new Color(1f, 0f, 0.2f, 0.35f),
        new Color(0f, 1f, 0.8f, 0.25f),
        new Color(1f, 0.9f, 0f, 0.2f)
    };
    int _glitchColorIdx = 0;

    // 静态噪点
    float _staticTimer = 0f;
    bool _showStatic = false;

    [SerializeField] private string startScene = "StartScene";

    void OnTriggerEnter(Collider other)
    {
        if (_won) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (other.GetComponentInParent<PlayerMovement>() != null)
        {
            _won = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Update()
    {
        if (!_won) return;
        _timer += Time.deltaTime;

        // 黑屏渐入
        if (_blackAlpha < 1f)
            _blackAlpha += Time.deltaTime * 1.0f;

        // 打字机
        if (_blackAlpha >= 0.95f && _charIndex < _fullText.Length)
        {
            _typeTimer += Time.deltaTime;
            if (_typeTimer > 0.07f)
            {
                _typeTimer = 0f;
                _charIndex++;
                _displayText = _fullText.Substring(0, _charIndex);
                // 每打一个字触发小故障
                if (Random.value > 0.5f) TriggerGlitch(0.06f, 0.6f);
            }
        }

        // 故障计时
        _glitchTimer += Time.deltaTime;
        float interval = _charIndex >= _fullText.Length ? Random.Range(1.5f, 4f) : Random.Range(0.3f, 1f);
        if (_glitchTimer > interval)
        {
            _glitchTimer = 0f;
            TriggerGlitch(Random.Range(0.05f, 0.2f), Random.Range(0.4f, 1.2f));
        }

        if (_glitching)
        {
            _glitchDuration -= Time.deltaTime;
            if (_glitchDuration <= 0f) _glitching = false;

            // 更新扫描线偏移
            for (int i = 0; i < _scanlineOffsets.Length; i++)
                _scanlineOffsets[i] = Random.Range(-_glitchIntensity * 20f, _glitchIntensity * 20f);
        }

        // 静态噪点
        _staticTimer += Time.deltaTime;
        if (_staticTimer > Random.Range(0.05f, 0.15f))
        {
            _staticTimer = 0f;
            _showStatic = Random.value > 0.6f && _glitching;
        }

        // 按钮出现
        if (_charIndex >= _fullText.Length)
        {
            _showButtons = true;
            if (_buttonAlpha < 1f)
                _buttonAlpha += Time.deltaTime * 0.5f;
        }
    }

    void TriggerGlitch(float duration, float intensity)
    {
        _glitching = true;
        _glitchDuration = duration;
        _glitchIntensity = intensity;
        _glitchColorIdx = Random.Range(0, _glitchColors.Length);
    }

    void OnGUI()
    {
        if (!_won) return;

        // 黑屏
        GUI.color = new Color(0, 0, 0, _blackAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (_blackAlpha < 0.9f) return;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.42f;

        // ── 故障层 ──
        if (_glitching)
        {
            // 彩色偏移层
            Color gc = _glitchColors[_glitchColorIdx];
            var glitchStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 78,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = gc }
            };

            float gox = Random.Range(-12f, 12f) * _glitchIntensity;
            float goy = Random.Range(-6f, 6f) * _glitchIntensity;
            GUI.Label(new Rect(gox, cy - 50 + goy, Screen.width, 100), _displayText, glitchStyle);

            // 第二层偏移（反方向）
            Color gc2 = _glitchColors[(_glitchColorIdx + 1) % _glitchColors.Length];
            glitchStyle.normal = new GUIStyleState { textColor = gc2 };
            GUI.Label(new Rect(-gox * 0.5f, cy - 50 - goy * 0.5f, Screen.width, 100), _displayText, glitchStyle);

            // 扫描线故障块
            for (int i = 0; i < _scanlineOffsets.Length; i++)
            {
                float lineY = cy - 40 + i * (100f / _scanlineOffsets.Length);
                float lineH = Random.Range(2f, 8f);
                GUI.color = new Color(gc.r, gc.g, gc.b, 0.15f * _glitchIntensity);
                GUI.DrawTexture(new Rect(_scanlineOffsets[i], lineY, Screen.width, lineH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // 静态噪点块
            if (_showStatic)
            {
                for (int i = 0; i < 6; i++)
                {
                    float nx = Random.Range(0f, Screen.width);
                    float ny = Random.Range(cy - 60f, cy + 60f);
                    float nw = Random.Range(20f, 120f);
                    float nh = Random.Range(2f, 10f);
                    GUI.color = new Color(1f, 1f, 1f, Random.Range(0.05f, 0.2f));
                    GUI.DrawTexture(new Rect(nx, ny, nw, nh), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }

        // ── 主标题 ──
        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 78,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.95f, 0.92f, 0.82f, 1f) }
        };

        float mainOffX = _glitching ? Random.Range(-3f, 3f) * _glitchIntensity : 0f;
        GUI.Label(new Rect(mainOffX, cy - 50, Screen.width, 100), _displayText, titleStyle);

        // 光标闪烁
        if (_charIndex < _fullText.Length && (int)(_timer * 5) % 2 == 0)
        {
            GUI.Label(new Rect(mainOffX, cy - 50, Screen.width + 60, 100), _displayText + "▌", titleStyle);
        }

        // ── 分隔线 ──
        if (_charIndex >= _fullText.Length)
        {
            float lineAlpha = Mathf.Clamp01(_buttonAlpha * 2f);
            GUI.color = new Color(0.5f, 0.45f, 0.35f, lineAlpha);
            float lw = 400f;
            GUI.DrawTexture(new Rect(cx - lw * 0.5f, cy + 62f, lw, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 副标题
            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.55f, 0.52f, 0.45f, lineAlpha) }
            };
            float subOff = _glitching ? Random.Range(-2f, 2f) : 0f;
            GUI.Label(new Rect(subOff, cy + 72f, Screen.width, 32f),
                "Containment: Failed.  You: Alive.  For now.", subStyle);
        }

        if (!_showButtons) return;

        // ── 按钮 ──
        float bw = 190f, bh = 54f, gap = 20f;
        float bx = cx - bw - gap * 0.5f;
        float by = cy + 130f;

        GUI.color = new Color(1f, 1f, 1f, _buttonAlpha);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.9f, 0.88f, 0.8f) }
        };

        if (GUI.Button(new Rect(bx, by, bw, bh), "▶   Restart", btnStyle))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (GUI.Button(new Rect(bx + bw + gap, by, bw, bh), "✕   Quit", btnStyle))
            SceneManager.LoadScene(startScene);

        // 底部小字
        var hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.3f, 0.3f, 0.3f, _buttonAlpha * 0.8f) }
        };
        GUI.Label(new Rect(0f, by + bh + 18f, Screen.width, 22f), "Thanks for playing", hintStyle);

        GUI.color = Color.white;
    }
}