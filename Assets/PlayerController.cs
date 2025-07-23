using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public LayerMask groundMask;
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Lock rotation to prevent unwanted tilting
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        Move();
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }
    }

    void Move()
    {
        // Only consider keyboard input for movement
        float moveX = Input.GetAxisRaw("Horizontal"); // Left/Right (A/D)
        float moveZ = Input.GetAxisRaw("Vertical");   // Forward/Backward (W/S)

        Vector3 moveDirection = new Vector3(moveX, 0f, moveZ).normalized;

        Vector3 moveVelocity = moveDirection * moveSpeed;

        // Preserve vertical velocity (gravity)
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
    }

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
