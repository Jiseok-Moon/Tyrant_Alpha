using UnityEngine;
using UnityEngine.SceneManagement; // 씬 재시작

// [기획 의도] 게임의 전체 생명 주기(Life Cycle) 관리.
// 싱글톤(Singleton) 패턴을 적용하여 프로젝트 어디서든 접근 가능하게 설계했으며,
// 사망 처리 및 일시정지 등 게임 상태 전환 시 Time.timeScale을 제어해 로직의 일시적인 중단을 보장함.

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject gameOverPanel;
    public GameObject pausePanel;
    private bool isPaused = false;

    void Awake() { Instance = this; }


    // [시스템 매커니즘] 확장성을 고려한 UI 패널 관리.
    // 단순히 게임을 멈추는 것에 그치지 않고, 각 상태(GameOver, Pause)에 맞는 UI를 활성화하여 
    // 유저가 다음 액션(재시작, 종료)을 명확히 선택할 수 있도록 구현함.

    void Update()
    {
        // ESC 키로 일시정지 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // --- [사망 처리] ---
    public void TriggerGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true); // 패널 켜기
            Time.timeScale = 0f;          // 게임 멈춤
        }
    }

    // --- [일시 정지 관련] ---
    public void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // --- [버튼 연결용 함수들] ---
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); // 실제 빌드 시 종료
#endif
    }
}