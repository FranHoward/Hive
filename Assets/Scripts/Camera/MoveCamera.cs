using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveCamera : MonoBehaviour
{
    public Transform cameraPosition;
    private Vector3 offset = new Vector3(0f, 0.5f, 0f);

    private void Update()
    {
        transform.position = cameraPosition.position + offset;
    }
}

