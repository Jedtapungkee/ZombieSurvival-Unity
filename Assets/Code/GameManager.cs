using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    void Start()
    {
        // โหลดฉาก Environment เพิ่มเข้ามา
        if (!SceneManager.GetSceneByName("Environment").isLoaded)
        {
            SceneManager.LoadScene("Environment", LoadSceneMode.Additive);
        }

        // โหลดฉาก Player เพิ่มเข้ามา
        if (!SceneManager.GetSceneByName("Player").isLoaded)
        {
            SceneManager.LoadScene("Player", LoadSceneMode.Additive);
        }
    }
}
