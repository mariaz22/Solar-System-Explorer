using UnityEngine;
using UnityEngine.InputSystem;

public class FreeFlyCamera : MonoBehaviour
{
    public float moveSpeed = 40f;
    public float fastMultiplier = 4f;
    public float lookSensitivity = 0.15f;

    float yaw;
    float pitch;

    void Start()
    {
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (mouse.rightButton.isPressed)
        {
            Vector2 d = mouse.delta.ReadValue();
            yaw += d.x * lookSensitivity;
            pitch -= d.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        Vector3 input = Vector3.zero;
        if (kb.wKey.isPressed) input.z += 1f;
        if (kb.sKey.isPressed) input.z -= 1f;
        if (kb.dKey.isPressed) input.x += 1f;
        if (kb.aKey.isPressed) input.x -= 1f;
        if (kb.eKey.isPressed) input.y += 1f;
        if (kb.qKey.isPressed) input.y -= 1f;

        if (input.sqrMagnitude > 0.01f)
        {
            if (CameraController.Instance != null) CameraController.Instance.CancelAutoMove();

            float speed = moveSpeed * (kb.leftShiftKey.isPressed ? fastMultiplier : 1f);
            Vector3 move = transform.right * input.x + transform.up * input.y + transform.forward * input.z;
            transform.position += move.normalized * speed * Time.deltaTime;
        }
    }
}
