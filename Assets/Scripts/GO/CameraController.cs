using Unity.Mathematics;
using UnityEngine;
using Input = UnityEngine.Input;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    float _cameraSpeed = 1f;
    [SerializeField]
    float _panSpeed = 1f;
    [SerializeField]
    float _zoomSpeed = 1f;

    private void LateUpdate()
    {
        Movement();
        Zoom();
        RotateCamera();
    }

    private void RotateCamera()
    {
        if (Input.GetMouseButton(1))
        {
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            Quaternion rotationX = Quaternion.Euler(-mouseDelta.y, 0f, 0f);
            Quaternion rotationY = Quaternion.Euler(0f, mouseDelta.x, 0f);
            
            transform.rotation = rotationY * transform.rotation * rotationX;
        }
    }

    void Zoom()
    {
        var scrollDelta = Input.mouseScrollDelta.y;

        if (scrollDelta == 0) return;

        Vector3 newPosition = (scrollDelta * Time.deltaTime * transform.forward) * _zoomSpeed;
        transform.position += newPosition;

        if (transform.position.y < 3)
            transform.position = new (transform.position.x, 3, transform.position.z);
    }

    void Movement()
    {
        var input = new float2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        Vector3 moveDirection = (transform.forward * input.y) + (transform.right * input.x);
        transform.position += _cameraSpeed * Time.deltaTime * moveDirection;

        if (Input.GetKey(KeyCode.E))
        {
            transform.position += _cameraSpeed * Time.deltaTime * transform.up;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            transform.position -= _cameraSpeed * Time.deltaTime * transform.up;
        }
    }
}

/// <summary>
/// Ugly way of sharing data between ECS and GO.
/// Yes, this is a terrible way of doing it
/// </summary>
public static class ECSWorldInterface
{
    public static float2 position;
}