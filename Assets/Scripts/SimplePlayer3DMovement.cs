using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePlayer3DMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    private Transform cameraTransform;
    private CharacterController characterController;

    public float jumpHeight = 3f;
    public float gravity = -9.81f;

    [SerializeField]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;

    public float mouseSensitivity = 100f;
    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        cameraTransform = gameObject.GetComponentInChildren<Camera>().transform;
        characterController = gameObject.GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Note: cameraTransform.forward and right will include vertical (y) movement,
        // which can be removed if you want strictly horizontal movement.
        Vector3 forwardMovement = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized * vertical;
        Vector3 rightMovement = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized * horizontal;

        Vector3 movement = (forwardMovement + rightMovement).normalized * speed;
        characterController.Move(movement * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = jumpHeight;
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void LateUpdate()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;


        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Clamp the vertical rotation between -90 and 90 degrees

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}
