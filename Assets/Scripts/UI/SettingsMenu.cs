using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// name: only labels the entry in the Inspector list
[Serializable] public class BoardSkin { public string name; public Sprite sprite; public Vector2 squareSize = Vector2.one, gridOffset; }
[Serializable] public class PieceSkin { public string name; public Sprite[] white = new Sprite[6], black = new Sprite[6]; } // Pawn, Knight, Bishop, Rook, Queen, King

// Loads saved settings, applies them at launch, and saves each change as the player makes it
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] BoardView boardView;
    [SerializeField] BoardSkin[] boards;
    [SerializeField] PieceSkin[] pieceSets;

    [SerializeField] Slider masterSlider, musicSlider, sfxSlider;
    [SerializeField] Transform boardList, pieceList; 
    [SerializeField] Button optionPrefab;
    [SerializeField] Toggle fullscreenToggle;

    static readonly Color Unselected = new(0.5f, 0.5f, 0.5f);

    void Awake()
    {
        Bind(masterSlider, "MasterVolume", 1f, v => AudioListener.volume = v);
        var audio = AudioManager.Instance;
        Bind(musicSlider, "MusicVolume", audio.MusicVolume, v => audio.MusicVolume = v);
        Bind(sfxSlider, "SfxVolume", audio.SfxVolume, v => audio.SfxVolume = v);
        Bind(boardList, "Board", boards.Select(b => b.sprite).ToArray(), i => boardView.SetBoard(boards[i]));
        Bind(pieceList, "Pieces", pieceSets.Select(p => p.white[5]).ToArray(), i => boardView.SetPieces(pieceSets[i])); // white king
        Bind(fullscreenToggle, "Fullscreen", Screen.fullScreen, v => Screen.fullScreen = v);
    }

    // Each Bind: load the saved value (or the fallback), apply it now, then apply and save on every change
    static void Bind(Slider slider, string key, float fallback, Action<float> apply)
    {
        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(key, fallback));
        apply(slider.value);
        slider.onValueChanged.AddListener(v => { apply(v); PlayerPrefs.SetFloat(key, v); });
    }

    // One thumbnail button per option; the chosen one is bright, the rest dimmed
    void Bind(Transform list, string key, Sprite[] thumbs, Action<int> apply)
    {
        var buttons = new Button[thumbs.Length];
        void Select(int i)
        {
            for (int j = 0; j < buttons.Length; j++) buttons[j].image.color = j == i ? Color.white : Unselected;
            apply(i);
            PlayerPrefs.SetInt(key, i);
        }

        for (int i = 0; i < thumbs.Length; i++)
        {
            int index = i; // each button keeps its own number
            buttons[i] = Instantiate(optionPrefab, list);
            buttons[i].transform.GetChild(0).GetComponent<Image>().sprite = thumbs[i]; // prefab: the thumbnail is its first child
            buttons[i].onClick.AddListener(() => Select(index));
        }
        Select(Mathf.Clamp(PlayerPrefs.GetInt(key, 0), 0, thumbs.Length - 1)); // a removed option can't crash
    }

    static void Bind(Toggle toggle, string key, bool fallback, Action<bool> apply)
    {
        toggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1);
        apply(toggle.isOn);
        toggle.onValueChanged.AddListener(v => { apply(v); PlayerPrefs.SetInt(key, v ? 1 : 0); });
    }
}
