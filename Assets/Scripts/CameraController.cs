using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;   // drag your Player here
    public float distance = 5f;
    public float mouseSensitivity = 2f;
    public float height = 2f;

    private float yaw;
    private float pitch;
    private float minPitch = -20f;
    private float maxPitch = 60f;

    void LateUpdate() {
        if (target == null) return;

        // Mouse look
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Position camera around the player (like Fortnite)
        Vector3 offset = new Vector3(0, height, -distance);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        transform.position = target.position + rotation * offset;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
