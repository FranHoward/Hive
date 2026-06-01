using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;

    float xRotation;
    float yRotation;

    public float maxLookAngle = 60f;

    private float yaw;
    private float pitch;

    private bool _lookEnabled = true;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetLookEnabled(bool enabled)
    {
        _lookEnabled = enabled;
    }

    private void LateUpdate()
    {
        if (!_lookEnabled) return;

        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yaw += mouseX;
        pitch -= mouseY;

        pitch = Mathf.Clamp(pitch, -80f, 80f);

        // Limit left/right looking
        yaw = Mathf.Clamp(yaw, -maxLookAngle, maxLookAngle);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0);

        yRotation += mouseX;
        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}