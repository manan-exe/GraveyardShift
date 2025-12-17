using UnityEngine;

//this script helps the visuals of the main menu scene
//it pans the camera over the map
public class CameraPan : MonoBehaviour
{
    //amount of rotation camera moves left and right
    //specifically i think the unit is degrees
    public float panAmount = 20f;
    //speed of the camera panning
    public float panSpeed = 0.5f;
    //controls up and down movement speed
    public float verticalBobSpeed = 0.5f;
    //controls how much camera moves up and down
    public float bobHeight = 0.2f;

    //stores the cameras original y position and where it was facing
    private float startY;
    private float startRotationY;

    //this method triggers when the game object exists
    void Start()
    {
        //set initial position and rotation
        startY = transform.position.y;
        startRotationY = transform.eulerAngles.y;
    }

    //update camera pan movement frame by frame
    void Update()
    {
        //this was a really strange equation
        //it somehow uses a sine function to control smooth movement
        //calculates left and right rotation
        float yRotation = startRotationY + Mathf.Sin(Time.time * panSpeed) * panAmount;

        //applies new rotation value
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z);

        //also uses a sine function in the same way as the equation above
        //calculates new camera bob position
        float newY = startY + Mathf.Sin(Time.time * verticalBobSpeed) * bobHeight;
        //apply new position.
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
