using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    public float openAngle = 90f;
    public float duration = 1.2f;

    bool _opened;

    public void Open()
    {
        if (_opened) return;
        _opened = true;
        StartCoroutine(AnimateOpen());
    }

    IEnumerator AnimateOpen()
    {
        Quaternion startRot = transform.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, openAngle, 0f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            transform.localRotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }
        transform.localRotation = endRot;
    }
}
