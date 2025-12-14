using UnityEngine;

public class CameraPan : MonoBehaviour
{
    public float panAmount = 20f;   // degrees left/right
    public float panSpeed = 0.5f;   // how fast it swings
    public float verticalBobSpeed = 0.5f;
    public float bobHeight = 0.2f;

    private float startY;
    private float startRotationY;

    void Start()
    {
        startY = transform.position.y;
        startRotationY = transform.eulerAngles.y;
    }

    void Update()
    {
        // Side-to-side rotation (oscillation)
        float yRotation = startRotationY + Mathf.Sin(Time.time * panSpeed) * panAmount;
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z);

        // Gentle up/down bobbing
        float newY = startY + Mathf.Sin(Time.time * verticalBobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
