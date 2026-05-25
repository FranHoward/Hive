using UnityEngine;

public class ObjectPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float pickupRange = 5f;

    [Header("Hold Position (relative to camera)")]
    public float holdRight = 0.4f;
    public float holdDown = 0.3f;
    public float holdForward = 0.8f;

    [Header("Inspect Settings")]
    public float inspectDistance = 1.5f;
    public float inspectScale = 2f;
    public float rotationSensitivity = 3f;

    public MonoBehaviour playerController;
    public CameraController cameraController;

    private UnityEngine.Camera _cam;
    private GameObject _heldObject;
    private Rigidbody _heldRb;

    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Vector3 _originalScale;
    private RigidbodyConstraints _originalConstraints;

    private bool _isInspecting = false;
    private bool _isDragging = false;
    private Vector3 _lastMousePosition;

    void Start()
    {
        _cam = GetComponent<UnityEngine.Camera>();
    }

    void Update()
    {
        if (_heldObject == null)
        {
            TryPickup();
            return;
        }

        if (_isInspecting)
        {
            HandleInspectRotation();

            if (Input.GetMouseButtonDown(1))
            {
                ExitInspectMode();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                EnterInspectMode();
            }

            if (Input.GetMouseButtonDown(1))
            {
                ReturnObject();
            }
        }
    }

    void FixedUpdate()
    {
        if (_heldObject != null)
        {
            HoldObject();
        }
    }

    void TryPickup()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = _cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            if (hit.collider.CompareTag("Pickupable"))
            {
                _heldObject = hit.collider.gameObject;
                _heldRb = _heldObject.GetComponent<Rigidbody>();

                _originalPosition = _heldObject.transform.position;
                _originalRotation = _heldObject.transform.rotation;
                _originalScale = _heldObject.transform.localScale;

                if (_heldRb != null)
                {
                    _originalConstraints = _heldRb.constraints;
                    _heldRb.linearVelocity = Vector3.zero;
                    _heldRb.angularVelocity = Vector3.zero;
                    _heldRb.isKinematic = true;
                    _heldRb.constraints = RigidbodyConstraints.FreezeRotation;
                }

                foreach (Collider col in _heldObject.GetComponentsInChildren<Collider>())
                    col.enabled = false;
            }
        }
    }

    void HoldObject()
    {
        if (_isInspecting)
        {
            Vector3 inspectPosition = _cam.transform.position
                + _cam.transform.forward * inspectDistance;

            _heldRb.MovePosition(inspectPosition);
        }
        else
        {
            Vector3 targetPosition = _cam.transform.position
                + _cam.transform.forward * holdForward
                + _cam.transform.right * holdRight
                - _cam.transform.up * holdDown;

            _heldRb.MovePosition(targetPosition);
            _heldRb.MoveRotation(_cam.transform.rotation);
        }
    }

    void EnterInspectMode()
    {
        _isInspecting = true;

        _heldObject.transform.localScale = _originalScale * inspectScale;

        if (playerController != null) playerController.enabled = false;
        if (cameraController != null) cameraController.SetLookEnabled(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ExitInspectMode()
    {
        _isInspecting = false;

        _heldObject.transform.localScale = _originalScale;

        if (playerController != null) playerController.enabled = true;
        if (cameraController != null) cameraController.SetLookEnabled(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void HandleInspectRotation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            _lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }

        if (_isDragging)
        {
            Vector3 delta = Input.mousePosition - _lastMousePosition;
            _lastMousePosition = Input.mousePosition;

            _heldObject.transform.Rotate(
                _cam.transform.right,
                delta.y * rotationSensitivity,
                Space.World
            );
            _heldObject.transform.Rotate(
                Vector3.up,
                -delta.x * rotationSensitivity,
                Space.World
            );
        }
    }

    void ReturnObject()
    {
        if (_isInspecting) ExitInspectMode();

        if (_heldRb != null)
        {
            _heldRb.isKinematic = false;
            _heldRb.constraints = _originalConstraints;
            _heldRb.linearVelocity = Vector3.zero;
            _heldRb.angularVelocity = Vector3.zero;
        }

        foreach (Collider col in _heldObject.GetComponentsInChildren<Collider>())
            col.enabled = true;

        _heldObject.transform.position = _originalPosition;
        _heldObject.transform.rotation = _originalRotation;
        _heldObject.transform.localScale = _originalScale;

        _heldObject = null;
        _heldRb = null;
        _isInspecting = false;
    }
}