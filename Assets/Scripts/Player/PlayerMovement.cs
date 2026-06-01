using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    private float dashMultiplier = 2f;
    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Keybinds")]
    private KeyCode jumpKey = KeyCode.Space;
    private KeyCode dashKey = KeyCode.LeftShift;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask ground;
    bool grounded;

    [Header("Animation")]
    private Animator anim;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
        anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, ground);

        MyInput();

        if (grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;

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

        if (Input.GetKeyDown(dashKey))
        {
            moveSpeed *= dashMultiplier;
        }

        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        Vector3 flatvelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (flatvelocity.magnitude > moveSpeed)
        {
            Vector3 limitedVelocity = flatvelocity.normalized * moveSpeed;
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
        float speedValue = 0f;

        if (horizontalInput != 0 || verticalInput != 0)
        {
            speedValue = Input.GetKey(KeyCode.LeftShift)
                ? 1f      // Run
                : 0.5f;   // Walk
        }

        anim.SetFloat("speed", speedValue);
        anim.SetBool("grounded", grounded);
    }
}