using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalWin : MonoBehaviour
{
    [Header("Config")]
    public string winSceneName = "03_WinScenes";
    public string playerTag = "Player";

    private void Reset()
    {
        // ตั้งค่าเริ่มต้นให้แน่ใจว่าคอลไลเดอร์เป็น Trigger
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        if (rb) { rb.useGravity = false; rb.isKinematic = true; }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[GoalWin] Trigger with: {other.name} (tag={other.tag})");

        bool isPlayer =
            other.CompareTag(playerTag) ||                                  // ตัวที่ชนมี Tag = Player
            (other.attachedRigidbody && other.attachedRigidbody.CompareTag(playerTag)) || // หรือ Rigidbody ที่ติดมากับตัวที่ชนมี Tag = Player
            (other.GetComponentInParent<Transform>()?.CompareTag(playerTag) ?? false);    // หรือพาเรนต์มี Tag = Player

        if (isPlayer)
        {
            Debug.Log($"[GoalWin] WIN! Loading scene: {winSceneName}");
            SceneManager.LoadScene(winSceneName, LoadSceneMode.Single);
        }
    }
}
