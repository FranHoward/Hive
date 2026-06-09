using UnityEngine;

public class Readable : MonoBehaviour
{
    [Header("Book Settings")]
    public string title = "Research Log";

    [TextArea(10, 30)]
    public string fullText = "";

    [Header("Auto Pagination")]
    public int charsPerPage = 300;

    public string[] GetPages()
    {
        if (string.IsNullOrEmpty(fullText)) return new string[] { "" };

        var pages = new System.Collections.Generic.List<string>();
        string remaining = fullText;

        while (remaining.Length > 0)
        {
            if (remaining.Length <= charsPerPage)
            {
                pages.Add(remaining.Trim());
                break;
            }

            int cutAt = charsPerPage;
            int lastNewline = remaining.LastIndexOf('\n', cutAt);
            if (lastNewline > 0) cutAt = lastNewline;

            pages.Add(remaining.Substring(0, cutAt).Trim());
            remaining = remaining.Substring(cutAt).TrimStart();
        }

        return pages.ToArray();
    }
}