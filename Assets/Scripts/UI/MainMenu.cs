using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Play(bool vsAI)
    {
        BoardView.VsAI = vsAI;
        SceneManager.LoadScene("ClassicMode");
    }

    public void Quit() => Application.Quit();
}
