using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    public float openAngle = 90f;

	public Rigidbody2D doorRigidbody;
    private float initialRotation;

    void Start()
    {
       
        initialRotation = doorRigidbody.rotation;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            doorRigidbody.angularVelocity = 0f;
            doorRigidbody.rotation = initialRotation + openAngle;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            doorRigidbody.angularVelocity = 0f;
            doorRigidbody.rotation = initialRotation;
        }
    }
}