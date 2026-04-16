using UnityEngine;
using UnityEngine.UI;

public class ZoomInputHandler : MonoBehaviour
{
    [SerializeField] private Button zoomToggleButton;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private float zoomAmount = 2f;

    private bool isZoomingIn = true; // Default to Zoom In mode
    private Text buttonText;

    private void Start()
    {
        // Find camera controller if not assigned
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }

        // Get the button text component
        if (zoomToggleButton != null)
        {
            buttonText = zoomToggleButton.GetComponentInChildren<Text>();
            
            // Setup button listener
            zoomToggleButton.onClick.AddListener(OnZoomTogglePressed);
            
            // Set initial button text
            UpdateButtonText();
        }
    }

    private void OnZoomTogglePressed()
    {
        if (cameraController == null) return;

        if (isZoomingIn)
        {
            // Currently in Zoom In mode, zoom in
            cameraController.ZoomIn(zoomAmount);
        }
        else
        {
            // Currently in Zoom Out mode, zoom out
            cameraController.ZoomOut(zoomAmount);
        }

        // Toggle the mode
        isZoomingIn = !isZoomingIn;
        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        if (buttonText != null)
        {
            buttonText.text = isZoomingIn ? "Zoom In" : "Zoom Out";
        }
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (zoomToggleButton != null)
        {
            zoomToggleButton.onClick.RemoveAllListeners();
        }
    }
}
