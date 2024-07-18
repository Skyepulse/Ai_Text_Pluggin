using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Sword : MonoBehaviour
{
    public XRGrabInteractable grabInteractable;
    public Transform pivotPoint;
    public HingeJoint hj;
    public Rigidbody rb;
    public XRDirectInteractor rHand;
    public XRDirectInteractor lHand;

    public bool isLeftHanded = false;

    private bool isGrabbed = false;

    [Range(0f, 1f)]
    public float torqueBoost = 0f;

    public Transform sphere;

    private bool shouldAddTorque = false;
    private float torquefrequency;
    public float minFrequency = 4f;
    private float maxFrequency = 1f;
    private float timeToMaxFrequency = 10f; // 5 seconds
    private float timer = 0f;

    private float previousAngle = 0f;
    private float totalRotation = 0f;
    private int rotations = 0;
    private bool isClockwise = true;

    void Start()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }

        rb.maxAngularVelocity = 30f;
    }

    void FixedUpdate()
    {
        if (isGrabbed)
        {
            float currentAngle = transform.eulerAngles.z;
            float deltaAngle = Mathf.DeltaAngle(previousAngle, currentAngle);
            totalRotation += deltaAngle;
            previousAngle = currentAngle;

            // Check for full rotation in either direction
            if (Mathf.Abs(totalRotation) >= 360f && !shouldAddTorque)
            {
                rotations++;
                isClockwise = totalRotation > 0;
                totalRotation = 0f;

                shouldAddTorque = true;
                torquefrequency = minFrequency;
                timer = torquefrequency;

                ApplyBoost();
            }

            if (shouldAddTorque)
            {
                if (timer <= 0)
                {
                    ApplyBoost();
                    torquefrequency = Mathf.Lerp(minFrequency, maxFrequency, timeToMaxFrequency / 10f);
                    timer = torquefrequency;
                }
                else
                {
                    timer -= Time.deltaTime;
                    timeToMaxFrequency -= Time.deltaTime;
                }
            }
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        //We check if we grab with left or right hand.
        if (lHand.IsSelecting(grabInteractable))
        {
            
        }
        isGrabbed = true;
        previousAngle = transform.eulerAngles.z;
        totalRotation = 0f;
        rotations = 0;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (lHand.IsSelecting(grabInteractable))
        {
            
        }
        isGrabbed = false;
        timer = 0f;
        timeToMaxFrequency = 10f;
        torquefrequency = minFrequency;
        shouldAddTorque = false;
        totalRotation = 0f;
        rotations = 0;
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    void ApplyBoost()
    {
        Vector3 sphereZVector = isLeftHanded ? -sphere.forward: sphere.forward;
        float direction = isClockwise ? 1f : -1f;
        rb.AddTorque(sphereZVector * torqueBoost * direction, ForceMode.Impulse);
    }
}
