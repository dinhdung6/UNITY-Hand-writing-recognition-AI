using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Struct containing info relevant to capture camera placement and orientation for one written character
/// </summary>
public struct WritingAreaInfo
{
    public float size; // size of area encompassing lines to predict
    public Vector3 center; // center of collection of lines to predict
    public Vector3 normal; // the direction the writing is facing
    public Vector3 upNormal; // up normal, usually as applicable to writing surface
}

/// <summary>
/// Handwriting predictor: captures handwriting image from relevant surface and feeds it to the prediction model
/// as a RenderTexture. Extends ML-Agents Agent.
/// </summary>
public class HandwritingAgent : Agent
{
    private readonly char[] allChars =
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd',
        'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r',
        's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F',
        'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
        'U', 'V', 'W', 'X', 'Y', 'Z'
    };

    [Tooltip("Handwriting level configuration")]
    public HandwritingLevelConfig levelConfig;

    [Tooltip("Layer mask for prediction camera's culling mask")]
    public LayerMask relevantLayers;

    [Tooltip("Background color of prediction camera")]
    public Color cameraBackgroundColor;

    [Tooltip("String of characters passed so far. Exposed for flexibility")]
    public string parsedText;

    [Tooltip("Text 3D component used to display parsed text in game")]
    public TextMesh textMesh;

    [Tooltip("Capture button to validate the prediction")]
    public Button captureButton;

    [Tooltip("Should colors be inverted in the camera capture?")]
    public bool invert = true;

    // Fields for score and time TextMeshProUGUI components
    public TextMeshProUGUI scoreText; // Displays score
    public TextMeshProUGUI timeText;  // Displays elapsed time

    private int score = 0; // Score variable
    private float startTime = 0f; // Stores game start time
    private bool isGameActive = false; // Tracks if the game is active


    private Camera _agentCam;
    private char _lastPredict = Char.MinValue;
    private float _lastConfidence;
    private Invert _cameraInvertComponent;

    // Track the last stroke time and stroke count
    private float _lastStrokeTime;
    private int _currentStrokeCount;

    // Maximum number of strokes allowed before making a prediction
    public int maxStrokesBeforePredict = 5;

    // Time delay for prediction after last stroke
    public float strokeTimeout = 10.0f;

    // Distance to consider a stroke as complete
    public float strokeCompletionDistance = 0.1f;

    private Vector3 _lastStrokePosition; // Position of the last stroke
    private bool captureTriggered = false; // Flag to stop predictions after capture

    // List of target words for validation
    private List<string> targetWords = new List<string> { "xinh", "chu" }; // Add more words as needed
    private int currentTargetIndex = 0; // Index to track the current target word
    private const float ConfidenceThreshold = 0.8f; // Minimum confidence level for validation

    // Start is called before the first frame update
    private void Start()
    {
        _currentStrokeCount = 0;
        _lastStrokePosition = Vector3.zero; // Initialize last stroke position

        // Set up the capture button to trigger validation
        if (captureButton != null)
            captureButton.onClick.AddListener(ValidateAndDisplayResult);

        InitializeAgentCamera();
        targetWords = new List<string>(levelConfig.targetWords);
        GamePauseManager.Instance.OnGameStarted += StartGame;
    }

    private void StartGame()
    {
        isGameActive = true;
        startTime = Time.time;
        score = 0;
        UpdateScore(0); // Initialize score display
        UpdateTime(0f); // Initialize time display
    }

    private void InitializeAgentCamera()
    {
        // Create prediction camera and set up properties
        GameObject cameraGO = new GameObject("Agent Camera");
        _agentCam = cameraGO.AddComponent<Camera>();
        _cameraInvertComponent = cameraGO.AddComponent<Invert>(); // Add image inverter post-effect
        _agentCam.orthographic = true;
        _agentCam.cullingMask = relevantLayers;
        _agentCam.backgroundColor = cameraBackgroundColor;
        _agentCam.clearFlags = CameraClearFlags.Color; // Camera only renders text lines

        // Create target render texture. Dimension is 28x28
        RenderTexture rt = new RenderTexture(28, 28, 3);
        _agentCam.targetTexture = rt;
        agentParameters.agentRenderTextures = new List<RenderTexture>(new RenderTexture[] { rt }); // Assign to agent parameter
    }

    // Update is called once per frame
    void Update()
    {
        _cameraInvertComponent.invert = invert;

        if (Input.GetKeyDown(KeyCode.X))
        {
            ResetInput(); // Reset the input on 'X' key press
        }

        if (_currentStrokeCount > 0 && Time.time - _lastStrokeTime > strokeTimeout && !captureTriggered)
        {
            PredictWriting(); // Check for timeout to automatically predict
        }
        if (isGameActive)
        {
            // Calculate elapsed time
            float elapsedTime = Time.time - startTime;
            UpdateTime(elapsedTime);
        }
    }

    public void UpdateScore(int points)
    {
        score += points;
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score; // Display updated score
        }
    }

    public void UpdateTime(float elapsedTime)
    {
        if (timeText != null)
        {
            timeText.text = "Time: " + elapsedTime.ToString("F2") + "s"; // Display formatted time
        }
    }

    public void AwardPoints(int points)
    {
        UpdateScore(points); // Call UpdateScore to modify and display new score
    }

    public void ResetInput()
    {
        parsedText = "";
        _currentStrokeCount = 0; // Reset stroke count
        captureTriggered = false; // Allow predictions again
        if (textMesh) textMesh.text = parsedText;
    }

    private void PredictWriting()
    {
        // Check if the current stroke count exceeds the max allowed
        if (_currentStrokeCount >= maxStrokesBeforePredict && !captureTriggered)
        {
            // Call the prediction method with the current writing area info
            WritingAreaInfo areaInfo = GetCurrentWritingAreaInfo(); // Get the writing area info
            StartCoroutine(PredictWithDelay(areaInfo)); // Make the prediction with delay
        }
    }

    /// <summary>
    /// Coroutine to delay prediction processing
    /// </summary>
    private IEnumerator PredictWithDelay(WritingAreaInfo writingAreaInfo)
    {
        yield return new WaitForSeconds(10.0f); // Delay for 10 seconds before predicting
        Predict(writingAreaInfo); // Make the prediction
    }

    /// <summary>
    /// Updates the parsed text and stroke count after a successful prediction
    /// </summary>
    public void UpdateParsedText(char predictedChar)
    {
        parsedText += predictedChar; // Append the predicted character
        _currentStrokeCount = 0; // Reset stroke count after prediction
        _lastStrokeTime = Time.time; // Update the last stroke time

        if (textMesh) textMesh.text = parsedText; // Update text mesh display
    }

    /// <summary>
    /// Records a stroke and determines if it is part of the same character
    /// </summary>
    /// <param name="newStrokePosition">The position of the new stroke</param>
    public void RecordStroke(Vector3 newStrokePosition)
    {
        // Only record strokes if capture has not been triggered
        if (!captureTriggered)
        {
            if (_currentStrokeCount == 0 || Vector3.Distance(newStrokePosition, _lastStrokePosition) <= strokeCompletionDistance)
            {
                _currentStrokeCount++; // Increment stroke count
            }
            else
            {
                _currentStrokeCount = 1; // New stroke is the first stroke for a new character
            }

            _lastStrokePosition = newStrokePosition; // Update the last stroke position
            _lastStrokeTime = Time.time; // Update last stroke time
        }
    }

    public void Predict(WritingAreaInfo writingAreaInfo)
    {
        if (!captureTriggered)
        {
            _agentCam.transform.position = writingAreaInfo.center - writingAreaInfo.normal;
            _agentCam.transform.rotation = Quaternion.LookRotation(writingAreaInfo.normal, writingAreaInfo.upNormal);
            _agentCam.orthographicSize = writingAreaInfo.size;
            _agentCam.Render();
            RequestDecision();
        }
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        int indexAtMax = vectorAction.ToList().IndexOf(vectorAction.Max()); // Get index of max value
        _lastPredict = allChars[indexAtMax];
        _lastConfidence = vectorAction[indexAtMax];

        // Update parsed text
        UpdateParsedText(_lastPredict);
    }

    private WritingAreaInfo GetCurrentWritingAreaInfo()
    {
        return new WritingAreaInfo
        {
            size = 1.0f,
            center = transform.position,
            normal = Vector3.forward,
            upNormal = Vector3.up
        };
    }

    /// <summary>
    /// Validates the parsedText against the current target word and displays true or false
    /// </summary>
    private void ValidateAndDisplayResult()
    {
        captureTriggered = true; // Stop further predictions

        bool isValid = ValidatePrediction();
        parsedText = isValid ? "true" : "false";

        if (textMesh != null)
        {
            textMesh.text = parsedText; // Display result on the textMesh
            Debug.Log("Validation Result Displayed: " + parsedText);
        }
        else
        {
            Debug.LogError("TextMesh is not assigned.");
        }

        // Increase score by 10 if the validation is true
        if (isValid)
        {
            UpdateScore(10);
        }

        // Move to the next target word after validation
        currentTargetIndex++;
        if (currentTargetIndex >= targetWords.Count)
        {
            currentTargetIndex = 0; // Loop back to the first word
        }
    }

    /// <summary>
    /// Checks if the parsed text matches the current target word with confidence
    /// </summary>
    private bool ValidatePrediction()
    {
        string currentTargetWord = targetWords[currentTargetIndex];
        return parsedText.Equals(currentTargetWord, StringComparison.OrdinalIgnoreCase) && _lastConfidence >= ConfidenceThreshold;
    }
}


