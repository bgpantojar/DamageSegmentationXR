using UnityEngine;
using TMPro;
using MixedReality.Toolkit.UX;

public class QuadSliderControllerScale : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI valueDisplayText; // TextMeshPro to display the scale
    [SerializeField] private Transform quadTransform; // Reference to the Quad
    [SerializeField] private Slider slider; // Reference to the MRTK Slider

    private float previousSliderValue = 0f; // To track changes in the slider value

    private void Start()
    {
        // Ensure references are assigned
        if (slider == null || valueDisplayText == null || quadTransform == null)
        {
            Debug.LogError("Slider, TextMeshPro, or Quad is not assigned!");
            return;
        }

        // Display the initial scale value
        valueDisplayText.text = $"Scale X: {quadTransform.localScale.x:F4}   Scale Y: {quadTransform.localScale.y:F4}";

        // Initialize the slider value and subscribe to slider events
        previousSliderValue = slider.Value;
        slider.OnValueUpdated.AddListener(OnSliderValueUpdated);
    }

    private void OnSliderValueUpdated(SliderEventData eventData)
    {
        // Get the new slider value from the event data
        float newSliderValue = eventData.NewValue;

        // Calculate the delta (difference between the new and previous slider values)
        float deltaValue = newSliderValue - previousSliderValue;

        // Update the Quad's scale
        Vector3 quadScale = quadTransform.localScale;
        quadScale.x += deltaValue;
        quadScale.y += (deltaValue * 9.0f / 16.0f);
        quadTransform.localScale = quadScale;

        // Update the TextMeshPro with the Quad's new scale
        valueDisplayText.text = $"Scale X: {quadTransform.localScale.x:F4}   Scale Y: {quadTransform.localScale.y:F4}";

        // Update the previous slider value for the next calculation
        previousSliderValue = newSliderValue;
    }

    private void OnDestroy()
    {
        // Unsubscribe from the slider event to prevent memory leaks
        if (slider != null)
        {
            slider.OnValueUpdated.RemoveListener(OnSliderValueUpdated);
        }
    }
}
