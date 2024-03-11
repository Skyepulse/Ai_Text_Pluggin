using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hearing : MonoBehaviour
{
    [SerializeField]
    private float hearingDistance = 10f;
    public float HearingDistance { get { return hearingDistance; } set { hearingDistance = value; } }

    private SphereCollider sphereCollider;
    private bool canHearPlayer = false;
    public bool CanHearPlayer { get { return canHearPlayer; } }

    void Start()
    {
        sphereCollider = gameObject.GetComponent<SphereCollider>();
        sphereCollider.radius = hearingDistance;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            //Debug.Log("I can hear the player");
            canHearPlayer = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            //Debug.Log("I can't hear the player anymore");
            canHearPlayer = false;
        }
    }
}
