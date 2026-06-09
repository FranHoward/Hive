using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    private float dashMultiplier = 1.5f;
    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    private bool readyToJump;

    [Header("Keybinds")]
    private KeyCode jumpKey = KeyCode.Space;
    private KeyCode dashKey = KeyCode.LeftShift;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask ground;
    private Collider col;
    private bool grounded;

    [Header("Animation")]
    private Animator anim;

    public Transform orientation;

    private float horizontalInput;
    private float verticalInput;

    private Vector3 moveDirection;
    private Rigidbody rb;

    private bool isWalking;
    private bool isRunning;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;

        anim = GetComponentInChildren<Animator>();
        col = GetComponent<Collider>();
    }

    private void Update()
    {
        // Debug.Log(playerHeight);

        grounded = Physics.Raycast(
            rb.position + Vector3.up * 1f,
            Vector3.down,
            playerHeight + 0.2f,
            ground
        );

        // Debug.Log("Grounded: " + grounded);

        MyInput();

        if (grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0f;

        SpeedControl();

        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Debug.Log(grounded);
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        float currentSpeed = moveSpeed;

        if (Input.GetKey(dashKey))
        {
            currentSpeed *= dashMultiplier;
        }

        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * currentSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * currentSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        float maxSpeed = moveSpeed;

        if (Input.GetKey(dashKey))
        {
            maxSpeed *= dashMultiplier;
        }

        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (flatVelocity.magnitude > maxSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        anim.SetTrigger("jump");

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void UpdateAnimations()
    {
        bool hasMovementInput = horizontalInput != 0 || verticalInput != 0;

        isRunning = hasMovementInput && Input.GetKey(dashKey);
        isWalking = hasMovementInput && !Input.GetKey(dashKey);

        anim.SetBool("walking", isWalking);
        anim.SetBool("running", isRunning);
        anim.SetBool("grounded", grounded);
    }
}