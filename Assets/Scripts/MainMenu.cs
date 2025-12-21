using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject settingsPanel;
    //private object Appilcation;

    void Start()
    {
        settingsPanel.SetActive(false);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);


    }

    // Update is called once per frame
    public void QuitGame()
    {
        Debug.Log("Player has quit the game");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

    }

    public void Setting()
    {
        if (settingsPanel == null) return;

        // Toggle settings panel
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

}
