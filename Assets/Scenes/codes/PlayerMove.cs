using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 5f;     // Speed of movement
    public float rotationSpeed = 100f; // Speed of rotation

    void Update()
    {
        // Movement input
        float moveHorizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrow
        float moveVertical = Input.GetAxis("Vertical");     // W/S or Up/Down Arrow

        // Movement vector
        Vector3 movement = new Vector3(moveHorizontal, 0f, moveVertical).normalized;

        // Move the character
        if (movement.magnitude > 0f) // Ensures no movement when keys are not pressed
        {
            transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
        }

        // Rotation input
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

}
