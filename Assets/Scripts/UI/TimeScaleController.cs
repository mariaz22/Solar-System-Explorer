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

    private bool isPaused = false;

    void Start()
    {
        timeSlider.minValue = 1f;
        timeSlider.maxValue = 1000f;
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
            UpdateLabel(value);
        }
        else
        {
            speedLabel.text = $"Speed: {Mathf.RoundToInt(value)}x (paused)";
        }
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            pauseButtonText.text = "Resume";
            speedLabel.text = $"Speed: {Mathf.RoundToInt(timeSlider.value)}x (paused)";
        }
        else
        {
            float value = timeSlider.value;
            Time.timeScale = value;
            Time.fixedDeltaTime = 0.02f * value;
            pauseButtonText.text = "Pause";
            UpdateLabel(value);
        }
    }

    void UpdateLabel(float scale)
    {
        speedLabel.text = $"Speed: {Mathf.RoundToInt(scale)}x";
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}