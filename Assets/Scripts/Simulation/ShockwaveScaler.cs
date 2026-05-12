using UnityEngine;

public class ShockwaveScaler : MonoBehaviour
{
    float    _startR, _endR, _duration, _elapsed;
    Material _mat;
    Color    _baseCol;

    public void Init(float startR, float endR, float duration, Material mat)
    {
        _startR   = startR;
        _endR     = endR;
        _duration = duration;
        _mat      = mat;
        _baseCol  = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        float r = Mathf.Lerp(_startR, _endR, t);
        transform.localScale = Vector3.one * r * 2f;

        if (_mat != null && _mat.HasProperty("_BaseColor"))
        {
            Color c = _baseCol;
            c.a = Mathf.Lerp(_baseCol.a, 0f, t);
            _mat.SetColor("_BaseColor", c);
        }

        if (_elapsed >= _duration)
            Destroy(gameObject);
    }
}
