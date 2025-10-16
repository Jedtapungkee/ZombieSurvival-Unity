using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameOverUI : MonoBehaviour
{
    [Header("Bindings")]
    public Health playerHealth;     // Health ของผู้เล่น
    public GameObject gameOverPanel; // แพเนล Game Over (Canvas child)
    public Text messageText;         // (ทางเลือก) ข้อความแสดงผล

    [Header("Score Display")]
#if TMP_PRESENT
    [Tooltip("TextMeshPro สำหรับแสดงคะแนนสุดท้าย")]
    public TextMeshProUGUI finalScoreTextTMP;
#endif
    [Tooltip("Text สำหรับแสดงคะแนนสุดท้าย (สำรอง)")]
    public Text finalScoreTextLegacy;
    [Tooltip("ข้อความนำหน้าคะแนน")]
    public string scorePrefix = "Final Score: ";
    [Tooltip("ScoreUI ที่ต้องซ่อนเมื่อ Game Over")]
    public ScoreUI gameplayScoreUI;

    [Header("Behavior")] 
    public bool pauseTimeOnGameOver = true; 
    public bool fadeIn = true; 
    public float fadeTime = 0.3f; 

    [Header("Auto-Bind Player")] 
    public bool autoBindByTag = true; 
    public string playerTag = "Player"; 
    public float rebindInterval = 1f; 

    [Header("Time Over Settings")]
    [Tooltip("เปิดใช้งานระบบ Game Over เมื่อหมดเวลา")]
    public bool enableTimeOverDetection = true;
    [Tooltip("ข้อความแสดงเมื่อหมดเวลา")]
    public string timeOverMessage = "Time's Up!"; 

    private CanvasGroup panelCg;
    private bool shown;
    private bool timeOverTriggered = false; // ป้องกันการแสดง Game Over ซ้ำ
    private Coroutine rebindRoutine;

    [Header("Input & Cursor")]
    public bool unlockCursorOnGameOver = true;
    public bool lockCursorOnRestart = true;
    public Behaviour[] disableOnGameOver; // ใส่สคริปต์ที่ควรปิด เช่น กล้อง/ตัวควบคุมผู้เล่น
#if ENABLE_INPUT_SYSTEM
    public PlayerInput playerInput;             // ถ้าใช้ Input System
    public bool switchToUIActionMap = true;
    public string uiActionMap = "UI";
    public string gameplayActionMap = "Player"; // ใช้ตอนกลับเข้าเกม (ถ้าต้องการ)
    public bool disablePlayerInputOnGameOver = true; // ปิด PlayerInput เพื่อกันอินพุตไปที่เกมเพลย์
#endif

    void Awake()
    {
        if (gameOverPanel != null)
        {
            panelCg = gameOverPanel.GetComponent<CanvasGroup>();
            if (panelCg == null) panelCg = gameOverPanel.AddComponent<CanvasGroup>();
            gameOverPanel.SetActive(true); // ต้อง active เพื่อให้ควบคุม alpha
            panelCg.alpha = 0f;
            panelCg.interactable = false;
            panelCg.blocksRaycasts = false;
        }
    }

    void OnEnable()
    {
        if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        else if (autoBindByTag) StartRebind();
        
        // Subscribe to ScoreManager events
        ScoreManager.OnGameOver += OnGameOver;
        
        // Subscribe to Time Over events
        if (enableTimeOverDetection)
        {
            SimpleDayNight.OnTimeOver += OnTimeOver;
        }
    }

    void OnDisable()
    {
        if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        StopRebind();
        
        // Unsubscribe from ScoreManager events
        ScoreManager.OnGameOver -= OnGameOver;
        
        // Unsubscribe from Time Over events
        SimpleDayNight.OnTimeOver -= OnTimeOver;
    }

    private void OnPlayerDied()
    {
        if (shown) return;
        
        // แจ้ง ScoreManager ว่า Player ตาย (เพื่อเก็บคะแนนสุดท้ายและ reset)
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnPlayerDied();
        }
        
        // เลือกข้อความที่เหมาะสม
        string message = timeOverTriggered ? timeOverMessage : "Game Over";
        ShowGameOver(message);
    }
    
    private void OnGameOver(int finalScore)
    {
        // แสดงคะแนนสุดท้ายใน Game Over UI
        DisplayFinalScore(finalScore);
    }
    
    private void OnTimeOver()
    {
        if (shown || timeOverTriggered) return;
        
        timeOverTriggered = true;
        Debug.Log("[GameOverUI] Time Over detected - Player will be killed by SimpleDayNight system");
        
        // ไม่ต้องเรียก ShowGameOver ที่นี่ เพราะ Player จะตายจาก SimpleDayNight
        // และ OnPlayerDied() จะถูกเรียกอัตโนมัติ ซึ่งจะแสดง Game Over อยู่แล้ว
    }
    
    private void ShowGameOver(string message = "Game Over")
    {
        if (shown) return;
        shown = true;
        if (pauseTimeOnGameOver) Time.timeScale = 0f;
        if (messageText != null) messageText.text = message;

        // ซ่อน Score UI ระหว่างเล่น
        if (gameplayScoreUI != null)
        {
            gameplayScoreUI.HideUI();
        }

        // เก็บคะแนนสุดท้าย (สำหรับกรณี Time Over)
        if (message == timeOverMessage && ScoreManager.Instance != null)
        {
            int currentScore = ScoreManager.Instance.CurrentScore;
            DisplayFinalScore(currentScore);
            Debug.Log($"[GameOverUI] Time Over! Final Score: {currentScore}");
        }

        // ปิดสคริปต์ควบคุมการมอง/ขยับ และปลดล็อคเมาส์
        if (disableOnGameOver != null)
        {
            foreach (var b in disableOnGameOver)
            {
                if (b != null) b.enabled = false;
            }
        }
        if (unlockCursorOnGameOver)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
#if ENABLE_INPUT_SYSTEM
        if (playerInput != null)
        {
            if (disablePlayerInputOnGameOver)
            {
                playerInput.enabled = false; // ตัดอินพุตเกมเพลย์ออกทั้งหมด
            }
            else if (switchToUIActionMap)
            {
                // สลับเป็นแผนผัง UI เพื่อให้คลิกปุ่มได้และไม่ส่ง look input เข้าเกม
                playerInput.SwitchCurrentActionMap(uiActionMap);
            }
        }
#endif
        if (panelCg != null && fadeIn)
        {
            StartCoroutine(FadeInRoutine());
        }
        else if (panelCg != null)
        {
            panelCg.alpha = 1f; panelCg.interactable = true; panelCg.blocksRaycasts = true;
        }
        else if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }
    
    private void DisplayFinalScore(int score)
    {
        string scoreText = scorePrefix + score.ToString("N0");
        
#if TMP_PRESENT
        if (finalScoreTextTMP != null)
        {
            finalScoreTextTMP.text = scoreText;
            return;
        }
#endif
        
        if (finalScoreTextLegacy != null)
        {
            finalScoreTextLegacy.text = scoreText;
        }
    }

    private System.Collections.IEnumerator FadeInRoutine()
    {
        float t = 0f;
        panelCg.interactable = false; panelCg.blocksRaycasts = true; // ให้รับคลิกระหว่างเฟดได้
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            panelCg.alpha = Mathf.Lerp(0f, 1f, t / fadeTime);
            yield return null;
        }
        panelCg.alpha = 1f; panelCg.interactable = true; panelCg.blocksRaycasts = true;
    }

    public void RestartScene()
    {
        // รีเซ็ตคะแนนก่อน Restart
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
            Debug.Log("[GameOverUI] Score reset before restart");
        }
        
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
        if (lockCursorOnRestart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#if ENABLE_INPUT_SYSTEM
        if (playerInput != null)
        {
            playerInput.enabled = true;
            if (switchToUIActionMap && !string.IsNullOrEmpty(gameplayActionMap))
            {
                playerInput.SwitchCurrentActionMap(gameplayActionMap);
            }
        }
#endif
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartRebind()
    {
        StopRebind();
        rebindRoutine = StartCoroutine(RebindLoop());
    }

    private void StopRebind()
    {
        if (rebindRoutine != null) { StopCoroutine(rebindRoutine); rebindRoutine = null; }
    }

    private System.Collections.IEnumerator RebindLoop()
    {
        while (playerHealth == null)
        {
            TryBindPlayerByTag();
            if (playerHealth != null) yield break;
            yield return new WaitForSeconds(rebindInterval);
        }
    }

    private void TryBindPlayerByTag()
    {
        try
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null)
            {
                var hp = go.GetComponent<Health>();
                if (hp != null)
                {
                    playerHealth = hp;
                    playerHealth.Died += OnPlayerDied;
                }
            }
        }
        catch { }
    }
}
