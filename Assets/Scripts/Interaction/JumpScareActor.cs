using System.Collections;
using UnityEngine;

// FNAF-style jump scare. Lives on a bug model that is kept INACTIVE in the
// scene; when something enables the GameObject (see JumpScareTrigger), it snaps
// in front of the player's camera with its face filling the view, tilts its
// head back and forth, and reaches its front legs out to the screen edges like
// it's grabbing you. Then it disables itself again.
public class JumpScareActor : MonoBehaviour
{
    [Header("Lunge")]
    [Tooltip("Distance in front of the camera the bug first appears.")]
    public float startDistance = 1.8f;

    [Tooltip("Distance the bug lunges in to (smaller = right in your face).")]
    public float endDistance = 0.85f;

    [Tooltip("Seconds the lunge from start to end distance takes. Smaller = more sudden.")]
    public float lungeDuration = 0.18f;

    [Tooltip("Seconds the bug stays in your face after the lunge.")]
    public float holdDuration = 1.0f;

    [Header("Framing")]
    [Tooltip("Local-space point on the bug to center on screen (its eyes/face).")]
    public Vector3 faceLocalOffset = new Vector3(0f, 0.824f, 0.79f);

    [Tooltip("Nudge the framed face up/down on screen.")]
    public float verticalNudge = 0f;

    [Tooltip("How much the bug jitters while held in your face.")]
    public float shakeAmount = 0.03f;

    [Header("Head tilt")]
    [Tooltip("Max head-tilt angle (degrees) as it rocks back and forth.")]
    public float tiltAngle = 18f;

    [Tooltip("How fast the head rocks back and forth.")]
    public float tiltSpeed = 9f;

    [Header("Grabbing legs")]
    public Transform leftFrontLeg;
    public Transform rightFrontLeg;

    [Tooltip("How long the front legs stretch out to reach for you.")]
    public float legLength = 1.0f;

    [Tooltip("How thick the reaching arms are. Bigger = easier to see.")]
    public float legThickness = 0.16f;

    [Tooltip("How far to the sides (screen edges) the legs reach.")]
    public float legGrabSpread = 0.5f;

    [Tooltip("Vertical offset of the grab point relative to the camera. Raise so the arms aren't cut off at the bottom.")]
    public float legGrabRaise = 0.12f;

    [Tooltip("How far in front of the camera the legs grab toward.")]
    public float legGrabForward = 0.1f;

    [Tooltip("How fast the legs open/close in a grasping motion.")]
    public float graspSpeed = 11f;

    [Tooltip("How much the legs open/close while grasping.")]
    public float graspAmount = 0.12f;

    [Header("Audio")]
    [Tooltip("Scream/roar to play when the scare fires. Falls back to Resources/Audio/jumpFinal if left empty.")]
    public AudioClip screamClip;

    [Range(0f, 1f)]
    public float screamVolume = 1f;

    [Tooltip("Disable the bug again once the scare finishes.")]
    public bool disableAfter = true;

    private void OnEnable()
    {
        // Fully qualify UnityEngine.Camera: this project has its own Camera class
        // that would otherwise shadow it.
        var mainCam = UnityEngine.Camera.main;
        if (mainCam == null)
        {
            if (disableAfter) gameObject.SetActive(false);
            return;
        }

        if (screamClip == null)
            screamClip = Resources.Load<AudioClip>("Audio/jumpFinal");

        // PlayClipAtPoint spawns a temporary AudioSource that outlives this
        // GameObject getting disabled, so the scream always plays in full even
        // though the scare itself only lasts ~lungeDuration + holdDuration.
        if (screamClip != null)
            AudioSource.PlayClipAtPoint(screamClip, mainCam.transform.position, screamVolume);

        StartCoroutine(Lunge(mainCam.transform));
    }

    private IEnumerator Lunge(Transform cam)
    {
        float elapsed = 0f;
        float t = 0f;
        while (t < lungeDuration)
        {
            t += Time.deltaTime;
            elapsed += Time.deltaTime;
            float k = lungeDuration > 0f ? Mathf.Clamp01(t / lungeDuration) : 1f;
            // Ease-in so it accelerates toward your face.
            Frame(cam, Mathf.Lerp(startDistance, endDistance, k * k), elapsed, 0f);
            yield return null;
        }

        float h = 0f;
        while (h < holdDuration)
        {
            h += Time.deltaTime;
            elapsed += Time.deltaTime;
            Frame(cam, endDistance, elapsed, shakeAmount);
            yield return null;
        }

        if (disableAfter) gameObject.SetActive(false);
    }

    private void Frame(Transform cam, float distance, float time, float jitter)
    {
        // Face the camera, upright, then rock the head back and forth (roll).
        Quaternion look = Quaternion.LookRotation(-cam.forward, Vector3.up);
        float tilt = Mathf.Sin(time * tiltSpeed) * tiltAngle;
        Quaternion rot = look * Quaternion.Euler(0f, 0f, tilt);

        // Place the bug so its face lands at the screen-center point.
        Vector3 faceTarget = cam.position + cam.forward * distance + cam.up * verticalNudge;
        if (jitter > 0f) faceTarget += Random.insideUnitSphere * jitter;
        transform.rotation = rot;
        transform.position = faceTarget - rot * faceLocalOffset;

        PoseLeg(cam, leftFrontLeg, time);
        PoseLeg(cam, rightFrontLeg, time);
    }

    private void PoseLeg(Transform cam, Transform leg, float time)
    {
        if (leg == null) return;

        // Work out which side of the screen this leg is actually on (the bug
        // faces the camera, so its left/right is mirrored) and reach further out
        // to that same side, so the arms always splay outward, not inward.
        float screenSide = Vector3.Dot(leg.position - cam.position, cam.right) >= 0f ? 1f : -1f;

        // A grab point out to the side of the camera, opening/closing over time.
        float spread = legGrabSpread + Mathf.Sin(time * graspSpeed) * graspAmount;
        Vector3 grabPoint = cam.position
                            + cam.right * (screenSide * spread)
                            + cam.up * legGrabRaise
                            + cam.forward * legGrabForward;

        // Aim the cylinder's length (local +Y) from its base toward that point.
        Vector3 dir = grabPoint - leg.position;
        if (dir.sqrMagnitude < 0.0001f) return;
        Vector3 localDir = transform.InverseTransformDirection(dir.normalized);
        leg.localRotation = Quaternion.FromToRotation(Vector3.up, localDir);

        // Stretch and thicken the leg into a visible reaching arm.
        leg.localScale = new Vector3(legThickness, legLength, legThickness);
    }
}
