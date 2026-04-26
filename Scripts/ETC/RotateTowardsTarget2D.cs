using UnityEngine;

public class RotateTowardsTarget2D : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;       
    public float rotationSpeed = 5f;

    void Update()
    {
        if (target == null) return;
        Vector2 direction = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
