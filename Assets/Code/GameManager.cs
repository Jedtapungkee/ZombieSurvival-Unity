using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Day/Night System")]
    public bool useMainSceneLighting = true; // ใช้ Lighting จาก Mainscene
    
    void Start()
    {
        LoadSceneAdditive("Environment");
        LoadSceneAdditive("Player");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void LoadSceneAdditive(string sceneName)
    {
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (useMainSceneLighting)
        {
            // ให้ Mainscene ควบคุม Lighting (สำหรับ Day/Night System)
            if (scene.name == "Environment")
            {
                // ไม่ตั้ง Environment เป็น Active Scene
                // เพื่อให้ Mainscene ควบคุม Lighting Settings
                Debug.Log("✅ Environment โหลดแล้ว - Mainscene ควบคุม Lighting");
                
                // ปิด Lighting ใน Environment Scene ถ้ามี
                DisableSceneLighting(scene);
            }
        }
        else
        {
            // ใช้วิธีเดิม - ให้ Environment ควบคุม Lighting
            if (scene.name == "Environment")
            {
                SceneManager.SetActiveScene(scene);
                Debug.Log("✅ Environment ถูกตั้งเป็น Active Scene แล้ว (ใช้ Lighting จาก Environment)");
            }
        }
        
        // เมื่อโหลดครบทุก Scene แล้ว
        if (SceneManager.GetSceneByName("Environment").isLoaded && 
            SceneManager.GetSceneByName("Player").isLoaded)
        {
            OnAllScenesLoaded();
        }
    }
    
    void DisableSceneLighting(Scene scene)
    {
        // หา GameObject ที่มี Light ใน Environment Scene และปิดใช้งาน
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            Light[] lights = obj.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.enabled = false;
                    Debug.Log($"🔄 ปิด Directional Light ใน {obj.name}");
                }
            }
        }
    }
    
    void OnAllScenesLoaded()
    {
        Debug.Log("🎮 ทุก Scene โหลดเสร็จแล้ว - เริ่มเกม!");
        
        // หา Player และ Goal Target
        FindAndSetupGameComponents();
    }
    
    void FindAndSetupGameComponents()
    {
        // หา Player จาก Player Scene
        Scene playerScene = SceneManager.GetSceneByName("Player");
        if (playerScene.isLoaded)
        {
            GameObject[] playerObjects = playerScene.GetRootGameObjects();
            foreach (GameObject obj in playerObjects)
            {
                if (obj.name.Contains("Player") || obj.tag == "Player")
                {
                    Debug.Log($"🎯 พบ Player: {obj.name}");
                    // ส่งข้อมูลไปยัง GameManager หรือ DayNightCycle
                    NotifySystemsPlayerFound(obj.transform);
                    break;
                }
            }
        }
        
        // หา Safe Zone จาก Environment Scene
        Scene environmentScene = SceneManager.GetSceneByName("Environment");
        if (environmentScene.isLoaded)
        {
            GameObject[] envObjects = environmentScene.GetRootGameObjects();
            foreach (GameObject obj in envObjects)
            {
                if (obj.name.Contains("SafeZone") || obj.tag == "Goal")
                {
                    Debug.Log($"🏁 พบ SafeZone: {obj.name}");
                    NotifySystemsGoalFound(obj.transform);
                    break;
                }
            }
        }
    }
    
    void NotifySystemsPlayerFound(Transform player)
    {
        // เก็บข้อมูล Player ไว้ก่อน - จะใช้ในขั้นตอนหลัง
        Debug.Log($"✅ พบ Player: {player.name}");
        
        // TODO: เชื่อมต่อกับ GameManager ในขั้นตอนถัดไป
    }
    
    void NotifySystemsGoalFound(Transform goal)
    {
        // เก็บข้อมูล Goal ไว้ก่อน - จะใช้ในขั้นตอนหลัง
        Debug.Log($"✅ พบ Goal: {goal.name}");
        
        // TODO: เชื่อมต่อกับ GameManager ในขั้นตอนถัดไป
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}