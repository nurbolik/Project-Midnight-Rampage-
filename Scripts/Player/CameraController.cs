using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Target")]
    [SerializeField] public Transform _target;
    [SerializeField] private Vector3 _offset = new Vector3(0, 0, -10);

    [Header("Follow Settings")]
    [SerializeField] private float _smoothTime = 0.2f;
    [SerializeField] private float _mouseFollowIntensity = 0.1f;
    [SerializeField] private float _maxFollowDistance = 0.5f;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 1.45f;
    [SerializeField] private float _maxRotationAngle = 4f;

    [Header("Zoom Pan Settings")]
    [SerializeField] private float _zoomPanDistance = 5f;

    [Header("Shake Settings")]
    [SerializeField] private float _shakeMagnitude = 0.1f;
    [SerializeField] private float _dampingSpeed = 1f;

    [Header("Death Cam")]
    [SerializeField] private float _orbitRotationSpeed = 7f;

    private Camera _cam;
    private Vector3 _velocity;
    private Vector3 _shakeOffset;
    private Vector3 _panOffset;

    private float _currentRotation;
    private float _currentShakeTime;

    private bool _enableOrbit;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        _cam = Camera.main;

        if (_target != null)
            transform.position = _target.position + _offset;
    }

    private void Update()
    {
        if (_target == null) return;

        HandlePan();
        FollowTarget();

        if (_enableOrbit)
            Orbit();
        else
            Rotate();

        ApplyShake();
    }

    private void FollowTarget()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorld.z = _target.position.z; 
        Vector3 direction = (mouseWorld - _target.position).normalized;
        Vector3 mouseOffset = direction * _mouseFollowIntensity;
        mouseOffset = Vector3.ClampMagnitude(mouseOffset, _maxFollowDistance);

        Vector3 desiredPosition = _target.position + _offset + mouseOffset + _panOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition + _shakeOffset,
            ref _velocity,
            _smoothTime
        );
    }

    private void Rotate()
    {
        float screenCenterX = Screen.width * 0.5f;
        float mouseX = Mouse.current.position.ReadValue().x;
        float mouseDelta = (mouseX - screenCenterX) / screenCenterX;

        float targetRotation = mouseDelta * _maxRotationAngle;
        _currentRotation = Mathf.Lerp(_currentRotation, targetRotation, Time.deltaTime * _rotationSpeed);

        transform.rotation = Quaternion.Euler(0f, 0f, _currentRotation);
    }

    private void Orbit()
    {
        transform.RotateAround(
            _target.position,
            Vector3.forward,
            _orbitRotationSpeed * Time.deltaTime
        );
    }

    private void HandlePan()
    {
        bool isZoomHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

        if (!isZoomHeld)
        {
            _panOffset = Vector3.zero;
            return;
        }

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorld.z = _target.position.z;

        Vector3 direction = (mouseWorld - _target.position).normalized;
        _panOffset = direction * _zoomPanDistance;
    }

    private void ApplyShake()
    {
        if (_currentShakeTime <= 0f)
        {
            _shakeOffset = Vector3.zero;
            return;
        }

        _shakeOffset = Random.insideUnitSphere * _shakeMagnitude;
        _currentShakeTime -= Time.deltaTime * _dampingSpeed;
    }

    public void TriggerShake(float duration, float magnitude)
    {
        _currentShakeTime = duration;
        _shakeMagnitude = magnitude;
    }

    public void SetOrbitRotation(bool enabled)
    {
        _enableOrbit = enabled;
    }
}