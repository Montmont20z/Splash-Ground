using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The player

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 15, -10); // Camera position relative to player
    public float smoothSpeed = 5f; // How smoothly camera follows
    public bool lookAtTarget = true;

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("Camera target not set!");
            return;
        }

        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;

        // Smoothly move camera
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
        transform.position = smoothedPosition;

        // Look at player
        if (lookAtTarget)
        {
            transform.LookAt(target.position + Vector3.up * 1f); // Look slightly above player
        }
    }
}