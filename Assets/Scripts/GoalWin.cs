using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GoalWin : MonoBehaviour
{
    [Header("Config")]
    public string winSceneName = "03_WinScenes";
#if UNITY_EDITOR
    [Tooltip("เลือก Scene โดยตรง (ใน Editor เท่านั้น) จะตั้งชื่อให้ winSceneName อัตโนมัติ")]
    public SceneAsset winScene;
#endif
    public string playerTag = "Player";

    private void Reset()
    {
        // ตั้งค่าเริ่มต้นให้แน่ใจว่าคอลไลเดอร์เป็น Trigger
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        if (rb) { rb.useGravity = false; rb.isKinematic = true; }
    }

    private void Awake()
    {
#if UNITY_EDITOR
        // กันกรณีค่า string เก่าไม่ได้อัปเดต แม้เลือก SceneAsset ไว้แล้ว
        if (winScene != null)
        {
            string path = AssetDatabase.GetAssetPath(winScene);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(name))
                winSceneName = name;
        }
#endif
        Debug.Log($"[GoalWin] '{name}' ready. Win scene = '{winSceneName}'", this);
    }

    private void OnTriggerEnter(Collider other)
    {
    Debug.Log($"[GoalWin] Trigger from '{name}' with: {other.name} (tag={other.tag}); winSceneName='{winSceneName}'");

        bool isPlayer =
            other.CompareTag(playerTag) ||                                  // ตัวที่ชนมี Tag = Player
            (other.attachedRigidbody && other.attachedRigidbody.CompareTag(playerTag)) || // หรือ Rigidbody ที่ติดมากับตัวที่ชนมี Tag = Player
            (other.GetComponentInParent<Transform>()?.CompareTag(playerTag) ?? false);    // หรือพาเรนต์มี Tag = Player

        if (isPlayer)
        {
            if (!Application.CanStreamedLevelBeLoaded(winSceneName))
            {
                Debug.LogError($"[GoalWin] Cannot load scene '{winSceneName}'.\n- Make sure the name matches exactly the scene asset (without .unity).\n- Add it to File > Build Profiles (Scenes in Build).\nTip: In the inspector, assign the 'Win Scene' field to set the name automatically.", this);
                return;
            }

            // บันทึกคะแนนก่อนเปลี่ยน Scene (เก็บไว้ใน CurrentScore เพื่อแสดงใน Win Scene)
            if (ScoreManager.Instance != null)
            {
                Debug.Log($"[GoalWin] Player won with score: {ScoreManager.Instance.CurrentScore}");
            }
            
            Debug.Log($"[GoalWin] WIN! Loading scene: {winSceneName}");
            SceneManager.LoadScene(winSceneName, LoadSceneMode.Single);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (winScene != null)
        {
            string path = AssetDatabase.GetAssetPath(winScene);
            if (!string.IsNullOrEmpty(path))
            {
                // ตัดชื่อไฟล์ .unity มาเป็นชื่อซีน
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(name))
                    winSceneName = name;
            }
        }
    }
#endif
}
