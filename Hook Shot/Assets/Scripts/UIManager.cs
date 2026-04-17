using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject startPanel;        // StartPanel with CanvasGroup
    [SerializeField] private GameObject restartPanel;      // RestartPanel with CanvasGroup
    [SerializeField] private GameObject nextLevelPanel;    // NextLevelPanel with CanvasGroup

    [Header("Buttons")]
    [SerializeField] private Button startButton;           // StartButton inside StartPanel
    [SerializeField] private Button restartButton;         // RestartButton inside RestartPanel
    [SerializeField] private Button nextLevelButton;       // NextLevelButton inside NextLevelPanel
    [SerializeField] private Button zoomButton;            // ZoomButton (Button component)
    [SerializeField] private Button speedButton;           // SpeedButton

    [Header("Text References")]
    [SerializeField] private TMP_Text zoomButtonText;      // Child TMP text of ZoomButton
    [SerializeField] private TMP_Text speedButtonText;     // Child TMP text of SpeedButton

    [Header("References")]
    [SerializeField] private CameraController cameraController; // Camera script reference

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.4f;    // Panel fade duration in seconds

    private void Start()
    {
        // Set up button listeners
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonPressed);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartButtonPressed);

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelButtonPressed);

        if (zoomButton != null)
            zoomButton.onClick.AddListener(OnZoomButtonPressed);

        if (speedButton != null)
            speedButton.onClick.AddListener(OnSpeedButtonPressed);

        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart += OnGameStart;
            GameManager.Instance.OnLevelComplete += OnLevelComplete;
            GameManager.Instance.OnGameFailed += OnGameFailed;
        }

        // Initial panel states
        HidePanelImmediate(restartPanel);
        HidePanelImmediate(nextLevelPanel);
        ShowPanelImmediate(startPanel);

        // Initialize button texts
        UpdateZoomText();
        UpdateSpeedText();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart -= OnGameStart;
            GameManager.Instance.OnLevelComplete -= OnLevelComplete;
            GameManager.Instance.OnGameFailed -= OnGameFailed;
        }
    }

    // ─────────────────── Button Callbacks ───────────────────

    private void OnStartButtonPressed()
    {
        StartCoroutine(HidePanelCoroutine(startPanel, () =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartGame();
        }));
    }

    private void OnRestartButtonPressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
    }

    private void OnNextLevelButtonPressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadNextLevel();
    }

    private void OnZoomButtonPressed()
    {
        if (cameraController != null)
            cameraController.ZoomToggle();

        UpdateZoomText();
    }

    private void OnSpeedButtonPressed()
    {
        if (SpeedController.Instance != null)
        {
            SpeedController.Instance.CycleSpeed();
            UpdateSpeedText();
        }
    }

    // ─────────────────── Event Handlers ───────────────────

    private void OnGameStart()
    {
        // Panels already handled in OnStartButtonPressed
    }

    private void OnLevelComplete()
    {
        // Check if this is the last level
        int currentIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;

        if (currentIndex >= sceneCount - 1)
        {
            // Last level — change button text to "You Win!"
            TMP_Text btnText = nextLevelButton != null ? nextLevelButton.GetComponentInChildren<TMP_Text>() : null;
            if (btnText != null)
                btnText.text = "You Win! Restart";
        }

        StartCoroutine(ShowPanelCoroutine(nextLevelPanel));
    }

    private void OnGameFailed()
    {
        // Zoom camera to ball first, then show restart panel
        if (cameraController != null)
        {
            cameraController.ZoomToBallOnFail(() =>
            {
                StartCoroutine(ShowPanelCoroutine(restartPanel));
            });
        }
        else
        {
            StartCoroutine(ShowPanelCoroutine(restartPanel));
        }
    }

    // ─────────────────── Text Updates ───────────────────

    private void UpdateZoomText()
    {
        if (zoomButtonText == null) return;

        if (cameraController != null && cameraController.IsZoomedIn)
            zoomButtonText.text = "Zoom Out";
        else
            zoomButtonText.text = "Zoom In";
    }

    private void UpdateSpeedText()
    {
        if (speedButtonText == null || SpeedController.Instance == null) return;
        speedButtonText.text = SpeedController.Instance.CurrentPercentage + "%";
    }

    // ─────────────────── Panel Fade Methods ───────────────────

    private IEnumerator ShowPanelCoroutine(GameObject panel)
    {
        if (panel == null) yield break;

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            panel.SetActive(true);
            yield break;
        }

        cg.alpha = 0f;
        panel.SetActive(true);
        cg.interactable = false;
        cg.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private IEnumerator HidePanelCoroutine(GameObject panel, System.Action onComplete = null)
    {
        if (panel == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            panel.SetActive(false);
            onComplete?.Invoke();
            yield break;
        }

        cg.interactable = false;
        cg.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        cg.alpha = 0f;
        panel.SetActive(false);

        onComplete?.Invoke();
    }

    private void ShowPanelImmediate(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(true);
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    private void HidePanelImmediate(GameObject panel)
    {
        if (panel == null) return;
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        panel.SetActive(false);
    }
}
