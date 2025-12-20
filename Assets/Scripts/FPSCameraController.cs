using UnityEngine;

public class FPSCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's root transform (usually the object with the CharacterController).")]
    public Transform playerBody;

    [Header("Mouse / Look")]
    public float mouseSensitivity = 100f;
    [Tooltip("Maximum degrees you can look up (positive values).")]
    public float maxLookUpAngle = 90f;
    [Tooltip("Maximum degrees you can look down (positive values).")]
    public float maxLookDownAngle = 60f;

    [Header("First Person")]
    [Tooltip("Camera offset from player world position when in first-person (e.g. head height).")]
    public Vector3 firstPersonOffset = new Vector3(0f, 1.6f, 1f);

    [Header("Third Person")]
    public bool startInThirdPerson = false;
    [Tooltip("Key to toggle third / first person")]
    public KeyCode toggleKey = KeyCode.V;
    [Tooltip("Camera offset applied to player position when computing third-person origin (usually head height).")]
    public Vector3 thirdPersonOffset = new Vector3(0f, 1.5f, 0f);
    [Tooltip("Default distance behind the player for third-person camera.")]
    public float thirdPersonDistance = 3f;
    [Tooltip("Minimum camera distance when collision occurs.")]
    public float thirdPersonMinDistance = 0.5f;
    [Tooltip("Smooth time for camera position smoothing.")]
    public float thirdPersonSmoothTime = 0.08f;
    [Tooltip("Layers to consider for camera obstruction (use LayerMask in inspector).")]
    public LayerMask collisionMask = -1; // default: everything

    // internal rotation state
    private float xRotation = 0f;   // vertical (pitch)
    private float yaw = 0f;         // horizontal (used for 3rd-person orbit)
    private bool isThirdPerson;

    // smoothing state
    private Vector3 currentVelocity = Vector3.zero;
    private float currentDistance;

    void Start()
    {
        if (playerBody == null)
        {
            Debug.LogError("FPSCameraController: playerBody is not assigned.");
            enabled = false;
            return;
        }

        // Initialize rotation values from player's orientation
        yaw = playerBody.eulerAngles.y;
        xRotation = 0f;

        // Start mode
        isThirdPerson = startInThirdPerson;
        currentDistance = thirdPersonDistance;

        // If collisionMask left untouched (0), use default physics layers
        if (collisionMask == 0) collisionMask = Physics.DefaultRaycastLayers;

        // Lock cursor by default
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleToggleInput();
        HandleMouseLook();
        HandleCursorToggle();
    }

    void HandleToggleInput()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isThirdPerson = !isThirdPerson;
        }
    }

    void HandleCursorToggle()
    {
        // Esc to unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click to lock cursor again
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void HandleMouseLook()
    {
        // Read mouse delta
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        if (!isThirdPerson)
        {
            // FIRST PERSON: rotate player yaw, rotate camera pitch
            // Horizontal: rotate the player body
            playerBody.Rotate(Vector3.up * mouseX);

            // Vertical: rotate camera pitch and clamp
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -maxLookUpAngle, maxLookDownAngle);

            // Apply rotation and position
            transform.rotation = Quaternion.Euler(xRotation, playerBody.eulerAngles.y, 0f);
            transform.position = playerBody.position + firstPersonOffset;
        }
        else
        {
            // THIRD PERSON: camera orbits the player independently
            yaw += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -maxLookUpAngle, maxLookDownAngle);

            // Compute camera rotation (pitch = xRotation, yaw = yaw)
            Quaternion camRotation = Quaternion.Euler(xRotation, yaw, 0f);

            // Origin point (the pivot the camera orbits around)
            Vector3 pivot = playerBody.position + thirdPersonOffset;

            // Desired camera position before collision handling
            Vector3 desiredPos = pivot - (camRotation * Vector3.forward) * thirdPersonDistance;

            // Cast a ray from pivot toward desiredPos to detect obstacles
            Vector3 dir = (desiredPos - pivot).normalized;
            float maxDist = thirdPersonDistance;
            RaycastHit hit;
            float targetDistance = thirdPersonDistance;

            if (Physics.SphereCast(pivot, 0.25f, dir, out hit, maxDist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                // Move camera in front of obstacle, with a small offset
                targetDistance = Mathf.Clamp(hit.distance - 0.1f, thirdPersonMinDistance, thirdPersonDistance);
            }

            // Smoothly adjust currentDistance
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref currentVelocity.z, thirdPersonSmoothTime);

            Vector3 finalPos = pivot - (camRotation * Vector3.forward) * currentDistance;

            // Smooth position
            transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref currentVelocity, thirdPersonSmoothTime);

            // Smooth rotation (small lerp for stability)
            transform.rotation = Quaternion.Slerp(transform.rotation, camRotation, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }
    }
}
