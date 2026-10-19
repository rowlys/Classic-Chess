using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Esc opens and closes the pause menu
public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pausePanel, settingsPanel;

    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (settingsPanel.activeSelf) { settingsPanel.SetActive(false); pausePanel.SetActive(true); }
        else pausePanel.SetActive(!pausePanel.activeSelf);
    }

    public void GoHome() => SceneManager.LoadScene("MainMenu");
}
