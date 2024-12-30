using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PopupManager : MonoBehaviour
{
    public GameObject popupPanel; // Assign your PopupPanel here
    public TextMeshProUGUI popupText; // Assign your PopupText here

    // Dictionary to hold the popup messages for each scene
    private Dictionary<string, List<string>> scenePopupMessages = new Dictionary<string, List<string>>()
    {
        { "nhac1", new List<string> { "xinh", "chu" } }, // Popup messages for scene 1
        { "nhac2", new List<string> { "xanh", "sau" } } // Popup messages for scene 2
    };

    // The time to wait before showing each message (in seconds)
    private Dictionary<string, List<float>> scenePopupTimings = new Dictionary<string, List<float>>()
    {
        { "nhac1", new List<float> { 15f, 30f } }, // Timings for scene 1
        { "nhac2", new List<float> { 30f, 40f } } // Timings for scene 2
    };

    private void Start()
    {
        // Initially hide the popup
        popupPanel.SetActive(false);

        // Start the coroutine to show popups
        StartCoroutine(ShowPopups());
    }

    private IEnumerator ShowPopups()
    {
        string currentSceneName = SceneManager.GetActiveScene().name; // Get the current scene name
        if (scenePopupTimings.ContainsKey(currentSceneName))
        {
            List<float> timings = scenePopupTimings[currentSceneName];
            List<string> messages = scenePopupMessages[currentSceneName];

            // Loop through each message and timing
            for (int i = 0; i < messages.Count; i++)
            {
                yield return new WaitForSeconds(timings[i]); // Wait for the specified time
                ShowPopup(messages[i]); // Show the popup message
            }
        }
    }

    public void ShowPopup(string message)
    {
        popupText.text = message; // Set the message
        popupPanel.SetActive(true); // Show the popup
    }

    public void ClosePopup()
    {
        popupPanel.SetActive(false); // Hide the popup
    }
}
