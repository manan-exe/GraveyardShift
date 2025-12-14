using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target;     // Player
    public float distance = 5f;
    public float height = 2f;
    public float mouseSensitivity = 2f;
    public float minPitch = -20f;
    public float maxPitch = 60f;

    private float yaw;
    private float pitch;

    void LateUpdate() {
        if (!target) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rot * new Vector3(0, height, -distance);

        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}