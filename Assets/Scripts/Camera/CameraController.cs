using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    public float transitionTime = 1.5f;
    public float distanceMultiplier = 5f;
    public Camera targetCamera;

    Coroutine current;

    void Awake()
    {
        Instance = this;
        if (targetCamera == null) targetCamera = Camera.main;
    }

    public void MoveTo(Planet planet)
    {
        if (planet == null || targetCamera == null) return;

        float dist = Mathf.Max(1f, planet.radius) * distanceMultiplier;
        Vector3 offset = new Vector3(0f, dist * 0.4f, -dist);
        Vector3 endPos = planet.transform.position + offset;

        if (current != null) StopCoroutine(current);
        current = StartCoroutine(MoveRoutine(endPos, planet.transform.position));
    }

    public void CancelAutoMove()
    {
        if (current != null) { StopCoroutine(current); current = null; }
    }

    IEnumerator MoveRoutine(Vector3 endPos, Vector3 lookAt)
    {
        Transform t = targetCamera.transform;
        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;
        Quaternion endRot = Quaternion.LookRotation(lookAt - endPos);

        float elapsed = 0f;
        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, elapsed / transitionTime);
            t.position = Vector3.Lerp(startPos, endPos, k);
            t.rotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }
        t.position = endPos;
        t.rotation = endRot;
    }
}
