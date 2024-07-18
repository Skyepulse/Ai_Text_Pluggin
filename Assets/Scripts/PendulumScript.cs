using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PendulumScript : MonoBehaviour
{
    public XRGrabInteractable grabInteractable;
    public Rigidbody ballBearingRigidbody;
    public float spinForce = 100f;

    private bool isGrabbed = false;

    void Start()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    void FixedUpdate()
    {
        if (isGrabbed)
        {
            // Apply torque to spin the object
            ballBearingRigidbody.AddTorque(transform.up * spinForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        ballBearingRigidbody.angularDrag = 0; // Reduce angular drag for a smooth spin
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        ballBearingRigidbody.angularDrag = 0.5f; // Adjust angular drag to slow down gradually
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }
}