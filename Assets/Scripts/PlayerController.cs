using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    //public float rotationSpeed = 720f; // Degrees per second
    public float sprintSpeed = 8f;

    [Header("References")]
    private CharacterController controller;
    //private Vector3 moveDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("Character Controller is missing! Please add one.");
        }
    }

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        // Get input from WASD or Arrow keys
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down arrows

        // Calculate movement direction (relative to world space)
        //Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        //if (direction.magnitude >= 0.1f)
        //{
        // Move the player
        //moveDirection = direction * moveSpeed;
        //controller.Move(moveDirection * Time.deltaTime);

        // Rotate player to face movement direction
        //Quaternion targetRotation = Quaternion.LookRotation(direction);
        //transform.rotation = Quaternion.RotateTowards(
        //    transform.rotation,
        //    targetRotation,
        //    rotationSpeed * Time.deltaTime
        //);
        //}
        // Move relative to player orientation
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;

        // Check for sprinting
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        // Move the player
        controller.Move(moveDirection.normalized * currentSpeed * Time.deltaTime);
    }

    // Visualize player in Scene view
    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.blue;
    //    Gizmos.DrawWireSphere(transform.position, 0.5f);
    //}
}