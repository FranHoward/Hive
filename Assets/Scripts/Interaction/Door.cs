using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    public float openAngle = 90f;
    public float duration = 1.2f;

    public Vector3 rotationAxis = Vector3.up;

    private bool _opened;

    public void Open()
    {
        if (_opened) return;

        _opened = true;
        // The leaf swings into the corridor, so its collider would otherwise jut
        // into the walkway as an invisible wall. Once open, the doorway should be
        // freely passable — drop the collision on the panel(s).
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;
        StartCoroutine(AnimateOpen());
    }

    IEnumerator AnimateOpen()
    {
        Quaternion startRot = transform.localRotation;

        Quaternion endRot =
            startRot * Quaternion.AngleAxis(openAngle, rotationAxis.normalized);

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