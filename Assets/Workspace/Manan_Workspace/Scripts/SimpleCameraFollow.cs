using UnityEngine;


//handles player camera
//i abandoned the camera that zooms when you start aiming. it was just not working and was really annoying
//this one is simple and just moves camera with mouse
public class SimpleCameraFollow : MonoBehaviour
{
    //target to follow (which is the player)
    public Transform target;
    //distance away from player since it is third person
    public float distance = 5f;
    //height above player
    public float height = 2f;
    //camera sensitivity
    public float mouseSensitivity = 2f;
    //how much camera can look up or down
    //this had to be fine tuned to mimic natural head movement range
    public float minPitch = -20f;
    public float maxPitch = 60f;

    //horizontal and vertical rotation
    private float yaw;
    private float pitch;

    void LateUpdate() {
        //if player does not exist do nothing
        if (!target) return;

        //horizontal movement
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        //vertical movement
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        //makes sure camera does not go too steep of an angle
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        //rotate camera from current position
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        //offset to keep camera at the same height and distance from player
        Vector3 offset = rot * new Vector3(0, height, -distance);

        //adjust camera position with respect to player
        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}