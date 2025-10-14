using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class WinMenu : MonoBehaviour
{
    [Header("ชื่อซีนเมนูหลัก (ต้องอยู่ใน Build Settings)")]
    public string mainMenuScene = "01_MainMenu";

    [Header("Score Display")]
    [Tooltip("TextMeshPro สำหรับแสดงคะแนนชัยชนะ")]
    public TextMeshProUGUI scoreTextTMP;
    [Tooltip("Legacy Text สำหรับแสดงคะแนน (สำรอง)")]
    public Text scoreTextLegacy;
    [Tooltip("คำนำหน้าข้อความคะแนน")] public string scorePrefix = "Your Score: ";
    [Tooltip("รูปแบบตัวเลข เช่น N0, N2")] public string numberFormat = "N0";
    [Tooltip("พยายามหา Text อัตโนมัติจากลูกของ GameObject นี้ถ้าไม่ได้อ้างอิงไว้")]
    public bool autoFindText = true;

    [Header("Cursor Settings")]
    [Tooltip("ตั้งค่าเมาส์เมื่อเข้าหน้า Win")] public bool setCursorOnOpen = true;
    public CursorLockMode lockStateOnOpen = CursorLockMode.None;
    public bool cursorVisibleOnOpen = true;

    void OnEnable()
    {
        // หา text อัตโนมัติหากยังไม่ได้อ้างอิง
        if (autoFindText)
        {
            if (scoreTextTMP == null)
                scoreTextTMP = GetComponentInChildren<TextMeshProUGUI>(true);
            if (scoreTextLegacy == null)
                scoreTextLegacy = GetComponentInChildren<Text>(true);
        }

        RefreshScore();
    }

    void Start()
    {
        // รอให้ฉากโหลดเสร็จ แล้วค่อยตั้งค่า UI
        SetupUIInteraction();
    }

    private void SetupUIInteraction()
    {
        // รีเซ็ต Time Scale กรณีที่เกมพอส/สโลว์อยู่
        Time.timeScale = 1f;
        
        // ตรวจสอบและสร้าง EventSystem ถ้าไม่มี (สำหรับให้ปุ่มทำงาน)
        EnsureEventSystem();
        
        // แสดง/ปลดล็อคเมาส์เพื่อให้กดปุ่มได้ (บังคับให้แน่ใจ)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // ปิด PlayerInput ถ้ามี (เพื่อไม่ให้รบกวน UI)
#if ENABLE_INPUT_SYSTEM
        var playerInput = FindObjectOfType<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null && playerInput.enabled)
        {
            playerInput.enabled = false;
            Debug.Log("[WinMenu] Disabled PlayerInput to allow UI interaction");
        }
#endif
        
        Debug.Log($"[WinMenu] UI Setup complete. Cursor: visible={Cursor.visible}, lockState={Cursor.lockState}, TimeScale={Time.timeScale}");
    }
    
    private void EnsureEventSystem()
    {
        // ตรวจว่ามี EventSystem ในฉากหรือไม่
        if (EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            EventSystem eventSystem = eventSystemObj.AddComponent<EventSystem>();
            
#if ENABLE_INPUT_SYSTEM
            // ถ้าใช้ New Input System ต้องใช้ InputSystemUIInputModule
            var inputModule = eventSystemObj.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[WinMenu] Created EventSystem with InputSystemUIInputModule");
#else
            // ถ้าใช้ Legacy Input ใช้ StandaloneInputModule
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("[WinMenu] Created EventSystem with StandaloneInputModule");
#endif
        }
        else
        {
            // ตรวจว่า EventSystem ยัง enabled อยู่หรือไม่
            if (!EventSystem.current.enabled)
            {
                EventSystem.current.enabled = true;
                Debug.Log("[WinMenu] Re-enabled EventSystem");
            }
            
            // ตรวจว่ามี Input Module ที่เหมาะสมหรือไม่
#if ENABLE_INPUT_SYSTEM
            var inputModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                // ลบ StandaloneInputModule เก่าถ้ามี
                var oldModule = EventSystem.current.GetComponent<StandaloneInputModule>();
                if (oldModule != null) Destroy(oldModule);
                
                // เพิ่ม InputSystemUIInputModule
                EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[WinMenu] Added InputSystemUIInputModule to existing EventSystem");
            }
#endif
            
            Debug.Log($"[WinMenu] EventSystem exists: {EventSystem.current.name}");
        }
    }

    public void RefreshScore()
    {
        int score = 0;
        if (ScoreManager.Instance != null)
        {
            score = ScoreManager.Instance.CurrentScore;
        }
        else
        {
            Debug.LogWarning("[WinMenu] ScoreManager not found. Showing 0 as score.", this);
        }

        string text = scorePrefix + score.ToString(numberFormat);

        if (scoreTextTMP != null)
        {
            scoreTextTMP.text = text;
            return;
        }

        if (scoreTextLegacy != null)
        {
            scoreTextLegacy.text = text;
        }
    }

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
