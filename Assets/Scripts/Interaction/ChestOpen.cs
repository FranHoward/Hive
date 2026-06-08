using UnityEngine;

public class ChestOpen : MonoBehaviour
{
    public GameObject key;  // 拖入 Room2_Key
    private Animation _anim;

    void Start()
    {
        _anim = GetComponent<Animation>();
    }

    public void Open()
    {
        if (_anim != null)
            _anim.Play("ChestAnim");

        if (key != null)
        {
            key.SetActive(true);
            Rigidbody rb = key.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = false;
            }
        }
    }
}