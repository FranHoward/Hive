using UnityEngine;

// Slowly turns this object to face the player (the main camera), clamped to a
// cone around its starting orientation so wall-set bug heads can't rotate back
// into the wall. Used by the bugs peering out of the Room1 wall breach.
public class StareAtPlayer : MonoBehaviour
{
    [Tooltip("What to stare at. Leave empty to use the main camera (the player's view).")]
    public Transform target;

    [Tooltip("Degrees per second the head turns toward the player.")]
    public float turnSpeed = 120f;

    [Tooltip("Max angle (degrees) it may turn away from its starting facing, so it never spins into the wall.")]
    public float maxAngle = 70f;

    private Quaternion _rest;

    private void Awake()
    {
        _rest = transform.rotation;
        // Fully qualify UnityEngine.Camera: this project has its own Camera class.
        if (target == null && UnityEngine.Camera.main != null)
            target = UnityEngine.Camera.main.transform;
    }

    private void Update()
    {
        if (target == null)
        {
            if (UnityEngine.Camera.main == null) return;
            target = UnityEngine.Camera.main.transform;
        }

        Vector3 dir = target.position - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        // Aim the face (+Z) at the player, but never more than maxAngle from rest.
        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        look = Quaternion.RotateTowards(_rest, look, maxAngle);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, look, turnSpeed * Time.deltaTime);
    }
}
