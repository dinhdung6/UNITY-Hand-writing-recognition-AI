using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class CaptureImage : MonoBehaviour
{
    public Camera captureCamera;          // Assign the Camera with the RenderTexture here
    public RenderTexture renderTexture;   // Assign the same RenderTexture attached to the Camera here
    public Button captureButton;          // Assign the UI Button here

    private void Start()
    {
        // Add an event listener to the button to call CapturePhoto when clicked
        if (captureButton != null)
            captureButton.onClick.AddListener(CapturePhoto);
    }

    public void CapturePhoto()
    {
        // Ensure the camera has a render texture assigned
        if (renderTexture == null || captureCamera == null)
        {
            Debug.LogError("RenderTexture or Camera is not assigned.");
            return;
        }

        // Set the camera's target texture temporarily for capture
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        // Create a Texture2D with the same size as the RenderTexture
        Texture2D capturedImage = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

        // Read the RenderTexture contents into the Texture2D
        capturedImage.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        capturedImage.Apply();

        // Reset the active RenderTexture to what it was before
        RenderTexture.active = currentRT;

        // Convert the Texture2D to a PNG
        byte[] imageBytes = capturedImage.EncodeToPNG();

        // Define the custom file path
        string filePath = @"C:\Users\bong\Downloads\unity\CapturedImage.png";

        // Ensure the directory exists
        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Save the PNG to the specified file path
        File.WriteAllBytes(filePath, imageBytes);
        Debug.Log("Captured image saved to: " + filePath);

        // Cleanup: destroy the Texture2D to free memory
        Destroy(capturedImage);
    }
}
