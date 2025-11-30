
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private object Appilcation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Play()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Update is called once per frame
    void Quit()
    {
        EditorApplication.isPlaying = false;
        //Appilcation.Quit();
        Debug.Log("Player has quit the game");

    }
}
