using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private string gameScene = "02_MainScene";

    public void OnStartClicked()
    {
        // รีเซ็ตคะแนนก่อนเริ่มเกมใหม่
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
            Debug.Log("[MenuUI] Score reset before starting new game");
        }
        
        // โหลดซีนเล่นจริง
        SceneManager.LoadScene(gameScene, LoadSceneMode.Single);
    }

    public void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
