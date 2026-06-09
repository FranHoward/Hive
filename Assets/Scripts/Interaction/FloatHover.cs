using UnityEngine;

// Makes a resting pickup item hover and gently sway above an anchor point, easing
// back onto the hover path after it has been picked up and dropped. While the
// player is actually holding / inspecting it, this yields control to ObjectPickup
// so the spring-hold and rotate-to-inspect still work.
//
// The item's Rigidbody is expected to have gravity OFF (so a dropped item stays
// where it is and drifts back to the hover instead of falling).
[RequireComponent(typeof(Rigidbody))]
public class FloatHover : MonoBehaviour
{
    [Header("Hover anchor")]
    [Tooltip("Point to hover around. Defaults to where the object is when it activates.")]
    public Transform anchor;

    [Header("Bob (up/down)")]
    public float bobAmplitude = 0.07f;
    public float bobSpeed = 0.5f;      // cycles per second

    [Header("Sway (gentle turn around Y)")]
    public float swayAngle = 14f;      // degrees
    public float swaySpeed = 0.35f;    // cycles per second

    [Header("Settling")]
    [Tooltip("How fast it eases back onto the hover path after being dropped.")]
    public float settleSpeed = 3f;

    [Tooltip("Player pickup. Hovering pauses while this is holding the item.")]
    public ObjectPickup pickup;

    Rigidbody _rb;
    Vector3 _anchorPos;
    Quaternion _baseRot;
    float _t;

    void Awake() => _rb = GetComponent<Rigidbody>();

    void OnEnable()
    {
        // Captured when the slot reveals the note, so it hovers wherever it sits.
        _anchorPos = anchor != null ? anchor.position : transform.position;
        _baseRot = transform.rotation;
        _t = 0f;
    }

    // Driven in FixedUpdate via the Rigidbody: an interpolated non-kinematic body
    // ignores transform writes made in Update (the physics pass overwrites them),
    // so we move it with MovePosition/MoveRotation and zero its velocity instead.
    void FixedUpdate()
    {
        // While the player holds / inspects it, ObjectPickup owns the transform.
        if (_rb == null || (pickup != null && pickup.HeldObject == gameObject)) return;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _t += Time.fixedDeltaTime;
        float twoPi = Mathf.PI * 2f;
        Vector3 targetPos = _anchorPos + Vector3.up * (Mathf.Sin(_t * bobSpeed * twoPi) * bobAmplitude);
        Quaternion targetRot = _baseRot * Quaternion.Euler(0f, Mathf.Sin(_t * swaySpeed * twoPi) * swayAngle, 0f);

        // Exponential ease so a just-dropped note drifts smoothly back to the path.
        float k = 1f - Mathf.Exp(-settleSpeed * Time.fixedDeltaTime);
        _rb.MovePosition(Vector3.Lerp(_rb.position, targetPos, k));
        _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, k));
    }
}
