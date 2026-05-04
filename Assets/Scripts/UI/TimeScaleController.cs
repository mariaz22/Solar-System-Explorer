using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeScaleController : MonoBehaviour
{
    [Header("UI References")]
    public Slider timeSlider;
    public Button pauseButton;
    public TMP_Text speedLabel;
    public TMP_Text pauseButtonText;

    bool isPaused = false;

    void Start()
    {
        timeSlider.minValue = 1f;
        timeSlider.maxValue = 100f;
        timeSlider.value = 1f;
        Time.timeScale = 1f;

        timeSlider.onValueChanged.AddListener(OnSliderChanged);
        pauseButton.onClick.AddListener(TogglePause);

        UpdateLabel(1f);
    }

    void OnSliderChanged(float value)
    {
        if (!isPaused)
        {
            Time.timeScale = value;
            Time.fixedDeltaTime = 0.02f * value;
        }
        UpdateLabel(value);
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            pauseButtonText.text = "> RESUME";
            pauseButtonText.color = new Color(0.2f, 1f, 0.4f);
        }
        else
        {
            float value = timeSlider.value;
            Time.timeScale = value;
            Time.fixedDeltaTime = 0.02f * value;
            pauseButtonText.text = "|| PAUSE";
            pauseButtonText.color = new Color(0.0f, 0.85f, 1f);
        }
        UpdateLabel(timeSlider.value);
    }

    void UpdateLabel(float scale)
    {
        int rounded = Mathf.RoundToInt(scale);
        speedLabel.text = isPaused ? $"{rounded}x PAUSED" : $"{rounded}x";
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
