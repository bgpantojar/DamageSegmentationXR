using UnityEngine;
using TMPro;
using MixedReality.Toolkit.UX;

public class QuadSliderControllerY : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI valueDisplayText; // TextMeshPro to display the Y position
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

        // Read the TextMeshPro with the Quad's new Y position
        valueDisplayText.text = $"Position Y: {quadTransform.position.y:F4}";

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

        // Update the Quad's Y position
        Vector3 quadPosition = quadTransform.position;
        quadPosition.y += deltaValue;
        quadTransform.position = quadPosition;

        // Update the TextMeshPro with the Quad's new Y position
        valueDisplayText.text = $"Position Y: {quadTransform.position.y:F4}";

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