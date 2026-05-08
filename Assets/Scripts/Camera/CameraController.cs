using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    public float transitionTime = 1.5f;
    public float distanceMultiplier = 5f;
    public Camera targetCamera;

    Coroutine current;
    bool following;

    void Awake()
    {
        Instance = this;
        if (targetCamera == null) targetCamera = Camera.main;
    }

    public void MoveTo(Planet planet)
    {
        if (planet == null || targetCamera == null || following) return;

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

    public void StartFollowing(Transform followTarget)
    {
        following = true;
        var fly = targetCamera?.GetComponent<FreeFlyCamera>();
        if (fly != null) fly.enabled = false;
        if (current != null) StopCoroutine(current);

        // Snap camera immediately behind probe (horizontal only, so Q/E don't invert camera)
        if (targetCamera != null && followTarget != null)
        {
            Vector3 hFwd = HorizontalForward(followTarget.forward);
            Vector3 snap = followTarget.position - hFwd * 8f + Vector3.up * 3f;
            targetCamera.transform.position = snap;
            targetCamera.transform.LookAt(followTarget.position);
        }

        current = StartCoroutine(FollowRoutine(followTarget));
    }

    public void StopFollowing()
    {
        following = false;
        if (current != null) { StopCoroutine(current); current = null; }
        var fly = targetCamera?.GetComponent<FreeFlyCamera>();
        if (fly != null) fly.enabled = true;
    }

    IEnumerator FollowRoutine(Transform followTarget)
    {
        while (followTarget != null)
        {
            Transform t = targetCamera.transform;
            // Always use horizontal forward so camera never goes below probe
            Vector3 hFwd = HorizontalForward(followTarget.forward);
            Vector3 behindProbe = followTarget.position - hFwd * 8f + Vector3.up * 3f;
            t.position = Vector3.Lerp(t.position, behindProbe, Time.deltaTime * 4f);
            Vector3 toProbe = followTarget.position - t.position;
            if (toProbe.sqrMagnitude > 0.001f)
                t.rotation = Quaternion.Slerp(t.rotation,
                    Quaternion.LookRotation(toProbe), Time.deltaTime * 4f);
            yield return null;
        }
        StopFollowing();
    }

    static Vector3 HorizontalForward(Vector3 forward)
    {
        Vector3 h = new Vector3(forward.x, 0f, forward.z);
        return h.sqrMagnitude > 0.001f ? h.normalized : Vector3.forward;
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
            
            // Pulse scan overlay during transition
            float scanK = Mathf.Sin(k * Mathf.PI);
            HUDController.Instance?.SetScanAlpha(scanK);

            t.position = Vector3.Lerp(startPos, endPos, k);
t.rotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }
        t.position = endPos;
        t.rotation = endRot;
        HUDController.Instance?.SetScanAlpha(0f);
        }
        }
