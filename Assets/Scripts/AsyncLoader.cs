using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class AsyncLoader : MonoBehaviour
{
    [Header("Loading Screen UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private TextMeshProUGUI tipsText;

    [Header("Loading Animation")]
    [SerializeField] private AnimationCurve loadingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float smoothingSpeed = 2f;
    [SerializeField] private bool useSmoothing = true;

    [Header("Loading Tips")]
    [SerializeField]
    private string[] loadingTips = {
        "Tip: Explore every corner to find hidden mushrooms!",
        "Tip: Different mushrooms have unique personalities.",
        "Tip: Check your research book regularly.",
        "Tip: Some mushrooms only appear at certain times.",
        "Tip: Be patient - some mushrooms are shy!"
    };
    [SerializeField] private float tipChangeInterval = 3f;

    [Header("Minimum Loading Time")]
    [SerializeField] private float minimumLoadTime = 2f;

    // Static reference for global access
    public static AsyncLoader Instance { get; private set; }

    // Loading state
    private AsyncOperation currentAsyncOperation;
    private bool isLoading = false;
    private float targetProgress = 0f;
    private float currentDisplayProgress = 0f;
    private float loadStartTime;
    private Coroutine tipRotationCoroutine;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize loading panel as hidden
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    void Update()
    {
        if (isLoading && currentAsyncOperation != null)
        {
            UpdateLoadingProgress();
        }
    }

    #region Public Loading Methods

    /// <summary>
    /// Load a scene asynchronously with loading screen
    /// </summary>
    /// <param name="sceneName">Name of the scene to load</param>
    /// <param name="loadMode">Scene load mode</param>
    public void LoadSceneAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        if (!isLoading)
        {
            StartCoroutine(LoadSceneCoroutine(sceneName, loadMode));
        }
        else
        {
            Debug.LogWarning("Already loading a scene! Please wait for current operation to complete.");
        }
    }

    /// <summary>
    /// Load a scene by build index asynchronously with loading screen
    /// </summary>
    /// <param name="sceneIndex">Build index of the scene to load</param>
    /// <param name="loadMode">Scene load mode</param>
    public void LoadSceneAsync(int sceneIndex, LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        if (!isLoading)
        {
            StartCoroutine(LoadSceneCoroutine(sceneIndex, loadMode));
        }
        else
        {
            Debug.LogWarning("Already loading a scene! Please wait for current operation to complete.");
        }
    }

    /// <summary>
    /// Show loading screen for custom loading operations
    /// </summary>
    /// <param name="customLoadingOperation">Your custom loading coroutine</param>
    public void ShowLoadingScreen(System.Func<System.Collections.IEnumerator> customLoadingOperation)
    {
        if (!isLoading)
        {
            StartCoroutine(CustomLoadingCoroutine(customLoadingOperation));
        }
        else
        {
            Debug.LogWarning("Already loading! Please wait for current operation to complete.");
        }
    }

    /// <summary>
    /// Update progress manually for custom loading operations (0 to 1)
    /// </summary>
    /// <param name="progress">Progress value between 0 and 1</param>
    public void UpdateProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
    }

    #endregion

    #region Loading Coroutines

    private IEnumerator LoadSceneCoroutine(string sceneName, LoadSceneMode loadMode)
    {
        yield return StartLoading();

        // Start async scene loading
        currentAsyncOperation = SceneManager.LoadSceneAsync(sceneName, loadMode);
        if (currentAsyncOperation != null)
        {
            // Prevent scene from activating automatically
            currentAsyncOperation.allowSceneActivation = false;

            // Wait for loading to complete (90% in Unity)
            yield return StartCoroutine(WaitForAsyncOperation());

            // Activate the scene
            currentAsyncOperation.allowSceneActivation = true;
            yield return currentAsyncOperation;
        }

        yield return EndLoading();
    }

    private IEnumerator LoadSceneCoroutine(int sceneIndex, LoadSceneMode loadMode)
    {
        yield return StartLoading();

        // Start async scene loading
        currentAsyncOperation = SceneManager.LoadSceneAsync(sceneIndex, loadMode);
        if (currentAsyncOperation != null)
        {
            // Prevent scene from activating automatically
            currentAsyncOperation.allowSceneActivation = false;

            // Wait for loading to complete (90% in Unity)
            yield return StartCoroutine(WaitForAsyncOperation());

            // Activate the scene
            currentAsyncOperation.allowSceneActivation = true;
            yield return currentAsyncOperation;
        }

        yield return EndLoading();
    }

    private IEnumerator CustomLoadingCoroutine(System.Func<System.Collections.IEnumerator> customLoadingOperation)
    {
        yield return StartLoading();

        // Run custom loading operation
        if (customLoadingOperation != null)
        {
            yield return StartCoroutine(customLoadingOperation());
        }

        yield return EndLoading();
    }

    private IEnumerator WaitForAsyncOperation()
    {
        while (!currentAsyncOperation.isDone)
        {
            // Unity reports up to 0.9 (90%) progress for scene loading
            // We'll map 0-0.9 to 0-100% for display
            float rawProgress = currentAsyncOperation.progress;
            targetProgress = rawProgress / 0.9f;

            if (rawProgress >= 0.9f)
            {
                // Loading is basically complete
                targetProgress = 1f;
                break;
            }

            yield return null;
        }
    }

    #endregion

    #region Loading Screen Management

    private IEnumerator StartLoading()
    {
        isLoading = true;
        loadStartTime = Time.time;
        targetProgress = 0f;
        currentDisplayProgress = 0f;

        // Show loading panel
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        // Initialize UI
        UpdateLoadingUI();

        // Start tip rotation
        if (tipsText != null && loadingTips.Length > 0)
        {
            tipRotationCoroutine = StartCoroutine(RotateLoadingTips());
        }

        yield return null;
    }

    private IEnumerator EndLoading()
    {
        // Ensure minimum loading time
        float elapsedTime = Time.time - loadStartTime;
        if (elapsedTime < minimumLoadTime)
        {
            targetProgress = 1f;
            yield return new WaitForSeconds(minimumLoadTime - elapsedTime);
        }

        // Ensure progress reaches 100%
        targetProgress = 1f;
        yield return StartCoroutine(WaitForProgressCompletion());

        // Stop tip rotation
        if (tipRotationCoroutine != null)
        {
            StopCoroutine(tipRotationCoroutine);
            tipRotationCoroutine = null;
        }

        // Hide loading panel
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        // Reset state
        isLoading = false;
        currentAsyncOperation = null;
    }

    private IEnumerator WaitForProgressCompletion()
    {
        while (currentDisplayProgress < 0.99f)
        {
            yield return null;
        }

        // Small delay before hiding
        yield return new WaitForSeconds(0.5f);
    }

    #endregion

    #region UI Updates

    private void UpdateLoadingProgress()
    {
        // Smooth progress animation
        if (useSmoothing)
        {
            currentDisplayProgress = Mathf.Lerp(
                currentDisplayProgress,
                targetProgress,
                Time.deltaTime * smoothingSpeed
            );
        }
        else
        {
            currentDisplayProgress = targetProgress;
        }

        // Apply animation curve for more interesting progress movement
        float curvedProgress = loadingCurve.Evaluate(currentDisplayProgress);

        UpdateLoadingUI(curvedProgress);
    }

    private void UpdateLoadingUI(float progress = 0f)
    {
        // Update slider
        if (loadingSlider != null)
        {
            loadingSlider.value = progress;
        }

        // Update percentage text
        if (percentageText != null)
        {
            int percentage = Mathf.RoundToInt(progress * 100f);
            percentageText.text = $"{percentage}%";
        }

        // Update loading text with dots animation
        if (loadingText != null)
        {
            int dotCount = Mathf.FloorToInt(Time.time * 2f) % 4;
            string dots = new string('.', dotCount);
            loadingText.text = $"Loading{dots}";
        }
    }

    private IEnumerator RotateLoadingTips()
    {
        int currentTipIndex = 0;

        while (isLoading)
        {
            if (tipsText != null && loadingTips.Length > 0)
            {
                tipsText.text = loadingTips[currentTipIndex];
                currentTipIndex = (currentTipIndex + 1) % loadingTips.Length;
            }

            yield return new WaitForSeconds(tipChangeInterval);
        }
    }

    #endregion

    #region Static Helper Methods

    /// <summary>
    /// Static method to load scene from anywhere in your code
    /// </summary>
    /// <param name="sceneName">Scene name to load</param>
    public static void LoadScene(string sceneName)
    {
        if (Instance != null)
        {
            Instance.LoadSceneAsync(sceneName);
        }
        else
        {
            Debug.LogError("AsyncLoader instance not found! Make sure AsyncLoader is present in the scene.");
            SceneManager.LoadScene(sceneName); // Fallback
        }
    }

    /// <summary>
    /// Static method to load scene by index from anywhere in your code
    /// </summary>
    /// <param name="sceneIndex">Scene build index to load</param>
    public static void LoadScene(int sceneIndex)
    {
        if (Instance != null)
        {
            Instance.LoadSceneAsync(sceneIndex);
        }
        else
        {
            Debug.LogError("AsyncLoader instance not found! Make sure AsyncLoader is present in the scene.");
            SceneManager.LoadScene(sceneIndex); // Fallback
        }
    }

    #endregion
}

