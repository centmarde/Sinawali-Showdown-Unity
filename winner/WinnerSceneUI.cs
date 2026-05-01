using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinnerSceneUI : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "MainScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button mainMenuButton;

    private void OnEnable()
    {
        AutoFindUI();
        RefreshWinnerText();
        WireButtons();
    }

    private void AutoFindUI()
    {
        if (winnerText == null)
        {
            var textObjects = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in textObjects)
            {
                if (tmp != null && tmp.name.ToLowerInvariant().Contains("winner"))
                {
                    winnerText = tmp;
                    break;
                }
            }
        }

        if (replayButton == null || mainMenuButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button == null) continue;
                string name = button.name.ToLowerInvariant();

                if (replayButton == null && (name.Contains("replay") || name.Contains("again")))
                {
                    replayButton = button;
                    continue;
                }

                if (mainMenuButton == null && (name.Contains("menu") || name.Contains("main")))
                {
                    mainMenuButton = button;
                }
            }
        }
    }

    private void RefreshWinnerText()
    {
        if (winnerText == null)
        {
            return;
        }

        string winnerName = string.IsNullOrEmpty(WinnerState.WinnerName) ? "Winner" : WinnerState.WinnerName;
        winnerText.text = "Winner: " + winnerName;
    }

    private void WireButtons()
    {
        if (replayButton != null)
        {
            replayButton.onClick.RemoveListener(HandleReplayClicked);
            replayButton.onClick.AddListener(HandleReplayClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
            mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        }
    }

    private void HandleReplayClicked()
    {
        WinnerState.Clear();
        ResetGraveyardIfPossible();
        if (!string.IsNullOrEmpty(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    private void HandleMainMenuClicked()
    {
        WinnerState.Clear();
        ResetGraveyardIfPossible();
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private static void ResetGraveyardIfPossible()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.ClearGraveyard();
    }
}
