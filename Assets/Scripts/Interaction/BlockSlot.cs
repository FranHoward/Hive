using UnityEngine;
using UnityEngine.Events;

public class BlockSlot : MonoBehaviour
{
    public string requiredBlockId = "puzzle-block";
    [Tooltip("Where the inserted block snaps to. Defaults to this transform if null.")]
    public Transform snapTarget;
    public UnityEvent onInsert;

    [Header("Auto-Insert")]
    [Tooltip("Reference to the player's pickup. When the held block is within snapDistance of the snap point, it auto-inserts.")]
    public ObjectPickup pickup;
    public float snapDistance = 0.5f;

    public bool IsFilled { get; private set; }

    void Update()
    {
        if (IsFilled || pickup == null || pickup.HeldObject == null) return;

        BlockTag held = pickup.HeldObject.GetComponent<BlockTag>();
        if (held == null || held.blockId != requiredBlockId) return;

        Vector3 snapPos = snapTarget != null ? snapTarget.position : transform.position;
        if (Vector3.Distance(pickup.HeldObject.transform.position, snapPos) <= snapDistance)
            TryInsert(held, pickup);
    }

    public bool TryInsert(BlockTag block, ObjectPickup p)
    {
        if (IsFilled || block == null) return false;
        if (block.blockId != requiredBlockId) return false;

        IsFilled = true;

        if (p != null) p.ForceRelease();

        var rb = block.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (var col in block.GetComponentsInChildren<Collider>())
            col.enabled = false;

        Transform target = snapTarget != null ? snapTarget : transform;
        block.transform.SetParent(target, worldPositionStays: false);
        block.transform.localPosition = Vector3.zero;
        block.transform.localRotation = Quaternion.identity;

        onInsert?.Invoke();
        return true;
    }
}
