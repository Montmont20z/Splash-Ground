//using UnityEngine;

//[RequireComponent(typeof(CharacterController))]
//public class PlayerController : MonoBehaviour
//{
//    [Header("Movement Settings")]
//    public float moveSpeed = 4f;
//    public float sprintSpeed = 8f;

//    [Header("Gravity")]
//    public float gravity = -9.81f;
//    public float grounStick = -1f;
//    private float verticalVelocity = 0f;

//    [Header("References")]
//    public ArenaManager arena;
//    private CharacterController controller;

//    void Start()
//    {
//        controller = GetComponent<CharacterController>();
//        if (controller == null) Debug.LogError("CharacterController missing!");
//        if (arena == null) Debug.LogWarning("ArenaManager reference not assigned. Movement will still have gravity but not tile-blocking.");
//    }

//    void Update()
//    {
//        HandleMovement();

//        // make sure player tp back to make in case fall off
//        if (transform.position.y <= -10)
//        {
//            arena.PlacePlayerOnArenaCenter();
//        }
//    }

//    void HandleMovement()
//    {
//        // Get input from WASD or Arrow keys
//        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
//        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down arrows
//        // Move relative to player orientation
//        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
//        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

//        // Check for sprinting
//        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
//        Vector3 desiredHorizontal = moveDirection * currentSpeed; // units per second

//        bool isGrounded = controller.isGrounded;

//        if (isGrounded && verticalVelocity < 0f)
//        {
//            // Stick to ground
//            verticalVelocity = grounStick;
//        }
//        else
//        {
//            verticalVelocity += gravity * Time.deltaTime;
//        }
//        // Compute desired displacement this frame
//        Vector3 desiredDisplacement = desiredHorizontal * Time.deltaTime + Vector3.up * (verticalVelocity * Time.deltaTime);

//        // If no arena reference, just move with gravity (colliders still work if tiles have colliders)
//        if (arena == null)
//        {
//            controller.Move(desiredDisplacement);
//            return;
//        }

//        // Try to move horizontally only if the destination cell contains a tile.
//        // We'll separate horizontal and vertical movement:
//        Vector3 currentPos = transform.position;
//        Vector3 horizontalDisplacement = new Vector3(desiredHorizontal.x, 0f, desiredHorizontal.z) * Time.deltaTime;
//        Vector3 verticalDisplacement = Vector3.up * (verticalVelocity * Time.deltaTime);

//        // First, compute the target world position if we applied the horizontal move
//        Vector3 horizTargetPos = currentPos + horizontalDisplacement;

//        // Check if there is a tile under the horizTargetPos (i.e., target grid cell contains a tile)
//        if (IsPositionOnTile(horizTargetPos))
//        {
//            // Safe to move horizontally and then apply vertical move
//            controller.Move(horizontalDisplacement + verticalDisplacement);
//        }
//        else
//        {
//            // Not safe to move full horizontal; try sliding along X then Z
//            Vector3 moveX = new Vector3(horizontalDisplacement.x, 0f, 0f);
//            Vector3 moveZ = new Vector3(0f, 0f, horizontalDisplacement.z);

//            bool moved = false;
//            if (moveX.sqrMagnitude > 0f && IsPositionOnTile(currentPos + moveX))
//            {
//                controller.Move(moveX + verticalDisplacement);
//                moved = true;
//            }
//            else if (moveZ.sqrMagnitude > 0f && IsPositionOnTile(currentPos + moveZ))
//            {
//                controller.Move(moveZ + verticalDisplacement);
//                moved = true;
//            }

//            if (!moved)
//            {
//                // Block horizontal; still apply vertical (so player will fall if no tile below)
//                controller.Move(verticalDisplacement);
//            }
//        }


//    }

//    bool IsPositionOnTile(Vector3 worldPos)
//    {
//        if (arena == null) return true; // if no arena reference, assume allowed

//        float ts = Mathf.Approximately(arena.tileSize, 0f) ? 1f : arena.tileSize;

//        // Map world position to nearest tile indices.
//        // If your level origin is not (0,0) you may need to subtract origin offset here.
//        int gx = Mathf.RoundToInt(worldPos.x / ts);
//        int gz = Mathf.RoundToInt(worldPos.z / ts);

//        FloorTile t = arena.GetTile(gx, gz);
//        return t != null;
//    }
//}
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
    public float grounStick = -1f;
    private float verticalVelocity = 0f;

    [Header("References")]
    public ArenaManager arena;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null) Debug.LogError("CharacterController missing!");
        if (arena == null) Debug.LogWarning("ArenaManager reference not assigned. Movement will still have gravity but not tile-blocking.");
    }

    void Update()
    {
        HandleMovement();

        // make sure player tp back to make in case fall off
        if (transform.position.y <= -10)
        {
            arena.PlacePlayerOnArenaCenter();
        }
    }

    void HandleMovement()
    {
        // Get input from WASD or Arrow keys
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down arrows
        // Move relative to player orientation
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

        // Check for sprinting
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
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
        if (Input.GetKeyDown(KeyCode.Space) && coyoteTimeCounter > 0f)
        {
            verticalVelocity = jumpForce;
            coyoteTimeCounter = 0f; // Consume coyote time
        }

        if (isGrounded && verticalVelocity < 0f)
        {
            // Stick to ground
            verticalVelocity = grounStick;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Compute desired displacement this frame
        Vector3 desiredDisplacement = desiredHorizontal * Time.deltaTime + Vector3.up * (verticalVelocity * Time.deltaTime);

        // If no arena reference, just move with gravity (colliders still work if tiles have colliders)
        if (arena == null)
        {
            controller.Move(desiredDisplacement);
            return;
        }

        // Try to move horizontally only if the destination cell contains a tile.
        // We'll separate horizontal and vertical movement:
        Vector3 currentPos = transform.position;
        Vector3 horizontalDisplacement = new Vector3(desiredHorizontal.x, 0f, desiredHorizontal.z) * Time.deltaTime;
        Vector3 verticalDisplacement = Vector3.up * (verticalVelocity * Time.deltaTime);

        // First, compute the target world position if we applied the horizontal move
        Vector3 horizTargetPos = currentPos + horizontalDisplacement;

        // If player is in the air (jumping or falling), allow free horizontal movement
        if (!isGrounded)
        {
            controller.Move(horizontalDisplacement + verticalDisplacement);
        }
        // Check if there is a tile under the horizTargetPos (i.e., target grid cell contains a tile)
        else if (IsPositionOnTile(horizTargetPos))
        {
            // Safe to move horizontally and then apply vertical move
            controller.Move(horizontalDisplacement + verticalDisplacement);
        }
        else
        {
            // Not safe to move full horizontal; try sliding along X then Z
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
                // Block horizontal; still apply vertical (so player will fall if no tile below)
                controller.Move(verticalDisplacement);
            }
        }
    }

    bool IsPositionOnTile(Vector3 worldPos)
    {
        if (arena == null) return true; // if no arena reference, assume allowed

        float ts = Mathf.Approximately(arena.tileSize, 0f) ? 1f : arena.tileSize;

        // Map world position to nearest tile indices.
        // If your level origin is not (0,0) you may need to subtract origin offset here.
        int gx = Mathf.RoundToInt(worldPos.x / ts);
        int gz = Mathf.RoundToInt(worldPos.z / ts);

        FloorTile t = arena.GetTile(gx, gz);
        return t != null;
    }
}