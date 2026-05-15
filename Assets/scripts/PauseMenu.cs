using UnityEngine;
using UnityEngine.InputSystem; // 記得加這一行
using UnityEngine.SceneManagement; // 1. 必須加入這個命名空間才能切換場景

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public static bool isPaused = false;

    void Update()
    {
        // 改用新版 Input System 的語法偵測 Esc
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // 務必先恢復時間
        isPaused = false;   // 重置暫停標記
        
        // 重新載入當前場景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("遊戲重啟成功"); 
    }
}