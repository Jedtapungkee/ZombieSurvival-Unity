using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    [SerializeField] private string menuScene = "01_MainMenu";
    // ถ้าอยากมีซีนระบบคงอยู่ตลอด (Persistent) ใส่ชื่อไว้ได้ เช่น "_Core"
    [SerializeField] private string persistentScene = ""; // เว้นว่างถ้าไม่ใช้

    private async void Awake()
    {
        // โหลดซีนระบบ (ถ้ามี)
        if (!string.IsNullOrEmpty(persistentScene))
        {
            var loadCore = SceneManager.LoadSceneAsync(persistentScene, LoadSceneMode.Single);
            await loadCore;
        }

        // เปิดเมนูหลัก
        var loadMenu = SceneManager.LoadSceneAsync(menuScene, 
                          string.IsNullOrEmpty(persistentScene) ? LoadSceneMode.Single 
                                                                 : LoadSceneMode.Additive);
        await loadMenu;

        // ตั้ง active scene เป็นเมนู (กรณี Additive)
        if (!string.IsNullOrEmpty(persistentScene))
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(menuScene));
        }
    }
}
