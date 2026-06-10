using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameScene = "SampleScene";

    [Header("Fonts")]
    [SerializeField] private Font titleFont;

    [Header("Title")]
    [SerializeField] private string titleText = "Hive";

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.42f;

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 72,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.95f, 0.92f, 0.82f, 1f) }
        };

        if (titleFont != null)
            titleStyle.font = titleFont;

        GUI.Label(new Rect(0, cy - 120f, Screen.width, 100f), titleText, titleStyle);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.9f, 0.88f, 0.8f) }
        };

        float bw = 220f;
        float bh = 60f;

        if (GUI.Button(new Rect(cx - bw / 2f, cy + 30f, bw, bh), "▶   Start", btnStyle))
        {
            SceneManager.LoadScene(gameScene);
        }
    }
}