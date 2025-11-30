using UnityEngine;

public class FPSCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform playerBody; // playe object 
    public float mouseSensitivity = 100f;
    public float maxLookUpAngle = 90f;
    public float maxLookDownAngle = 60f;

    private float xRotation = 0f; // track vertical rotation

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // lock cursor to center of screen
        Cursor.visible = false; // hide cursor
    }

    void Update()
    {
        HandleMouseLook();

        // Esc to unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None; // unlock cursor
            Cursor.visible = true; // show cursor
        }

        // Click to lock cursor again
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked; // lock cursor to center of screen
            Cursor.visible = false; // hide cursor
        }

    }

    void HandleMouseLook()
    {
        // Get mouse movement input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;


        // Adjust vertical rotation and clamp it
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookUpAngle, maxLookDownAngle);
        // Apply vertical rotation to camera
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        // Rotate player body horizontally
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}
