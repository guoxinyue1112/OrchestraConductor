using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleFPSController : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float verticalClamp = 80f;
    [SerializeField] private float initialPitch = 0f;

    private float _pitch;
    private bool _inputEnabled;

    private void Start()
    {
        _pitch = initialPitch;

        if (cameraPivot != null)
        {
            cameraPivot.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }

        SetInputEnabled(false);
    }

    private void Update()
    {
        if (!_inputEnabled)
        {
            return;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            UpdateLook();
        }

        UpdateMovement();
    }

    public void SetInputEnabled(bool inputEnabled)
    {
        _inputEnabled = inputEnabled;

        if (_inputEnabled)
        {
            LockCursor();
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateMovement()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        float speed = keyboard.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            horizontal += 1f;
        }

        if (keyboard.sKey.isPressed)
        {
            vertical -= 1f;
        }

        if (keyboard.wKey.isPressed)
        {
            vertical += 1f;
        }

        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 worldMovement = transform.TransformDirection(input) * (speed * Time.deltaTime);
        transform.position += worldMovement;
    }

    private void UpdateLook()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        float mouseX = delta.x * lookSensitivity * Time.deltaTime;
        float mouseY = delta.y * lookSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX, Space.World);

        _pitch = Mathf.Clamp(_pitch - mouseY, -verticalClamp, verticalClamp);

        if (cameraPivot != null)
        {
            cameraPivot.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
