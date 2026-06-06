using UnityEngine;

// Fires a jump scare once, when the player walks into this trigger volume.
// Put this on a GameObject with a trigger collider in the hallway and point
// scareActor at the (inactive) jump-scare bug.
public class JumpScareTrigger : MonoBehaviour
{
    [Tooltip("The inactive jump-scare bug to switch on. Its JumpScareActor does the rest.")]
    public GameObject scareActor;

    [Tooltip("Only fire once. Leave on for a classic one-shot scare.")]
    public bool oneShot = true;

    private bool _fired;

    private void OnTriggerEnter(Collider other)
    {
        if (oneShot && _fired) return;

        // The player is the only thing with a Rigidbody walking the hall; a tag
        // check is a bonus in case the player gets tagged later.
        bool isPlayer = other.attachedRigidbody != null || other.CompareTag("Player");
        if (!isPlayer) return;

        _fired = true;
        if (scareActor != null) scareActor.SetActive(true);
    }
}
