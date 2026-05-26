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
    public KeyCode unlockKey = KeyCode.F;

    Readable lookingAtReadable;
    Lockable lookingAtLockable;
    Readable openPage;

    void Start()
    {
        if (cam == null) cam = UnityEngine.Camera.main;
    }

    void Update()
    {
        if (openPage != null)
        {
            if (Input.GetKeyDown(unlockKey) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                ClosePage();
            return;
        }

        UpdateRaycast();

        bool holdingSomething = pickup != null && pickup.HeldObject != null;

        if (lookingAtReadable != null && !holdingSomething && Input.GetMouseButtonDown(0))
            OpenPage(lookingAtReadable);

        if (lookingAtLockable != null && lookingAtLockable.isLocked && Input.GetKeyDown(unlockKey))
        {
            Key held = GetHeldKey();
            if (held != null) lookingAtLockable.TryUnlock(held);
        }
    }

    void UpdateRaycast()
    {
        lookingAtReadable = null;
        lookingAtLockable = null;
        if (cam == null) return;
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            lookingAtReadable = hit.collider.GetComponentInParent<Readable>();
            lookingAtLockable = hit.collider.GetComponentInParent<Lockable>();
        }
    }

    Key GetHeldKey()
    {
        if (pickup == null || pickup.HeldObject == null) return null;
        return pickup.HeldObject.GetComponent<Key>();
    }

    void OpenPage(Readable page)
    {
        openPage = page;
        if (playerController != null) playerController.enabled = false;
        if (cameraController != null) cameraController.SetLookEnabled(false);
        if (pickup != null) pickup.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ClosePage()
    {
        openPage = null;
        if (playerController != null) playerController.enabled = true;
        if (cameraController != null) cameraController.SetLookEnabled(true);
        if (pickup != null) pickup.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnGUI()
    {
        if (openPage != null) { DrawPageDialog(); return; }
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

        if (lookingAtLockable != null && lookingAtLockable.isLocked)
        {
            Key held = GetHeldKey();
            if (held != null && held.keyId == lookingAtLockable.requiredKeyId)
                prompt = "Press F to unlock";
            else
                prompt = "Locked.";
        }
        else if (lookingAtReadable != null && (pickup == null || pickup.HeldObject == null))
        {
            prompt = "Left-Click to read";
        }

        if (prompt == null) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28, alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        float w = 520, h = 56;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.7f;
        GUI.Box(new Rect(x, y, w, h), "");
        GUI.Label(new Rect(x, y, w, h), prompt, style);
    }
}
