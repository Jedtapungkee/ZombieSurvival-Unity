using UnityEngine;
using UnityEngine.SceneManagement;

public class WinMenu : MonoBehaviour
{
    [Header("ชื่อซีนเมนูหลัก (ต้องอยู่ใน Build Settings)")]
    public string mainMenuScene = "01_MainMenu";

    public void OnBackToMenu()
    {
        Debug.Log($"[WinMenu] Back clicked -> Load {mainMenuScene}");
        SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
    }

    public void OnQuit()
    {
        Debug.Log("[WinMenu] Quit clicked");
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
