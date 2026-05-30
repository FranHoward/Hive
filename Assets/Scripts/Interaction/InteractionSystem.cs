using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    [Header("References")]
    public UnityEngine.Camera cam;
    public ObjectPickup pickup;
    public MonoBehaviour playerController;
    public CameraController cameraController;

    [Header("Settings")]
    public float interactRange = 4f;
    public KeyCode interactKey = KeyCode.F;

    Readable lookingAtReadable;
    Lockable lookingAtLockable;
    Keypad   lookingAtKeypad;
    BlockSlot lookingAtBlockSlot;
    InteractableGlow lookingAtGlow;

    Readable openPage;
    bool _uiSuspended;

    void Start()
    {
        if (cam == null) cam = UnityEngine.Camera.main;
    }

    void Update()
    {
        if (openPage != null)
        {
            if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                ClosePage();
            return;
        }

        if (_uiSuspended) return;

        UpdateRaycast();

        bool holdingSomething = pickup != null && pickup.HeldObject != null;

        if (lookingAtReadable != null && !holdingSomething && Input.GetMouseButtonDown(0))
            OpenPage(lookingAtReadable);

        if (lookingAtLockable != null && lookingAtLockable.isLocked && Input.GetKeyDown(interactKey))
        {
            Key held = GetHeldKey();
            if (held != null) lookingAtLockable.TryUnlock(held);
        }

        if (lookingAtKeypad != null && !lookingAtKeypad.IsSolved && Input.GetKeyDown(interactKey))
            lookingAtKeypad.Activate(this);
    }

    void UpdateRaycast()
    {
        if (lookingAtGlow != null)
        {
            lookingAtGlow.SetGlow(false);
            lookingAtGlow = null;
        }

        lookingAtReadable = null;
        lookingAtLockable = null;
        lookingAtKeypad = null;
        lookingAtBlockSlot = null;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            if (hit.collider.CompareTag("Interactable"))
            {
                lookingAtGlow = hit.collider.GetComponentInParent<InteractableGlow>();

                if (lookingAtGlow != null)
                    lookingAtGlow.SetGlow(true);
            }

            lookingAtReadable  = hit.collider.GetComponentInParent<Readable>();
            lookingAtLockable  = hit.collider.GetComponentInParent<Lockable>();
            lookingAtKeypad    = hit.collider.GetComponentInParent<Keypad>();
            lookingAtBlockSlot = hit.collider.GetComponentInParent<BlockSlot>();
        }
    }

    Key GetHeldKey()
    {
        if (pickup == null || pickup.HeldObject == null) return null;
        return pickup.HeldObject.GetComponent<Key>();
    }

    BlockTag GetHeldBlock()
    {
        if (pickup == null || pickup.HeldObject == null) return null;
        return pickup.HeldObject.GetComponent<BlockTag>();
    }

    public void SuspendForUI()
    {
        _uiSuspended = true;
        if (playerController != null) playerController.enabled = false;
        if (cameraController != null) cameraController.SetLookEnabled(false);
        if (pickup != null) pickup.enabled = false;
    }

    public void ResumeFromUI()
    {
        _uiSuspended = false;
        if (playerController != null) playerController.enabled = true;
        if (cameraController != null) cameraController.SetLookEnabled(true);
        if (pickup != null) pickup.enabled = true;
    }

    void OpenPage(Readable page)
    {
        openPage = page;
        SuspendForUI();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ClosePage()
    {
        openPage = null;
        ResumeFromUI();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnGUI()
    {
        if (openPage != null) { DrawPageDialog(); return; }
        if (_uiSuspended) return;
        DrawPrompt();
    }

    void DrawPageDialog()
    {
        float w = Mathf.Min(960f, Screen.width * 0.8f);
        float h = Mathf.Min(560f, Screen.height * 0.75f);
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        GUI.Box(new Rect(x, y, w, h), "");

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 40, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(x, y + 24, w, 56), openPage.title, titleStyle);

        var bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26, wordWrap = true, alignment = TextAnchor.UpperLeft
        };
        GUI.Label(new Rect(x + 40, y + 110, w - 80, h - 170), openPage.text, bodyStyle);

        var hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18, alignment = TextAnchor.LowerCenter,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };
        GUI.Label(new Rect(x, y + h - 40, w, 28), "Press F, Esc, or Right-Click to close", hintStyle);
    }

    void DrawPrompt()
    {
        string prompt = null;
        bool holding = pickup != null && pickup.HeldObject != null;

        if (lookingAtLockable != null && lookingAtLockable.isLocked)
        {
            Key held = GetHeldKey();
            prompt = (held != null && held.keyId == lookingAtLockable.requiredKeyId)
                ? "Press F to unlock"
                : "Locked.";
        }
        else if (lookingAtKeypad != null && !lookingAtKeypad.IsSolved)
        {
            prompt = "Press F to use keypad";
        }
        else if (lookingAtBlockSlot != null && !lookingAtBlockSlot.IsFilled)
        {
            BlockTag held = GetHeldBlock();
            prompt = (held != null && held.blockId == lookingAtBlockSlot.requiredBlockId)
                ? "Bring the block to the slot"
                : "An empty slot. Something fits here.";
        }
        else if (lookingAtReadable != null && !holding)
        {
            prompt = "Left-Click to read";
        }

        if (prompt == null) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28, alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        float w = 560, h = 56;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.7f;
        GUI.Box(new Rect(x, y, w, h), "");
        GUI.Label(new Rect(x, y, w, h), prompt, style);
    }
}
