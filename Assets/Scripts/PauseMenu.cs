using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static bool IsGamePaused = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        IsGamePaused = false;
        // Hide pause menu UI here
    }

    void PauseGame()
    {
        Time.timeScale = 0f;
        IsGamePaused = true;
        // Show pause menu UI here
    }
}
