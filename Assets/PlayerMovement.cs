using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public CharacterController charController;
    private Animator anim;
    public Camera camera;

    public bool canMove = true;
    private float gravityY = 0.0f;
    public float mass = 1.0f;
    private bool isJumping;
    private bool isGrounded;
    private bool isRunning = false;
    private bool isCrouching = false;
    private float lerpRun = 0.0f;
    private float lerpCrouch = 0.0f;

    public float jumpCooldownTimer = 0f;
    public float jumpCooldown = 1f;

    private Vector3 jumpMomentum = Vector3.one;

    public bool lockRotation = false; // ✅ Lock movement-based rotation when needed (e.g., knife throw)

    void Start()
    {
        charController = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        isGrounded = true;
    }

    void Update()
    {
        if (!canMove)
        {
            return;
        }
        if (jumpCooldownTimer > 0f) jumpCooldownTimer -= Time.deltaTime;

        float dX = Input.GetAxis("Horizontal");
        float dY = Input.GetAxis("Vertical");

        Vector3 movementVector = new Vector3(dX, 0, dY);
        movementVector = Quaternion.AngleAxis(camera.transform.eulerAngles.y, Vector3.up) * movementVector;
        movementVector.Normalize();

        HandleGravity();

        if (charController.isGrounded)
        {
            isJumping = false;
            anim.SetBool("IsJumping", false);
            isGrounded = true;
            anim.SetBool("IsGrounded", true);
            anim.SetBool("IsFalling", false);

            if (Input.GetKeyDown(KeyCode.Space) && jumpCooldownTimer <= 0f)
            {
                AnimatorStateInfo currentState = anim.GetCurrentAnimatorStateInfo(0);
                bool notInTransition = !anim.IsInTransition(0);
                bool isIdle = currentState.IsName("Idle");
                bool isWalking = currentState.IsName("Walk_Run") && anim.GetBool("Walking");

                if (notInTransition && (isIdle || isWalking))
                {
                    isJumping = true;
                    anim.SetBool("IsJumping", true);
                    gravityY = 3.0f;
                    jumpMomentum = movementVector * (isRunning ? 4.0f : 2.0f);
                    jumpCooldownTimer = jumpCooldown;
                }
            }
        }

        Vector3 newMoveVector = isGrounded ? movementVector : jumpMomentum;
        newMoveVector.y = gravityY;

        HandleCrouching();
        HandleCrouchWalk(movementVector);
        HandleRunning(movementVector);

        charController.Move(newMoveVector * Time.deltaTime);
        RotateCharacter(movementVector);
    }

    private void HandleGravity()
    {
        gravityY += Physics.gravity.y * mass * Time.deltaTime;
        if (charController.isGrounded) gravityY = -0.5f;
    }

    private float lerpCrouchWalk = 0.0f;

    private void HandleCrouching()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = true;
            anim.SetBool("IsCrouching", true);
            lerpCrouchWalk = 0.0f;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            anim.SetBool("IsCrouching", false);
            anim.SetFloat("Crouch_Walk", 0.0f);
        }
    }

    private void HandleCrouchWalk(Vector3 movementVector)
    {
        if (isCrouching && movementVector != Vector3.zero)
        {
            float delta = Mathf.Lerp(0.0f, 1.0f, lerpCrouchWalk);
            if (lerpCrouchWalk < 1.0f)
                lerpCrouchWalk += 2.0f * Time.deltaTime;

            anim.SetFloat("Crouch_Walk", delta);
        }
        else if (isCrouching)
        {
            lerpCrouchWalk = 0.0f;
            anim.SetFloat("Crouch_Walk", 0.0f);
        }
    }

    private void HandleRunning(Vector3 movementVector)
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isRunning = true;
            lerpRun = 0.0f;
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isRunning = false;
            lerpRun = 0.0f;
        }

        if (movementVector != Vector3.zero)
        {
            anim.SetBool("Walking", true);

            if (isRunning)
            {
                float delta = Mathf.Lerp(0.5f, 1.0f, lerpRun);
                if (lerpRun < 1.0f)
                    lerpRun += 2.0f * Time.deltaTime;
                anim.SetFloat("Walk_Run", delta);
            }
            else
            {
                float delta = Mathf.Lerp(anim.GetFloat("Walk_Run"), 0.5f, 1.0f * Time.deltaTime);
                anim.SetFloat("Walk_Run", delta);
            }
        }
        else
        {
            anim.SetBool("Walking", false);
        }
    }

    private void RotateCharacter(Vector3 movementVector)
    {
        if (!lockRotation && movementVector != Vector3.zero)
        {
            Quaternion rotationDirection = Quaternion.LookRotation(movementVector, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationDirection, 360 * Time.deltaTime);
        }
    }

    private void OnAnimatorMove()
    {
        Vector3 _move = anim.deltaPosition;
        _move.y = gravityY * Time.deltaTime;
        charController.Move(_move);
    }
}
