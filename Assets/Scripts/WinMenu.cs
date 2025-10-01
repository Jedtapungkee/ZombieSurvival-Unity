using UnityEngine;
using UnityEngine.SceneManagement;

public class WinMenu : MonoBehaviour
{
    [Header("ชื่อซีนเมนูหลัก (ต้องอยู่ใน Build Settings)")]
    public string mainMenuScene = "01_MainMenu";

    public void OnBackToMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
