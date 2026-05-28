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
    public float rotationSensitivity = 3f;

    [Header("Drop")]
    public KeyCode dropKey = KeyCode.Q;

    [Header("Hold Physics")]
    [Tooltip("Spring stiffness pulling the held object toward its target position.")]
    public float positionStiffness = 400f;
    [Tooltip("Spring damping to keep the held object from oscillating.")]
    public float positionDamping = 35f;

    public MonoBehaviour playerController;
    public CameraController cameraController;

    private Collider _playerCollider;
    private UnityEngine.Camera _cam;
    private GameObject _heldObject;
    private Rigidbody _heldRb;

    public GameObject HeldObject => _heldObject;

    private float _originalDrag;
    private float _originalAngularDrag;
    private bool _originalUseGravity;
    private CollisionDetectionMode _originalCollisionMode;
    private RigidbodyConstraints _originalConstraints;

    private bool _isInspecting;
    private bool _isDragging;
    private Vector3 _lastMousePosition;
    private Quaternion _userRotation = Quaternion.identity;

    private PickableGlow _currentGlow;

    void Start()
    {
        _cam = GetComponent<UnityEngine.Camera>();
        if (playerController != null)
            _playerCollider = playerController.GetComponentInChildren<Collider>();
    }

    void Update()
    {
        if (_heldObject == null)
        {
            UpdateHoverGlow();
            TryPickup();
            return;
        }

        ClearHoverGlow();

        // Q drops at any time while holding.
        if (Input.GetKeyDown(dropKey))
        {
            DropObject();
            return;
        }

        if (_isInspecting)
        {
            HandleInspectRotation();

            // RMB exits inspect (back to hold-at-hand).
            if (Input.GetMouseButtonDown(1)) ExitInspectMode();
        }
        else
        {
            // LMB while already holding enters inspect.
            if (Input.GetMouseButtonDown(0)) EnterInspectMode();
        }
    }

    void HandleInspectRotation()
    {
        // Hold LMB and drag the mouse to rotate the inspected object.
        // Cursor is unlocked during the drag so Input.mousePosition deltas update.
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            _lastMousePosition = Input.mousePosition;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (cameraController != null) cameraController.SetLookEnabled(false);
        }
        if (Input.GetMouseButtonUp(0) && _isDragging)
        {
            EndDrag();
        }
        if (_isDragging)
        {
            Vector3 delta = Input.mousePosition - _lastMousePosition;
            _lastMousePosition = Input.mousePosition;
            Quaternion yaw   = Quaternion.AngleAxis(-delta.x * rotationSensitivity, Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(delta.y * rotationSensitivity, Vector3.right);
            _userRotation = yaw * pitch * _userRotation;
        }
    }

    void FixedUpdate()
    {
        if (_heldObject != null) HoldObject();
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
                _userRotation = Quaternion.identity;

                if (_heldRb != null)
                {
                    _originalConstraints = _heldRb.constraints;
                    _originalDrag = _heldRb.linearDamping;
                    _originalAngularDrag = _heldRb.angularDamping;
                    _originalUseGravity = _heldRb.useGravity;
                    _originalCollisionMode = _heldRb.collisionDetectionMode;

                    _heldRb.linearVelocity = Vector3.zero;
                    _heldRb.angularVelocity = Vector3.zero;
                    _heldRb.useGravity = false;
                    _heldRb.linearDamping = 1f;
                    _heldRb.angularDamping = 5f;
                    _heldRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }

                if (_playerCollider != null)
                {
                    foreach (Collider col in _heldObject.GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(col, _playerCollider, true);
                }
            }
        }
    }

    void HoldObject()
    {
        Vector3 targetPos;
        if (_isInspecting)
        {
            targetPos = _cam.transform.position + _cam.transform.forward * inspectDistance;
        }
        else
        {
            targetPos = _cam.transform.position
                + _cam.transform.forward * holdForward
                + _cam.transform.right * holdRight
                - _cam.transform.up * holdDown;
        }

        Vector3 toTarget = targetPos - _heldRb.position;
        Vector3 force = toTarget * positionStiffness - _heldRb.linearVelocity * positionDamping;
        _heldRb.AddForce(force, ForceMode.Acceleration);

        _heldRb.MoveRotation(_cam.transform.rotation * _userRotation);
    }

    void EnterInspectMode()
    {
        _isInspecting = true;
    }

    void ExitInspectMode()
    {
        _isInspecting = false;
        EndDrag();
    }

    void RestorePlayerCollision()
    {
        if (_heldObject == null || _playerCollider == null) return;
        foreach (Collider col in _heldObject.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(col, _playerCollider, false);
    }

    void RestorePhysicsState()
    {
        if (_heldRb == null) return;
        _heldRb.linearDamping = _originalDrag;
        _heldRb.angularDamping = _originalAngularDrag;
        _heldRb.useGravity = _originalUseGravity;
        _heldRb.collisionDetectionMode = _originalCollisionMode;
        _heldRb.constraints = _originalConstraints;
    }

    void EndDrag()
    {
        if (!_isDragging) return;
        _isDragging = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (cameraController != null) cameraController.SetLookEnabled(true);
    }

    public void ForceRelease()
    {
        if (_heldObject == null) return;
        _isInspecting = false;
        EndDrag();
        RestorePlayerCollision();
        _heldObject = null;
        _heldRb = null;
    }

    void DropObject()
    {
        _isInspecting = false;
        EndDrag();
        RestorePlayerCollision();
        RestorePhysicsState();
        _heldObject = null;
        _heldRb = null;
    }

    void UpdateHoverGlow()
    {
        Ray ray = _cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

        PickableGlow newGlow = null;

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            if (hit.collider.CompareTag("Pickupable"))
                newGlow = hit.collider.GetComponentInParent<PickableGlow>();
        }

        if (_currentGlow == newGlow) return;

        ClearHoverGlow();

        _currentGlow = newGlow;
        if (_currentGlow != null)
            _currentGlow.SetGlow(true);
    }

    void ClearHoverGlow()
    {
        if (_currentGlow != null)
        {
            _currentGlow.SetGlow(false);
            _currentGlow = null;
        }
    }
}
