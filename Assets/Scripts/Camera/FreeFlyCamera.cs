using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class FreeFlyCamera : MonoBehaviour
{
    public float moveSpeed = 40f;
    public float fastMultiplier = 4f;
    public float lookSensitivity = 0.15f;
    public float panSensitivity = 0.08f;
    public float zoomSensitivity = 2f;
    public float referenceDistance = 80f;
    public float minSpeedMultiplier = 0.25f;
    public float maxSpeedMultiplier = 5f;
    public float maxDistanceFromCenter = 320f;
    public Transform center;
    public PlanetSelectionUI selectionUI;

    float yaw;
    float pitch;

    void Start()
    {
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;

        if (center == null)
        {
            var sun = GameObject.Find("Sun");
            if (sun != null) center = sun.transform;
        }

        if (selectionUI == null)
            selectionUI = Object.FindAnyObjectByType<PlanetSelectionUI>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (kb.fKey.wasPressedThisFrame)
            FocusSelectedPlanet();

        float speedFactor = GetDistanceSpeedFactor();
        bool pointerOverUI = IsPointerOverUI();

        if (!mouse.rightButton.isPressed)
            SyncRotationFromTransform();

        if (mouse.rightButton.isPressed)
        {
            Vector2 d = mouse.delta.ReadValue();
            if (!pointerOverUI || d.sqrMagnitude > 0f)
            {
                CancelAutoMove();
                yaw += d.x * lookSensitivity;
                pitch -= d.y * lookSensitivity;
                pitch = Mathf.Clamp(pitch, -89f, 89f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
        }

        if (mouse.middleButton.isPressed)
        {
            Vector2 d = mouse.delta.ReadValue();
            if (d.sqrMagnitude > 0.01f)
            {
                CancelAutoMove();
                Vector3 pan = (-transform.right * d.x - transform.up * d.y) * panSensitivity * speedFactor;
                transform.position += pan;
            }
        }

        Vector2 scroll = mouse.scroll.ReadValue();
        if (!pointerOverUI && Mathf.Abs(scroll.y) > 0.01f)
        {
            CancelAutoMove();
            transform.position += transform.forward * scroll.y * zoomSensitivity * speedFactor * Time.deltaTime;
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
            CancelAutoMove();

            float speed = moveSpeed * speedFactor * (kb.leftShiftKey.isPressed ? fastMultiplier : 1f);
            Vector3 move = transform.right * input.x + transform.up * input.y + transform.forward * input.z;
            transform.position += move.normalized * speed * Time.deltaTime;
        }
    }

    void LateUpdate()
    {
        Vector3 centerPosition = GetCenterPosition();
        Vector3 offset = transform.position - centerPosition;
        if (offset.sqrMagnitude <= maxDistanceFromCenter * maxDistanceFromCenter) return;

        transform.position = centerPosition + offset.normalized * maxDistanceFromCenter;
    }

    float GetDistanceSpeedFactor()
    {
        float distance = Vector3.Distance(transform.position, GetCenterPosition());
        return Mathf.Clamp(distance / referenceDistance, minSpeedMultiplier, maxSpeedMultiplier);
    }

    Vector3 GetCenterPosition()
    {
        return center != null ? center.position : Vector3.zero;
    }

    void FocusSelectedPlanet()
    {
        if (selectionUI == null)
            selectionUI = Object.FindAnyObjectByType<PlanetSelectionUI>();

        if (selectionUI == null || selectionUI.SelectedPlanet == null) return;
        if (CameraController.Instance != null) CameraController.Instance.MoveTo(selectionUI.SelectedPlanet);
    }

    void SyncRotationFromTransform()
    {
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x > 180f ? e.x - 360f : e.x;
    }

    void CancelAutoMove()
    {
        if (CameraController.Instance != null) CameraController.Instance.CancelAutoMove();
    }

    static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
