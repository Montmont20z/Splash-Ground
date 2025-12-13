using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System; // Use TextMeshPro for better text

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public float gameDuration = 120f;              // Total time to survive (seconds)
    public float minHealthPercentage = 80f;         // Lose if health drops below this
    public float healthRange;
    
    
    [Header("References")]
    public ArenaManager arenaManager;

    [Header("UI - Game HUD")]
    public TextMeshProUGUI healthPercentageText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI minHealthText;
    public Slider healthBar;                       // Visual health bar

    [Header("UI - Win/Loss Screens")]
    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI winStatsText;           // Show final stats on win
    public TextMeshProUGUI loseStatsText;          // Show final stats on lose

    [Header("Audio")]
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip warningSound;                 // Play when health is low

    [Header("Player References")]
    public GameObject player; 

    // Private state
    private float timeRemaining;
    private bool gameActive = true;
    private bool hasPlayedWarning = false;
    private AudioSource audioSource;

    // Stats tracking
    private float lowestHealthReached = 100f;
    private int totalTilesCleansed = 0;

    void Start()
    {
        // Initialize
        timeRemaining = gameDuration;
        gameActive = true;

        healthRange = 100f - minHealthPercentage;
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Hide win/loss panels
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        // Validate references
        if (arenaManager == null)
        {
            arenaManager = FindObjectOfType<ArenaManager>();
            if (arenaManager == null)
            {
                Debug.LogError("ArenaManager not found! Assign it in Inspector.");
            }
        }
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }
        }

        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log($"Game started: Survive for {gameDuration}s, keep health above {minHealthPercentage}%");
    }

    void Update()
    {
        if (!gameActive) return;

        UpdateTimer();
        UpdateHealthDisplay();
        CheckWinLoseConditions();
    }

    void UpdateTimer()
    {
        timeRemaining -= Time.deltaTime;

        if (timeRemaining < 0)
            timeRemaining = 0;

        // Format time as MM:SS
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        if (timerText != null)
        {
            timerText.text = $"Time: {minutes:00}:{seconds:00}";

            // Change color when time is running out
            if (timeRemaining < 10f)
            {
                timerText.color = Color.red;
            }
            else if (timeRemaining < 30f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }

    void UpdateHealthDisplay()
    {
        if (arenaManager == null) return;

        float currentHealth = arenaManager.GetHealthPercentage();

        // Track lowest health
        if (currentHealth < lowestHealthReached)
        {
            lowestHealthReached = currentHealth;
        }

        // Update health percentage text
        if (healthPercentageText != null)
        {
            healthPercentageText.text = $"Floor Health: {currentHealth:F1}%";

            // Change color based on health
            if (currentHealth < minHealthPercentage + healthRange * 0.2)
            {
                healthPercentageText.color = Color.red;
            }
            else if (currentHealth < minHealthPercentage + healthRange * 0.5)
            {
                healthPercentageText.color = Color.yellow;
            }
            else
            {
                healthPercentageText.color = Color.green;
            }
        }

        float barDisplay = Mathf.InverseLerp(minHealthPercentage, 100f, currentHealth);

        barDisplay = Mathf.Clamp01(barDisplay);

        // Update health bar
        if (healthBar != null)
        {
            healthBar.value = barDisplay; // Slider uses 0-1 range
        }


        // Update minimum health indicator
        if (minHealthText != null)
        {
            minHealthText.text = $"Min Required: {minHealthPercentage:F0}%";
        }

        // Play warning sound when health is critical
        if (currentHealth < minHealthPercentage + 5f && !hasPlayedWarning)
        {
            PlaySound(warningSound);
            hasPlayedWarning = true;
        }

        // Reset warning flag when health recovers
        if (currentHealth > minHealthPercentage + 10f)
        {
            hasPlayedWarning = false;
        }
    }

    void CheckWinLoseConditions()
    {
        float currentHealth = arenaManager.GetHealthPercentage();

        // LOSE CONDITION: Health drops below minimum
        if (currentHealth < minHealthPercentage)
        {
            LoseGame("Floor contamination exceeded acceptable levels!");
        }

        // WIN CONDITION: Timer reaches zero with health above minimum
        if (timeRemaining <= 0 && currentHealth >= minHealthPercentage)
        {
            WinGame();
        }
    }

    void WinGame()
    {
        if (!gameActive) return; // Prevent multiple calls

        gameActive = false;
        DisablePlayerControls();
        Time.timeScale = 0f; // Pause game

        Debug.Log("GAME WON!");

        // Show cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Play win sound
        PlaySound(winSound);

        // Show win panel
        if (winPanel != null)
        {
            winPanel.SetActive(true);

            // Display final stats
            if (winStatsText != null)
            {
                float finalHealth = arenaManager.GetHealthPercentage();
                string stats = $"VICTORY!\n\n" +
                              $"Final Floor Health: {finalHealth:F1}%\n" +
                              $"Lowest Health: {lowestHealthReached:F1}%\n" +
                              $"Time Survived: {FormatTime(gameDuration)}";
                winStatsText.text = stats;
            }
        }
    }

 
    void LoseGame(string reason)
    {
        if (!gameActive) return; // Prevent multiple calls

        gameActive = false;
        DisablePlayerControls();
        Time.timeScale = 0f; // Pause game

        Debug.Log($"GAME LOST: {reason}");

        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Play lose sound
        PlaySound(loseSound);

        // Show lose panel
        if (losePanel != null)
        {
            losePanel.SetActive(true);

            // Display final stats
            if (loseStatsText != null)
            {
                float finalHealth = arenaManager.GetHealthPercentage();
                float timeElapsed = gameDuration - timeRemaining;
                string stats = $"DEFEAT!\n\n" +
                              $"{reason}\n\n" +
                              $"Final Floor Health: {finalHealth:F1}%\n" +
                              $"Required: {minHealthPercentage:F0}%\n" +
                              $"Time Survived: {FormatTime(timeElapsed)}";
                loseStatsText.text = stats;
            }
        }
    }

    // Public methods for UI buttons
    public void RestartLevel()
    {
        Debug.Log("RestartLevel button clicked!");
        Time.timeScale = 1f; // Resume time
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel()
    {
        Debug.Log("NextLevel button clicked!");
        Time.timeScale = 1f;
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"Loading scene {nextSceneIndex}");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("No next level! Going to main menu or restarting...");
            SceneManager.LoadScene(0); // Go to first scene (main menu)
        }
    }

    public void LoadMainMenu()
    {
        Debug.Log("LoadMainMenu button clicked!");
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // Assumes scene 0 is main menu
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // Helper methods
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    // Public method to track player actions (call from SprayShooter)
    public void OnTileCleansed()
    {
        totalTilesCleansed++;
    }
    void DisablePlayerControls()
    {
        if (player == null) return;

        // Disable player movement
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("Player movement disabled");
        }

        // Disable spray shooting
        SprayShooter sprayShooter = Camera.main.GetComponent<SprayShooter>();
        if (sprayShooter != null)
        {
            sprayShooter.enabled = false;
            Debug.Log("Spray shooter disabled");
        }

        // Disable camera control
        FPSCameraController cameraController = Camera.main.GetComponent<FPSCameraController>();
        if (cameraController != null)
        {
            cameraController.enabled = false;
            Debug.Log("Camera controller disabled");
        }

    }


    [SerializeField] GameObject pauseMenu;
    public void PauseMenu()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0;
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1;
    }
}