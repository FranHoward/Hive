using UnityEngine;

public class ChestOpen : MonoBehaviour
{
    public GameObject key;
    private Animation _anim;

    void Start()
    {
        _anim = GetComponent<Animation>();
        if (key != null) key.SetActive(false);
    }

    public void Open()
    {
        if (_anim != null)
            _anim.Play("ChestAnim");

        if (key != null)
        {
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
        }
    }
}
