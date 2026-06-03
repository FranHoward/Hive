using UnityEngine;

// Makes the bug's pupils follow the player without turning the whole body.
// Each pupil is nudged across its eyeball toward the player (within a clamped
// range) so it looks like the eyes are tracking the user. Attach to the
// Hive_Bug root and assign the pupil transforms.
public class EyeTracker : MonoBehaviour
{
    [Tooltip("What the eyes track. Leave empty to automatically use the main camera (the player's view).")]
    public Transform target;

    [Tooltip("The pupil transforms to move (e.g. LeftPupil and RightPupil).")]
    public Transform[] eyes;

    [Tooltip("Max distance a pupil may slide across the eye from its rest spot, in the pupil parent's local units.")]
    public float maxOffset = 0.3f;

    [Tooltip("Horizontal sensitivity multiplier. 1 = pupil points exactly at the target; higher exaggerates left/right gaze.")]
    public float horizontalGain = 2f;

    [Tooltip("Vertical sensitivity multiplier. 1 = pupil points exactly at the target; raise carefully — too high makes the bug look above/below you.")]
    public float verticalGain = 1f;

    [Tooltip("How quickly the eyes ease toward where they should be looking. Higher = snappier.")]
    public float trackSpeed = 12f;

    private Vector3[] restLocalPositions;

    private void Awake()
    {
        // Fully qualify UnityEngine.Camera: this project has its own Camera class
        // that would otherwise shadow it.
        if (target == null && UnityEngine.Camera.main != null)
        {
            target = UnityEngine.Camera.main.transform;
        }

        if (eyes != null)
        {
            restLocalPositions = new Vector3[eyes.Length];
            for (int i = 0; i < eyes.Length; i++)
            {
                if (eyes[i] != null)
                {
                    restLocalPositions[i] = eyes[i].localPosition;
                }
            }
        }
    }

    private void Update()
    {
        if (eyes == null || restLocalPositions == null) return;

        if (target == null)
        {
            if (UnityEngine.Camera.main == null) return;
            target = UnityEngine.Camera.main.transform;
        }

        for (int i = 0; i < eyes.Length; i++)
        {
            Transform eye = eyes[i];
            if (eye == null) continue;

            // Direction from the eye toward the player, expressed in the eye's
            // parent space so the offset lines up with the rest position.
            Vector3 worldDir = target.position - eye.position;
            if (worldDir.sqrMagnitude < 0.0001f) continue;

            Vector3 localDir = eye.parent != null
                ? eye.parent.InverseTransformDirection(worldDir.normalized)
                : worldDir.normalized;

            // Slide the pupil within the eye's facing plane (x/y) so it lands
            // where the eye is actually looking. localDir.xy is the off-axis
            // component of the (unit) gaze direction, so scaling it by the
            // pupil's rest distance from the eye centre (the eyeball radius)
            // places the pupil on the eye surface pointing at the target. This
            // is proportional to the real gaze angle, so the pupil points *at*
            // the player instead of pegging to an extreme. Clamp keeps it from
            // sliding past the rim for very off-axis targets.
            float eyeRadius = Mathf.Abs(restLocalPositions[i].z);
            Vector2 lateral = new Vector2(localDir.x * horizontalGain, localDir.y * verticalGain) * eyeRadius;
            lateral = Vector2.ClampMagnitude(lateral, maxOffset);

            Vector3 desired = restLocalPositions[i] + new Vector3(lateral.x, lateral.y, 0f);
            eye.localPosition = Vector3.Lerp(
                eye.localPosition, desired, 1f - Mathf.Exp(-trackSpeed * Time.deltaTime));
        }
    }
}
