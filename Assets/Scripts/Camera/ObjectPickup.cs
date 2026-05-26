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

    [Header("Hold Physics")]
    [Tooltip("Spring stiffness pulling the held object toward the hand position.")]
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

    private Vector3 _originalScale;
    private float _originalDrag;
    private float _originalAngularDrag;
    private bool _originalUseGravity;
    private CollisionDetectionMode _originalCollisionMode;
    private RigidbodyConstraints _originalConstraints;

    private bool _isInspecting = false;
    private bool _isDragging = false;
    private Vector3 _lastMousePosition;

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
            TryPickup();
            return;
        }

        if (_isInspecting)
        {
            HandleInspectRotation();
            if (Input.GetMouseButtonDown(1)) ExitInspectMode();
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) EnterInspectMode();
            if (Input.GetMouseButtonDown(1)) DropObject();
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
                _originalScale = _heldObject.transform.localScale;

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
        if (_isInspecting)
        {
            Vector3 inspectPos = _cam.transform.position + _cam.transform.forward * inspectDistance;
            _heldRb.MovePosition(inspectPos);
            return;
        }

        Vector3 targetPos = _cam.transform.position
            + _cam.transform.forward * holdForward
            + _cam.transform.right * holdRight
            - _cam.transform.up * holdDown;

        Vector3 toTarget = targetPos - _heldRb.position;
        Vector3 force = toTarget * positionStiffness - _heldRb.linearVelocity * positionDamping;
        _heldRb.AddForce(force, ForceMode.Acceleration);

        _heldRb.MoveRotation(_cam.transform.rotation);
    }

    void EnterInspectMode()
    {
        _isInspecting = true;
        if (_heldRb != null) _heldRb.isKinematic = true;
        _heldObject.transform.localScale = _originalScale * inspectScale;
        if (playerController != null) playerController.enabled = false;
        if (cameraController != null) cameraController.SetLookEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ExitInspectMode()
    {
        _isInspecting = false;
        if (_heldRb != null) _heldRb.isKinematic = false;
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
        if (Input.GetMouseButtonUp(0)) _isDragging = false;

        if (_isDragging)
        {
            Vector3 delta = Input.mousePosition - _lastMousePosition;
            _lastMousePosition = Input.mousePosition;
            _heldObject.transform.Rotate(_cam.transform.right, delta.y * rotationSensitivity, Space.World);
            _heldObject.transform.Rotate(Vector3.up, -delta.x * rotationSensitivity, Space.World);
        }
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

    public void ForceRelease()
    {
        if (_heldObject == null) return;
        if (_isInspecting) ExitInspectMode();
        RestorePlayerCollision();
        _heldObject = null;
        _heldRb = null;
        _isInspecting = false;
    }

    void DropObject()
    {
        if (_isInspecting) ExitInspectMode();
        RestorePlayerCollision();
        RestorePhysicsState();
        _heldObject = null;
        _heldRb = null;
        _isInspecting = false;
    }
}
