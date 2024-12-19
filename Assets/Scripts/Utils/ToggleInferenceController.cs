using UnityEngine;
using MixedReality.Toolkit.UX;

public class ToggleInferenceController : MonoBehaviour
{
    [SerializeField]
    private GameManager gameManager; // Reference to the GameManager script

    [SerializeField]
    private PressableButton pressableButton; // Reference to the MRTK Pressable Button

    private bool isToggled = false; // Internal state to manage the toggle behavior

    private void Start()
    {
        // Ensure references are assigned
        if (gameManager == null)
        {
            Debug.LogError("GameManager is not assigned!");
            return;
        }

        if (pressableButton == null)
        {
            Debug.LogError("PressableButton is not assigned!");
            return;
        }

        // Subscribe to the OnClicked event
        pressableButton.OnClicked.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        // Toggle the state
        isToggled = !isToggled;

        // Update the enableInference variable in the GameManager
        gameManager.ToggleInference(isToggled);

        //Debug.Log($"enableInference toggled: {isToggled}");
    }

    private void OnDestroy()
    {
        // Unsubscribe from the OnClicked event to prevent memory leaks
        if (pressableButton != null)
        {
            pressableButton.OnClicked.RemoveListener(OnButtonClicked);
        }
    }
}