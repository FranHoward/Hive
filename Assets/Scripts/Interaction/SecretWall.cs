using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecretWall : MonoBehaviour
{
    [Header("Slide Settings")]
    public float slideDistance = 3f;
    public float slideSpeed = 2f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;
    private bool isMoving = false;

    void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + Vector3.up * slideDistance;
    }

    public void OpenWall()
    {
        if (!isOpen && !isMoving)
            StartCoroutine(SlideWall(openPos));
    }

    public void CloseWall()
    {
        if (isOpen && !isMoving)
            StartCoroutine(SlideWall(closedPos));
    }

    public void ToggleWall()
    {
        if (!isMoving)
            StartCoroutine(SlideWall(isOpen ? closedPos : openPos));
    }

    IEnumerator SlideWall(Vector3 targetPos)
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, targetPos, slideSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
        isOpen = !isOpen;
        isMoving = false;
    }
}