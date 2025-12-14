using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Drag your "Level 1" scene name here in the Inspector if you want, 
    // or just type the exact name in the string below.

    public void LoadGameLevel()
    {
        // CHANGE "GameScene" TO THE EXACT NAME OF YOUR PLAYING LEVEL
        SceneManager.LoadScene("Level1");
    }

    public void LoadIntro()
    {
        SceneManager.LoadScene("IntroScene");
    }
}