using UnityEngine;
using UnityEngine.InputSystem;

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
}
