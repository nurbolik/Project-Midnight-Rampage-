using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRotation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _mainCamera;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationThreshold = 0.2f; // Minimum distance to start rotation
    [SerializeField] private float _rotationSpeedNear = 10f;   // Speed when mouse is near
    [SerializeField] private float _rotationSpeedFar = 25f;    // Speed when mouse is far
    [SerializeField] private float _rotationSmoothness = 5f;   // Smooth factor when mouse is very close

    private void Update()
    {
        RotateTowardMouse();
    }

    private void RotateTowardMouse()
    {
        if (_mainCamera == null || Mouse.current == null) return;

        
        Vector3 mousePosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePosition.z = transform.position.z; 

        Vector2 direction = mousePosition - transform.position;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float rotationFactor;

        if (direction.magnitude > _rotationThreshold)
        {
            rotationFactor = Mathf.Lerp(_rotationSpeedNear, _rotationSpeedFar, direction.magnitude);
        }
        else
        {
            rotationFactor = _rotationSmoothness;
        }

        float angle = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, rotationFactor * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}