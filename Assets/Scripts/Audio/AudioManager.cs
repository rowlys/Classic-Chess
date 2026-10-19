using UnityEngine;

[DefaultExecutionOrder(-100)] 
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] AudioSource musicSource, sfxSource;
    [SerializeField] AudioClip moveSound, captureSound, checkSound, gameOverSound;

    public float MusicVolume { get => musicSource.volume; set => musicSource.volume = value; }
    public float SfxVolume { get => sfxSource.volume; set => sfxSource.volume = value; }

    void Awake()
    {
        if (Instance) { Destroy(gameObject); return; } // the copy in a newly loaded scene: keep the one already playing
        Instance = this;
        DontDestroyOnLoad(gameObject);

        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", MusicVolume);
        SfxVolume = PlayerPrefs.GetFloat("SfxVolume", SfxVolume);
    }

    public void PlayMove(bool gameOver, bool check, bool capture)
    {
        var clip = gameOver ? gameOverSound : check ? checkSound : capture ? captureSound : moveSound;
        if (clip) sfxSource.PlayOneShot(clip); 
    }
}
