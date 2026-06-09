using System.Collections;
using UnityEngine;

public class ChestAnimation : MonoBehaviour
{
    public Transform lidHinge;
    public float openAngle = -90f;
    public float duration = 0.7f;

    bool _opened;

    public void Open()
    {
        if (_opened || lidHinge == null) return;
        _opened = true;
        StartCoroutine(AnimateOpen());
    }

    IEnumerator AnimateOpen()
    {
        Quaternion startRot = lidHinge.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(openAngle, 0f, 0f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            lidHinge.localRotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }
        lidHinge.localRotation = endRot;
    }
}
