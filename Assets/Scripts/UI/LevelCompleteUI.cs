using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelCompleteUI : MonoBehaviour
{
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI switchesUsedText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button quitButton;

    [SerializeField] private float delayBeforeFade     = 0.6f;
    [SerializeField] private float fadeDuration        = 1.2f;
    [SerializeField] private float panelFadeInDelay    = 0.3f;
    [SerializeField] private float panelFadeInDuration = 0.6f;

    private SwitchController _switchController;

    private void Awake()
    {
        _switchController = FindFirstObjectByType<SwitchController>();

        if (fadeOverlay != null)
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);

        if (panel != null)
        {
            panel.alpha = 0f;
            panel.gameObject.SetActive(false);
        }

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(RestartLevel);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void OnEnable()  => LevelExit.OnPlayerReachedExit += BeginEndSequence;
    private void OnDisable() => LevelExit.OnPlayerReachedExit -= BeginEndSequence;

    private void BeginEndSequence() => StartCoroutine(EndSequence());

    private IEnumerator EndSequence()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            player.IsInputLocked = true;

        yield return new WaitForSeconds(delayBeforeFade);
        yield return StartCoroutine(FadeToBlack());

        PopulateStats();

        if (panel != null)
        {
            panel.gameObject.SetActive(true);
            yield return new WaitForSeconds(panelFadeInDelay);
            yield return StartCoroutine(FadeInPanel());
        }
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration));
            yield return null;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, 1f);
    }

    private IEnumerator FadeInPanel()
    {
        if (panel == null) yield break;

        float elapsed = 0f;
        while (elapsed < panelFadeInDuration)
        {
            elapsed += Time.deltaTime;
            panel.alpha = Mathf.SmoothStep(0f, 1f, elapsed / panelFadeInDuration);
            yield return null;
        }
        panel.alpha = 1f;
    }

    private void PopulateStats()
    {
        if (switchesUsedText == null || _switchController == null) return;
        switchesUsedText.text = "Switches Used: " + _switchController.MovesUsed;
    }

    private void RestartLevel()
    {
        LevelExit.OnPlayerReachedExit -= BeginEndSequence;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}