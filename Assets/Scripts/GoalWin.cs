using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalWin : MonoBehaviour
{
    [SerializeField] string winSceneName = "03_WinScene";
    [SerializeField] string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        // หา GameObject เจ้าของจริงของคอลลไลเดอร์ (ถ้ามี Rigidbody จะอ้างเจ้าของนั้นก่อน)
        GameObject root = other.attachedRigidbody ? other.attachedRigidbody.gameObject
                                                  : other.transform.root.gameObject;

        if (root.CompareTag(playerTag))
        {
            // ป้องกันกรณีมี pause หรือ timeScale ถูกแก้
            Time.timeScale = 1f;

            // โหลดซีนฉากชนะ
            SceneManager.LoadScene(winSceneName);
        }
    }
}
