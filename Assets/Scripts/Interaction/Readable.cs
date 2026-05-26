using UnityEngine;

public class Readable : MonoBehaviour
{
    [Header("Page Contents")]
    public string title = "Page";
    [TextArea(3, 12)] public string text = "Write your hint here.";
}
