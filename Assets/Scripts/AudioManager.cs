using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("AudioManager");
                instance = go.AddComponent<AudioManager>();
            }
            return instance;
        }
    }

    private AudioSource audioSource;

    private AudioClip selectClip;
    private AudioClip swapClip;
    private AudioClip matchClip;
    private AudioClip gravityClip;
    private AudioClip victoryClip;
    private AudioClip defeatClip;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // SFX clips 
        selectClip = ProceduralAudioSynth.CreateBeep();
        swapClip = ProceduralAudioSynth.CreateSlide();
        matchClip = ProceduralAudioSynth.CreateChime();
        gravityClip = ProceduralAudioSynth.CreateRumble();
        victoryClip = ProceduralAudioSynth.CreateFanfare();
        defeatClip = ProceduralAudioSynth.CreateSadTune();
    }

    public void PlaySelect() => Play(selectClip);
    public void PlaySwap() => Play(swapClip);
    public void PlayMatch() => Play(matchClip);
    public void PlayGravityShift() => Play(gravityClip);
    public void PlayVictory() => Play(victoryClip);
    public void PlayDefeat() => Play(defeatClip);

    private void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
