using UnityEngine;

// Flashlight on/off toggle. The flashlight is only usable while the player is
// actually holding it (via ObjectPickup); while held it shows a "Press R" prompt
// and the toggle key switches the beam on/off.
public class FlashlightToggle : MonoBehaviour
{
    [Tooltip("The light to switch on/off. Defaults to a Light on this object or its children.")]
    public Light flashlightLight;

    [Tooltip("Key that toggles the flashlight on/off.")]
    public KeyCode toggleKey = KeyCode.R;

    [Tooltip("The flashlight is only usable while held by this pickup.")]
    public ObjectPickup requireHeldBy;

    Rigidbody _rb;

    void Awake()
    {
        if (flashlightLight == null) flashlightLight = GetComponentInChildren<Light>(true);

        // Let the flashlight fall and rest under gravity when it isn't being held.
        _rb = GetComponent<Rigidbody>();
        if (_rb != null) _rb.useGravity = true;
    }

    // Only usable once the player has actually picked the flashlight up.
    bool Held => requireHeldBy != null && requireHeldBy.HeldObject == gameObject;

    void Update()
    {
        if (flashlightLight == null || !Held) return;

        // Keep the beam pointing where the player looks, no matter how the
        // flashlight model itself is tilted in the hand.
        flashlightLight.transform.rotation = requireHeldBy.transform.rotation;

        if (Input.GetKeyDown(toggleKey))
            flashlightLight.enabled = !flashlightLight.enabled;
    }

    void OnGUI()
    {
        if (!Held) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        float w = 360f, h = 40f;
        GUI.Label(new Rect((Screen.width - w) * 0.5f, Screen.height - 80f, w, h),
                  "Press " + toggleKey + " to turn On/Off", style);
    }
}
