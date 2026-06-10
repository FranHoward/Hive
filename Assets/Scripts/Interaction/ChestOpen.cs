using System.Collections;
using UnityEngine;

public class ChestOpen : MonoBehaviour
{
    public GameObject key;
    public float keyRevealDelay = 2f;

    private Animation _anim;
    private Coroutine _revealRoutine;

    void Start()
    {
        _anim = GetComponent<Animation>();
        if (key != null)
        {
            FloatHover hover = key.GetComponent<FloatHover>();
            if (hover != null)
                hover.enabled = false;

            key.SetActive(false);
        }
    }

    public void Open()
    {
        if (_anim != null)
            _anim.Play("ChestAnim");

        if (key != null)
        {
            if (_revealRoutine != null)
                StopCoroutine(_revealRoutine);

            _revealRoutine = StartCoroutine(RevealKeyAfterDelay());
        }
    }

    IEnumerator RevealKeyAfterDelay()
    {
        if (keyRevealDelay > 0f)
            yield return new WaitForSeconds(keyRevealDelay);

        key.SetActive(true);
        Rigidbody rb = key.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = false;
        }

        FloatHover hover = key.GetComponent<FloatHover>();
        if (hover != null)
            hover.enabled = true;

        _revealRoutine = null;
    }
}
