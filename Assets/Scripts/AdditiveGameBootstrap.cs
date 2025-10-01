using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AdditiveGameBootstrap : MonoBehaviour
{
    [Header("Scenes")]
    public string mapSceneName = "Environment";
    [Tooltip("ตั้งเป็น true ถ้าอยากให้ใช้ Lighting/Lightmap ของ Environment")]
    public bool setEnvironmentActiveForLighting = true;

    [Header("Player")]
    public GameObject playerPrefab;
    [Tooltip("ชื่อวัตถุใน Environment ที่ใช้เป็นจุดเกิด")]
    public string spawnPointName = "PlayerSpawnPoint";
    [Tooltip("ถ้าหาไม่เจอด้วยชื่อ จะลองหาด้วย Tag นี้ (ไม่จำเป็นต้องตั้ง)")]
    public string optionalSpawnTag = "PlayerSpawn";
    public bool parentPlayerUnderEnvironment = false; // ย้าย Player ไปอยู่ Scene ของ Environment

    private IEnumerator Start()
    {
        // 1) โหลดแผนที่แบบ Additive
        var op = SceneManager.LoadSceneAsync(mapSceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        var envScene = SceneManager.GetSceneByName(mapSceneName);

        // 2) ตั้ง Active Scene สำหรับแสง/Lightmap (เลือกได้)
        if (setEnvironmentActiveForLighting && envScene.IsValid())
            SceneManager.SetActiveScene(envScene);

        // 3) หา SpawnPoint ใน Environment
        Transform spawn = FindSpawnInScene(envScene, spawnPointName, optionalSpawnTag);
        Vector3 pos = spawn ? spawn.position : Vector3.zero;
        Quaternion rot = spawn ? spawn.rotation : Quaternion.identity;

        // 4) สร้าง Player จาก Prefab
        if (playerPrefab != null)
        {
            GameObject player = Instantiate(playerPrefab, pos, rot);

            // ย้าย Player ไปอยู่ Scene ของแผนที่ (ถ้าต้องการให้จัดกลุ่มอยู่ซีนเดียวกัน)
            if (parentPlayerUnderEnvironment && envScene.IsValid())
                SceneManager.MoveGameObjectToScene(player, envScene);
        }
        else
        {
            Debug.LogError("[AdditiveGameBootstrap] Missing playerPrefab.");
        }
    }

    private Transform FindSpawnInScene(Scene scene, string name, string tag)
    {
        if (!scene.IsValid()) return null;

        // หาโดยชื่อก่อน
        foreach (var root in scene.GetRootGameObjects())
        {
            Transform t = FindByNameRecursive(root.transform, name);
            if (t) return t;
        }

        // หาโดย Tag (ถ้ากำหนด)
        if (!string.IsNullOrEmpty(tag))
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                Transform found = FindByTagRecursive(root.transform, tag);
                if (found) return found;
            }
        }

        return null;
    }

    private Transform FindByNameRecursive(Transform root, string target)
    {
        if (root.name == target) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindByNameRecursive(root.GetChild(i), target);
            if (r) return r;
        }
        return null;
    }

    private Transform FindByTagRecursive(Transform root, string tag)
    {
        if (root.CompareTag(tag)) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindByTagRecursive(root.GetChild(i), tag);
            if (r) return r;
        }
        return null;
    }
}
