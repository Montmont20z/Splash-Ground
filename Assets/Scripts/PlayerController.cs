using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float sprintSpeed = 8f;

    [Header("Jump Settings")]
    public float jumpForce = 8f;
    public float coyoteTime = 0.15f; // Grace period after leaving ground
    private float coyoteTimeCounter;

    [Header("Gravity")]
    public float gravity = -9.81f;
    public float groundStick = -1f;
    private float verticalVelocity = 0f;

    [Header("References")]
    public ArenaManager arena;
    public Animator animator; // assign in inspector or will auto-find in children
    private CharacterController controller;

    // Animator parameter hashes
    private readonly int isMovingHash = Animator.StringToHash("IsMoving");
    private readonly int isSprintingHash = Animator.StringToHash("IsSprinting");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int jumpTriggerHash = Animator.StringToHash("Jump");

    // internal
    private bool wasGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null) Debug.LogError("CharacterController missing!");
        if (arena == null) Debug.LogWarning("ArenaManager reference not assigned. Movement will still have gravity but not tile-blocking.");

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null) Debug.LogWarning("Animator not assigned and none found in children. Animations will not play.");
        }

        if (animator != null && animator.applyRootMotion)
            Debug.LogWarning("Animator.applyRootMotion is enabled. For CharacterController-driven movement, disable Apply Root Motion on the Animator component.");
    }

    void Update()
    {
        HandleMovement();

        // make sure player tp back to make in case fall off
        if (transform.position.y <= -10f)
        {
            if (arena != null) arena.PlacePlayerOnArenaCenter();
        }
    }

    void HandleMovement()
    {
        // Input
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
        float vertical = Input.GetAxisRaw("Vertical");   // W/S or Up/Down arrows

        // Move relative to player orientation
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

        // Sprint check
        bool sprintInput = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = sprintInput ? sprintSpeed : moveSpeed;
        Vector3 desiredHorizontal = moveDirection * currentSpeed; // units per second

        bool isGrounded = controller.isGrounded;

        // Coyote time handling
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump input
        bool wantsToJump = Input.GetKeyDown(KeyCode.Space) && coyoteTimeCounter > 0f;
        if (wantsToJump)
        {
            verticalVelocity = jumpForce;
            coyoteTimeCounter = 0f; // consume
            if (animator != null) animator.SetTrigger(jumpTriggerHash);
        }

        if (isGrounded && verticalVelocity < 0f)
        {
            // Stick to ground
            verticalVelocity = groundStick;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Compute displacement vectors for this frame
        Vector3 horizontalDisplacement = new Vector3(desiredHorizontal.x, 0f, desiredHorizontal.z) * Time.deltaTime;
        Vector3 verticalDisplacement = Vector3.up * (verticalVelocity * Time.deltaTime);

        // --- Animation parameters (explicit state approach, no blend tree) ---
        if (animator != null)
        {
            bool isMoving = desiredHorizontal.sqrMagnitude > 0.01f;
            bool isSprinting = isMoving && sprintInput;

            animator.SetBool(isMovingHash, isMoving);
            animator.SetBool(isSprintingHash, isSprinting);
            animator.SetBool(isGroundedHash, isGrounded);
        }

        // --- Movement application (tile-checking if arena exists) ---
        if (arena == null)
        {
            controller.Move(desiredHorizontal * Time.deltaTime + verticalDisplacement);
            UpdateLandingState(isGrounded);
            return;
        }

        Vector3 currentPos = transform.position;
        Vector3 horizTargetPos = currentPos + horizontalDisplacement;

        if (!isGrounded)
        {
            // In air: allow free horizontal movement
            controller.Move(horizontalDisplacement + verticalDisplacement);
        }
        else if (IsPositionOnTile(horizTargetPos))
        {
            // Safe to move horizontally and vertically
            controller.Move(horizontalDisplacement + verticalDisplacement);
        }
        else
        {
            // Try sliding along X then Z
            Vector3 moveX = new Vector3(horizontalDisplacement.x, 0f, 0f);
            Vector3 moveZ = new Vector3(0f, 0f, horizontalDisplacement.z);

            bool moved = false;
            if (moveX.sqrMagnitude > 0f && IsPositionOnTile(currentPos + moveX))
            {
                controller.Move(moveX + verticalDisplacement);
                moved = true;
            }
            else if (moveZ.sqrMagnitude > 0f && IsPositionOnTile(currentPos + moveZ))
            {
                controller.Move(moveZ + verticalDisplacement);
                moved = true;
            }

            if (!moved)
            {
                // Block horizontal; still apply vertical so player will fall if no tile below
                controller.Move(verticalDisplacement);
            }
        }

        UpdateLandingState(isGrounded);
    }

    void UpdateLandingState(bool isGrounded)
    {
        // Optional: trigger a "Land" behavior if you want
        if (!wasGrounded && isGrounded)
        {
            // Landed this frame: you can trigger a landing animation or play SFX here
            // e.g. animator.SetTrigger("Land");
        }
        wasGrounded = isGrounded;
    }

    bool IsPositionOnTile(Vector3 worldPos)
    {
        if (arena == null) return true; // if no arena reference, assume allowed

        float ts = Mathf.Approximately(arena.tileSize, 0f) ? 1f : arena.tileSize;

        int gx = Mathf.RoundToInt(worldPos.x / ts);
        int gz = Mathf.RoundToInt(worldPos.z / ts);

        FloorTile t = arena.GetTile(gx, gz);
        return t != null;
    }
}
