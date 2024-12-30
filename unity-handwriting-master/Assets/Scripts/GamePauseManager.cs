using UnityEngine;
using UnityEngine.UI;

public class GamePauseManager : MonoBehaviour
{
    public Button startButton; // Button to start the game
    public Button pauseButton; // Button to pause the game
    public Button continueButton; // Button to continue the game
    public static GamePauseManager Instance; // Singleton instance

    public delegate void GameStarted(); // Delegate for game started event
    public event GameStarted OnGameStarted; // Event when game starts

    private void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this object in the scene
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate
        }
    }

    private void Start()
    {
        startButton.onClick.AddListener(StartGame);
        pauseButton.onClick.AddListener(PauseGame);
        continueButton.onClick.AddListener(ContinueGame);
        Time.timeScale = 0; // Pause the game at the start
        continueButton.gameObject.SetActive(false); // Hide continue button at the start
    }

    void StartGame()
    {
        Time.timeScale = 1; // Resume the game
        OnGameStarted?.Invoke(); // Invoke the start event
        startButton.gameObject.SetActive(false); // Hide the start button
        continueButton.gameObject.SetActive(false); // Hide continue button
    }

    public void PauseGame()
    {
        Time.timeScale = 0; // Pause the game
        continueButton.gameObject.SetActive(true); // Show continue button
    }

    public void ContinueGame()
    {
        Time.timeScale = 1; // Resume the game
        continueButton.gameObject.SetActive(false); // Hide continue button
    }
}
