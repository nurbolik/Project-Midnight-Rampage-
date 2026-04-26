using UnityEngine;
using UnityEngine.InputSystem; 


public class Crosshair : MonoBehaviour
{
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        if (_cam == null || Mouse.current == null)
            return;

        
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(mouseScreenPos);

   
        mouseWorld.z = 0f;

        transform.position = mouseWorld;
    }
}